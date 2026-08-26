using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Platform.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Platform.Application.FiscalYears;

public sealed class FiscalYearService : IFiscalYearService, IFiscalYearAccessQueryService
{
    private readonly IPlatformDbContext _db;

    public FiscalYearService(IPlatformDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<FiscalYearResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken)
    {
        var query = _db.FiscalYears.AsNoTracking().AsQueryable();

        if (organizationId is { } orgId)
        {
            query = query.Where(f => f.OrganizationId == orgId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(f => EF.Functions.ILike(f.Code, pattern) || EF.Functions.ILike(f.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(f => f.StartDate) : query.OrderBy(f => f.StartDate);

        var projected = query.Select(f => new FiscalYearResponse(f.Id, f.OrganizationId, f.Code, f.Name, f.IsActive, f.StartDate, f.EndDate));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<FiscalYearResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var fiscalYear = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(fiscalYear);
    }

    public async Task<FiscalYearResponse> CreateAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.FiscalYears.AnyAsync(
            f => f.OrganizationId == request.OrganizationId && f.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A fiscal year with code '{request.Code}' already exists in this organization.");
        }

        var fiscalYear = FiscalYear.Create(request.OrganizationId, request.Code, request.Name, request.StartDate, request.EndDate);
        _db.FiscalYears.Add(fiscalYear);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(fiscalYear);
    }

    public async Task<FiscalYearResponse> UpdateAsync(Guid id, UpdateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        var fiscalYear = await FindOrThrowAsync(id, cancellationToken);
        fiscalYear.Update(request.Name, request.IsActive, request.StartDate, request.EndDate);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(fiscalYear);
    }

    public async Task<bool> IsSelectableAsync(Guid fiscalYearId, CancellationToken cancellationToken)
    {
        return await _db.FiscalYears.AnyAsync(f => f.Id == fiscalYearId && f.IsActive, cancellationToken);
    }

    private async Task<FiscalYear> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.FiscalYears.SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalYear), id);

    private static FiscalYearResponse ToResponse(FiscalYear fiscalYear) => new(
        fiscalYear.Id, fiscalYear.OrganizationId, fiscalYear.Code, fiscalYear.Name, fiscalYear.IsActive, fiscalYear.StartDate, fiscalYear.EndDate);
}
