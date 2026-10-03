using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.PaymentMethods;

public sealed class PaymentMethodService : IPaymentMethodService
{
    private readonly ISalesDbContext _db;

    public PaymentMethodService(ISalesDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PaymentMethodResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.PaymentMethods.AsNoTracking().AsQueryable();

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
        return new PagedResult<PaymentMethodResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PaymentMethodResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await FindOrThrowAsync(id, cancellationToken));

    public async Task<PaymentMethodResponse> CreateAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var method = PaymentMethod.Create(request.Code, ToDetails(
            request.Name, request.Description, request.IconMediaId, request.Group, request.Gateway, request.Instructions,
            request.BankName, request.BankAccountNumber, request.BankAccountHolder, request.BankBranch, request.DisplayOrder,
            request.IsActive, request.IsDefault, request.MinOrderAmount, request.MaxOrderAmount));

        if (await _db.PaymentMethods.AnyAsync(m => m.Code == method.Code, cancellationToken))
        {
            throw new ConflictException($"A payment method with the code '{method.Code}' already exists.");
        }

        _db.PaymentMethods.Add(method);
        await EnforceSingleDefaultAsync(method, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(method);
    }

    public async Task<PaymentMethodResponse> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var method = await FindOrThrowAsync(id, cancellationToken);
        method.Update(ToDetails(
            request.Name, request.Description, request.IconMediaId, request.Group, request.Gateway, request.Instructions,
            request.BankName, request.BankAccountNumber, request.BankAccountHolder, request.BankBranch, request.DisplayOrder,
            request.IsActive, request.IsDefault, request.MinOrderAmount, request.MaxOrderAmount));

        await EnforceSingleDefaultAsync(method, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(method);
    }

    /// <summary>Hard delete. Orders and payments keep a text snapshot of the method, so they are unaffected.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var method = await FindOrThrowAsync(id, cancellationToken);
        _db.PaymentMethods.Remove(method);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicPaymentMethodResponse>> ListPublicAsync(CancellationToken cancellationToken)
    {
        var methods = await _db.PaymentMethods.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return methods.Select(m => new PublicPaymentMethodResponse(
            m.Id, m.Code, m.Name, m.Description, m.IconMediaId, EnumWire.ToWire(m.Group), m.Instructions,
            m.DisplayOrder, m.IsDefault, m.MinOrderAmount, m.MaxOrderAmount)).ToList();
    }

    private async Task EnforceSingleDefaultAsync(PaymentMethod saved, CancellationToken cancellationToken)
    {
        if (!saved.IsDefault)
        {
            return;
        }

        var others = await _db.PaymentMethods.Where(m => m.IsDefault && m.Id != saved.Id).ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.ClearDefault();
        }
    }

    private static PaymentMethodDetails ToDetails(
        string name, string? description, string? iconMediaId, string group, string? gateway, string? instructions,
        string? bankName, string? bankAccountNumber, string? bankAccountHolder, string? bankBranch, int displayOrder,
        bool isActive, bool isDefault, decimal? minOrderAmount, decimal? maxOrderAmount) =>
        new(name, description, iconMediaId, EnumWire.Parse<PaymentMethodGroup>(group, "payment method group"), gateway,
            instructions, bankName, bankAccountNumber, bankAccountHolder, bankBranch, displayOrder, isActive, isDefault,
            minOrderAmount, maxOrderAmount);

    private async Task<PaymentMethod> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.PaymentMethods.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentMethod), id);

    internal static PaymentMethodResponse ToResponse(PaymentMethod m) => new(
        m.Id, m.Code, m.Name, m.Description, m.IconMediaId, EnumWire.ToWire(m.Group), m.Gateway, m.Instructions,
        m.BankName, m.BankAccountNumber, m.BankAccountHolder, m.BankBranch, m.DisplayOrder, m.IsActive, m.IsDefault,
        m.MinOrderAmount, m.MaxOrderAmount, m.CreatedAtUtc, m.UpdatedAtUtc ?? m.CreatedAtUtc);
}
