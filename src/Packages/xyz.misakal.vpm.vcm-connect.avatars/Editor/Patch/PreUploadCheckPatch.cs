using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor.Api;
using VRChatContentPublisherConnect.Editor;
using YesPatchFrameworkForVRChatSdk.PatchApi;
using YesPatchFrameworkForVRChatSdk.PatchApi.Extensions;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;

namespace VRChatContentPublisherConnect.Avatars.Editor.Patch {
    [HarmonyPatch]
    internal class PreUploadCheckPatch : YesPatchBase {
        public override string Id => "xyz.misakal.vpm.vcm-connect.avatars.pre-build-and-upload-check";
        public override string DisplayName => "Pre Build and Upload Check for Avatars";

        public override string Description =>
            "Prevents build and upload if Content Manager is enabled but not connected.";

        public override string Category => PatchConst.Category;

        public override bool IsDefaultEnabled => true;

        private readonly Harmony _harmony = new("xyz.misakal.vpm.vcm-connect.avatars.pre-build-and-upload-check");

        public override void Patch() {
            _harmony.PatchAll(typeof(PreUploadCheckPatch));
        }

        public override void UnPatch() {
            _harmony.UnpatchSelf();
        }

        [HarmonyPrefix]
    #if VCCM_AVATAR_SDK_3_8_1_OR_NEWER
        [HarmonyPatch(typeof(VRCSdkControlPanelAvatarBuilder), nameof(VRCSdkControlPanelAvatarBuilder.BuildAndUpload),
            typeof(GameObject), typeof(List<PerPlatformOverrides.Option>),
            typeof(VRCAvatar), typeof(string),
            typeof(CancellationToken))]
    #else
        [HarmonyPatch(typeof(VRCSdkControlPanelAvatarBuilder), nameof(VRCSdkControlPanelAvatarBuilder.BuildAndUpload),
            typeof(GameObject), typeof(VRCAvatar),
            typeof(string), typeof(CancellationToken))]
    #endif
        public static bool Prefix(ref Task __result, VRCSdkControlPanelAvatarBuilder __instance, object[] __args) {
            var originalUploadMethod = AccessTools.Method(typeof(PreUploadCheckPatch), nameof(OriginalBuildAndUpload));
            __result = RunPreUploadCheckAsync(__instance, () => {
                var fullArgs = new List<object> { __instance };
                fullArgs.AddRange(__args);

                return (Task)originalUploadMethod.Invoke(null, fullArgs.ToArray());
            });

            return false;
        }

        private static async Task RunPreUploadCheckAsync(
            VRCSdkControlPanelAvatarBuilder builderInstance, Func<Task> uploadAction) {
            try {
                await PreUploadCheck.PreUploadCheckAsync();
            }
            catch (Exception e) {
                throw HandleUploadError(builderInstance, e);
            }

            await uploadAction();
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    #if VCCM_AVATAR_SDK_3_8_1_OR_NEWER
        [HarmonyPatch(typeof(VRCSdkControlPanelAvatarBuilder), nameof(VRCSdkControlPanelAvatarBuilder.BuildAndUpload),
            typeof(GameObject), typeof(List<PerPlatformOverrides.Option>),
            typeof(VRCAvatar), typeof(string),
            typeof(CancellationToken))]
        private static Task OriginalBuildAndUpload(
            object instance,
            GameObject target,
            List<PerPlatformOverrides.Option> overrides,
            VRCAvatar avatar,
            string thumbnailPath = null,
            CancellationToken cancellationToken = default)
    #else
        [HarmonyPatch(typeof(VRCSdkControlPanelAvatarBuilder), nameof(VRCSdkControlPanelAvatarBuilder.BuildAndUpload),
            typeof(GameObject), typeof(VRCAvatar),
            typeof(string), typeof(CancellationToken))]
        private static Task OriginalBuildAndUpload(
            object instance,
            GameObject target,
            VRCAvatar avatar,
            string thumbnailPath = null,
            CancellationToken cancellationToken = default)
    #endif
        {
            // stub method, will be replaced by original method
            throw new NotImplementedException("This is a stub for the original method.");
        }

    #region Handle Upload Error

        [CanBeNull] private static MethodInfo _handleUploadErrorMethod;

        private static Exception HandleUploadError(VRCSdkControlPanelAvatarBuilder builderInstance, Exception ex) {
            if (_handleUploadErrorMethod is not null) {
                _handleUploadErrorMethod?.Invoke(builderInstance, new object[] { ex });
                return ex;
            }

            _handleUploadErrorMethod = AccessTools.Method(
                typeof(VRCSdkControlPanelAvatarBuilder), "HandleUploadError",
                new[] {
                    typeof(Exception)
                });

            if (_handleUploadErrorMethod is null) {
                YesLogger.LogError(
                    ex, $"{LoggerConst.LoggerPrefix}{nameof(PreUploadCheckPatch)}",
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