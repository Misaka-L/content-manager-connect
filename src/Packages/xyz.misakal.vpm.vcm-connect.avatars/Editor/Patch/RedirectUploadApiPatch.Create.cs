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
        // public static async Task<VRCAvatar> CreateNewAvatar(
        // string id, VRCAvatar data, string pathToBundle, string pathToImage,
        // Action<string, float> onProgress = null, CancellationToken cancellationToken = default)
        [HarmonyPatch(typeof(VRCApi), nameof(VRCApi.CreateNewAvatar),
            typeof(string), typeof(VRCAvatar), typeof(string), typeof(string)
            , typeof(Action<string, float>), typeof(CancellationToken))]
        [HarmonyPrefix]
        internal static bool VrcApiCreateNewAvatarPrefix(ref Task<VRCAvatar> __result,
            string id, VRCAvatar data, string pathToBundle, string pathToImage,
            Action<string, float> onProgress = null,
            CancellationToken cancellationToken = default
        ) {
        #if VCCM_AVATAR_SDK_3_9_0_OR_NEWER
            var app = ConnectEditorApp.Instance;
            if (app == null) {
                _logger.LogWarning(
                    "[VRCCM.Connect] AvatarCreateApiPatch: ConnectEditorApp instance is null. Skipping patch.");
                return true;
            }

            var settings = app.ServiceProvider.GetRequiredService<AppSettingsService>();
            if (!settings.GetSettings().UseContentManager)
                return true;

            __result = Task.Run(async () => {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id), "Avatar ID cannot be null or empty.");

                if (string.IsNullOrWhiteSpace(pathToBundle) || string.IsNullOrWhiteSpace(pathToImage)) {
                    throw new ArgumentException("Path to bundle or image cannot be null or empty.");
                }

                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                // Send Create Avatar Request to App, Start new task

                var rpcClient = app.ServiceProvider.GetRequiredService<RpcClientService>();
                if (rpcClient.State == RpcClientState.Disconnected) {
                    _logger.LogWarning(
                        "[VRCCM.Connect] RPC client is disconnected. Attempting to restore session...");

                    try {
                        await rpcClient.RestoreSessionAsync();
                    }
                    catch (Exception ex) {
                        _logger.LogError(
                            $"[VRCCM.Connect] Failed to restore RPC session: {ex.Message}");
                        throw;
                    }
                }

                var bundleFileName = "Avatar - " + data.Name + " - Asset bundle - " + Application.unityVersion + "_" +
                                     ApiAvatar.VERSION.ApiVersion +
                                     "_" + Tools.Platform + "_" + API.GetServerEnvironmentForApiUrl();
                var imageFileName = "Avatar - " + data.Name + " - Thumbnail - " + Application.unityVersion + "_" +
                                    ApiAvatar.VERSION.ApiVersion +
                                    "_" + Tools.Platform + "_" + API.GetServerEnvironmentForApiUrl() +
                                    Path.GetExtension(pathToImage);

                _logger.LogDebug($"AvatarId: {id} PathToBundle: {pathToBundle} BundleFileName: {bundleFileName}");

                var fileId = await rpcClient.UploadFileAsync(pathToBundle, bundleFileName);
                _logger.LogDebug("Bundle File Id: " + fileId);
                var imageFileId = await rpcClient.UploadFileAsync(pathToImage, imageFileName);
                _logger.LogDebug("Image File Id: " + imageFileId);

                await rpcClient.CreateAvatarPublishTaskAsync(
                    id,
                    fileId,
                    data.Name,
                    Tools.Platform,
                    Tools.UnityVersion.ToString(),
                    imageFileId,
                    data.Description,
                    data.Tags.ToArray(),
                    data.ReleaseStatus,
                    APIUser.CurrentUser?.id
                );
                return data;
            }, cancellationToken);

            return false;
        #else
            return true;
        #endif
        }
    }
}