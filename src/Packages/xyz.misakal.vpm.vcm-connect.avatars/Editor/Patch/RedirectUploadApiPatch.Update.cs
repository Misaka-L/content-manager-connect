using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase.Editor.Api;
using VRChatContentPublisherConnect.Editor;
using VRChatContentPublisherConnect.Editor.Services;
using VRChatContentPublisherConnect.Editor.Services.Rpc;

namespace VRChatContentPublisherConnect.Avatars.Editor.Patch {
    internal partial class RedirectUploadApiPatch {
        // public static async Task<VRCAvatar> UpdateAvatarBundle(
        // string id, VRCAvatar data, string pathToBundle,
        // Action<string, float> onProgress = null, CancellationToken cancellationToken = default)
        [HarmonyPatch(typeof(VRCApi), nameof(VRCApi.UpdateAvatarBundle),
            typeof(string), typeof(VRCAvatar), typeof(string), typeof(Action<string, float>),
            typeof(CancellationToken))]
        [HarmonyPrefix]
        internal static bool UploadAvatarBundlePrefix(
            ref Task<VRCAvatar> __result,
            string id, VRCAvatar data, string pathToBundle,
            Action<string, float> onProgress = null,
            CancellationToken cancellationToken = default
        ) {
            var app = ConnectEditorApp.Instance;
            if (app == null) {
                _logger.LogWarning(
                    "[VRCCM.Connect] AvatarBundleUploadApiPatch: ConnectEditorApp instance is null. Skipping patch.");
                return true;
            }

            var settings = app.ServiceProvider.GetRequiredService<AppSettingsService>();
            if (!settings.GetSettings().UseContentManager)
                return true;

            __result = Task.Run(async () => {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id), "Avatar ID cannot be null or empty.");

                if (string.IsNullOrEmpty(pathToBundle))
                    throw new ArgumentNullException(nameof(pathToBundle), "Path to bundle cannot be null or empty.");

                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(pathToBundle))
                    throw new FileNotFoundException("The specified bundle file does not exist.", pathToBundle);

                var bundleFileName = "Avatar - " + data.Name + " - Asset bundle - " + Application.unityVersion + "_" +
                                     ApiAvatar.VERSION.ApiVersion +
                                     "_" + Tools.Platform + "_" + API.GetServerEnvironmentForApiUrl();

                _logger.LogDebug($"AvatarId: {id} PathToBundle: {pathToBundle} BundleFileName: {bundleFileName}");
                // Send Bundle File to App, Start new task

                var rpcClient = app.ServiceProvider.GetRequiredService<RpcClientService>();
                if (rpcClient.State == RpcClientState.Disconnected) {
                    _logger.LogWarning("RPC client is disconnected. Attempting to restore session...");

                    try {
                        await rpcClient.RestoreSessionAsync();
                        _logger.LogInfo("RPC session restored successfully.");
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex,
                            "Failed to restore RPC session. Aborting avatar bundle upload.");

                        throw new InvalidOperationException("RPC client is not connected.", ex);
                    }
                }

                var fileId = await rpcClient.UploadFileAsync(pathToBundle, bundleFileName);
                _logger.LogDebug("Bundle File Id: " + fileId);

                await rpcClient.CreateAvatarPublishTaskAsync(id, fileId, data.Name, Tools.Platform,
                    Tools.UnityVersion.ToString(), authorId: APIUser.CurrentUser?.id);

                return data;
            });

            // replace method
            return false;
        }
    }
}