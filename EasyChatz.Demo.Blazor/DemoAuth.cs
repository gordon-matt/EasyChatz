using System.Security.Claims;
using EasyChatz.Models;
using EasyChatz.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace EasyChatz.Demo;

public static class DemoUsers
{
    public static readonly ChatUser[] All =
    [
        new("alex", "Alex Morgan", true, true),
        new("blair", "Blair Chen", false, true),
        new("casey", "Casey Nguyen", false, true),
    ];

    public static ChatUser? Find(string? id) =>
        All.FirstOrDefault(user => string.Equals(user.Id, id, StringComparison.OrdinalIgnoreCase));
}

public sealed class DemoUserDirectory : IChatUserDirectory
{
    public Task<ChatUser?> FindAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(DemoUsers.Find(userId));

    public Task<IReadOnlyDictionary<string, ChatUser>> FindManyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var found = new Dictionary<string, ChatUser>(StringComparer.Ordinal);
        foreach (string id in userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            var user = DemoUsers.Find(id);
            if (user is not null)
            {
                found[user.Id] = user;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, ChatUser>>(found);
    }
}

public static class DemoAuth
{
    public static IServiceCollection AddDemoUsers(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "easychatz.demo";
                // None, so the React demo on another origin can send the cookie. Requires HTTPS.
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
            });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddSingleton<IChatUserDirectory, DemoUserDirectory>();
        return services;
    }

    public static WebApplication MapDemoAuth(this WebApplication app, string? corsPolicy = null)
    {
        var signIn = app.MapPost("/account/sign-in", (Delegate)SignIn).DisableAntiforgery();
        var signOut = app.MapPost("/account/sign-out", (Delegate)SignOut).DisableAntiforgery();
        if (!string.IsNullOrEmpty(corsPolicy))
        {
            signIn.RequireCors(corsPolicy);
            signOut.RequireCors(corsPolicy);
        }

        return app;
    }

    private static async Task<IResult> SignIn(HttpContext http)
    {
        string? userId = http.Request.HasFormContentType
            ? http.Request.Form["userId"].ToString()
            : null;

        if (string.IsNullOrWhiteSpace(userId) && http.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            var body = await http.Request.ReadFromJsonAsync<SignInBody>();
            userId = body?.UserId;
        }

        var user = DemoUsers.Find(userId);
        if (user is null)
        {
            return Results.BadRequest();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new("ec-admin", user.IsAdministrator ? "true" : "false"),
            new("ec-rooms", user.CanCreateRooms ? "true" : "false"),
        };

        if (user.IsAdministrator)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        }

        await http.SignInAsync(new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        bool scripted = http.Request.Headers[EasyChatz.Constants.RequestHeaderName] == EasyChatz.Constants.RequestHeaderValue;
        return scripted
            ? Results.Ok(new { user.Id, user.DisplayName })
            : Results.Redirect("/");
    }

    private static async Task<IResult> SignOut(HttpContext http)
    {
        await http.SignOutAsync();
        return Results.Redirect("/");
    }

    private sealed record SignInBody(string? UserId);
}
