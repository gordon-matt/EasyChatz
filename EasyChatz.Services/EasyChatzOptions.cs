namespace EasyChatz.Services;

public sealed class EasyChatzOptions
{
    public const string SectionName = "EasyChatz";

    /// <summary>Apply migrations when the host starts. Turn this off when a deployment pipeline owns the schema.</summary>
    public bool AutoMigrate { get; set; } = true;

    public string ApiPrefix { get; set; } = Constants.DefaultApiPrefix;

    public string HubPath { get; set; } = Constants.DefaultHubPath;

    public string AnnouncementsRoomName { get; set; } = Constants.AnnouncementsRoomName;

    /// <summary>
    /// Name shown for the seeded Announcements room ("Started by …"). The host sets this to its own name.
    /// </summary>
    public string SystemDisplayName { get; set; } = "EasyChatz";

    public int MaxMessageLength { get; set; } = Constants.MaxLengths.Message;

    public int MaxRoomNameLength { get; set; } = Constants.MaxLengths.RoomName;

    public int DefaultPageSize { get; set; } = 50;

    public int MaxPageSize { get; set; } = 200;
}
