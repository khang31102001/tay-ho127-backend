using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Platform.Application.SystemSettings;

public interface ISystemSettingService
{
    Task<PagedResult<SystemSettingResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken);

    Task<SystemSettingResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SystemSettingResponse> CreateAsync(CreateSystemSettingRequest request, CancellationToken cancellationToken);

    Task<SystemSettingResponse> UpdateAsync(Guid id, UpdateSystemSettingRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
