<img src="https://github.com/gordon-matt/EasyChatz/blob/master/_Misc/Logo.png" alt="Logo" width="350px" />

An embeddable chat for .NET 10. It is written in Blazor and also runs in ASP.NET Core MVC, React, or any HTML page through a custom element. Rooms and direct messages are stored in MySQL, PostgreSQL, SQL Server, or SQLite, and live updates go through SignalR.

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
| `EasyChatz.Services` | Room and message rules, unread notifications, and the Extenso repositories. |
| `EasyChatz.AspNetCore` | `AddEasyChatz()`, `MapEasyChatzApi()`, and `MapEasyChatzHub()`. |
| `EasyChatz.Components` | The `Chat` Razor component, its JavaScript and CSS, and an HTTP client for WebAssembly. |
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

Parameters: `Theme` (`light`, `dark`, `auto`, `bootstrap`), `Height`, `Class`, `ShowPeople`, `OnConversationChanged`, and `OnMessage`. Other attributes are passed to the outer element.

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

`kind` is `room` or `direct`.

### React and plain HTML

`EasyChatz.CustomElements` is a WebAssembly app that registers `<ec-chat>`. Publish it, serve its `_framework` and `_content` folders from your site, and give it an `appsettings.json` that points at the API and the hub:

```json
{
  "EasyChatz": {
    "ApiBaseUrl": "https://chat.example.com/api/easychatz",
    "HubUrl": "https://chat.example.com/chathub",
    "IncludeCredentials": true
  }
}
```

```html
<link rel="stylesheet" href="/_content/EasyChatz.Components/easychatz.css" />
<ec-chat theme="auto" height="640px"></ec-chat>
<script src="/_framework/blazor.webassembly.js"></script>
```

Attributes are kebab-case versions of the component parameters, and the same `ec-conversation` event is raised. The React demo has a wrapper component, `src/EasyChatz.jsx`, and a Vite plugin that publishes the custom elements and serves their output, so `npm run dev` is all that's needed. Start the Blazor demo first: the API, the hub, and the sign-in cookie live there.

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

## Behaviour

- Room messages are saved by the service, then broadcast to a SignalR group named after the room.
- Direct messages are sent only through the hub (`SendPrivate`). The HTTP API does not accept them. Each one creates an unread notification for the recipient.
- Messages are paged with `take` (1–200, default 50) and `beforeId`. Pages come back oldest first.
- Markup is stripped. A message longer than 2000 characters, or a room name longer than 100, is rejected.
- Announcements cannot be renamed or deleted, and its name is reserved. Only an administrator can post there.
- A room can be renamed by its administrator and deleted by its owner or an administrator. The person who created it is the room administrator.
- A message can be deleted by the person who sent it.
- Opening a direct conversation marks that sender's notifications read.
- Writes to the HTTP API require the header `X-EasyChatz: 1`. GET and HEAD are exempt. In-process Blazor Server and MVC do not go through that header.

The hub raises `newMessage`, `newPrivateMessage`, `removeChatMessage`, `addChatRoom`, `updateChatRoom`, `removeChatRoom`, `onRoomDeleted`, `setOnlineUsers`, `addUser`, `removeUser`, and `chatError`.

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
