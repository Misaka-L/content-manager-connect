using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using VRChatContentPublisherConnect.Editor.Exceptions;
using VRChatContentPublisherConnect.Editor.Extensions;
using VRChatContentPublisherConnect.Editor.Models;
using VRChatContentPublisherConnect.Editor.Models.RpcApi.Request;
using VRChatContentPublisherConnect.Editor.Models.RpcApi.Request.Task;
using VRChatContentPublisherConnect.Editor.Models.RpcApi.Response;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;
using Random = System.Random;

namespace VRChatContentPublisherConnect.Editor.Services.Rpc;

internal sealed class RpcClientService {
    public event EventHandler<RpcClientState>? StateChanged;
    public event EventHandler<string>? IdentityPromptChanged;

    public RpcClientState State { get; private set; } = RpcClientState.Disconnected;
    public string? InstanceName { get; private set; }

    private readonly string _clientId;
    private readonly IRpcClientIdProvider _clientIdProvider;

    private readonly IRpcClientSessionProvider _sessionProvider;
    private string? _token;

    private readonly YesLogger _logger = new(LoggerConst.LoggerPrefix + nameof(RpcClientService));
    private readonly AppSettingsService _appSettingsService;
    private readonly AppSettings _appSettings;

    private Uri? _baseUrl;
    private string? _identityPrompt;

    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _serializerOptions = new() {
        RespectNullableAnnotations = true
    };

