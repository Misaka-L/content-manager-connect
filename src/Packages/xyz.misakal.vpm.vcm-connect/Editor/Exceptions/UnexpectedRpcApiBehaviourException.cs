using System;

namespace VRChatContentPublisherConnect.Editor.Exceptions;

internal class UnexpectedRpcApiBehaviourException : Exception {
    public UnexpectedRpcApiBehaviourException(string message) : base(message) { }
}