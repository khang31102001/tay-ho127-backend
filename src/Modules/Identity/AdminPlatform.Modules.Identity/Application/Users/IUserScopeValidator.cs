namespace AdminPlatform.Modules.Identity.Application.Users;

/// <summary>Port the Identity module depends on to validate a Brand/FiscalYear working-context switch
/// against the caller's actual scope, without referencing the Organization module directly. Implemented
/// at the Host composition root, backed by Organization's exposed IUserScopeQueryService.</summary>
public interface IUserScopeValidator
{
    Task<bool> HasBrandAccessAsync(Guid userId, Guid brandId, CancellationToken cancellationToken);

    Task<bool> HasFiscalYearAccessAsync(Guid userId, Guid fiscalYearId, CancellationToken cancellationToken);
}
