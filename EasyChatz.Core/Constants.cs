namespace EasyChatz;

/// <summary>
/// Provides constants.
/// </summary>
public static class Constants
{
    /// <summary>Database schema for every provider that supports one. SQLite stores the same tables without a schema.</summary>
    public const string Schema = "chatz";

    /// <summary>Kept apart from the host's <c>__EFMigrationsHistory</c> so both can share a database.</summary>
    public const string MigrationsHistoryTable = "__EasyChatzMigrationsHistory";

    /// <summary>
    /// The default API prefix constant.
    /// </summary>
    public const string DefaultApiPrefix = "api/easychatz";

    /// <summary>
    /// The default hub path constant.
    /// </summary>
    public const string DefaultHubPath = "/chathub";

    /// <summary>Query flag for a connection that only listens for unread changes and does not join presence.</summary>
    public const string WatchQueryName = "watch";

    /// <summary>
    /// The announcements room name constant.
    /// </summary>
    public const string AnnouncementsRoomName = "Announcements";

    /// <summary>Owner of the seeded Announcements room. Not a real account.</summary>
    public const string SystemUserId = "system";

    /// <summary>
    /// Gets the user group.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user group.</returns>
    public static string UserGroup(string userId) => "user:" + userId;

    /// <summary>
    /// The request header name constant.
    /// </summary>
    public const string RequestHeaderName = "X-EasyChatz";

    /// <summary>
    /// The request header value constant.
    /// </summary>
    public const string RequestHeaderValue = "1";

    /// <summary>
    /// Provides tables.
    /// </summary>
    public static class Tables
    {
        /// <summary>
        /// The rooms constant.
        /// </summary>
        public const string Rooms = "Rooms";
        /// <summary>
        /// The messages constant.
        /// </summary>
        public const string Messages = "Messages";
        /// <summary>
        /// The notifications constant.
        /// </summary>
        public const string Notifications = "Notifications";
    }

    /// <summary>
    /// Provides max lengths.
    /// </summary>
    public static class MaxLengths
    {
        /// <summary>
        /// The room name constant.
        /// </summary>
        public const int RoomName = 100;
        /// <summary>
        /// The message constant.
        /// </summary>
        public const int Message = 2000;
        /// <summary>
        /// The user identifier constant.
        /// </summary>
        public const int UserId = 450;
    }

    /// <summary>
    /// Provides themes.
    /// </summary>
    public static class Themes
    {
        /// <summary>
        /// The light constant.
        /// </summary>
        public const string Light = "light";
        /// <summary>
        /// The dark constant.
        /// </summary>
        public const string Dark = "dark";
        /// <summary>
        /// The auto constant.
        /// </summary>
        public const string Auto = "auto";
        /// <summary>
        /// The bootstrap constant.
        /// </summary>
        public const string Bootstrap = "bootstrap";
    }

    /// <summary>
    /// Provides dom events.
    /// </summary>
    public static class DomEvents
    {
        /// <summary>
        /// The conversation constant.
        /// </summary>
        public const string Conversation = "ec-conversation";
        /// <summary>
        /// The message constant.
        /// </summary>
        public const string Message = "ec-message";
    }

    /// <summary>
    /// Provides hub events.
    /// </summary>
    public static class HubEvents
    {
        /// <summary>
        /// The new message constant.
        /// </summary>
        public const string NewMessage = "newMessage";
        /// <summary>
        /// The new private message constant.
        /// </summary>
        public const string NewPrivateMessage = "newPrivateMessage";
        /// <summary>
        /// The remove message constant.
        /// </summary>
        public const string RemoveMessage = "removeChatMessage";
        /// <summary>
        /// The add room constant.
        /// </summary>
        public const string AddRoom = "addChatRoom";
        /// <summary>
        /// The update room constant.
        /// </summary>
        public const string UpdateRoom = "updateChatRoom";
        /// <summary>
        /// The remove room constant.
        /// </summary>
        public const string RemoveRoom = "removeChatRoom";
        /// <summary>
        /// The room deleted constant.
        /// </summary>
        public const string RoomDeleted = "onRoomDeleted";
        /// <summary>
        /// The set online users constant.
        /// </summary>
        public const string SetOnlineUsers = "setOnlineUsers";
        /// <summary>
        /// The add user constant.
        /// </summary>
        public const string AddUser = "addUser";
        /// <summary>
        /// The remove user constant.
        /// </summary>
        public const string RemoveUser = "removeUser";
        /// <summary>
        /// The error constant.
        /// </summary>
        public const string Error = "chatError";
        /// <summary>
        /// The unread changed constant.
        /// </summary>
        public const string UnreadChanged = "unreadChanged";
    }

    /// <summary>
    /// Provides database providers.
    /// </summary>
    public static class DatabaseProviders
    {
        /// <summary>
        /// The my SQL constant.
        /// </summary>
        public const string MySql = "MySql";
        /// <summary>
        /// The npgsql constant.
        /// </summary>
        public const string Npgsql = "Npgsql";
        /// <summary>
        /// The sqlite constant.
        /// </summary>
        public const string Sqlite = "Sqlite";
        /// <summary>
        /// The SQL server constant.
        /// </summary>
        public const string SqlServer = "SqlServer";
    }
}
