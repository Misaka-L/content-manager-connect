using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UnityEditor;
using VRChatContentPublisherConnect.Editor.Exceptions.PreUploadCheck;
using VRChatContentPublisherConnect.Editor.Services;
using VRChatContentPublisherConnect.Editor.Services.Rpc;

namespace VRChatContentPublisherConnect.Editor;

internal static class PreUploadCheck {
    public static bool IsTaskRunning { get; private set; }

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
            if (await rpcClientService.IsConnectionValidAsync()) {
                RunContinueUpload();
                return;
            }

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

        if (await rpcClientService.IsConnectionValidAsync())
            return;

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

    private static async Task<bool> CanRestoreSessionAsync(RpcClientService rpcClientService) {
        try {
            return await rpcClientService.GetLastSessionInfoAsync() is not null;
        }
        catch (Exception) {
            return false;
        }
    }
}