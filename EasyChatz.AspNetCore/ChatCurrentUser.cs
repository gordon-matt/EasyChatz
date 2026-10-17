using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EasyChatz;

/// <summary>Reads the signed-in user from the HTTP request, or from the Blazor circuit when there is no request.</summary>
public sealed class ChatCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider services,
    IChatUserDirectory directory) : IChatCurrentUser
{
    public async Task<ChatUser?> GetAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            principal = await FromCircuitAsync();
        }

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var person = await directory.FindAsync(id, cancellationToken);
        bool administrator = person?.IsAdministrator == true
            || principal.IsInRole("Administrator")
            || principal.HasClaim("ec-admin", "true");
        bool canCreateRooms = administrator || person?.CanCreateRooms == true || principal.HasClaim("ec-rooms", "true");
        string displayName = person?.DisplayName
            ?? principal.Identity?.Name
            ?? id;

        return new ChatUser(id, displayName, administrator, canCreateRooms || person is null);
    }

    private async Task<ClaimsPrincipal?> FromCircuitAsync()
    {
        var authentication = services.GetService<AuthenticationStateProvider>();
        if (authentication is null)
        {
            return null;
        }

        try
        {
            return (await authentication.GetAuthenticationStateAsync()).User;
        }
        catch (InvalidOperationException)
        {
            // No circuit, for example a background task. The HTTP principal is the other path.
            return null;
        }
    }
}
