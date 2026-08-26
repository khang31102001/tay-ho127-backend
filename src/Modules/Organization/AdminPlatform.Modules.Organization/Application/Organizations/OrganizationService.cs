using AdminPlatform.Common.Pagination;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using OrganizationEntity = AdminPlatform.Modules.Organization.Domain.Organization;

namespace AdminPlatform.Modules.Organization.Application.Organizations;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationDbContext _db;

    public OrganizationService(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<OrganizationResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(o => EF.Functions.ILike(o.Code, pattern) || EF.Functions.ILike(o.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name);

        var projected = query.Select(o => new OrganizationResponse(o.Id, o.Code, o.Name, o.IsActive, o.CreatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<OrganizationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var organization = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(organization);
    }

    public async Task<OrganizationResponse> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.Organizations.AnyAsync(o => o.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"An organization with code '{request.Code}' already exists.");
        }

        var organization = OrganizationEntity.Create(request.Code, request.Name);
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(organization);
    }

    public async Task<OrganizationResponse> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await FindOrThrowAsync(id, cancellationToken);
        organization.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(organization);
    }

    private async Task<OrganizationEntity> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Organizations.SingleOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Organization), id);

    private static OrganizationResponse ToResponse(OrganizationEntity organization) =>
        new(organization.Id, organization.Code, organization.Name, organization.IsActive, organization.CreatedAtUtc);
}
