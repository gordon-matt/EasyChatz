namespace EasyChatz;

/// <summary>Paths relative to <see cref="Constants.DefaultApiPrefix"/>.</summary>
public static class ApiRoutes
{
    public const string Me = "me";
    public const string Rooms = "rooms";
    public const string Room = "rooms/{roomId:int}";
    public const string RoomMessages = "rooms/{roomId:int}/messages";
    public const string Conversations = "conversations";
    public const string Conversation = "conversations/{userId}";
    public const string ConversationMessages = "conversations/{userId}/messages";
    public const string Message = "messages/{messageId:int}";
    public const string Notifications = "notifications";
    public const string NotificationRead = "notifications/{fromUserId}/read";
}
