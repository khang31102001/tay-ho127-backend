using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.ModifierGroups;

public sealed class ModifierGroupService : IModifierGroupService
{
    private readonly ICatalogDbContext _db;

    public ModifierGroupService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ModifierGroupResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.ModifierGroups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(g => EF.Functions.ILike(g.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name);

        var pagedGroups = await query.Include(g => g.Options).AsSplitQuery().ToPagedResultAsync(request, cancellationToken);
        var items = pagedGroups.Items.Select(ToResponse).ToList();
        return new PagedResult<ModifierGroupResponse>(items, pagedGroups.Page, pagedGroups.PageSize, pagedGroups.TotalItems);
    }

    public async Task<ModifierGroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<ModifierGroupResponse> CreateAsync(CreateModifierGroupRequest request, CancellationToken cancellationToken)
    {
        var group = ModifierGroup.Create(request.Name, ParseSelectionType(request.SelectionType), request.IsRequired, ToInputs(request.Options));
        _db.ModifierGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(group);
    }

    public async Task<ModifierGroupResponse> UpdateAsync(Guid id, UpdateModifierGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await FindOrThrowAsync(id, cancellationToken);
        group.Update(request.Name, ParseSelectionType(request.SelectionType), request.IsRequired, ToInputs(request.Options));
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(group);
    }

    /// <summary>Hard delete; its options and its links to products are removed with it (cascade).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await FindOrThrowAsync(id, cancellationToken);
        _db.ModifierGroups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<ModifierOptionInput> ToInputs(IReadOnlyList<ModifierOptionRequest> options) =>
        options.Select(o => new ModifierOptionInput(o.Id, o.Label, o.PriceAdjustment, o.IsDefault)).ToList();

    private static ModifierSelectionType ParseSelectionType(string selectionType) =>
        Enum.TryParse<ModifierSelectionType>(selectionType, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{selectionType}' is not a valid selection type.");

    private async Task<ModifierGroup> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.ModifierGroups.Include(g => g.Options).SingleOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ModifierGroup), id);

    internal static ModifierGroupResponse ToResponse(ModifierGroup group) => new(
        group.Id,
        group.Name,
        group.SelectionType.ToString().ToLowerInvariant(),
        group.IsRequired,
        group.Options
            .OrderBy(o => o.SortOrder)
            .Select(o => new ModifierOptionResponse(o.Id, o.Label, o.PriceAdjustment, o.IsDefault))
            .ToList(),
        group.CreatedAtUtc,
        group.UpdatedAtUtc);
}
