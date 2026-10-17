namespace EasyChatz.Services;

/// <summary>Used until the ASP.NET Core host replaces it with the SignalR implementation.</summary>
internal sealed class NullChatRealtime : IChatRealtime
{
    public Task PublishRoomMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishPrivateMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishMessageRemovedAsync(ChatMessageRemovedDto message, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishRoomAddedAsync(ChatRoomDto room, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishRoomUpdatedAsync(ChatRoomUpdatedDto room, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishRoomRemovedAsync(ChatRoomRemovedDto room, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
