using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Sales.Application;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.Payments;
using AdminPlatform.Modules.Sales.Application.PaymentSessions;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.Modules.Sales.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdminPlatform.UnitTests.Sales;

public class SalesServiceTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    // ---------- fakes ----------

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 31, 3, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public string? Email => "admin@x.vn";
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions { get; set; } = [SalesPermissionCodes.OrdersCancel];
        public Guid? CurrentBrandId => null;
        public Guid? CurrentFiscalYearId => null;
    }

    private sealed class FakeCatalog : ICatalogPricingProvider
    {
        public Dictionary<Guid, PricedProduct> Products { get; } = [];

        public Task<IReadOnlyDictionary<Guid, PricedProduct>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, PricedProduct>>(
                Products.Where(p => productIds.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value));
    }

    private sealed class FakePromotions : IPromotionPricing
    {
        public Guid PromotionId { get; } = Guid.NewGuid();
        public int UsesLeft { get; set; } = 1;
        public int Released { get; private set; }
        public decimal Discount { get; set; } = 10_000;

        public Task<PromotionPricingResult> EvaluateAsync(string code, decimal subtotal, decimal shippingFee, IReadOnlyList<PromotionLine> lines, CancellationToken cancellationToken) =>
            Task.FromResult(code == "SALE10"
                ? new PromotionPricingResult(true, PromotionId, "SALE10", Discount, 0, null)
                : new PromotionPricingResult(false, null, null, 0, 0, "Mã giảm giá không tồn tại."));

        public Task<bool> TryRedeemAsync(Guid promotionId, CancellationToken cancellationToken)
        {
            if (UsesLeft <= 0)
            {
                return Task.FromResult(false);
            }

            UsesLeft--;
            return Task.FromResult(true);
        }

        public Task ReleaseAsync(Guid promotionId, CancellationToken cancellationToken)
        {
            UsesLeft++;
            Released++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCustomers : ICustomerDirectory
    {
        public HashSet<Guid> Known { get; } = [];

        public Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken) => Task.FromResult(Known.Contains(customerId));
    }

    private sealed class FakeCodes : IOrderCodeGenerator
    {
        private int _next;

        public Task<string> NextAsync(OrderSettings settings, CancellationToken cancellationToken) =>
            Task.FromResult($"{settings.OrderCodePrefix}-{++_next:00000}");
    }

    private sealed class FakeNotifier : IOrderNotifier
    {
        public int Placed { get; private set; }
        public List<string> Changes { get; } = [];

        public Task OrderPlacedAsync(OrderNotification notification, CancellationToken cancellationToken)
        {
            Placed++;
            return Task.CompletedTask;
        }

        public Task OrderStatusChangedAsync(OrderNotification notification, string fromStatus, string toStatus, CancellationToken cancellationToken)
        {
            Changes.Add($"{fromStatus}>{toStatus}");
            return Task.CompletedTask;
        }
    }

    private sealed class Harness
    {
        public SalesDbContext Db { get; } = new(new DbContextOptionsBuilder<SalesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public FakeClock Clock { get; } = new();
        public FakeUser User { get; } = new();
        public FakeCatalog Catalog { get; } = new();
        public FakePromotions Promotions { get; } = new();
        public FakeCustomers Customers { get; } = new();
        public FakeNotifier Notifier { get; } = new();
        public Guid DishId { get; } = Guid.NewGuid();
        public Guid SizeGroupId { get; } = Guid.NewGuid();
        public Guid LargeOptionId { get; } = Guid.NewGuid();
        public OrderService Orders { get; }
        public PaymentSessionService Sessions { get; }
        public PaymentService Payments { get; }

        public Harness()
        {
            Catalog.Products[DishId] = new PricedProduct(
                DishId, "Bánh cuốn nhân thịt", 56_000, true, "media-1",
                [new PricedModifierGroup(SizeGroupId, "Size", false, [new PricedModifierOption(LargeOptionId, "Lớn", 5_000)])]);

            Db.DeliveryMethods.Add(DeliveryMethod.Create("within_5km", new("Giao gần", null, DeliveryMethodType.Delivery, 10_000, 200_000, 20, 35, null, 1, true, true)));
            Db.DeliveryMethods.Add(DeliveryMethod.Create("pickup", new("Tự lấy", null, DeliveryMethodType.Pickup, 0, null, 10, 15, "127 Đinh Tiên Hoàng", 2, true, false)));
            Db.PaymentMethods.Add(PaymentMethod.Create("cod", new("Tiền mặt", null, null, PaymentMethodGroup.Cod, null, null, null, null, null, null, 1, true, true, null, null)));
            Db.PaymentMethods.Add(PaymentMethod.Create("bank_transfer", new("Chuyển khoản", null, null, PaymentMethodGroup.BankTransfer, null, null, "VCB", "0123", "CTY", null, 2, true, false, null, null)));
            Db.PaymentMethods.Add(PaymentMethod.Create("off", new("Tắt", null, null, PaymentMethodGroup.Cod, null, null, null, null, null, null, 3, false, false, null, null)));
            Db.SaveChanges();

            var pricer = new OrderPricer(Db, Catalog, Promotions);
            var placer = new OrderPlacer(Db, Promotions, new FakeCodes(), Notifier, Clock, NullLogger<OrderPlacer>.Instance);
            Orders = new OrderService(Db, pricer, placer, Customers, Promotions, Notifier, User, Clock, NullLogger<OrderService>.Instance);
            Sessions = new PaymentSessionService(Db, pricer, placer, Customers, User, Clock);
            Payments = new PaymentService(Db, User, Clock);
        }

        public CreateOrderRequest Request(
            int quantity = 2,
            string payment = "cod",
            string delivery = "within_5km",
            string? discountCode = null,
            string? key = "key-1",
            IReadOnlyList<SelectedOptionRequest>? modifiers = null) =>
            new("Nguyễn Văn A", "0901 234 567", null, "12 Đinh Tiên Hoàng", delivery, payment,
                [new OrderItemRequest(DishId, quantity, null, modifiers)], true, null, null, discountCode, key);
    }

    // ---------- placing an order ----------

    [Fact]
    public async Task Order_is_priced_from_the_catalog_and_the_delivery_method()
    {
        var h = new Harness();

        var order = await h.Orders.CreateAsync(h.Request(), null, None);

        Assert.Equal(112_000, order.Subtotal);
        Assert.Equal(10_000, order.DeliveryFee); // below the 200.000 free-shipping threshold
        Assert.Equal(122_000, order.TotalAmount);
        Assert.Equal("0901234567", order.Phone);
        Assert.Equal("pending", order.OrderStatus);
        Assert.Equal("pending", order.PaymentStatus);
        Assert.Equal("TH127-00001", order.OrderCode);
        Assert.NotNull(order.PaymentId);
        Assert.Equal(1, h.Notifier.Placed);
        Assert.Equal(1, await h.Db.Payments.CountAsync());
        Assert.Equal(1, h.Promotions.UsesLeft);
    }

    [Fact]
    public async Task Modifiers_surcharge_counts_per_quantity_and_free_shipping_applies_over_the_threshold()
    {
        var h = new Harness();

        var order = await h.Orders.CreateAsync(h.Request(quantity: 4, modifiers: [new SelectedOptionRequest(h.SizeGroupId, h.LargeOptionId)]), null, None);

        Assert.Equal(244_000, order.Subtotal); // (56.000 + 5.000) x 4
        Assert.Equal(0, order.DeliveryFee);
        Assert.Equal(244_000, order.TotalAmount);
        var item = Assert.Single(order.Items);
        Assert.Equal(56_000, item.UnitPrice);
        Assert.Equal("Lớn", Assert.Single(item.Modifiers).OptionLabel);
    }

    [Fact]
    public async Task A_pickup_order_uses_the_methods_address_and_is_flagged_pickup()
    {
        var h = new Harness();

        var order = await h.Orders.CreateAsync(h.Request(delivery: "pickup") with { DeliveryAddress = null }, null, None);

        Assert.True(order.IsPickup);
        Assert.Equal("127 Đinh Tiên Hoàng", order.DeliveryAddressSnapshot);
        Assert.Equal(0, order.DeliveryFee);
    }

    [Fact]
    public async Task A_delivery_order_needs_an_address()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => h.Orders.CreateAsync(h.Request() with { DeliveryAddress = "  " }, null, None));
    }

    [Fact]
    public async Task Unsellable_products_unknown_modifiers_and_disabled_methods_are_refused()
    {
        var h = new Harness();

        h.Catalog.Products[h.DishId] = h.Catalog.Products[h.DishId] with { IsActive = false };
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.CreateAsync(h.Request(), null, None));
        h.Catalog.Products[h.DishId] = h.Catalog.Products[h.DishId] with { IsActive = true };

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => h.Orders.CreateAsync(h.Request(modifiers: [new SelectedOptionRequest(h.SizeGroupId, Guid.NewGuid())]), null, None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.CreateAsync(h.Request(payment: "off"), null, None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.CreateAsync(h.Request(delivery: "nope"), null, None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.CreateAsync(h.Request(quantity: 0), null, None));
        Assert.Empty(h.Db.Orders);
    }

    [Fact]
    public async Task A_method_paid_up_front_cannot_be_ordered_directly()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.CreateAsync(h.Request(payment: "bank_transfer"), null, None));
    }

    [Fact]
    public async Task The_same_idempotency_key_returns_the_first_order()
    {
        var h = new Harness();

        var first = await h.Orders.CreateAsync(h.Request(), null, None);
        var second = await h.Orders.CreateAsync(h.Request(), null, None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await h.Db.Orders.CountAsync());
        Assert.Equal(1, h.Notifier.Placed);
    }

    [Fact]
    public async Task Discount_comes_from_the_promotion_rules_and_takes_one_use()
    {
        var h = new Harness();

        var order = await h.Orders.CreateAsync(h.Request(discountCode: "SALE10"), null, None);

        Assert.Equal(10_000, order.Discount);
        Assert.Equal("SALE10", order.DiscountCode);
        Assert.Equal(h.Promotions.PromotionId, order.PromotionId);
        Assert.Equal(112_000 - 10_000 + 10_000, order.TotalAmount);
        Assert.Equal(0, h.Promotions.UsesLeft);
    }

    [Fact]
    public async Task A_refused_code_is_a_400_and_an_exhausted_code_is_a_409()
    {
        var h = new Harness();

        var unknown = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.CreateAsync(h.Request(discountCode: "NOPE"), null, None));
        Assert.Contains("không tồn tại", unknown.Message);

        h.Promotions.UsesLeft = 0;
        await Assert.ThrowsAsync<ConflictException>(() => h.Orders.CreateAsync(h.Request(discountCode: "SALE10"), null, None));
        Assert.Empty(h.Db.Orders);
    }

    [Fact]
    public async Task A_signed_in_customer_is_linked_only_when_the_account_exists()
    {
        var h = new Harness();
        var known = Guid.NewGuid();
        h.Customers.Known.Add(known);

        var linked = await h.Orders.CreateAsync(h.Request(key: "k1"), known, None);
        var guest = await h.Orders.CreateAsync(h.Request(key: "k2"), Guid.NewGuid(), None);

        Assert.Equal(known, linked.CustomerId);
        Assert.Null(guest.CustomerId);
    }

    // ---------- looking an order up ----------

    [Fact]
    public async Task A_guest_needs_both_the_code_and_the_phone_to_see_an_order()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(), null, None);

        var found = await h.Orders.LookupAsync(new OrderLookupRequest(order.OrderCode, "+84901234567"), None);
        Assert.Equal(order.Id, found.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => h.Orders.LookupAsync(new OrderLookupRequest(order.OrderCode, "0909999999"), None));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Orders.LookupAsync(new OrderLookupRequest(order.OrderCode, "abc"), None));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Orders.LookupAsync(new OrderLookupRequest("NOPE", "0901234567"), None));
    }

    [Fact]
    public async Task A_customer_only_sees_their_own_orders()
    {
        var h = new Harness();
        var mine = Guid.NewGuid();
        h.Customers.Known.Add(mine);
        var order = await h.Orders.CreateAsync(h.Request(key: "k1"), mine, None);
        await h.Orders.CreateAsync(h.Request(key: "k2"), null, None);

        var page = await h.Orders.ListForCustomerAsync(mine, new(), None);
        Assert.Equal(order.Id, Assert.Single(page.Items).Id);

        Assert.Equal(order.Id, (await h.Orders.GetForCustomerAsync(mine, order.OrderCode, None)).Id);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Orders.GetForCustomerAsync(Guid.NewGuid(), order.OrderCode, None));
    }

    // ---------- status changes ----------

    [Fact]
    public async Task Status_follows_the_state_machine_and_records_who_changed_it()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(), null, None);

        var confirmed = await h.Orders.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest("confirmed", "ok"), None);

        Assert.Equal("confirmed", confirmed.OrderStatus);
        Assert.Equal(["preparing", "cancelled"], confirmed.NextStatuses);
        Assert.Equal("admin@x.vn", confirmed.StatusHistory.Last().ChangedBy);
        Assert.Contains("pending>confirmed", h.Notifier.Changes);
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => h.Orders.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest("completed", null), None));
    }

    [Fact]
    public async Task Cancelling_needs_its_own_permission()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(), null, None);
        h.User.Permissions = ["orders.update-status"];

        await Assert.ThrowsAsync<ForbiddenException>(
            () => h.Orders.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest("cancelled", null), None));
    }

    [Fact]
    public async Task Cancelling_cancels_an_unpaid_payment_and_gives_the_code_use_back()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(discountCode: "SALE10"), null, None);

        var cancelled = await h.Orders.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest("cancelled", null), None);

        Assert.Equal("cancelled", cancelled.OrderStatus);
        Assert.Equal("cancelled", cancelled.PaymentStatus);
        Assert.Equal(1, h.Promotions.UsesLeft);
        Assert.Equal(1, h.Promotions.Released);
        Assert.NotNull(cancelled.CancelledAt);
    }

    [Fact]
    public async Task Cancelling_a_paid_order_leaves_the_payment_for_a_manual_refund()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(), null, None);
        await h.Payments.TransitionAsync(order.PaymentId!.Value, new TransitionPaymentRequest("paid", null), None);

        var cancelled = await h.Orders.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest("cancelled", null), None);

        Assert.Equal("paid", cancelled.PaymentStatus);
        Assert.Contains("hoàn tiền", cancelled.StatusHistory.Last().Note);
    }

    // ---------- payments ----------

    [Fact]
    public async Task A_payment_transition_updates_the_order_and_appends_an_audit_entry()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(), null, None);

        var paid = await h.Payments.TransitionAsync(order.PaymentId!.Value, new TransitionPaymentRequest("paid", "đã nhận"), None);

        Assert.Equal("paid", paid.Status);
        Assert.Equal(["refunded"], paid.NextStatuses);
        Assert.Equal("paid", (await h.Orders.GetByIdAsync(order.Id, None)).PaymentStatus);
        var trail = await h.Payments.ListTransactionsAsync(paid.Id, None);
        Assert.Equal(["created", "charge"], trail.Select(t => t.Action));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => h.Payments.TransitionAsync(paid.Id, new TransitionPaymentRequest("cancelled", null), None));
    }

    [Fact]
    public async Task A_guest_can_retry_a_failed_payment_only()
    {
        var h = new Harness();
        var order = await h.Orders.CreateAsync(h.Request(), null, None);
        var lookup = new OrderLookupRequest(order.OrderCode, "0901234567");

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Orders.RetryPaymentAsync(lookup, None));

        await h.Payments.TransitionAsync(order.PaymentId!.Value, new TransitionPaymentRequest("failed", null), None);
        var retried = await h.Orders.RetryPaymentAsync(lookup, None);

        Assert.Equal("pending", retried.PaymentStatus);
    }

    // ---------- payment sessions ----------

    [Fact]
    public async Task A_session_reserves_the_order_without_creating_it_or_using_the_code()
    {
        var h = new Harness();

        var session = await h.Sessions.CreateAsync(h.Request(payment: "bank_transfer", discountCode: "SALE10"), null, None);

        Assert.Equal("pending", session.Status);
        Assert.Equal("qr", session.Channel);
        Assert.StartsWith("PAY", session.ReferenceCode);
        Assert.Equal(112_000, session.Subtotal);
        Assert.Equal(112_000 - 10_000 + 10_000, session.TotalAmount);
        Assert.Equal("VCB", session.BankName);
        Assert.Equal("Bánh cuốn nhân thịt", Assert.Single(session.Items).ProductName);
        Assert.Empty(h.Db.Orders);
        Assert.Equal(1, h.Promotions.UsesLeft);
        Assert.Equal(session.Id, (await h.Sessions.CreateAsync(h.Request(payment: "bank_transfer", discountCode: "SALE10"), null, None)).Id);
    }

    [Fact]
    public async Task A_cash_on_delivery_method_does_not_get_a_session()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => h.Sessions.CreateAsync(h.Request(payment: "cod"), null, None));
    }

    [Fact]
    public async Task Confirming_a_session_creates_the_order_with_a_paid_payment_and_closes_it()
    {
        var h = new Harness();
        var session = await h.Sessions.CreateAsync(h.Request(payment: "bank_transfer", discountCode: "SALE10"), null, None);

        var confirmed = await h.Sessions.ConfirmAsync(session.Id, None);

        Assert.Equal("success", confirmed.Status);
        Assert.NotNull(confirmed.OrderId);
        var order = await h.Orders.GetByIdAsync(confirmed.OrderId!.Value, None);
        Assert.Equal(confirmed.OrderCode, order.OrderCode);
        Assert.Equal("paid", order.PaymentStatus);
        Assert.Equal(session.TotalAmount, order.TotalAmount);
        Assert.Equal(0, h.Promotions.UsesLeft);
        Assert.Equal(["created", "charge"], (await h.Payments.ListTransactionsAsync(order.PaymentId!.Value, None)).Select(t => t.Action));

        // Confirming again (a double click) changes nothing.
        Assert.Equal(order.Id, (await h.Sessions.ConfirmAsync(session.Id, None)).OrderId);
        Assert.Equal(1, await h.Db.Orders.CountAsync());
    }

    [Fact]
    public async Task Confirming_refuses_when_the_price_changed_since_the_quote()
    {
        var h = new Harness();
        var session = await h.Sessions.CreateAsync(h.Request(payment: "bank_transfer"), null, None);
        h.Catalog.Products[h.DishId] = h.Catalog.Products[h.DishId] with { Price = 60_000 };

        await Assert.ThrowsAsync<ConflictException>(() => h.Sessions.ConfirmAsync(session.Id, None));
        Assert.Empty(h.Db.Orders);
    }

    [Fact]
    public async Task An_expired_session_cannot_be_confirmed()
    {
        var h = new Harness();
        var session = await h.Sessions.CreateAsync(h.Request(payment: "bank_transfer"), null, None);
        h.Clock.UtcNow = h.Clock.UtcNow.AddMinutes(16);

        await Assert.ThrowsAsync<ConflictException>(() => h.Sessions.ConfirmAsync(session.Id, None));
        Assert.Equal("cancelled", (await h.Sessions.GetAsync(session.Id, None)).Status);
        Assert.Empty(h.Db.Orders);
    }

    [Fact]
    public async Task A_rejected_session_can_be_retried_by_the_customer_and_a_cancelled_one_cannot_be_confirmed()
    {
        var h = new Harness();
        var session = await h.Sessions.CreateAsync(h.Request(payment: "bank_transfer"), null, None);

        var rejected = await h.Sessions.RejectAsync(session.Id, new RejectPaymentSessionRequest("Chưa thấy tiền"), None);
        Assert.Equal("failed", rejected.Status);
        Assert.Equal("Chưa thấy tiền", rejected.ResolutionNote);

        Assert.Equal("pending", (await h.Sessions.RetryAsync(session.Id, None)).Status);
        Assert.Equal("cancelled", (await h.Sessions.CancelAsync(session.Id, None)).Status);
        await Assert.ThrowsAsync<ConflictException>(() => h.Sessions.ConfirmAsync(session.Id, None));
    }
}
