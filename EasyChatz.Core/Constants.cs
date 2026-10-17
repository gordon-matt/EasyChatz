namespace EasyChatz;

public static class Constants
{
    /// <summary>Database schema for every provider that supports one. SQLite stores the same tables without a schema.</summary>
    public const string Schema = "chatz";

    /// <summary>Kept apart from the host's <c>__EFMigrationsHistory</c> so both can share a database.</summary>
    public const string MigrationsHistoryTable = "__EasyChatzMigrationsHistory";

    public const string DefaultApiPrefix = "api/easychatz";

    public const string DefaultHubPath = "/chathub";

    public const string AnnouncementsRoomName = "Announcements";

    /// <summary>Owner of the seeded Announcements room. Not a real account.</summary>
    public const string SystemUserId = "system";

    public static string UserGroup(string userId) => "user:" + userId;

    public const string RequestHeaderName = "X-EasyChatz";

    public const string RequestHeaderValue = "1";

    public static class Tables
    {
        public const string Rooms = "Rooms";
        public const string Messages = "Messages";
        public const string Notifications = "Notifications";
    }

    public static class MaxLengths
    {
        public const int RoomName = 100;
        public const int Message = 2000;
        public const int UserId = 450;
    }

    public static class Themes
    {
        public const string Light = "light";
        public const string Dark = "dark";
        public const string Auto = "auto";
        public const string Bootstrap = "bootstrap";
    }

    public static class DomEvents
    {
        public const string Conversation = "ec-conversation";
        public const string Message = "ec-message";
    }

    public static class HubEvents
    {
        public const string NewMessage = "newMessage";
        public const string NewPrivateMessage = "newPrivateMessage";
        public const string RemoveMessage = "removeChatMessage";
        public const string AddRoom = "addChatRoom";
        public const string UpdateRoom = "updateChatRoom";
        public const string RemoveRoom = "removeChatRoom";
        public const string RoomDeleted = "onRoomDeleted";
        public const string SetOnlineUsers = "setOnlineUsers";
        public const string AddUser = "addUser";
        public const string RemoveUser = "removeUser";
        public const string Error = "chatError";
    }

    public static class DatabaseProviders
    {
        public const string MySql = "MySql";
        public const string Npgsql = "Npgsql";
        public const string Sqlite = "Sqlite";
        public const string SqlServer = "SqlServer";
    }
}
