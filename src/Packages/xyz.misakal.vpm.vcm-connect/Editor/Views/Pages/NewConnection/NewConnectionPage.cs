using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine.UIElements;
using VRChatContentPublisherConnect.Editor.Services.Rpc;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;

namespace VRChatContentPublisherConnect.Editor.Views.Pages.NewConnection;

internal sealed class NewConnectionPage : VisualElement {
    private const string VisualTreeAssetGuid = "280eea869a434efd8a9abf4dd83eee43";

    private const int LocalhostModeIndex = 0;
    private const int CustomUrlModeIndex = 1;

    private const string LocalhostOrigin = "http://localhost";

    private readonly YesLogger _logger = new(LoggerConst.LoggerPrefix + nameof(NewConnectionPage));

    private readonly RpcClientService _rpcClientService;

    private readonly RadioButtonGroup _connectionModeGroup;
    private readonly VisualElement _localhostContainer;
    private readonly VisualElement _customUrlContainer;
    private readonly TextField _portField;
    private readonly Label _resolvedUrlLabel;

    private readonly TextField _hostField;
    private readonly Button _connectButton;

    private readonly VisualElement _connectContainer;
    private readonly VisualElement _challengeContainer;

    private readonly Label _identityPromptLabel;
    private readonly TextField _challengeCodeInputField;
    private readonly Button _challengeButton;
    private readonly Button _cancelChallengeButton;

    public NewConnectionPage() {
        var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(VisualTreeAssetGuid);
        var visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
        visualTreeAsset.CloneTree(this);

        _connectionModeGroup = this.Q<RadioButtonGroup>("connection-mode-group");
        _localhostContainer = this.Q<VisualElement>("localhost-container");
        _customUrlContainer = this.Q<VisualElement>("custom-url-container");
        _portField = this.Q<TextField>("port-input-field");
        _resolvedUrlLabel = this.Q<Label>("resolved-url-label");

        _hostField = this.Q<TextField>("host-input-field");
        _connectButton = this.Q<Button>("connect-button");

        _connectContainer = this.Q<VisualElement>("connect-container");
        _challengeContainer = this.Q<VisualElement>("challenge-container");

        _identityPromptLabel = this.Q<Label>("identity-prompt-label");
        _challengeCodeInputField = this.Q<TextField>("code-input-field");
        _challengeButton = this.Q<Button>("challenge-button");
        _cancelChallengeButton = this.Q<Button>("cancel-challenge-button");

        if (ConnectEditorApp.Instance is not { } app) {
            _logger.LogWarning("ConnectEditorApp instance is not available.");
            throw new InvalidOperationException("ConnectEditorApp instance is not available.");
        }

        _rpcClientService = app.ServiceProvider.GetRequiredService<RpcClientService>();

        RegisterCallback<AttachToPanelEvent>(_ => {
            _rpcClientService.StateChanged += OnRpcClientServiceStateChanged;
        });

        RegisterCallback<DetachFromPanelEvent>(_ => {
            _rpcClientService.StateChanged -= OnRpcClientServiceStateChanged;
        });

        _connectionModeGroup.choices = new List<string> { "Localhost", "Custom URL" };
        _connectionModeGroup.value = LocalhostModeIndex;
        _connectionModeGroup.RegisterValueChangedCallback(_ => UpdateConnectionModeUi());

        _portField.RegisterValueChangedCallback(args => {
            _portField.SetValueWithoutNotify(KeepDigitsOnly(args.newValue));
            UpdateResolvedUrlHint();
        });

        _connectButton.clicked += async () => {
            if (!TryResolveHostUrl(out var hostUrl, out var validationError)) {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Invalid Host",
                    validationError,
                    "OK");
                return;
            }

            try {
                await _rpcClientService.RequestChallengeAsync(hostUrl);
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to request challenge: {ex}");

                UnityEditor.EditorUtility.DisplayDialog(
                    "Connection Error",
                    $"Failed to connect to {hostUrl}:\n\n{ex}",
                    "OK");
            }
        };

        _challengeCodeInputField.RegisterValueChangedCallback(args => {
            _challengeCodeInputField.SetValueWithoutNotify(args.newValue.ToUpperInvariant());
        });

        _challengeButton.clicked += async () => {
            try {
                await _rpcClientService.CompleteChallengeAsync(_challengeCodeInputField.text);
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to complete challenge: {ex}");

                UnityEditor.EditorUtility.DisplayDialog(
                    "Challenge Error",
                    $"Failed to complete challenge:\n\n{ex}",
                    "OK");
            }
        };

        _cancelChallengeButton.clicked += async () => {
            try {
                await _rpcClientService.ForgetAndDisconnectAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to cancel challenge: " + ex);

                UnityEditor.EditorUtility.DisplayDialog(
                    "Cancel Challenge Error",
                    $"Failed to cancel challenge:\n\n{ex}",
                    "OK");
            }
        };

        UpdateUi();
        UpdateConnectionModeUi();
    }

    private static string KeepDigitsOnly(string value) {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string BuildLocalhostUrl(string port) {
        return $"{LocalhostOrigin}:{port}";
    }

    private bool IsLocalhostMode => _connectionModeGroup.value != CustomUrlModeIndex;

    private void UpdateConnectionModeUi() {
        var isLocalhostMode = IsLocalhostMode;

        _localhostContainer.style.display = isLocalhostMode ? DisplayStyle.Flex : DisplayStyle.None;
        _customUrlContainer.style.display = isLocalhostMode ? DisplayStyle.None : DisplayStyle.Flex;

        UpdateResolvedUrlHint();
    }

    private void UpdateResolvedUrlHint() {
        if (!IsLocalhostMode) {
            _resolvedUrlLabel.style.display = DisplayStyle.None;
            return;
        }

        _resolvedUrlLabel.style.display = DisplayStyle.Flex;
        _resolvedUrlLabel.text = _portField.value.Length == 0
            ? "Enter the port of the local App."
            : $"Will connect to {BuildLocalhostUrl(_portField.value)}";
    }

    private bool TryResolveHostUrl(out string hostUrl, out string error) {
        hostUrl = string.Empty;
        error = string.Empty;

        if (IsLocalhostMode) {
            var portText = _portField.value;
            if (!int.TryParse(portText, out var port) || port < 1 || port > 65535) {
                error = portText.Length == 0
                    ? "Enter the port number of the local App."
                    : $"\"{portText}\" is not a valid port number. Enter a number between 1 and 65535.";

                return false;
            }

            hostUrl = BuildLocalhostUrl(portText);
            return true;
        }

        hostUrl = _hostField.value.Trim();
        if (hostUrl.Length == 0) {
            error = "Enter the host URL of the App to connect, for example http://192.168.1.10:59328";
            return false;
        }

        return true;
    }

    private void UpdateUi() {
        _identityPromptLabel.text =
            _rpcClientService.GetIdentityPrompt() ?? "Invalid Status, Try restart connect process?";

        if (_rpcClientService.State == RpcClientState.AwaitingChallenge) {
            _connectContainer.style.display = DisplayStyle.None;
            _challengeContainer.style.display = DisplayStyle.Flex;
        }
        else {
            _connectContainer.style.display = DisplayStyle.Flex;
            _challengeContainer.style.display = DisplayStyle.None;
        }
    }

    private void OnRpcClientServiceStateChanged(object sender, RpcClientState e) {
        MainThreadDispatcher.Dispatch(UpdateUi);
    }
}