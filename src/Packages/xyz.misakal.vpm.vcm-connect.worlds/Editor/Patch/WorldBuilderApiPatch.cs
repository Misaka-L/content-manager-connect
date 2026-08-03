using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor.Api;
using VRChatContentPublisherConnect.Editor;
using YesPatchFrameworkForVRChatSdk.PatchApi;
using YesPatchFrameworkForVRChatSdk.PatchApi.Extensions;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;

namespace VRChatContentPublisherConnect.Worlds.Editor.Patch {
    [HarmonyPatch]
    internal class WorldBuilderApiPatch : YesPatchBase {
        public override string Id => "xyz.misakal.vpm.vcm-connect.worlds.pre-build-and-upload-check";
        public override string DisplayName => "Pre Build and Upload Check for Worlds";

        public override string Description =>
            "Prevents build and upload if Content Publisher is enabled but not connected.";

        public override string Category => PatchConst.Category;

        public override bool IsDefaultEnabled => true;

        private readonly Harmony _harmony = new("xyz.misakal.vpm.vcm-connect.worlds.pre-build-and-upload-check");

        public override void Patch() {
            _harmony.PatchAll(typeof(WorldBuilderApiPatch));
        }

        public override void UnPatch() {
            _harmony.UnpatchSelf();
        }

        // public async Task BuildAndUpload(VRCWorld world, string signature, string thumbnailPath = null,
        // CancellationToken cancellationToken = default)
    #if VCCM_WORLD_SDK_3_7_2_OR_NEWER
        [HarmonyPatch(typeof(VRCSdkControlPanelWorldBuilder), nameof(VRCSdkControlPanelWorldBuilder.BuildAndUpload),
            typeof(VRCWorld), typeof(string), typeof(string), typeof(CancellationToken))]
    #else
        [HarmonyPatch(typeof(VRCSdkControlPanelWorldBuilder), nameof(VRCSdkControlPanelWorldBuilder.BuildAndUpload),
            typeof(VRCWorld), typeof(string), typeof(CancellationToken))]
    #endif
        [HarmonyPrefix]
        public static bool Prefix(ref Task __result, VRCSdkControlPanelWorldBuilder __instance, object[] __args) {
            var originalUploadMethod = AccessTools.Method(typeof(WorldBuilderApiPatch), nameof(OriginalBuildAndUpload));
            __result = RunPreUploadCheckAsync(__instance, () => {
                var fullArgs = new List<object> { __instance };
                fullArgs.AddRange(__args);

                return (Task)originalUploadMethod.Invoke(null, fullArgs.ToArray());
            });

            return false;
        }

        private static async Task RunPreUploadCheckAsync(
            VRCSdkControlPanelWorldBuilder builderInstance, Func<Task> uploadAction) {
            try {
                await PreUploadCheck.PreUploadCheckAsync();
            }
            catch (Exception e) {
                throw HandleUploadError(builderInstance, e);
            }

            await uploadAction();
        }

        [HarmonyReversePatch]
    #if VCCM_WORLD_SDK_3_7_2_OR_NEWER
        [HarmonyPatch(typeof(VRCSdkControlPanelWorldBuilder), nameof(VRCSdkControlPanelWorldBuilder.BuildAndUpload),
            typeof(VRCWorld), typeof(string), typeof(string), typeof(CancellationToken))]
        private static Task OriginalBuildAndUpload(object instance, VRCWorld world,
            string signature, string thumbnailPath = null,
            CancellationToken cancellationToken = default)
    #else
        [HarmonyPatch(typeof(VRCSdkControlPanelWorldBuilder), nameof(VRCSdkControlPanelWorldBuilder.BuildAndUpload),
            typeof(VRCWorld), typeof(string), typeof(CancellationToken))]
        private static Task OriginalBuildAndUpload(object instance, VRCWorld world, string thumbnailPath = null,
            CancellationToken cancellationToken = default)
    #endif
        {
            // This is never called
            throw new NotImplementedException("It's a reverse patch stub.");
        }

    #region Handle Upload Error

        [CanBeNull] private static MethodInfo _handleUploadErrorMethod;

        private static Exception HandleUploadError(VRCSdkControlPanelWorldBuilder builderInstance, Exception ex) {
            if (_handleUploadErrorMethod is not null) {
                _handleUploadErrorMethod?.Invoke(builderInstance, new object[] { ex });
                return ex;
            }

            _handleUploadErrorMethod = AccessTools.Method(
                typeof(VRCSdkControlPanelWorldBuilder), "HandleUploadError",
                new[] {
                    typeof(Exception)
                });

            if (_handleUploadErrorMethod is null) {
                YesLogger.LogError(
                    ex, $"{LoggerConst.LoggerPrefix}{nameof(WorldBuilderApiPatch)}",
                    "Unhandled exception during BuildAndUpload PreUpload Check, and HandleUploadError doesn't exist. Please report this to the developer.",
                    null);
                throw ex;
            }

            _handleUploadErrorMethod?.Invoke(builderInstance, new object[] { ex });
            return ex;
        }

    #endregion
    }
}