using System.Text.Json.Serialization;

namespace VRChatContentPublisherConnect.Editor.Models.RpcApi.Response;

internal record ErrorResponse(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("status")] int StatusCode,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail
);