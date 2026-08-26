using AdminPlatform.Common.Security;
using Microsoft.AspNetCore.Http;

namespace AdminPlatform.Common.Abstractions;

internal sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(AppClaimTypes.UserId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(AppClaimTypes.Email)?.Value;

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(AppClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    public IReadOnlyCollection<string> Permissions =>
        User?.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToArray() ?? [];

    public Guid? CurrentBrandId
    {
        get
        {
            var value = User?.FindFirst(AppClaimTypes.CurrentBrandId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? CurrentFiscalYearId
    {
        get
        {
            var value = User?.FindFirst(AppClaimTypes.CurrentFiscalYearId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
