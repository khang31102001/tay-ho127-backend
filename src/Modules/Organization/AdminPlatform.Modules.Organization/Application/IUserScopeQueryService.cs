namespace AdminPlatform.Modules.Organization.Application;

/// <summary>Organization's public read contract, consumed by the Host composition root to implement
/// Identity's IUserScopeValidator port when a user switches their working Brand/FiscalYear context —
/// see architecture assumption #6.</summary>
public interface IUserScopeQueryService
{
    Task<bool> HasBrandAccessAsync(Guid userId, Guid brandId, CancellationToken cancellationToken);
}
