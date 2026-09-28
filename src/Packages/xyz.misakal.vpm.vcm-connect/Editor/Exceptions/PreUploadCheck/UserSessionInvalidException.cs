using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions.PreUploadCheck;

public sealed class UserSessionInvalidException : UserSessionValidityException {
    public UserSessionInvalidException(string account, string? serverDetail) : base(
        $"VRChat Content Publisher App could not confirm that the VRChat account {account} is still signed in.\n" +
        "The account may have been signed out in the app, or the app failed to refresh its session.\n" +
        "Please make sure this account is signed in in the app." +
        FormatServerDetail(serverDetail)
    ) { }
}