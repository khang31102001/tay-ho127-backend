using AdminPlatform.Modules.Organization.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Organization.Application.UserScopes;

public sealed class UserScopeService : IUserScopeService, IUserScopeQueryService
{
    private readonly IOrganizationDbContext _db;

    public UserScopeService(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDepartmentResponse>> ListDepartmentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.UserDepartments
            .Where(ud => ud.UserId == userId)
            .Join(_db.Departments, ud => ud.DepartmentId, d => d.Id, (ud, d) => new UserDepartmentResponse(d.Id, d.Code, d.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignDepartmentAsync(Guid userId, AssignDepartmentRequest request, CancellationToken cancellationToken)
    {
        var departmentExists = await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            throw new NotFoundException(nameof(Department), request.DepartmentId);
        }

        var alreadyAssigned = await _db.UserDepartments.AnyAsync(
            ud => ud.UserId == userId && ud.DepartmentId == request.DepartmentId, cancellationToken);
        if (alreadyAssigned)
        {
            return;
        }

        _db.UserDepartments.Add(UserDepartment.Create(userId, request.DepartmentId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDepartmentAsync(Guid userId, Guid departmentId, CancellationToken cancellationToken)
    {
        var assignment = await _db.UserDepartments.SingleOrDefaultAsync(
            ud => ud.UserId == userId && ud.DepartmentId == departmentId, cancellationToken);
        if (assignment is null)
        {
            return;
        }

        _db.UserDepartments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserBrandResponse>> ListBrandsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.UserBrands
            .Where(ub => ub.UserId == userId)
            .Join(_db.Brands, ub => ub.BrandId, b => b.Id, (ub, b) => new UserBrandResponse(b.Id, b.Code, b.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignBrandAsync(Guid userId, AssignBrandRequest request, CancellationToken cancellationToken)
    {
        var brandExists = await _db.Brands.AnyAsync(b => b.Id == request.BrandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException(nameof(Brand), request.BrandId);
        }

        var alreadyAssigned = await _db.UserBrands.AnyAsync(
            ub => ub.UserId == userId && ub.BrandId == request.BrandId, cancellationToken);
        if (alreadyAssigned)
        {
            return;
        }

        _db.UserBrands.Add(UserBrand.Create(userId, request.BrandId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveBrandAsync(Guid userId, Guid brandId, CancellationToken cancellationToken)
    {
        var assignment = await _db.UserBrands.SingleOrDefaultAsync(
            ub => ub.UserId == userId && ub.BrandId == brandId, cancellationToken);
        if (assignment is null)
        {
            return;
        }

        _db.UserBrands.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasBrandAccessAsync(Guid userId, Guid brandId, CancellationToken cancellationToken)
    {
        return await _db.UserBrands.AnyAsync(ub => ub.UserId == userId && ub.BrandId == brandId, cancellationToken);
    }
}
