using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>A customer's order. Everything a customer or an admin later reads about it is a SNAPSHOT taken
/// when it was placed (names, prices, labels): editing the catalog or a method afterwards never rewrites
/// history. The customer is only an opaque id (guests have none). Money is computed by the Application layer
/// from trusted data; this entity re-derives the total and refuses inconsistent amounts.</summary>
public sealed class Order : AuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 320;
    public const int MaxAddressLength = 1000;
    public const int MaxNoteLength = 1000;
    public const int MaxItemCount = 100;
    public const int MaxQuantity = 99;

    private readonly List<OrderItem> _items = [];
    private readonly List<OrderOptionSelection> _optionSelections = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    public string OrderCode { get; private set; } = string.Empty;

    /// <summary>Client-generated key that makes placing the same order twice (double click, retry after a
    /// network error) return the first order instead of creating a second.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Opaque id of the signed-in customer; null for a guest. Never a foreign key.</summary>
    public Guid? CustomerId { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string DeliveryAddressSnapshot { get; private set; } = string.Empty;

    public string PaymentMethodCode { get; private set; } = string.Empty;
    public string PaymentMethodLabel { get; private set; } = string.Empty;
    public string DeliveryMethodCode { get; private set; } = string.Empty;
    public string DeliveryMethodLabel { get; private set; } = string.Empty;
    public bool IsPickup { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public string? DiscountCode { get; private set; }

    /// <summary>Id of the promotion that was redeemed for this order (so cancelling can give the use back).</summary>
    public Guid? PromotionId { get; private set; }

    public decimal ShippingDiscount { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal TotalAmount { get; private set; }

    public OrderStatus OrderStatus { get; private set; } = OrderStatus.Pending;

    /// <summary>A mirror of the payment's status, kept in step by <see cref="SyncPaymentStatus"/>, so lists
    /// and badges need no join.</summary>
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;

    public bool WantsUtensils { get; private set; }
    public string? CustomerNote { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;
    public IReadOnlyList<OrderOptionSelection> OptionSelections => _optionSelections;
    public IReadOnlyList<OrderStatusHistory> StatusHistory => _statusHistory;

    private Order()
    {
        // EF Core
    }

    public static Order Create(OrderDraft draft, DateTime nowUtc)
    {
        if (draft.Items.Count == 0)
        {
            throw new BusinessRuleValidationException("An order needs at least one item.");
        }

        if (draft.Items.Count > MaxItemCount)
        {
            throw new BusinessRuleValidationException($"An order can have at most {MaxItemCount} items.");
        }

        if (draft.Discount < 0 || draft.ShippingDiscount < 0 || draft.DeliveryFee < 0)
        {
            throw new BusinessRuleValidationException("Amounts cannot be negative.");
        }

        var subtotal = Money.Round(draft.Items.Sum(i => i.LineTotal) + draft.OptionSelections.Sum(o => o.PriceAdjustment));
        if (draft.Discount > subtotal)
        {
            throw new BusinessRuleValidationException("The discount cannot exceed the order subtotal.");
        }

        if (draft.ShippingDiscount > draft.DeliveryFee)
        {
            throw new BusinessRuleValidationException("The shipping discount cannot exceed the delivery fee.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = Guard.NotNullOrWhiteSpace(draft.OrderCode, nameof(draft.OrderCode)),
            IdempotencyKey = string.IsNullOrWhiteSpace(draft.IdempotencyKey) ? null : draft.IdempotencyKey.Trim(),
            CustomerId = draft.CustomerId,
            CustomerName = Guard.NotNullOrWhiteSpace(draft.CustomerName, nameof(draft.CustomerName)).Trim(),
            Phone = PhoneNumber.Normalize(draft.Phone),
            Email = string.IsNullOrWhiteSpace(draft.Email) ? null : draft.Email.Trim(),
            DeliveryAddressSnapshot = Guard.NotNullOrWhiteSpace(draft.DeliveryAddress, nameof(draft.DeliveryAddress)).Trim(),
            PaymentMethodCode = draft.PaymentMethodCode,
            PaymentMethodLabel = draft.PaymentMethodLabel,
            DeliveryMethodCode = draft.DeliveryMethodCode,
            DeliveryMethodLabel = draft.DeliveryMethodLabel,
            IsPickup = draft.IsPickup,
            Subtotal = subtotal,
            Discount = draft.Discount,
            DiscountCode = draft.DiscountCode,
            PromotionId = draft.PromotionId,
            ShippingDiscount = draft.ShippingDiscount,
            DeliveryFee = draft.DeliveryFee,
            TotalAmount = Money.Round(subtotal - draft.Discount + draft.DeliveryFee - draft.ShippingDiscount),
            WantsUtensils = draft.WantsUtensils,
            CustomerNote = string.IsNullOrWhiteSpace(draft.CustomerNote) ? null : draft.CustomerNote.Trim(),
        };

        order._items.AddRange(draft.Items);
        order._optionSelections.AddRange(draft.OptionSelections);
        order._statusHistory.Add(OrderStatusHistory.Create(null, OrderStatus.Pending, nowUtc, draft.CreatedBy, null, null));
        return order;
    }

    /// <summary>Moves the order along the state machine and records who did it. Idempotent callers must
    /// check the current status first: asking for the status the order already has is an error.</summary>
    public void ChangeStatus(OrderStatus to, DateTime nowUtc, string changedBy, Guid? changedById, string? note)
    {
        if (!OrderStatusRules.CanTransition(OrderStatus, to, IsPickup))
        {
            throw new BusinessRuleValidationException($"An order cannot move from '{EnumWire.ToWire(OrderStatus)}' to '{EnumWire.ToWire(to)}'.");
        }

        _statusHistory.Add(OrderStatusHistory.Create(OrderStatus, to, nowUtc, changedBy, changedById, note));
        OrderStatus = to;
        if (to == OrderStatus.Completed)
        {
            CompletedAtUtc = nowUtc;
        }
        else if (to == OrderStatus.Cancelled)
        {
            CancelledAtUtc = nowUtc;
        }
    }

    public void SyncPaymentStatus(PaymentStatus status) => PaymentStatus = status;

    public IReadOnlyList<OrderStatus> NextStatuses => OrderStatusRules.NextStatuses(OrderStatus, IsPickup);
}

/// <summary>Everything needed to place an order, with the amounts already derived from trusted data.</summary>
public sealed record OrderDraft(
    string OrderCode,
    string? IdempotencyKey,
    Guid? CustomerId,
    string CustomerName,
    string Phone,
    string? Email,
    string DeliveryAddress,
    string PaymentMethodCode,
    string PaymentMethodLabel,
    string DeliveryMethodCode,
    string DeliveryMethodLabel,
    bool IsPickup,
    IReadOnlyList<OrderItem> Items,
    IReadOnlyList<OrderOptionSelection> OptionSelections,
    decimal Discount,
    string? DiscountCode,
    Guid? PromotionId,
    decimal ShippingDiscount,
    decimal DeliveryFee,
    bool WantsUtensils,
    string? CustomerNote,
    string CreatedBy);

/// <summary>A dish line, as it was when ordered. <see cref="UnitPrice"/> is the dish's own price; the
/// modifiers' adjustments are added on top in <see cref="LineTotal"/>.</summary>
public sealed class OrderItem : Entity
{
    private readonly List<OrderItemModifier> _modifiers = [];

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Opaque Media id of the dish's first image at order time (Sales never reads Media).</summary>
    public string? ProductImageMediaId { get; private set; }

    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? ItemNote { get; private set; }
    public IReadOnlyList<OrderItemModifier> Modifiers => _modifiers;

    private OrderItem()
    {
        // EF Core
    }

    public static OrderItem Create(
        Guid productId,
        string productName,
        string? productImageMediaId,
        decimal unitPrice,
        int quantity,
        string? note,
        IReadOnlyList<OrderItemModifier> modifiers)
    {
        if (quantity is < 1 or > Order.MaxQuantity)
        {
            throw new BusinessRuleValidationException($"A quantity must be 1-{Order.MaxQuantity}.");
        }

        if (unitPrice < 0)
        {
            throw new BusinessRuleValidationException("A price cannot be negative.");
        }

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            ProductImageMediaId = productImageMediaId,
            UnitPrice = unitPrice,
            Quantity = quantity,
            ItemNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            LineTotal = Money.Round((unitPrice + modifiers.Sum(m => m.PriceAdjustment)) * quantity),
        };
        item._modifiers.AddRange(modifiers);
        return item;
    }
}

/// <summary>A dish option picked on one line (snapshot of its group/label/price).</summary>
public sealed class OrderItemModifier : Entity
{
    public Guid OrderItemId { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupName { get; private set; } = string.Empty;
    public Guid OptionId { get; private set; }
    public string OptionLabel { get; private set; } = string.Empty;
    public decimal PriceAdjustment { get; private set; }

    private OrderItemModifier()
    {
        // EF Core
    }

    public static OrderItemModifier Create(Guid groupId, string groupName, Guid optionId, string optionLabel, decimal priceAdjustment) =>
        new() { Id = Guid.NewGuid(), GroupId = groupId, GroupName = groupName, OptionId = optionId, OptionLabel = optionLabel, PriceAdjustment = priceAdjustment };
}

/// <summary>A whole-order option picked (snapshot). Its surcharge counts once in the order subtotal.</summary>
public sealed class OrderOptionSelection : Entity
{
    public Guid OrderId { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupName { get; private set; } = string.Empty;
    public Guid OptionId { get; private set; }
    public string OptionLabel { get; private set; } = string.Empty;
    public decimal PriceAdjustment { get; private set; }

    private OrderOptionSelection()
    {
        // EF Core
    }

    public static OrderOptionSelection Create(Guid groupId, string groupName, Guid optionId, string optionLabel, decimal priceAdjustment) =>
        new() { Id = Guid.NewGuid(), GroupId = groupId, GroupName = groupName, OptionId = optionId, OptionLabel = optionLabel, PriceAdjustment = priceAdjustment };
}

/// <summary>One change of an order's status. Append-only.</summary>
public sealed class OrderStatusHistory : Entity
{
    public Guid OrderId { get; private set; }
    public OrderStatus? FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    /// <summary>"Khách hàng", "Hệ thống" or the admin's name/email — display text.</summary>
    public string ChangedBy { get; private set; } = string.Empty;

    public Guid? ChangedById { get; private set; }
    public string? Note { get; private set; }

    private OrderStatusHistory()
    {
        // EF Core
    }

    internal static OrderStatusHistory Create(OrderStatus? from, OrderStatus to, DateTime atUtc, string changedBy, Guid? changedById, string? note) =>
        new()
        {
            Id = Guid.NewGuid(),
            FromStatus = from,
            ToStatus = to,
            ChangedAtUtc = atUtc,
            ChangedBy = changedBy,
            ChangedById = changedById,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
}
