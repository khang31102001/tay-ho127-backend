using System.Security.Claims;

namespace AdminPlatform.Common.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated caller's user id. Controllers should prefer injecting
    /// <see cref="Abstractions.ICurrentUser"/>; this is for the rare spot that only has a ClaimsPrincipal.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(AppClaimTypes.UserId)?.Value;
        return Guid.TryParse(value, out var id) ? id : throw new InvalidOperationException("No user id claim on the current principal.");
    }
}
