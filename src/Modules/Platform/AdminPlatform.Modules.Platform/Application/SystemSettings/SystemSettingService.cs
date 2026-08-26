using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Platform.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Platform.Application.SystemSettings;

public sealed class SystemSettingService : ISystemSettingService
{
    private readonly IPlatformDbContext _db;

    public SystemSettingService(IPlatformDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SystemSettingResponse>> ListAsync(PagedRequest request, Guid? organizationId, CancellationToken cancellationToken)
    {
        var query = _db.SystemSettings.AsNoTracking().AsQueryable();

        if (organizationId is { } orgId)
        {
            query = query.Where(s => s.OrganizationId == orgId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(s => EF.Functions.ILike(s.Code, pattern) || EF.Functions.ILike(s.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(s => s.Code) : query.OrderBy(s => s.Code);

        var projected = query.Select(s => new SystemSettingResponse(s.Id, s.Code, s.Name, s.Value, s.IsActive, s.OrganizationId));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<SystemSettingResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var setting = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(setting);
    }

    public async Task<SystemSettingResponse> CreateAsync(CreateSystemSettingRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.SystemSettings.AnyAsync(
            s => s.Code == request.Code && s.OrganizationId == request.OrganizationId, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A system setting with code '{request.Code}' already exists in this scope.");
        }

        var setting = SystemSetting.Create(request.Code, request.Name, request.Value, request.OrganizationId);
        _db.SystemSettings.Add(setting);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(setting);
    }

    public async Task<SystemSettingResponse> UpdateAsync(Guid id, UpdateSystemSettingRequest request, CancellationToken cancellationToken)
    {
        var setting = await FindOrThrowAsync(id, cancellationToken);
        setting.Update(request.Name, request.Value, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(setting);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var setting = await FindOrThrowAsync(id, cancellationToken);
        _db.SystemSettings.Remove(setting);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SystemSetting> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.SystemSettings.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(SystemSetting), id);

    private static SystemSettingResponse ToResponse(SystemSetting setting) =>
        new(setting.Id, setting.Code, setting.Name, setting.Value, setting.IsActive, setting.OrganizationId);
}
