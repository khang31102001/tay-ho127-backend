using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.Payments;

/// <summary>ToStatus: "pending" | "paid" | "failed" | "refunded" | "cancelled" — see the payment's NextStatuses
/// for what is allowed now. Every change appends an audit entry and updates the order's payment status.</summary>
public sealed record TransitionPaymentRequest(string ToStatus, string? Note);

public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    string OrderCode,
    string PaymentMethodCode,
    string PaymentMethodLabel,
    decimal Amount,
    string Status,
    string? TransactionId,
    string? Gateway,
    string? GatewayReference,
    DateTime? PaidAt,
    DateTime? FailedAt,
    IReadOnlyList<string> NextStatuses,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PaymentTransactionResponse(
    Guid Id,
    Guid PaymentId,
    string Action,
    string Result,
    string? Gateway,
    string? GatewayReference,
    string? Message,
    string ChangedBy,
    DateTime CreatedAt);

public interface IPaymentService
{
    /// <summary>Newest first. Search matches the order code or the method label.</summary>
    Task<PagedResult<PaymentResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken);

    Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The audit trail of the payment, oldest first. Append-only: there is no way to edit or delete it.</summary>
    Task<IReadOnlyList<PaymentTransactionResponse>> ListTransactionsAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>400 for a move the state machine does not allow.</summary>
    Task<PaymentResponse> TransitionAsync(Guid id, TransitionPaymentRequest request, CancellationToken cancellationToken);
}

public sealed class TransitionPaymentRequestValidator : AbstractValidator<TransitionPaymentRequest>
{
    public TransitionPaymentRequestValidator()
    {
        RuleFor(x => x.ToStatus).Must(value => EnumWire.TryParse<PaymentStatus>(value, out _))
            .WithMessage("The status is not a valid payment status.");
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public sealed class PaymentService : IPaymentService
{
    private readonly ISalesDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PaymentService(ISalesDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PagedResult<PaymentResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken)
    {
        var query = _db.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = EnumWire.Parse<PaymentStatus>(status, "payment status");
            query = query.Where(p => p.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.OrderCode, pattern) || EF.Functions.ILike(p.PaymentMethodLabel, pattern));
        }

        query = request.IsDescending ? query.OrderBy(p => p.CreatedAtUtc) : query.OrderByDescending(p => p.CreatedAtUtc);

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<PaymentResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await _db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), id));

    public async Task<IReadOnlyList<PaymentTransactionResponse>> ListTransactionsAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking().Include(p => p.Transactions)
            .SingleOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), paymentId);

        return payment.Transactions
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => new PaymentTransactionResponse(
                t.Id, t.PaymentId, EnumWire.ToWire(t.Action), EnumWire.ToWire(t.Result), t.Gateway, t.GatewayReference,
                t.Message, t.ChangedBy, t.CreatedAtUtc))
            .ToList();
    }

    public async Task<PaymentResponse> TransitionAsync(Guid id, TransitionPaymentRequest request, CancellationToken cancellationToken)
    {
        var to = EnumWire.Parse<PaymentStatus>(request.ToStatus, "payment status");
        var payment = await _db.Payments.Include(p => p.Transactions).SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), id);
        var order = await _db.Orders.SingleOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), payment.OrderId);

        payment.Transition(to, _clock.UtcNow, _currentUser.Email ?? "Quản trị viên", request.Note);
        order.SyncPaymentStatus(payment.Status);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    internal static PaymentResponse ToResponse(Payment p) => new(
        p.Id,
        p.OrderId,
        p.OrderCode,
        p.PaymentMethodCode,
        p.PaymentMethodLabel,
        p.Amount,
        EnumWire.ToWire(p.Status),
        p.TransactionId,
        p.Gateway,
        p.GatewayReference,
        p.PaidAtUtc,
        p.FailedAtUtc,
        p.NextStatuses.Select(s => EnumWire.ToWire(s)).ToList(),
        p.CreatedAtUtc,
        p.UpdatedAtUtc ?? p.CreatedAtUtc);
}
