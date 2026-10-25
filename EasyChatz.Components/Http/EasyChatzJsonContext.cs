using System.Text.Json.Serialization;

namespace EasyChatz.Components.Http;

/// <summary>Source-generated, so the WebAssembly build can trim without breaking serialization.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ChatUser))]
[JsonSerializable(typeof(ChatRoomDto))]
[JsonSerializable(typeof(ChatRoomUpdatedDto))]
[JsonSerializable(typeof(ChatRoomRemovedDto))]
[JsonSerializable(typeof(ChatMessageDto))]
[JsonSerializable(typeof(ChatMessageRemovedDto))]
[JsonSerializable(typeof(ChatConversationDto))]
[JsonSerializable(typeof(List<ChatRoomDto>))]
[JsonSerializable(typeof(List<ChatMessageDto>))]
[JsonSerializable(typeof(List<ChatConversationDto>))]
[JsonSerializable(typeof(ChatMentionDto))]
[JsonSerializable(typeof(List<ChatMentionDto>))]
[JsonSerializable(typeof(ChatPresenceDto))]
[JsonSerializable(typeof(List<ChatPresenceDto>))]
[JsonSerializable(typeof(SaveRoomRequest))]
[JsonSerializable(typeof(SendMessageRequest))]
[JsonSerializable(typeof(ProblemResponse))]
internal sealed partial class EasyChatzJsonContext : JsonSerializerContext;
