using System.Security.Claims;

namespace AdminPlatform.Common.Security;

/// <summary>Adds an admin caller's permission claims to the authenticated principal from the server-side source.
///
/// The access token deliberately carries no permission claims: with ~115 permission codes (all granted to SuperAdmin) the
/// token alone exceeded 3.5 KB, which overflowed the reverse proxy's response header buffer and is close to the 4 KB
/// per-cookie limit of browsers. Everything downstream (<see cref="PermissionAuthorizationHandler"/>,
/// <see cref="Abstractions.ICurrentUser"/>) keeps reading <see cref="AppClaimTypes.Permission"/> claims off the principal —
/// they are just filled in here, per request, instead of coming from the token.
///
/// Runs after the JWT signature is validated, so the user id (<c>sub</c>) is trustworthy. Any permission claims a token
/// still carries (tokens issued before this change) are dropped: the server-side source is the only authority.</summary>
public static class PermissionClaimsEnricher
{
    public static async Task EnrichAsync(
        ClaimsPrincipal? principal, IUserPermissionSource source, CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return;
        }

        // Only Admin tokens have roles/permissions; Customer tokens are left exactly as issued.
        if (principal.FindFirst(AppClaimTypes.AccountType)?.Value != AccountTypes.Admin)
        {
            return;
        }

        if (!Guid.TryParse(principal.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            return;
        }

        foreach (var identity in principal.Identities)
        {
            foreach (var carried in identity.FindAll(AppClaimTypes.Permission).ToList())
            {
                identity.RemoveClaim(carried);
            }
        }

        var snapshot = await source.GetAsync(userId, cancellationToken);
        principal.Identities.First().AddClaims(
            snapshot.Permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));
    }
}
