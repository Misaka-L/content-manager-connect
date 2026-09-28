using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions.PreUploadCheck;

// Base of the failures that come from the VRChat account session check, so callers can treat them alike.
public abstract class UserSessionValidityException : Exception {
    protected UserSessionValidityException(string message) : base(message) { }

    protected static string FormatServerDetail(string? serverDetail) {
        return string.IsNullOrEmpty(serverDetail)
            ? string.Empty
            : $"\n\nDetails from the app: {serverDetail}";
    }
}