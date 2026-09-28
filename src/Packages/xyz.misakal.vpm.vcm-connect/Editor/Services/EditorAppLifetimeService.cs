using System;
using System.Threading.Tasks;
using UnityEditor;
using VRChatContentPublisherConnect.Editor.Views;
using VRChatContentPublisherConnect.Editor.Services.Rpc;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;

namespace VRChatContentPublisherConnect.Editor.Services;

internal sealed class EditorAppLifetimeService {
    private readonly RpcClientService _rpcClientService;
    private readonly AppSettingsService _appSettingsService;
    private readonly MenuItemService _menuItemService;

    private readonly YesLogger _logger = new(LoggerConst.LoggerPrefix + nameof(EditorAppLifetimeService));

    public EditorAppLifetimeService(
        RpcClientService rpcClientService,
        AppSettingsService appSettingsService,
        MenuItemService menuItemService) {
        _rpcClientService = rpcClientService;
        _appSettingsService = appSettingsService;
        _menuItemService = menuItemService;
    }

    public async Task StartAsync() {
        var isFirstInstall = !_appSettingsService.IsOnboardingCompleted();
        _menuItemService.Init();

        if (isFirstInstall) {
            MainThreadDispatcher.Dispatch(() => {
                ContentManagerSettingsWindow.ShowSettings();
                EditorUtility.DisplayDialog(
                    "Welcome to VRChat Content Publisher Connect",
                    "It looks like this is your first time using VRChat Content Publisher Connect.\n\n" +
                    "The settings window has been opened automatically.\n" +
                    "Please connect to the VRChat Manager App to get started.",
                    "OK");

                _appSettingsService.SetOnboardingCompleted();
            });
        }

        var settings = _appSettingsService.GetSettings();

        try {
            await _rpcClientService.RestoreSessionAsync(false, settings.LaunchAppWhenStartup);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Failed to auto restore session");
        }
    }
}