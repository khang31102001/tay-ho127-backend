using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Catalog.Application.ModifierGroups;

public interface IModifierGroupService
{
    Task<PagedResult<ModifierGroupResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<ModifierGroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ModifierGroupResponse> CreateAsync(CreateModifierGroupRequest request, CancellationToken cancellationToken);

    Task<ModifierGroupResponse> UpdateAsync(Guid id, UpdateModifierGroupRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
