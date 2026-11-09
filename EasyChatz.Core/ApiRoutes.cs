namespace EasyChatz;

/// <summary>Paths relative to <see cref="Constants.DefaultApiPrefix"/>.</summary>
public static class ApiRoutes
{
    /// <summary>
    /// The me constant.
    /// </summary>
    public const string Me = "me";
    /// <summary>
    /// The rooms constant.
    /// </summary>
    public const string Rooms = "rooms";
    /// <summary>
    /// The room constant.
    /// </summary>
    public const string Room = "rooms/{roomId:int}";
    /// <summary>
    /// The room messages constant.
    /// </summary>
    public const string RoomMessages = "rooms/{roomId:int}/messages";
    /// <summary>
    /// The conversations constant.
    /// </summary>
    public const string Conversations = "conversations";
    /// <summary>
    /// The conversation constant.
    /// </summary>
    public const string Conversation = "conversations/{userId}";
    /// <summary>
    /// The conversation messages constant.
    /// </summary>
    public const string ConversationMessages = "conversations/{userId}/messages";
    /// <summary>
    /// The message constant.
    /// </summary>
    public const string Message = "messages/{messageId:int}";
    /// <summary>
    /// The mentions constant.
    /// </summary>
    public const string Mentions = "mentions";
    /// <summary>
    /// The features constant.
    /// </summary>
    public const string Features = "features";
    /// <summary>
    /// The room archive constant.
    /// </summary>
    public const string RoomArchive = "rooms/{roomId:int}/archive";
    /// <summary>
    /// The conversation block constant.
    /// </summary>
    public const string ConversationBlock = "conversations/{userId}/block";
    /// <summary>
    /// The notifications constant.
    /// </summary>
    public const string Notifications = "notifications";
    /// <summary>
    /// The notification read constant.
    /// </summary>
    public const string NotificationRead = "notifications/{fromUserId}/read";
}
