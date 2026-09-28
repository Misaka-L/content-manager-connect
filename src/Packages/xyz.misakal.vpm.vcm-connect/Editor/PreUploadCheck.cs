using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UnityEditor;
using VRC.Core;
using VRChatContentPublisherConnect.Editor.Exceptions.PreUploadCheck;
using VRChatContentPublisherConnect.Editor.Services;
using VRChatContentPublisherConnect.Editor.Services.Rpc;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;

namespace VRChatContentPublisherConnect.Editor;

internal static class PreUploadCheck {
    public static bool IsTaskRunning { get; private set; }

    private static readonly YesLogger _logger = new(LoggerConst.LoggerPrefix + nameof(PreUploadCheck));

    public static void RunPreUploadCheck(Action startUploadAction, Action? failedAction = null) {
        if (IsTaskRunning)
            return;

        if (ConnectEditorApp.Instance is not { } app) {
            EditorUtility.DisplayDialog(
                "Failed to Start Upload",
                "VRChat Content Publisher Connect is not initialized.",
                "OK");
            return;
        }

        var rpcClientService = app.ServiceProvider.GetRequiredService<RpcClientService>();
        var appSettingsService = app.ServiceProvider.GetRequiredService<AppSettingsService>();

        if (!appSettingsService.GetSettings().UseContentManager) {
            startUploadAction();
            return;
        }

        IsTaskRunning = true;
        _ = Task.Run(() => PreUploadCheckCore(rpcClientService, startUploadAction, failedAction)
        ).ConfigureAwait(false);
    }

    // so FUCK YOU, UNITY and Microsoft
    private static async void PreUploadCheckCore(
        RpcClientService rpcClientService,
        Action continueUpload,
        Action? failed) {
        try {
            if (!await rpcClientService.IsConnectionValidAsync()) {
                var canRestoreSession = await CanRestoreSessionAsync(rpcClientService);
                if (!canRestoreSession) {
                    MainThreadDispatcher.Dispatch(() => {
                        EditorUtility.DisplayDialog(
                            "Failed to Start Upload",
                            "RPC Client is not connected. And no session to restore.",
                            "OK");
                    });

                    RunFailed();
                    return;
                }

                try {
                    await rpcClientService.RestoreSessionAsync();
                }
                catch (Exception ex) {
                    MainThreadDispatcher.Dispatch(() => {
                        EditorUtility.DisplayDialog(
                            "Failed to Start Upload",
                            "RPC Client is not connected and failed to restore session.\n\n" + ex,
                            "OK");
                    });

                    RunFailed();
                    return;
                }
            }

            // The check shows its own dialog when it fails, so nothing to report here.
            if (await CheckUserSessionValidityAsync(rpcClientService) is not null) {
                RunFailed();
                return;
            }

            RunContinueUpload();
        }
        finally {
            MainThreadDispatcher.Dispatch(() => IsTaskRunning = false);
        }

        void RunFailed() {
            MainThreadDispatcher.Dispatch(() => {
                IsTaskRunning = false;
                failed?.Invoke();
            });
        }

        void RunContinueUpload() {
            MainThreadDispatcher.Dispatch(() => {
                IsTaskRunning = false;
                continueUpload();
            });
        }
    }

    public static async Task PreUploadCheckAsync() {
        if (ConnectEditorApp.Instance is not { } app)
            throw new InvalidOperationException("VRChat Content Publisher Connect is not initialized.");

        var appSettingsService = app.ServiceProvider.GetRequiredService<AppSettingsService>();
        if (!appSettingsService.GetSettings().UseContentManager)
            return;

        var rpcClientService = app.ServiceProvider.GetRequiredService<RpcClientService>();

        if (!await rpcClientService.IsConnectionValidAsync()) {
            var canRestoreSession = await CanRestoreSessionAsync(rpcClientService);
            if (!canRestoreSession)
                throw new NoSessionToRestoreException();

            try {
                await rpcClientService.RestoreSessionAsync();
            }
            catch (Exception ex) {
                throw new RestoreSessionFailedException(ex);
            }
        }

        if (await CheckUserSessionValidityAsync(rpcClientService) is { } userSessionException)
            throw userSessionException;
    }

    // Asks the app whether the VRChat account signed in in the SDK still has a valid session in the app.
    // Returns null when the upload may continue, otherwise the exception describing why it may not.
    // When a non-null value is returned, the dialog the user had to see is already shown, so callers only
    // have to surface the exception.
    private static async Task<UserSessionValidityException?> CheckUserSessionValidityAsync(
        RpcClientService rpcClientService) {
        var currentUser = APIUser.CurrentUser;

        // Not signed in in the SDK (or offline mode). Nothing to check against the app.
        if (string.IsNullOrEmpty(currentUser?.id)) {
            _logger.LogDebug("VRChat SDK is not signed in. Skipping VRChat account session check.");
            return null;
        }

        var userId = currentUser!.id;
        var account = string.IsNullOrEmpty(currentUser.displayName)
            ? userId
            : $"{currentUser.displayName} ({userId})";

        while (true) {
            UserSessionValidityResult result;

            try {
                result = await rpcClientService.CheckUserSessionValidityAsync(userId);
            }
            catch (Exception ex) {
                // Fail open. If the app cannot even answer the question, let the upload run and
                // report its own error instead of blocking the user here.
                _logger.LogWarning(ex, "Failed to check VRChat account session. Skipping the check.");
                return null;
            }

            switch (result.Validity) {
                case UserSessionValidity.Valid:
                    return null;

                case UserSessionValidity.NotSupported:
                    // Older app versions do not have this endpoint at all. They must not be blocked.
                    _logger.LogWarning(
                        "The app does not support checking the VRChat account session. Skipping the check.");
                    return null;

                case UserSessionValidity.SessionNotFound: {
                    var sessionNotFoundException = new UserSessionNotFoundException(account, result.Detail);
                    await ShowMessageDialogAsync("Failed to Start Upload", sessionNotFoundException.Message);

                    return sessionNotFoundException;
                }

                case UserSessionValidity.Invalid: {
                    var sessionInvalidException = new UserSessionInvalidException(account, result.Detail);

                    // The app reports both "signed out" and "could not check" as invalid, so let the user
                    // retry instead of telling them their account is definitely signed out.
                    if (await ShowDialogAsync("Failed to Start Upload", sessionInvalidException.Message,
                            "Retry", "Cancel"))
                        continue;

                    return sessionInvalidException;
                }

                default:
                    _logger.LogWarning(
                        $"Unexpected VRChat account session check result: {result.Validity}. Skipping the check.");
                    return null;
            }
        }
    }

    private static Task<bool> ShowMessageDialogAsync(string title, string message) {
        // An empty cancel button is not drawn, so only a single OK button is shown.
        return ShowDialogAsync(title, message, "OK", string.Empty);
    }

    // EditorUtility.DisplayDialog is main thread only, and this can be called from a background thread,
    // so hand it to the main thread and wait for the user to answer.
    private static Task<bool> ShowDialogAsync(string title, string message, string ok, string cancel) {
        var completion = new TaskCompletionSource<bool>();

        MainThreadDispatcher.Dispatch(() => {
            try {
                completion.SetResult(EditorUtility.DisplayDialog(title, message, ok, cancel));
            }
            catch (Exception ex) {
                completion.SetException(ex);
            }
        });

        return completion.Task;
    }

    private static async Task<bool> CanRestoreSessionAsync(RpcClientService rpcClientService) {
        try {
            return await rpcClientService.GetLastSessionInfoAsync() is not null;
        }
        catch (Exception) {
            return false;
        }
    }
}