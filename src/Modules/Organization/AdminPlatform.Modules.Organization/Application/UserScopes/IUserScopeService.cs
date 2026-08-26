namespace AdminPlatform.Modules.Organization.Application.UserScopes;

public interface IUserScopeService
{
    Task<IReadOnlyList<UserDepartmentResponse>> ListDepartmentsAsync(Guid userId, CancellationToken cancellationToken);

    Task AssignDepartmentAsync(Guid userId, AssignDepartmentRequest request, CancellationToken cancellationToken);

    Task RemoveDepartmentAsync(Guid userId, Guid departmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserBrandResponse>> ListBrandsAsync(Guid userId, CancellationToken cancellationToken);

    Task AssignBrandAsync(Guid userId, AssignBrandRequest request, CancellationToken cancellationToken);

    Task RemoveBrandAsync(Guid userId, Guid brandId, CancellationToken cancellationToken);
}
