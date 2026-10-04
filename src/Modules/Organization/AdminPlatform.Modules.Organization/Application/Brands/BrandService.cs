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

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<BrandResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
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

        if (request.Contact is { } contact)
        {
            brand.UpdateContact(new BrandContact(
                contact.Phone, contact.Hotline, contact.Email, contact.AddressLine, contact.Ward, contact.District,
                contact.Province, contact.OpenTime, contact.CloseTime, contact.BusinessHoursNote));
        }

        if (request.IsPrimary is { } isPrimary)
        {
            if (isPrimary)
            {
                // The website presents exactly one branch. The old primary is cleared and saved FIRST: the unique index on the
                // flag would reject two primaries inside one batched save.
                var others = await _db.Brands.Where(b => b.IsPrimary && b.Id != brand.Id).ToListAsync(cancellationToken);
                if (others.Count > 0)
                {
                    foreach (var other in others)
                    {
                        other.SetPrimary(false);
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            brand.SetPrimary(isPrimary);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(brand);
    }

    private async Task<Brand> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Brands.SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), id);

    internal static BrandContactDto ToContact(Brand b) =>
        new(b.Phone, b.Hotline, b.Email, b.AddressLine, b.Ward, b.District, b.Province, b.OpenTime, b.CloseTime, b.BusinessHoursNote);

    private static BrandResponse ToResponse(Brand b) =>
        new(b.Id, b.OrganizationId, b.Code, b.Name, b.IsActive, b.CreatedAtUtc, b.IsPrimary, ToContact(b));
}
