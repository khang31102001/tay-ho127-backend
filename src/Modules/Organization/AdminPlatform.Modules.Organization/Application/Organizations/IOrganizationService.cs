using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Organization.Application.Organizations;

public interface IOrganizationService
{
    Task<PagedResult<OrganizationResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<OrganizationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<OrganizationResponse> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken);

    Task<OrganizationResponse> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken);
}
