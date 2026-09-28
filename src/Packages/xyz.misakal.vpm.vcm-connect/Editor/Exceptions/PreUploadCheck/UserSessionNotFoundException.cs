using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions.PreUploadCheck;

public sealed class UserSessionNotFoundException : UserSessionValidityException {
    public UserSessionNotFoundException(string account, string? serverDetail) : base(
        $"The VRChat account {account} is not signed in in VRChat Content Publisher App.\n" +
        "Please sign in this account in the app, then try the upload again." +
        FormatServerDetail(serverDetail)
    ) { }
}