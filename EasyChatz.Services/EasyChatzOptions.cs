namespace EasyChatz.Services;

/// <summary>
/// Represents the easy chatz options.
/// </summary>
public sealed class EasyChatzOptions
{
    /// <summary>
    /// The section name constant.
    /// </summary>
    public const string SectionName = "EasyChatz";

    /// <summary>Apply migrations when the host starts. Turn this off when a deployment pipeline owns the schema.</summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Gets or sets the API prefix.
    /// </summary>
    public string ApiPrefix { get; set; } = Constants.DefaultApiPrefix;

    /// <summary>
    /// Gets or sets the hub path.
    /// </summary>
    public string HubPath { get; set; } = Constants.DefaultHubPath;

    /// <summary>
    /// Gets or sets the announcements room name.
    /// </summary>
    public string AnnouncementsRoomName { get; set; } = Constants.AnnouncementsRoomName;

    /// <summary>
    /// Name shown for the seeded Announcements room ("Started by …"). The host sets this to its own name.
    /// </summary>
    public string SystemDisplayName { get; set; } = "EasyChatz";

    /// <summary>
    /// Gets or sets the max message length.
    /// </summary>
    public int MaxMessageLength { get; set; } = Constants.MaxLengths.Message;

    /// <summary>
    /// Gets or sets the max room name length.
    /// </summary>
    public int MaxRoomNameLength { get; set; } = Constants.MaxLengths.RoomName;

    /// <summary>
    /// Gets or sets the default page size.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the max page size.
    /// </summary>
    public int MaxPageSize { get; set; } = 200;
}
