using System.IO;

namespace VRChatContentPublisherConnect.Editor.Services;

internal static class AppStorageService {
    public const string AppDataFolderName = "vrchat-content-manager-connect-81b7bca3";

    public static string GetStoragePath() {
        var libraryPath = GetProjectLibraryPath();
        var dataPath = Path.Combine(libraryPath, AppDataFolderName);
        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        return dataPath;
    }

    public static string GetTempPath() {
        var tempPath = Path.Combine(GetStoragePath(), "temp");
        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        return tempPath;
    }

    private static string GetProjectLibraryPath() {
        return Path.GetFullPath("Library");
    }
}