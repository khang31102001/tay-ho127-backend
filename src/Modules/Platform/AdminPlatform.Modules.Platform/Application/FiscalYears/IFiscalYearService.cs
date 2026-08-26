using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Platform.Application.FiscalYears;

public interface IFiscalYearService
{
    Task<PagedResult<FiscalYearResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken);

    Task<FiscalYearResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<FiscalYearResponse> CreateAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken);

    Task<FiscalYearResponse> UpdateAsync(Guid id, UpdateFiscalYearRequest request, CancellationToken cancellationToken);
}
