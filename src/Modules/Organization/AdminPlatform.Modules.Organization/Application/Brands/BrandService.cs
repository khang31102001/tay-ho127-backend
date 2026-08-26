using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Organization.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Organization.Application.Brands;

public sealed class BrandService : IBrandService
{
    private readonly IOrganizationDbContext _db;

    public BrandService(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<BrandResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken)
    {
        var query = _db.Brands.AsNoTracking().AsQueryable();

        if (organizationId is { } orgId)
        {
            query = query.Where(b => b.OrganizationId == orgId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(b => EF.Functions.ILike(b.Code, pattern) || EF.Functions.ILike(b.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name);

        var projected = query.Select(b => new BrandResponse(b.Id, b.OrganizationId, b.Code, b.Name, b.IsActive, b.CreatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<BrandResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(brand);
    }

    public async Task<BrandResponse> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.Brands.AnyAsync(
            b => b.OrganizationId == request.OrganizationId && b.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A brand with code '{request.Code}' already exists in this organization.");
        }

        var brand = Brand.Create(request.OrganizationId, request.Code, request.Name);
        _db.Brands.Add(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(brand);
    }

    public async Task<BrandResponse> UpdateAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await FindOrThrowAsync(id, cancellationToken);
        brand.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(brand);
    }

    private async Task<Brand> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Brands.SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), id);

    private static BrandResponse ToResponse(Brand brand) =>
        new(brand.Id, brand.OrganizationId, brand.Code, brand.Name, brand.IsActive, brand.CreatedAtUtc);
}
