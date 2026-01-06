using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions;

internal sealed class UnexpectedRpcResponseException : UnexpectedRpcApiBehaviourException {
    public UnexpectedRpcResponseException() : base("The response from the server was invalid.") { }
    public UnexpectedRpcResponseException(string message) : base(message) { }
}