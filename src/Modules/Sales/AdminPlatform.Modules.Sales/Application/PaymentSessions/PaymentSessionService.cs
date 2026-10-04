using System.Security.Cryptography;
using System.Text.Json;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Application.Settings;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.PaymentSessions;

internal sealed class PaymentSessionService : IPaymentSessionService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>No 0/O/1/I: the code is copied by hand into a bank transfer.</summary>
    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly ISalesDbContext _db;
    private readonly OrderPricer _pricer;
    private readonly OrderPlacer _placer;
    private readonly ICustomerDirectory _customers;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PaymentSessionService(
        ISalesDbContext db,
        OrderPricer pricer,
        OrderPlacer placer,
        ICustomerDirectory customers,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _pricer = pricer;
        _placer = placer;
        _customers = customers;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PaymentSessionResponse> CreateAsync(CreateOrderRequest request, Guid? customerId, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        if (key is not null)
        {
            var existing = await _db.PaymentSessions.SingleOrDefaultAsync(s => s.IdempotencyKey == key, cancellationToken);
            if (existing is not null)
            {
                await ExpireAndSaveAsync(existing, cancellationToken);
                return ToResponse(existing);
            }
        }

        var priced = await _pricer.PriceAsync(request, cancellationToken);
        if (priced.PaymentMethod.IsPaidOnDelivery)
        {
            throw new BusinessRuleValidationException("Phương thức này thanh toán khi nhận hàng — hãy đặt hàng trực tiếp, không cần phiên thanh toán.");
        }

        var verifiedCustomerId = customerId is { } id && await _customers.ExistsAsync(id, cancellationToken) ? customerId : null;
        var settings = await OrderSettingsStore.GetOrCreateAsync(_db, cancellationToken);
        var method = priced.PaymentMethod;

        var snapshot = new SessionSnapshot(
            priced.Items.Select(i => new PaymentSessionLineResponse(
                i.ProductId, i.ProductName, i.ProductImageMediaId, i.UnitPrice, i.Quantity, i.LineTotal, i.ItemNote,
                i.Modifiers.Select(m => new OrderModifierResponse(m.GroupId, m.GroupName, m.OptionId, m.OptionLabel, m.PriceAdjustment)).ToList())).ToList(),
            priced.OptionSelections.Select(o => new OrderModifierResponse(o.GroupId, o.GroupName, o.OptionId, o.OptionLabel, o.PriceAdjustment)).ToList());

        var session = PaymentSession.Create(
            new PaymentSessionDraft(
                NewReferenceCode(),
                method.Group == PaymentMethodGroup.BankTransfer ? PaymentChannel.Qr : PaymentChannel.DigitalWallet,
                method.Code,
                method.Name,
                verifiedCustomerId,
                request.CustomerName.Trim(),
                PhoneNumber.Normalize(request.Phone),
                string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                priced.DeliveryMethod.Code,
                priced.DeliveryMethod.Name,
                priced.DeliveryMethod.Type == DeliveryMethodType.Pickup,
                priced.DeliveryAddress,
                request.WantsUtensils,
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                priced.Subtotal,
                priced.DeliveryFee,
                priced.Discount,
                priced.Promotion?.Code,
                priced.Promotion?.PromotionId,
                priced.ShippingDiscount,
                priced.TotalAmount,
                method.BankName,
                method.BankAccountNumber,
                method.BankAccountHolder,
                key,
                JsonSerializer.Serialize(request, Json),
                JsonSerializer.Serialize(snapshot, Json)),
            _clock.UtcNow,
            TimeSpan.FromMinutes(settings.PaymentSessionMinutes));

        _db.PaymentSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    public async Task<PaymentSessionResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await FindOrThrowAsync(id, cancellationToken);
        await ExpireAndSaveAsync(session, cancellationToken);
        return ToResponse(session);
    }

    public async Task<PagedResult<PaymentSessionResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken)
    {
        var query = _db.PaymentSessions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = EnumWire.Parse<PaymentSessionStatus>(status, "session status");
            query = query.Where(s => s.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(s => EF.Functions.ILike(s.ReferenceCode, pattern)
                || EF.Functions.ILike(s.CustomerName, pattern)
                || EF.Functions.ILike(s.Phone, pattern));
        }

        query = request.IsDescending ? query.OrderBy(s => s.CreatedAtUtc) : query.OrderByDescending(s => s.CreatedAtUtc);

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<PaymentSessionResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PaymentSessionResponse> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await FindOrThrowAsync(id, cancellationToken);
        session.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    public async Task<PaymentSessionResponse> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await FindOrThrowAsync(id, cancellationToken);
        var settings = await OrderSettingsStore.GetOrCreateAsync(_db, cancellationToken);
        session.Retry(_clock.UtcNow, TimeSpan.FromMinutes(settings.PaymentSessionMinutes));
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    public async Task<PaymentSessionResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await FindOrThrowAsync(id, cancellationToken);
        if (session.Status == PaymentSessionStatus.Success)
        {
            return ToResponse(session);
        }

        if (await ExpireAndSaveAsync(session, cancellationToken) || session.Status != PaymentSessionStatus.Pending)
        {
            throw new ConflictException($"Phiên thanh toán đang ở trạng thái '{EnumWire.ToWire(session.Status)}', không thể xác nhận.");
        }

        var request = JsonSerializer.Deserialize<CreateOrderRequest>(session.RequestJson, Json)
            ?? throw new InvalidOperationException("The payment session holds no order request.");

        // The customer paid the amount that was quoted. If prices, fees or the code changed since, creating the
        // order automatically would charge a different amount than was paid — staff must handle that by hand.
        var priced = await _pricer.PriceAsync(request, cancellationToken);
        if (priced.TotalAmount != session.TotalAmount)
        {
            throw new ConflictException(
                $"Giá đã thay đổi từ lúc tạo phiên ({session.TotalAmount:N0}đ → {priced.TotalAmount:N0}đ). Hãy từ chối phiên và liên hệ khách hàng.");
        }

        var placed = await _placer.PlaceAsync(
            request,
            priced,
            new PlaceOrderOptions(session.CustomerId, $"session:{session.Id}", AlreadyPaid: true, _currentUser.Email ?? "Quản trị viên", null, session.ReferenceCode),
            cancellationToken);

        session.MarkSucceeded(placed.Order.Id, placed.Order.OrderCode);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    public async Task<PaymentSessionResponse> RejectAsync(Guid id, RejectPaymentSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await FindOrThrowAsync(id, cancellationToken);
        session.MarkFailed(request.Note ?? "Chưa nhận được tiền chuyển khoản.");
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    private async Task<bool> ExpireAndSaveAsync(PaymentSession session, CancellationToken cancellationToken)
    {
        if (!session.ExpireIfDue(_clock.UtcNow))
        {
            return false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<PaymentSession> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.PaymentSessions.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentSession), id);

    private static string NewReferenceCode()
    {
        Span<char> chars = stackalloc char[8];
        for (var index = 0; index < chars.Length; index++)
        {
            chars[index] = ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)];
        }

        return "PAY" + new string(chars);
    }

    private static PaymentSessionResponse ToResponse(PaymentSession s)
    {
        var snapshot = JsonSerializer.Deserialize<SessionSnapshot>(s.LinesJson, Json) ?? new SessionSnapshot([], []);
        return new PaymentSessionResponse(
            s.Id,
            s.ReferenceCode,
            EnumWire.ToWire(s.Status),
            EnumWire.ToWire(s.Channel),
            s.PaymentMethodCode,
            s.PaymentMethodLabel,
            s.CustomerName,
            s.Phone,
            s.Email,
            s.DeliveryMethodCode,
            s.DeliveryMethodLabel,
            s.IsPickup,
            s.DeliveryAddressSnapshot,
            s.WantsUtensils,
            s.CustomerNote,
            snapshot.Items,
            snapshot.Options,
            s.Subtotal,
            s.ShippingFee,
            s.Discount,
            s.DiscountCode,
            s.ShippingDiscount,
            s.TotalAmount,
            s.BankName,
            s.BankAccountNumber,
            s.BankAccountHolder,
            s.OrderId,
            s.OrderCode,
            s.ResolutionNote,
            s.CreatedAtUtc,
            s.UpdatedAtUtc ?? s.CreatedAtUtc,
            s.ExpiresAtUtc);
    }

    private sealed record SessionSnapshot(IReadOnlyList<PaymentSessionLineResponse> Items, IReadOnlyList<OrderModifierResponse> Options);
}
