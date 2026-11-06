using Microsoft.JSInterop;

namespace EasyChatz.Components;

/// <summary>
/// Unread direct-message count for a host layout. By default it listens on the chat hub without joining the online list;
/// set <see cref="TrackPresence"/> so the user also shows as online while this layout is open on any page.
/// </summary>
public partial class ChatUnreadBadge : ComponentBase, IAsyncDisposable
{
    private readonly string sessionId = Guid.NewGuid().ToString("N");

    private IJSObjectReference? module;
    private DotNetObjectReference<ChatUnreadBadge>? self;
    private bool connecting;
    private bool connected;
    private bool disposed;

    [Inject]
    private IChatNotificationService Notifications { get; set; } = default!;

    [Inject]
    private EasyChatzApiAddress Address { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    /// <summary>
    /// Gets or sets the theme.
    /// </summary>
    [Parameter]
    public string Theme { get; set; } = Constants.Themes.Auto;

    /// <summary>
    /// Gets or sets the class.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Counts this connection as the user being online. Without it, someone is only online while the Chat component is open.
    /// </summary>
    [Parameter]
    public bool TrackPresence { get; set; }

    /// <summary>
    /// Gets or sets the additional attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private int Count { get; set; }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync() => await RefreshAsync();

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (disposed || connected || connecting)
        {
            return;
        }

        connecting = true;
        try
        {
            module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/EasyChatz.Components/easychatz.js");
            self = DotNetObjectReference.Create(this);
            string hub = Address.HubUrl;
            if (!TrackPresence)
            {
                string join = hub.Contains('?') ? "&" : "?";
                hub += join + Constants.WatchQueryName + "=1";
            }

            await module.InvokeVoidAsync("watchUnread", sessionId, hub, Address.IncludeCredentials, self);
            connected = true;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or ObjectDisposedException or OperationCanceledException)
        {
            connecting = false;
        }
    }

    /// <summary>
    /// Handles the unread changed event.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public async Task OnUnreadChanged()
    {
        await RefreshAsync();
        if (!disposed)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        disposed = true;
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync("disconnect", sessionId);
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        self?.Dispose();
    }

    private async Task RefreshAsync()
    {
        var unread = await Notifications.CountUnreadAsync();
        Count = unread.IsSuccess ? unread.Value : 0;
    }
}
