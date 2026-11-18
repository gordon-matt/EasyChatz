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
    /// When set, people can use rooms, including the seeded announcements room.
    /// Turn this off for a host that only wants direct messages. Existing rooms stay stored and are hidden.
    /// </summary>
    public bool AllowRooms { get; set; } = true;

    /// <summary>
    /// When set, a direct message to someone new is a connection request. They can accept or decline.
    /// Until they accept, neither person sees the other as online, and the compose list stays limited
    /// to people who already accepted. Turn this off for a private site where anyone may message anyone.
    /// </summary>
    public bool RequireConnections { get; set; } = true;

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

    /// <summary>
    /// When set, a room's owner or an administrator can archive it. Archived rooms stay readable,
    /// accept no new messages, and stay out of the room list until someone asks to see them.
    /// </summary>
    public bool AllowRoomArchive { get; set; } = true;

    /// <summary>
    /// When set, a person can stop another person from sending them direct messages, and can undo that later.
    /// Room messages are not affected. Turning this off also stops existing blocks from being enforced.
    /// </summary>
    public bool AllowUserBlocks { get; set; } = true;

    /// <summary>
    /// When set, the author of a message can delete it. The thread keeps a note of who deleted it and when,
    /// including for messages deleted before this was turned off.
    /// </summary>
    public bool AllowMessageDelete { get; set; } = true;
}