    public RpcClientService(
        IRpcClientIdProvider clientIdProvider,
        IRpcClientSessionProvider sessionProvider,
        AppSettingsService appSettingsService
    ) {
        _clientIdProvider = clientIdProvider;
        _sessionProvider = sessionProvider;
        _appSettingsService = appSettingsService;
        _clientId = clientIdProvider.GetClientId();
        _httpClient = new HttpClient();

        _appSettings = _appSettingsService.GetSettings();

        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(new ProductHeaderValue("VRChatContentManager.ConnectEditorApp", "snapshot")));
    }

    public async ValueTask<bool> IsConnectionValidAsync() {
        if (State != RpcClientState.Connected)
            return false;

        try {
            await GetAuthMetadataAsyncCore();
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Connection is not valid. Disconnecting.");
            await DisconnectAsync();
        }

        return false;
    }

    public async Task RefreshTokenAsync() {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/refresh") {
            Content = JsonContent.Create(new RefreshTokenRequest(_clientIdProvider.GetClientName()))
        };

        var response = await SendAsync(request);

        var responseBody = await response.Content.ReadFromJsonAsync<ChallengeResponse>();
        if (responseBody is null)
            throw new UnexpectedRpcResponseException("Rpc api returned an null response.");

        _token = responseBody.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        try {
            await GetAuthMetadataAsyncCore();
            await _sessionProvider.SetSessionAsync(new RpcClientSession(_baseUrl!.ToString(), _token));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to validate token, disconnecting.");

            await ForgetAndDisconnectAsync();
        }
    }

    public async ValueTask<RpcClientSession?> GetLastSessionInfoAsync() {
        return await _sessionProvider.GetSessionsAsync();
    }

    public async Task RestoreSessionAsync() {
        var shouldLaunchApp = _appSettings.LaunchAppWhenReconnect;
        await RestoreSessionAsync(true, shouldLaunchApp);
    }

    public async Task RestoreSessionAsync(bool showProgressDialog, bool launchApp) {
        var session = await _sessionProvider.GetSessionsAsync();

        if (session is null) {
            throw new InvalidOperationException("No session to restore.");
        }

        using var progress = new SimpleProgressScope("Restoring RPC Client Session", showDialog: showProgressDialog);

        var hostUri = new Uri(session.Host);
        await RestoreSessionAsyncCore(session, launchApp, progressText => progress.Report(progressText));

        var metadata = await GetMetadataAsync(hostUri);

        _httpClient.BaseAddress = hostUri;
        _token = session.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        _baseUrl = hostUri;
        InstanceName = metadata.InstanceName;

        ChangeState(RpcClientState.Connected);
        await RefreshTokenAsync();
    }

    private async Task RestoreSessionAsyncCore(
        RpcClientSession session,
        bool launchApp,
        Action<string>? reportProgress = null
    ) {
        var hostUri = new Uri(session.Host);

        reportProgress?.Invoke("Try requesting auth metadata from existing instance (if have)...");
        try {
            await GetAuthMetadataForSessionAsync(session);
            // Successfully restored session
            return;
        }
        catch (Exception ex) when (ex is not UnexpectedRpcStatusCodeException &&
                                   ex is not UnexpectedRpcResponseException) {
            if (hostUri.Host != "localhost" && hostUri.Host != "127.0.0.1" && hostUri.Host != "[::1]")
                throw;

            if (!launchApp)
                return;

            _logger.LogWarning(ex, "Failed to restore session to local RPC server, trying to launch local app.");
        }

        reportProgress?.Invoke("Trying to launch local VRChat Content Publisher App...");
        TryLaunchLocalApp();
        // 3 seconds should be enough, unless user running on a potato PC
        await Task.Delay(TimeSpan.FromSeconds(3));

        for (var attempt = 0; attempt < 5; attempt++) {
            reportProgress?.Invoke($"Attempt {attempt + 1} to restore session to App.");
            try {
                await GetAuthMetadataForSessionAsync(session);
                break;
            }
            catch (Exception ex) when (ex is not UnexpectedRpcStatusCodeException &&
                                       ex is not UnexpectedRpcResponseException) {
                _logger.LogWarning(ex, $"Attempt {attempt + 1} to restore session to local RPC server failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        reportProgress?.Invoke("Checking if instance is ready for publish...");
        try {
            if (await IsInstanceReadyForPublish(session))
                return;
        }
        catch (NotSupportedException) {
            _logger.LogWarning("Instance does not support ready-for-publish check. Assuming it's ready.");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(3));
        for (var attempt = 0; attempt < 5; attempt++) {
            reportProgress?.Invoke($"Attempt {attempt + 1} to check if instance is ready for publish...");
            if (await IsInstanceReadyForPublish(session))
                return;

            _logger.LogWarning($"Attempt {attempt + 1} to check if instance is ready for publish failed.");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        _logger.LogWarning("Instance is not ready for publish after multiple attempts. Proceeding anyway.");
    }

    private async ValueTask<AuthMetadataResponse> GetAuthMetadataForSessionAsync(RpcClientSession session) {
        var hostUri = new Uri(session.Host);
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(hostUri, "/v1/auth/metadata"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await _httpClient.SendAsync(request);
        await HandleErrorResponseAsync(response);

        var authMetadata = await response.Content.ReadFromJsonAsync<AuthMetadataResponse>();
        if (authMetadata is null)
            throw new UnexpectedRpcResponseException("Rpc api returned an null response.");

        return authMetadata;
    }

    private async ValueTask<bool> IsInstanceReadyForPublish(RpcClientSession session) {
        var hostUri = new Uri(session.Host);
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(hostUri, "/v1/health/ready-for-publish"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new NotSupportedException("Instance does not support ready-for-publish check.");

        await HandleErrorResponseAsync(response);

        return response.StatusCode == HttpStatusCode.NoContent;
    }

    private void TryLaunchLocalApp() {
        MainThreadDispatcher.Dispatch(() =>
            Application.OpenURL("vrchat-content-manager://launch")
        );
    }

    public string GetClientId() {
        return _clientId;
    }

    public string? GetIdentityPrompt() {
        return _identityPrompt;
    }

    private async ValueTask<MetadataResponse> GetMetadataAsync(Uri hostUri) {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(hostUri, "/v1/meta"));
        var response = await _httpClient.SendAsync(request);

        await HandleErrorResponseAsync(response);

        var responseBody = await response.Content.ReadFromJsonAsync<MetadataResponse>(_serializerOptions);
        if (responseBody is null)
            throw new UnexpectedRpcResponseException("Rpc api returned an null response.");

        return responseBody;
    }

    public async ValueTask<string> RequestChallengeAsync(string baseUrl) {
        await ForgetAndDisconnectAsync();

        var baseUri = new Uri(baseUrl);

        var metadata = await GetMetadataAsync(baseUri);

        var identityPrompt = SetRandomIdentityPrompt();

        var requestUri = new Uri(baseUri, "/v1/auth/request-challenge");
        var requestBody = new RequestChallengeRequest(_clientId, identityPrompt, _clientIdProvider.GetClientName());
        var response = await _httpClient.PostAsJsonAsync(requestUri, requestBody, _serializerOptions);

        await HandleErrorResponseAsync(response);
        if (response.StatusCode != HttpStatusCode.NoContent) {
            throw new UnexpectedRpcStatusCodeException((int)response.StatusCode);
        }

        _baseUrl = baseUri;
        InstanceName = metadata.InstanceName;
        _httpClient.BaseAddress = baseUri;
        ChangeState(RpcClientState.AwaitingChallenge);

        return identityPrompt;
    }

    public async Task CompleteChallengeAsync(string code) {
        if (State != RpcClientState.AwaitingChallenge)
            throw new InvalidOperationException("Client is not awaiting a challenge.");
        if (_baseUrl is null)
            throw new InvalidOperationException("Base URL is not set. Call RequestChallengeAsync first.");
        if (_identityPrompt is null)
            throw new InvalidOperationException("IdentityPrompt is not set. Call RequestChallengeAsync first.");

        var requestBody = new ChallengeRequest(_clientId, code, _identityPrompt);
        var response = await _httpClient.PostAsJsonAsync("/v1/auth/challenge", requestBody, _serializerOptions);

        await HandleErrorResponseAsync(response);

        var responseBody = await response.Content.ReadFromJsonAsync<ChallengeResponse>(_serializerOptions);
        if (responseBody is null) {
            throw new UnexpectedRpcResponseException("Rpc api returned an null response.");
        }

        _token = responseBody.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        try {
            await GetAuthMetadataAsyncCore();
            await _sessionProvider.SetSessionAsync(new RpcClientSession(_baseUrl.ToString(), _token));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to validate token, disconnecting.");

            await ForgetAndDisconnectAsync();
            return;
        }

        ChangeState(RpcClientState.Connected);
    }

    private async ValueTask<AuthMetadataResponse> GetAuthMetadataAsyncCore() {
        var response = await _httpClient.GetAsync("/v1/auth/metadata");

        await HandleErrorResponseAsync(response);

        var responseBody = await response.Content.ReadFromJsonAsync<AuthMetadataResponse>(_serializerOptions);
        if (responseBody is null) {
            throw new UnexpectedRpcResponseException("Rpc api returned an null response.");
        }

        return responseBody;
    }

    internal async ValueTask<string> UploadFileAsync(string filePath, string fileName) {
        await using var fileStream = File.OpenRead(filePath);

        using var content = new MultipartFormDataContent();

        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        content.Add(fileContent, "file", fileName);

        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "/v1/files") {
            Content = content
        });

        var responseBody = await response.Content.ReadFromJsonAsync<UploadFileResponse>(_serializerOptions);
        if (responseBody is null) {
            throw new UnexpectedRpcResponseException("Rpc api returned an null response.");
        }

        return responseBody.FileId;
    }

    internal async ValueTask CreateWorldPublishTaskAsync(CreateWorldPublishTaskRequest request) {
        await SendAsync(new HttpRequestMessage(HttpMethod.Post, "/v1/tasks/world") {
            Content = JsonContent.Create(request)
        });
    }

    internal async ValueTask CreateAvatarPublishTaskAsync(
        string avatarId,
        string bundleFileId,
        string avatarName,
        string platform,
        string unityVersion,
        string? imageFileId = null,
        string? description = null,
        string[]? tags = null,
        string? releaseStatus = null) {
        var requestBody =
            new CreateAvatarPublishTaskRequest(avatarId,
                bundleFileId,
                avatarName,
                platform,
                unityVersion,
                imageFileId,
                description,
                tags,
                releaseStatus);

        await SendAsync(new HttpRequestMessage(HttpMethod.Post, "/v1/tasks/avatar") {
            Content = JsonContent.Create(requestBody)
        });
    }

    internal async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request) {
        if (State != RpcClientState.Connected)
            throw new InvalidOperationException("Client is not connected.");
        if (_baseUrl is null)
            throw new InvalidOperationException("Base URL is not set. Call RequestChallengeAsync first.");
        if (_token is null)
            throw new InvalidOperationException("Not authenticated. Call CompleteChallengeAsync first.");

        var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized) {
            _logger.LogWarning("Unauthorized response, disconnecting.");
            await ForgetAndDisconnectAsync();
        }

        await HandleErrorResponseAsync(response);
        return response;
    }

    private async ValueTask HandleErrorResponseAsync(HttpResponseMessage response) {
        if (response.IsSuccessStatusCode)
            return;

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(_serializerOptions);
        if (errorResponse is null)
            throw new UnexpectedRpcResponseException("Rpc api returned an error but the response is null.");

        throw new RpcApiErrorException(
            errorResponse.Type,
            errorResponse.StatusCode,
            errorResponse.Title,
            errorResponse.Detail
        );
    }

    public async Task ForgetAndDisconnectAsync() {
        await DisconnectAsync();
        await _sessionProvider.RemoveSessionAsync();
    }

    public Task DisconnectAsync() {
        ChangeState(RpcClientState.Disconnected);
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _baseUrl = null;
        _httpClient.BaseAddress = null;

        return Task.CompletedTask;
    }

    private void ChangeState(RpcClientState newState) {
        if (State == newState) return;

        State = newState;
        StateChanged?.Invoke(this, newState);
    }

    private string SetRandomIdentityPrompt() {
        var words = new[] {
            "Red", "Blue", "Green", "Yellow", "Purple", "Orange", "Black", "White", "Gray", "Silver",
            "Gold", "Bronze", "Copper", "Iron", "Steel", "Wooden", "Plastic", "Glass", "Crystal", "Diamond"
        };

        var random = new Random();
        var word1 = words[random.Next(words.Length)];
        var word2 = words[random.Next(words.Length)];

        _identityPrompt = $"{word1} {word2}";
        IdentityPromptChanged?.Invoke(this, _identityPrompt);
        return _identityPrompt;
    }
}

internal enum RpcClientState {
    Disconnected,
    AwaitingChallenge,
    Connected
}