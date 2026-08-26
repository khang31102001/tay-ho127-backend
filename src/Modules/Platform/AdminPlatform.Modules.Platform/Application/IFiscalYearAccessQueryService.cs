namespace AdminPlatform.Modules.Platform.Application;

/// <summary>Platform's public read contract, consumed by the Host composition root to implement Identity's
/// IUserScopeValidator.HasFiscalYearAccessAsync — see architecture assumption #6. The task's data model has
/// no per-user fiscal-year scoping table, so "access" here just means the fiscal year exists and is active.</summary>
public interface IFiscalYearAccessQueryService
{
    Task<bool> IsSelectableAsync(Guid fiscalYearId, CancellationToken cancellationToken);
}
