namespace EasyChatz;

/// <summary>
/// Provides constants.
/// </summary>
public static class Constants
{
    /// <summary>Database schema for providers that support one (SQL Server and PostgreSQL).</summary>
    public const string Schema = "chatz";

    /// <summary>
    /// Prepended to every table name on providers without schemas. In MySQL a schema is a database,
    /// and SQLite has none, so the tables stay unqualified and are prefixed instead.
    /// </summary>
    public const string TablePrefix = "EasyChatz_";

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

    /// <summary>Empty when <paramref name="tenantId"/> is missing. That is the single-tenant partition.</summary>
    /// <param name="tenantId">The tenant key from the host, or null.</param>
    /// <returns>The stored tenant id.</returns>
    public static string TenantKey(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? string.Empty : tenantId.Trim();

    /// <summary>The SignalR group for one person on a single-tenant host.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user group.</returns>
    public static string UserGroup(string userId) => UserGroup(tenantId: null, userId);

    /// <summary>
    /// The SignalR group for one person. A tenant key keeps the same user id in another tenant out of this group.
    /// </summary>
    /// <param name="tenantId">The tenant key, or null for a single-tenant host.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user group.</returns>
    public static string UserGroup(string? tenantId, string userId)
    {
        string tenant = TenantKey(tenantId);
        return tenant.Length == 0 ? "user:" + userId : "tenant:" + tenant + ":user:" + userId;
    }

    /// <summary>
    /// The SignalR group for a room. A single-tenant host keeps the room name, which is what existing clients join.
    /// </summary>
    /// <param name="tenantId">The tenant key, or null for a single-tenant host.</param>
    /// <param name="roomName">The room name.</param>
    /// <returns>The room group.</returns>
    public static string RoomGroup(string? tenantId, string roomName)
    {
        string tenant = TenantKey(tenantId);
        return tenant.Length == 0 ? roomName : "tenant:" + tenant + ":room:" + roomName;
    }

    /// <summary>The SignalR group every live connection in one tenant joins, apart from unread-only watchers.</summary>
    /// <param name="tenantId">The tenant key.</param>
    /// <returns>The tenant group.</returns>
    public static string TenantGroup(string? tenantId) => "tenant:" + TenantKey(tenantId);

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
        /// <summary>
        /// The user blocks constant.
        /// </summary>
        public const string UserBlocks = "UserBlocks";

        /// <summary>Direct-message connection requests.</summary>
        public const string Connections = "Connections";
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
        /// Wide enough for a GUID. Kept to 128 so two of them fit a MySQL index: under utf8mb4 a
        /// varchar(450) pair exceeds the 3072 byte key limit.
        /// </summary>
        public const int UserId = 128;

        /// <summary>Opaque tenant key. Long enough for an integer id or a short slug.</summary>
        public const int TenantId = 64;
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
        /// <summary>
        /// The message deleted constant.
        /// </summary>
        public const string MessageDeleted = "messageDeleted";
        /// <summary>
        /// The archive room constant.
        /// </summary>
        public const string ArchiveRoom = "archiveChatRoom";
        /// <summary>
        /// The user block changed constant.
        /// </summary>
        public const string UserBlockChanged = "userBlockChanged";

        /// <summary>A connection request was sent, accepted, or declined.</summary>
        public const string ConnectionChanged = "connectionChanged";
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
