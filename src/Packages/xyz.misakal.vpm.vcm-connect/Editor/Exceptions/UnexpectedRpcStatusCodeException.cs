using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions;

internal sealed class UnexpectedRpcStatusCodeException : UnexpectedRpcApiBehaviourException {
    public UnexpectedRpcStatusCodeException(int code) : base("Response returned an invalid status code: " + code) { }
}