using Microsoft.JSInterop;

namespace EasyChatz.Components;

/// <summary>
/// Unread direct-message count for a host layout. It listens on the chat hub without joining the online list.
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

    [Parameter]
    public string Theme { get; set; } = Constants.Themes.Auto;

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private int Count { get; set; }

    protected override async Task OnInitializedAsync() => await RefreshAsync();

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
            string join = Address.HubUrl.Contains('?') ? "&" : "?";
            string hub = Address.HubUrl + join + Constants.WatchQueryName + "=1";
            await module.InvokeVoidAsync("watchUnread", sessionId, hub, Address.IncludeCredentials, self);
            connected = true;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or ObjectDisposedException or OperationCanceledException)
        {
            connecting = false;
        }
    }

    [JSInvokable]
    public async Task OnUnreadChanged()
    {
        await RefreshAsync();
        if (!disposed)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

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
        var unread = await Notifications.GetUnreadAsync();
        Count = unread.IsSuccess && unread.Value is not null ? unread.Value.Sum(item => item.UnreadCount) : 0;
    }
}
