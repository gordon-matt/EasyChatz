<img src="https://github.com/gordon-matt/EasyChatz/blob/master/_Misc/Logo.png" alt="Logo" width="350px" />

An embeddable chat for .NET 10. It is written in Blazor and also runs in ASP.NET Core MVC, React, or any HTML page through a custom element. Rooms and direct messages are stored in MySQL, PostgreSQL, SQL Server, or SQLite, and live updates go through SignalR.

## Screenshots
<img src="https://github.com/gordon-matt/EasyChatz/blob/master/_Misc/Screenshots/Light.png" alt="Light Theme" width="100%" />

<img src="https://github.com/gordon-matt/EasyChatz/blob/master/_Misc/Screenshots/Dark.png" alt="Dark Theme" width="100%" />

Features in this version:

- Named rooms, including a seeded Announcements room that only administrators can post in
- Direct messages, with unread counts that clear when the conversation is opened
- Live delivery, presence, and room changes over SignalR
- Cursor paging of messages, HTML stripped from anything that is sent
- Light, dark, auto, and Bootstrap-friendly themes, and a layout that adapts to its container's width

EasyChatz does not authenticate anyone. The host already has accounts. Register `IChatUserDirectory` so display names, the administrator flag, and room-creation permission come from those accounts.

## Projects

