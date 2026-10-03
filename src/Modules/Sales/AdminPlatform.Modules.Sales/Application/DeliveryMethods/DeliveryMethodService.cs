using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.DeliveryMethods;

public sealed class DeliveryMethodService : IDeliveryMethodService
{
    private readonly ISalesDbContext _db;

    public DeliveryMethodService(ISalesDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<DeliveryMethodResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.DeliveryMethods.AsNoTracking().AsQueryable();

        if (isActive is { } active)
        {
            query = query.Where(m => m.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(m => EF.Functions.ILike(m.Code, pattern) || EF.Functions.ILike(m.Name, pattern));
        }

        query = request.IsDescending
            ? query.OrderByDescending(m => m.DisplayOrder).ThenByDescending(m => m.Name)
            : query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<DeliveryMethodResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<DeliveryMethodResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await FindOrThrowAsync(id, cancellationToken));

    public async Task<DeliveryMethodResponse> CreateAsync(CreateDeliveryMethodRequest request, CancellationToken cancellationToken)
    {
        var method = DeliveryMethod.Create(request.Code, ToDetails(request.Name, request.Description, request.Type, request.BaseFee,
            request.FreeShippingThreshold, request.EstimatedMinMinutes, request.EstimatedMaxMinutes, request.PickupAddress,
            request.DisplayOrder, request.IsActive, request.IsDefault));

        if (await _db.DeliveryMethods.AnyAsync(m => m.Code == method.Code, cancellationToken))
        {
            throw new ConflictException($"A delivery method with the code '{method.Code}' already exists.");
        }

        _db.DeliveryMethods.Add(method);
        await EnforceSingleDefaultAsync(method, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(method);
    }

    public async Task<DeliveryMethodResponse> UpdateAsync(Guid id, UpdateDeliveryMethodRequest request, CancellationToken cancellationToken)
    {
        var method = await FindOrThrowAsync(id, cancellationToken);
        method.Update(ToDetails(request.Name, request.Description, request.Type, request.BaseFee, request.FreeShippingThreshold,
            request.EstimatedMinMinutes, request.EstimatedMaxMinutes, request.PickupAddress, request.DisplayOrder, request.IsActive, request.IsDefault));

        await EnforceSingleDefaultAsync(method, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(method);
    }

    /// <summary>Hard delete. Orders keep a text snapshot of the method, so they are unaffected.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var method = await FindOrThrowAsync(id, cancellationToken);
        _db.DeliveryMethods.Remove(method);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicDeliveryMethodResponse>> ListPublicAsync(CancellationToken cancellationToken)
    {
        var methods = await _db.DeliveryMethods.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return methods.Select(m => new PublicDeliveryMethodResponse(
            m.Id, m.Code, m.Name, m.Description, EnumWire.ToWire(m.Type), m.BaseFee, m.FreeShippingThreshold,
            m.EstimatedMinMinutes, m.EstimatedMaxMinutes, m.PickupAddress, m.DisplayOrder, m.IsDefault)).ToList();
    }

    /// <summary>Only one method can be the default: when this one is, the flag is cleared on the others.</summary>
    private async Task EnforceSingleDefaultAsync(DeliveryMethod saved, CancellationToken cancellationToken)
    {
        if (!saved.IsDefault)
        {
            return;
        }

        var others = await _db.DeliveryMethods.Where(m => m.IsDefault && m.Id != saved.Id).ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.ClearDefault();
        }
    }

    private static DeliveryMethodDetails ToDetails(
        string name, string? description, string type, decimal baseFee, decimal? freeShippingThreshold,
        int? estimatedMin, int? estimatedMax, string? pickupAddress, int displayOrder, bool isActive, bool isDefault) =>
        new(name, description, EnumWire.Parse<DeliveryMethodType>(type, "delivery type"), baseFee, freeShippingThreshold,
            estimatedMin, estimatedMax, pickupAddress, displayOrder, isActive, isDefault);

    private async Task<DeliveryMethod> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.DeliveryMethods.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryMethod), id);

    internal static DeliveryMethodResponse ToResponse(DeliveryMethod m) => new(
        m.Id, m.Code, m.Name, m.Description, EnumWire.ToWire(m.Type), m.BaseFee, m.FreeShippingThreshold,
        m.EstimatedMinMinutes, m.EstimatedMaxMinutes, m.PickupAddress, m.DisplayOrder, m.IsActive, m.IsDefault,
        m.CreatedAtUtc, m.UpdatedAtUtc ?? m.CreatedAtUtc);
}
