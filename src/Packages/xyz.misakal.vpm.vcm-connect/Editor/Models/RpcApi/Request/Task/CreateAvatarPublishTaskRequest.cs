using System.Text.Json.Serialization;

namespace VRChatContentPublisherConnect.Editor.Models.RpcApi.Request.Task;

internal record CreateAvatarPublishTaskRequest(
    [property: JsonPropertyName("AvatarId")]
    string AvatarId,
    [property: JsonPropertyName("AvatarBundleFileId")]
    string AvatarBundleFileId,
    [property: JsonPropertyName("Name")]
    string Name,
    [property: JsonPropertyName("Platform")]
    string Platform,
    [property: JsonPropertyName("UnityVersion")]
    string UnityVersion,
    [property: JsonPropertyName("AuthorId")]
    string? AuthorId = null,
    [property: JsonPropertyName("ThumbnailFileId")]
    string? ThumbnailFileId = null,
    [property: JsonPropertyName("Description")]
    string? Description = null,
    [property: JsonPropertyName("Tags")]
    string[]? Tags = null,
    [property: JsonPropertyName("ReleaseStatus")]
    string? ReleaseStatus = null
);