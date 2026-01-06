using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions;

public sealed class RpcApiErrorException : Exception {
    public string Type { get; }
    public int Code { get; }
    public string Title { get; }
    public string Detail { get; }

    public RpcApiErrorException(string type, int code, string title, string detail) : base(
        $"Rpc Api Error occured: {title}: {detail}") {
        Type = type;
        Code = code;
        Title = title;
        Detail = detail;
    }
}