| Project | Purpose |
| --- | --- |
| `EasyChatz.Core` | DTOs, `IChatService`, `IChatUserDirectory`, `ApiRoutes`. Depends on Ardalis.Result. |
| `EasyChatz.Data` | Entities and the EF Core model. Tables live in the `chatz` schema. |
| `EasyChatz.Data.Sqlite` | SQLite. Register it with `AddEasyChatzSqlite()`. |
| `EasyChatz.Data.Sql` | SQL Server. Register it with `AddEasyChatzSqlServer()`. |
| `EasyChatz.Data.Npgsql` | PostgreSQL. Register it with `AddEasyChatzNpgsql()`. |
| `EasyChatz.Data.MySql` | MySQL (Oracle's provider). Register it with `AddEasyChatzMySql()`. |
| `EasyChatz.Services` | Room and message rules, unread notifications, the delivery queue, and the Extenso repositories. |
| `EasyChatz.AspNetCore` | `AddEasyChatz()`, `MapEasyChatzApi()`, and `MapEasyChatzHub()`. |
| `EasyChatz.Components` | The `Chat` and `ChatUnreadBadge` Razor components, their JavaScript and CSS, and an HTTP client for WebAssembly. |
| `EasyChatz.CustomElements` | A Blazor WebAssembly app that registers `<ec-chat>` for React and plain HTML. |
| `EasyChatz.Tests` | xUnit tests for rooms, announcements, direct messages, and paging. |
| `EasyChatz.Demo.*` | Blazor, MVC, and React demos. |

## How it fits together

The component talks to `IChatService`. On Blazor Server (and therefore MVC) that is the real service, in-process. In WebAssembly it is an HTTP client that calls the API. Either way the browser joins the SignalR hub, so every host maps both `MapEasyChatzApi()` and `MapEasyChatzHub()`.

Repositories are Extenso `IRepository<T>` instances bound to `IEasyChatzDbContextFactory`, not the host's own factory. A host can call `AddEntityFrameworkRepository()` for its own database without EasyChatz reading the wrong one. Services return `Ardalis.Result`.

## Getting started

### Users

```csharp
builder.Services.AddScoped<IChatUserDirectory, MyChatUserDirectory>();
builder.Services.AddEasyChatz();
builder.Services.AddEasyChatzSqlite(builder.Configuration.GetConnectionString("EasyChatz"));
```

`IChatUserDirectory` must be registered first. `AddEasyChatz` only adds a fallback when nothing else is registered, and that fallback treats the user id as the display name.

`ChatUser` is `(Id, DisplayName, IsAdministrator, CanCreateRooms)`. On ASP.NET Core, `ChatCurrentUser` reads the signed-in principal and then asks the directory. A user is an administrator when the directory says so, when they are in the `Administrator` role, or when they have the claim `ec-admin=true`. They can create rooms when they are an administrator, when the directory allows it, or when they have the claim `ec-rooms=true`.

### Database

Pick one provider. All four use schema `chatz` and a private migrations history table, `__EasyChatzMigrationsHistory`, so EasyChatz can share a database with the host. SQLite has no schemas; the provider ignores that warning and creates the same tables.

```csharp
builder.Services.AddEasyChatzSqlite(connectionString);     // EasyChatz.Data.Sqlite
builder.Services.AddEasyChatzSqlServer(connectionString);  // EasyChatz.Data.Sql
builder.Services.AddEasyChatzNpgsql(connectionString);     // EasyChatz.Data.Npgsql
builder.Services.AddEasyChatzMySql(connectionString);      // EasyChatz.Data.MySql
```

`AddEasyChatzSqlite()` with no connection string stores `EasyChatz.db` under the user's local application data folder. `AutoMigrate` is on by default and applies migrations at startup, then seeds the Announcements room. Turn it off when a deployment pipeline owns the schema:

```json
{
  "ConnectionStrings": {
    "EasyChatz": "Data Source=App_Data/EasyChatz.db"
  },
  "EasyChatz": {
    "AutoMigrate": true
  }
}
```

Other settings on `EasyChatzOptions`: `ApiPrefix` (default `api/easychatz`), `HubPath` (default `/chathub`), `AnnouncementsRoomName`, `MaxMessageLength` (2000), `MaxRoomNameLength` (100), `DefaultPageSize` (50), and `MaxPageSize` (200).

### Blazor

```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<IChatUserDirectory, MyChatUserDirectory>();
builder.Services.AddEasyChatz();
builder.Services.AddEasyChatzSqlite(builder.Configuration.GetConnectionString("EasyChatz"));

// ...

app.MapEasyChatzApi().RequireAuthorization();
app.MapEasyChatzHub().RequireAuthorization();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

Add the stylesheet to `App.razor`, and `@using EasyChatz.Components` to `_Imports.razor`:

```html
<link rel="stylesheet" href="@Assets["_content/EasyChatz.Components/easychatz.css"]" />
```

```razor
<Chat Theme="auto" Height="640px" OnConversationChanged="conversation => current = conversation" />
```

The component must render interactively. In Blazor WebAssembly, call `AddEasyChatzHttpClient(apiBaseUrl, hubUrl)` instead of `AddEasyChatz`, and host the API and hub in a server project.

Parameters: `Theme` (`light`, `dark`, `auto`, `bootstrap`), `Height`, `Class`, `ShowPeople`, `OnConversationChanged`, `OnMessage`, `WithUserId`, `OnOpenUser`, `OpenUserText`, and `MaxLength`. Other attributes are passed to the outer element. See [Integrating with the host](#integrating-with-the-host) for the ones a host uses to connect chat to the rest of its app.

### MVC

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<IChatUserDirectory, MyChatUserDirectory>();
builder.Services.AddEasyChatz();
builder.Services.AddEasyChatzSqlite(builder.Configuration.GetConnectionString("EasyChatz"));

// ...

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapBlazorHub();
app.MapEasyChatzApi().RequireAuthorization();
app.MapEasyChatzHub().RequireAuthorization();
```

In the layout, add the stylesheet and `<script src="~/_framework/blazor.server.js"></script>`. Then render the component in any view:

```cshtml
<component type="typeof(EasyChatz.Components.Chat)" render-mode="Server" param-Height="@("640px")" />
```

Parameters are passed as `param-Name`. Event callbacks aren't available this way, so the component also raises a DOM event that bubbles from its element:

```js
document.addEventListener('ec-conversation', e => console.log(e.detail.kind, e.detail.id, e.detail.name));
```

`kind` is `room` or `private`.

### React and plain HTML

`EasyChatz.CustomElements` is a Blazor WebAssembly app that registers `<ec-chat>`. It is not a NuGet package and not an npm package. Publish it and serve the static files from the site that hosts the page:

```bash
dotnet publish EasyChatz.CustomElements -c Release
```

Copy `wwwroot/_framework` and `wwwroot/_content` from that output so the site serves them at `/_framework` and `/_content`. In a Vite app, put both folders in `public/`. Asset names stay stable between publishes.

The app reads its API and hub addresses from `/appsettings.json` on the page's origin. In Vite that file is `public/appsettings.json`:

```json
{
  "EasyChatz": {
    "ApiBaseUrl": "https://chat.example.com/api/easychatz",
    "HubUrl": "https://chat.example.com/chathub",
    "IncludeCredentials": true
  }
}
```

A plain page loads the stylesheet and the runtime, then uses the element. The script is a classic script tag, not `type="module"`:

```html
<link rel="stylesheet" href="/_content/EasyChatz.Components/easychatz.css" />
<ec-chat theme="auto" height="640px" with-user-id="blair" show-people="true"></ec-chat>
<script src="/_framework/blazor.webassembly.js"></script>
```

Attributes are kebab-case versions of the component parameters. `with-user-id` opens that direct message when the element loads, and again when the attribute changes. `ec-conversation` is a DOM event. `event.detail` is `{ kind, id, name }`. `kind` is `room` or `private`. For a room, `id` is the room id. For a direct message, `id` is the other person's user id.

#### React

Add the same stylesheet and script to `index.html`. The script follows the React module:

```html
<link rel="stylesheet" href="/_content/EasyChatz.Components/easychatz.css" />
<div id="root"></div>
<script type="module" src="/src/main.jsx"></script>
<script src="/_framework/blazor.webassembly.js"></script>
```

React does not pass custom-element events through as props. Listen for them on the element. Boolean parameters are HTML attributes, so pass `"true"` and `"false"`:

```jsx
import { useEffect, useRef } from 'react'

export function Chat({
  theme = 'auto', height = '640px', withUserId, showPeople = true,
  maxLength, onConversation,
}) {
  const ref = useRef(null)
  const handler = useRef(onConversation)

  useEffect(() => {
    handler.current = onConversation
  })

  useEffect(() => {
    const element = ref.current
    const conversation = event => handler.current?.(event.detail)
    element.addEventListener('ec-conversation', conversation)
    return () => element.removeEventListener('ec-conversation', conversation)
  }, [])

  return (
    <ec-chat
      ref={ref}
      theme={theme}
      height={height}
      with-user-id={withUserId}
      show-people={String(showPeople)}
      max-length={maxLength}
    />
  )
}
```

`onConversation` receives `{ kind, id, name }`. The React demo's `src/EasyChatz.jsx` listens for that event the same way. Its Vite plugin publishes the custom-element app and copies `_framework` and `_content` into the build, so `npm run dev` is enough inside that demo. Another React app copies those folders itself. Start the API and the hub before the page. The sign-in cookie is issued by that host.

When the API is on another origin, it needs a CORS policy that allows credentials. Writes send an `X-EasyChatz` header, so that header must be allowed:

```csharp
builder.Services.AddCors(options => options.AddPolicy("EasyChatz", policy => policy
    .WithOrigins("https://app.example.com")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

app.UseCors();
app.MapEasyChatzApi().RequireAuthorization().RequireCors("EasyChatz");
app.MapEasyChatzHub().RequireAuthorization().RequireCors("EasyChatz");
```

`IncludeCredentials` sends the cookie on JSON calls and on the SignalR connection. For bearer tokens, add your own handler through the `configureClient` argument of `AddEasyChatzHttpClient`.

## Integrating with the host

EasyChatz keeps its own rooms, messages, and notifications in the `chatz` schema. It does not keep accounts, profile pages, or an email sender. The hooks below are how a host connects those.

### Directory

`IChatUserDirectory` is required for a real app. `ChatUser` is `(Id, DisplayName, IsAdministrator, CanCreateRooms)`.

- `Id` is the host's user id, the same value as `ClaimTypes.NameIdentifier`.
- `DisplayName` is whatever the host wants shown. A family app might use a person's name and fall back to the account name.
- `IsAdministrator` matches the host's administrator role.
- `CanCreateRooms` is who may create a room. Set it for administrators and anyone else the host allows, such as editors. The `ec-rooms` claim does the same job when the directory does not.

Register the directory before `AddEasyChatz()`.

### Open a direct message

`WithUserId` opens that conversation when the component loads, and again whenever the value changes. Use it for a link such as "Message this person":

```razor
<Chat WithUserId="@UserId" />

@code {
    [SupplyParameterFromQuery(Name = "withUser")]
    private string? UserId { get; set; }
}
```

The id has to be one the directory can find. If it cannot, the component shows the directory's not-found message.

### Open someone from the online list

`OnOpenUser` adds a button next to Message. The button is hidden when nothing is listening. `OpenUserText` defaults to `Open`.

```razor
<Chat OnOpenUser="OpenUser" OpenUserText="View profile" />

@code {
    private void OpenUser(ChatPresenceDto person) =>
        Navigation.NavigateTo($"/people/{person.UserId}");
}
```

`ChatPresenceDto` has the connection id, user id, display name, and administrator flag. It does not have a profile URL. The host decides where the button goes.

### Unread count in the layout

`ChatUnreadBadge` shows how many direct messages the signed-in user has not opened. Drop it in a nav link. It renders nothing when the count is zero.

```razor
<a href="/chat">Messages <ChatUnreadBadge /></a>
```

The badge calls `IChatNotificationService.GetUnreadAsync` and then listens on the chat hub. It connects with `?watch=1`, joins that user's group, and does not appear in the online list. Opening a conversation marks those notices read and pushes `unreadChanged`, so a badge elsewhere on the site drops without a refresh. `Theme` and `Class` match the chat component.

The badge needs the same `EasyChatzApiAddress` and hub mapping as `Chat`. It is a second SignalR connection, not a second copy of the chat UI.

### Hand unread messages to email or push

`IChatDeliveryQueue` is for a trusted background job. It is not mapped on the HTTP API, because it can see every recipient.

```csharp
public sealed class UnreadMessageJob(IChatDeliveryQueue queue, IEmailSender email)
{
    public async Task SendAsync(CancellationToken cancellationToken)
    {
        var pending = await queue.ListPendingAsync(DateTime.UtcNow.AddMinutes(-30), cancellationToken);
        foreach (var notice in pending)
        {
            await email.SendAsync(notice.ToUserId, notice.FromUserId, notice.Content, cancellationToken);
        }

        await queue.MarkDeliveredAsync(pending.Select(notice => notice.Id).ToArray(), cancellationToken);
    }
}
```

`ListPendingAsync` returns notices that are still unread, have no `DeliveredAtUtc`, and were created at or before the time you pass. Each `ChatPendingNotice` has `Id`, `MessageId`, `FromUserId`, `ToUserId`, `Content`, and `CreatedAtUtc`. `MarkDeliveredAsync` sets `DeliveredAtUtc`. It does not mark the conversation read, so the badge still shows until the recipient opens it.

Pass a time in the past when the host wants to wait before sending. Pass a time in the future to take everything that is waiting.

### Composer length

`MaxLength` caps the text box. The server still rejects anything over `EasyChatz:MaxMessageLength`, which defaults to 2000 and cannot be set higher than that. Set both to the same number when the host wants a shorter limit.

## Behaviour

- Room messages are saved by the service, then broadcast to a SignalR group named after the room.
- Direct messages are sent only through the hub (`SendPrivate`). The HTTP API does not accept them. Each one creates an unread notification for the recipient.
- Messages are paged with `take` (1–200, default 50) and `beforeId`. Pages come back oldest first.
- Markup is stripped. A message longer than `MaxMessageLength` (default 2000), or a room name longer than 100, is rejected. The composer can be capped lower with the `MaxLength` parameter.
- Announcements cannot be renamed or deleted, and its name is reserved. Only an administrator can post there.
- A room can be renamed by its administrator and deleted by its owner or an administrator. The person who created it is the room administrator.
- A message can be deleted by the person who sent it.
- Opening a direct conversation marks that sender's notifications read and pushes `unreadChanged` to that user's connections.
- Writes to the HTTP API require the header `X-EasyChatz: 1`. GET and HEAD are exempt. In-process Blazor Server and MVC do not go through that header.

The hub raises `newMessage`, `newPrivateMessage`, `removeChatMessage`, `addChatRoom`, `updateChatRoom`, `removeChatRoom`, `onRoomDeleted`, `setOnlineUsers`, `addUser`, `removeUser`, `unreadChanged`, and `chatError`. A connection opened with `?watch=1` receives `newPrivateMessage` and `unreadChanged` only, and is left out of presence.

## Demos

| Demo | Address |
| --- | --- |
| Blazor, which also hosts the API and hub | https://localhost:7411 |
| MVC | https://localhost:7412 |
| Custom element | https://localhost:7413 |
| React | http://localhost:5174 |

The demos use cookie authentication and SQLite at `App_Data/EasyChatz.db`. Sign in as Alex Morgan (administrator), Blair Chen, or Casey Nguyen. The cookie is `SameSite=None` and `Secure`, so use the HTTPS profile. The React demo talks to the Blazor demo, which allows `http://localhost:5174` and `https://localhost:7413`.

## Layout

The component fills its container's width and the `Height` you give it, and adapts its layout to the container rather than the window. Dialogs are fixed-position overlays, so don't place the component inside an element with a CSS `transform`, `filter`, or `perspective`. Those create a new containing block and misplace the overlays.

## Support

If you find this project useful, consider supporting its development.

<p align="center">
  <a href="https://buymeacoffee.com/vnmatt">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/BuyMeACoffee_Logo.png"
         alt="Buy Me a Coffee"
         height="40">
  </a>&nbsp;&nbsp;&nbsp;
  <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=gordon_matt%40live%2ecom&lc=AU&currency_code=AUD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/PayPal_Logo.png"
         alt="Donate with PayPal"
         height="40">
  </a>
</p>

<p align="center">
  Prefer using your phone? Scan the QR code:
</p>

<p align="center">
  <a href="https://buymeacoffee.com/vnmatt">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/BuyMeACoffee_QR.png"
         alt="Buy Me a Coffee QR Code"
         width="180">
  </a>
</p>

<img src="https://komarev.com/ghpvc/?username=gordon-matt&label=GitHub%20Hits%20Since%202025-06-01%3A%20&color=ff0000&style=flat" alt="gordon-matt" />
