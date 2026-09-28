using System;
using System.IO;
using System.Text.Json;
using VRChatContentPublisherConnect.Editor.Models;
using YesPatchFrameworkForVRChatSdk.PatchApi.Logging;

namespace VRChatContentPublisherConnect.Editor.Services;

internal sealed class AppSettingsService {
    private AppSettings? _settings;

    public void SetOnboardingCompleted() {
        File.WriteAllText(GetOnboardingPath(), DateTimeOffset.Now.ToString("O"));
    }

    public bool IsOnboardingCompleted() {
        var onboardingPath = GetOnboardingPath();
        if (!File.Exists(onboardingPath)) return false;

        var onboardingFileRaw = "";
        try {
            onboardingFileRaw = File.ReadAllText(onboardingPath);
        }
        catch (Exception e) {
            YesLogger.LogWarning(e,
                "VCCM." + nameof(AppStorageService),
                "Failed to read onboarding completed datetime file", null);
            return false;
        }
        
        if (!DateTimeOffset.TryParse(onboardingFileRaw, out var onboardingDateTime)) {
            return false;
        }

        return onboardingDateTime <= DateTimeOffset.Now;
    }

    private static string GetOnboardingPath() {
        return Path.Combine(AppStorageService.GetStoragePath(), "onboarding-completed-datetime");
    }

    public AppSettings GetSettings() {
        if (_settings is not null)
            return _settings;

        var settingsPath = GetSettingsPath();
        if (File.Exists(settingsPath)) {
            var json = File.ReadAllText(settingsPath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (_settings is not null)
                return _settings;
        }

        SaveSettings();
        return _settings!;
    }

    public void SaveSettings() {
        _settings ??= new AppSettings();

        var settingsPath = GetSettingsPath();
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions {
            WriteIndented = true
        });

        File.WriteAllText(settingsPath, json);
    }

    private static string GetSettingsPath() {
        return Path.Combine(AppStorageService.GetStoragePath(), "settings.json");
    }
}