using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Organization.Application;
using AdminPlatform.Modules.Platform.Application;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Identity's IUserScopeValidator port by delegating brand access to Organization's
/// IUserScopeQueryService and fiscal-year access to Platform's IFiscalYearAccessQueryService — see
/// architecture assumption #6.</summary>
internal sealed class IdentityUserScopeAdapter : IUserScopeValidator
{
    private readonly IUserScopeQueryService _userScopeQueryService;
    private readonly IFiscalYearAccessQueryService _fiscalYearAccessQueryService;

    public IdentityUserScopeAdapter(IUserScopeQueryService userScopeQueryService, IFiscalYearAccessQueryService fiscalYearAccessQueryService)
    {
        _userScopeQueryService = userScopeQueryService;
        _fiscalYearAccessQueryService = fiscalYearAccessQueryService;
    }

    public Task<bool> HasBrandAccessAsync(Guid userId, Guid brandId, CancellationToken cancellationToken) =>
        _userScopeQueryService.HasBrandAccessAsync(userId, brandId, cancellationToken);

    public Task<bool> HasFiscalYearAccessAsync(Guid userId, Guid fiscalYearId, CancellationToken cancellationToken) =>
        _fiscalYearAccessQueryService.IsSelectableAsync(fiscalYearId, cancellationToken);
}
