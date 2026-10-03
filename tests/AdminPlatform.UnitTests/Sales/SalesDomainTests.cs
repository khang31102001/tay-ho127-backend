using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Sales;

public class SalesDomainTests
{
    private static readonly DateTime Now = new(2026, 8, 31, 3, 0, 0, DateTimeKind.Utc);

    // ---------- EnumWire / PhoneNumber ----------

    [Theory]
    [InlineData(PaymentMethodGroup.BankTransfer, "bank_transfer")]
    [InlineData(PaymentMethodGroup.EWallet, "e_wallet")]
    [InlineData(PaymentMethodGroup.Cod, "cod")]
    public void EnumWire_round_trips_snake_case(PaymentMethodGroup group, string wire)
    {
        Assert.Equal(wire, EnumWire.ToWire(group));
        Assert.True(EnumWire.TryParse<PaymentMethodGroup>(wire.ToUpperInvariant(), out var parsed));
        Assert.Equal(group, parsed);
        Assert.False(EnumWire.TryParse<PaymentMethodGroup>("nope", out _));
    }

    [Theory]
    [InlineData("0901234567", "0901234567")]
    [InlineData("+84 901 234 567", "0901234567")]
    [InlineData("0901.234.567", "0901234567")]
    [InlineData("090-123-4567", "0901234567")]
    public void PhoneNumber_normalizes_to_one_stored_form(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumber.Normalize(input));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("+8490123456")]
    public void PhoneNumber_rejects_garbage(string input)
    {
        Assert.False(PhoneNumber.TryNormalize(input, out _));
    }

    // ---------- Order state machine ----------

    [Fact]
    public void A_delivery_order_goes_through_delivering_but_a_pickup_order_skips_it()
    {
        Assert.Contains(OrderStatus.Delivering, OrderStatusRules.NextStatuses(OrderStatus.Ready, isPickup: false));
        Assert.DoesNotContain(OrderStatus.Completed, OrderStatusRules.NextStatuses(OrderStatus.Ready, isPickup: false));

        Assert.Contains(OrderStatus.Completed, OrderStatusRules.NextStatuses(OrderStatus.Ready, isPickup: true));
        Assert.DoesNotContain(OrderStatus.Delivering, OrderStatusRules.NextStatuses(OrderStatus.Ready, isPickup: true));
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.Delivering)]
    public void An_order_can_be_cancelled_from_every_status_before_completed(OrderStatus from)
    {
        Assert.True(OrderStatusRules.CanTransition(from, OrderStatus.Cancelled, isPickup: false));
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void Completed_and_cancelled_orders_are_final(OrderStatus from)
    {
        Assert.Empty(OrderStatusRules.NextStatuses(from, isPickup: false));
    }

    [Fact]
    public void An_order_cannot_skip_ahead_or_go_back()
    {
        Assert.False(OrderStatusRules.CanTransition(OrderStatus.Pending, OrderStatus.Preparing, false));
        Assert.False(OrderStatusRules.CanTransition(OrderStatus.Preparing, OrderStatus.Confirmed, false));
    }

    // ---------- Payment state machine ----------

    [Fact]
    public void Payment_transitions_follow_the_state_machine()
    {
        Assert.True(PaymentStatusRules.CanTransition(PaymentStatus.Pending, PaymentStatus.Paid));
        Assert.True(PaymentStatusRules.CanTransition(PaymentStatus.Paid, PaymentStatus.Refunded));
        Assert.True(PaymentStatusRules.CanTransition(PaymentStatus.Failed, PaymentStatus.Pending));
        Assert.False(PaymentStatusRules.CanTransition(PaymentStatus.Paid, PaymentStatus.Cancelled));
        Assert.Empty(PaymentStatusRules.NextStatuses(PaymentStatus.Refunded));
        Assert.Empty(PaymentStatusRules.NextStatuses(PaymentStatus.Cancelled));
    }

    // ---------- Order ----------

    private static OrderDraft Draft(
        IReadOnlyList<OrderItem>? items = null,
        IReadOnlyList<OrderOptionSelection>? options = null,
        decimal discount = 0,
        decimal fee = 10_000,
        decimal shippingDiscount = 0) =>
        new(
            "TH127-260831-00001", "key-1", null, "Nguyễn Văn A", "+84 901 234 567", null, "12 Đinh Tiên Hoàng",
            "cod", "Tiền mặt", "within_5km", "Giao gần", false,
            items ?? [OrderItem.Create(Guid.NewGuid(), "Bánh cuốn", null, 56_000, 2, null, [])],
            options ?? [],
            discount, null, null, shippingDiscount, fee, true, null, "Khách hàng");

    [Fact]
    public void Order_derives_subtotal_and_total_and_normalizes_the_phone()
    {
        var modifier = OrderItemModifier.Create(Guid.NewGuid(), "Size", Guid.NewGuid(), "Lớn", 5_000);
        var item = OrderItem.Create(Guid.NewGuid(), "Bánh cuốn", "media-1", 56_000, 2, "ít hành", [modifier]);
        var option = OrderOptionSelection.Create(Guid.NewGuid(), "Nước mắm", Guid.NewGuid(), "Thêm", 5_000);

        var order = Order.Create(Draft([item], [option], discount: 6_000, fee: 10_000, shippingDiscount: 4_000), Now);

        Assert.Equal(122_000, item.LineTotal); // (56.000 + 5.000) x 2
        Assert.Equal(127_000, order.Subtotal); // lines + whole-order option, counted once
        Assert.Equal(127_000 - 6_000 + 10_000 - 4_000, order.TotalAmount);
        Assert.Equal("0901234567", order.Phone);
        Assert.Equal(OrderStatus.Pending, order.OrderStatus);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        var history = Assert.Single(order.StatusHistory);
        Assert.Null(history.FromStatus);
        Assert.Equal(OrderStatus.Pending, history.ToStatus);
    }

    [Fact]
    public void Order_refuses_an_empty_cart_and_inconsistent_discounts()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Order.Create(Draft(items: []), Now));
        Assert.Throws<BusinessRuleValidationException>(() => Order.Create(Draft(discount: 1_000_000), Now));
        Assert.Throws<BusinessRuleValidationException>(() => Order.Create(Draft(fee: 5_000, shippingDiscount: 6_000), Now));
    }

    [Fact]
    public void OrderItem_refuses_a_quantity_outside_one_to_ninety_nine()
    {
        Assert.Throws<BusinessRuleValidationException>(() => OrderItem.Create(Guid.NewGuid(), "x", null, 1_000, 0, null, []));
        Assert.Throws<BusinessRuleValidationException>(() => OrderItem.Create(Guid.NewGuid(), "x", null, 1_000, 100, null, []));
    }

    [Fact]
    public void ChangeStatus_records_history_and_stamps_the_final_times()
    {
        var order = Order.Create(Draft(), Now);

        order.ChangeStatus(OrderStatus.Confirmed, Now.AddMinutes(1), "admin@x.vn", Guid.NewGuid(), null);
        order.ChangeStatus(OrderStatus.Cancelled, Now.AddMinutes(2), "admin@x.vn", null, "khách đổi ý");

        Assert.Equal(OrderStatus.Cancelled, order.OrderStatus);
        Assert.Equal(Now.AddMinutes(2), order.CancelledAtUtc);
        Assert.Null(order.CompletedAtUtc);
        Assert.Equal(3, order.StatusHistory.Count);
        Assert.Throws<BusinessRuleValidationException>(() => order.ChangeStatus(OrderStatus.Confirmed, Now, "x", null, null));
    }

    // ---------- Payment ----------

    [Fact]
    public void A_new_payment_is_pending_and_every_transition_appends_an_audit_entry()
    {
        var order = Order.Create(Draft(), Now);
        var payment = Payment.CreateFor(order, null, null, Now, "Hệ thống", alreadyPaid: false);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(order.TotalAmount, payment.Amount);
        Assert.Single(payment.Transactions);

        payment.Transition(PaymentStatus.Paid, Now.AddMinutes(5), "admin@x.vn", null);
        payment.Transition(PaymentStatus.Refunded, Now.AddMinutes(9), "admin@x.vn", "hoàn tiền");

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(Now.AddMinutes(5), payment.PaidAtUtc);
        Assert.Equal(3, payment.Transactions.Count);
        Assert.Equal(PaymentTransactionAction.Refund, payment.Transactions.Last().Action);
        Assert.Throws<BusinessRuleValidationException>(() => payment.Transition(PaymentStatus.Paid, Now, "x", null));
    }

    [Fact]
    public void A_confirmed_payment_session_creates_the_payment_already_paid()
    {
        var order = Order.Create(Draft(), Now);
        var payment = Payment.CreateFor(order, null, "PAYABC", Now, "admin@x.vn", alreadyPaid: true);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(PaymentTransactionAction.Charge, payment.Transactions.Last().Action);
        Assert.Equal("admin@x.vn", payment.Transactions.Last().ChangedBy);
    }

    [Fact]
    public void A_failed_payment_records_a_failed_charge()
    {
        var order = Order.Create(Draft(), Now);
        var payment = Payment.CreateFor(order, null, null, Now, "Hệ thống", false);

        payment.Transition(PaymentStatus.Failed, Now, "admin@x.vn", null);

        Assert.Equal(PaymentTransactionResult.Failed, payment.Transactions.Last().Result);
        Assert.Equal(Now, payment.FailedAtUtc);
    }

    // ---------- Methods ----------

    private static DeliveryMethodDetails DeliveryDetails(
        DeliveryMethodType type = DeliveryMethodType.Delivery, decimal fee = 10_000, decimal? threshold = null, string? pickup = null) =>
        new("Giao", null, type, fee, threshold, 10, 20, pickup, 1, true, false);

    [Fact]
    public void Delivery_fee_is_waived_at_or_above_the_free_shipping_threshold()
    {
        var method = DeliveryMethod.Create("within_5km", DeliveryDetails(threshold: 100_000));

        Assert.Equal(10_000, method.ResolveFee(99_999));
        Assert.Equal(0, method.ResolveFee(100_000));
        Assert.Equal(10_000, DeliveryMethod.Create("x", DeliveryDetails()).ResolveFee(5_000_000));
    }

    [Fact]
    public void Delivery_method_rules_are_enforced()
    {
        Assert.Throws<BusinessRuleValidationException>(() => DeliveryMethod.Create("Bad Code", DeliveryDetails()));
        Assert.Throws<BusinessRuleValidationException>(() => DeliveryMethod.Create("pickup", DeliveryDetails(DeliveryMethodType.Pickup)));
        Assert.Throws<BusinessRuleValidationException>(() => DeliveryMethod.Create("neg", DeliveryDetails(fee: -1)));

        var pickup = DeliveryMethod.Create("pickup", DeliveryDetails(DeliveryMethodType.Pickup, 0, null, "127 Đinh Tiên Hoàng"));
        pickup.Update(DeliveryDetails(DeliveryMethodType.Delivery, 5_000, null, "ignored"));
        Assert.Null(pickup.PickupAddress);
        Assert.Equal("pickup", pickup.Code);
    }

    [Fact]
    public void Payment_method_eligibility_follows_min_and_max_order_amount()
    {
        var method = PaymentMethod.Create("vnpay", new("VNPay", null, null, PaymentMethodGroup.Card, "vnpay", null, null, null, null, null, 1, true, false, 20_000, 1_000_000));

        Assert.False(method.IsEligible(19_999));
        Assert.True(method.IsEligible(20_000));
        Assert.True(method.IsEligible(1_000_000));
        Assert.False(method.IsEligible(1_000_001));
        Assert.False(method.IsPaidOnDelivery);
        Assert.Throws<BusinessRuleValidationException>(() =>
            PaymentMethod.Create("bad", new("x", null, null, PaymentMethodGroup.Cod, null, null, null, null, null, null, 1, true, false, 10, 5)));
    }

    // ---------- OrderOptionGroup ----------

    [Fact]
    public void Order_option_values_keep_their_id_when_the_group_is_edited()
    {
        var group = OrderOptionGroup.Create("Rau", OptionSelectionType.Single, true,
            [new(null, "Tiêu chuẩn", 0, true), new(null, "Thêm rau", 5_000, false)]);
        var keptId = group.Values[1].Id;

        group.Update("Rau", OptionSelectionType.Single, true, [new(keptId, "Thêm nhiều rau", 7_000, false)]);

        var value = Assert.Single(group.Values);
        Assert.Equal(keptId, value.Id);
        Assert.Equal("Thêm nhiều rau", value.Label);
        Assert.Throws<BusinessRuleValidationException>(() =>
            group.Update("Rau", OptionSelectionType.Single, true, [new(Guid.NewGuid(), "x", 0, false)]));
        Assert.Throws<BusinessRuleValidationException>(() =>
            group.Update("Rau", OptionSelectionType.Single, true, [new(null, "a", 0, true), new(null, "b", 0, true)]));
    }

    // ---------- OrderSettings ----------

    [Fact]
    public void Order_code_is_built_from_the_settings()
    {
        var settings = OrderSettings.Create(new("th127", "yyMMdd", 5, 15));
        var local = new DateTime(2026, 8, 31, 10, 0, 0);

        Assert.Equal("TH127-260831-00128", settings.BuildCode(local, 128));
        Assert.Equal("TH127-260831", settings.CounterKey(local));

        settings.Update(new("AB", "none", 4, 15));
        Assert.Equal("AB-0007", settings.BuildCode(local, 7));
        Assert.Equal("AB", settings.CounterKey(local));
    }

    [Theory]
    [InlineData("", "yyMMdd", 5, 15)]
    [InlineData("TH 127", "yyMMdd", 5, 15)]
    [InlineData("TH127", "dd/MM", 5, 15)]
    [InlineData("TH127", "yyMMdd", 2, 15)]
    [InlineData("TH127", "yyMMdd", 5, 1)]
    public void Order_settings_reject_invalid_values(string prefix, string format, int length, int minutes)
    {
        Assert.Throws<BusinessRuleValidationException>(() => OrderSettings.Create(new(prefix, format, length, minutes)));
    }

    // ---------- PaymentSession ----------

    private static PaymentSession NewSession() => PaymentSession.Create(
        new PaymentSessionDraft("PAYABCD2345", PaymentChannel.Qr, "bank_transfer", "Chuyển khoản", null, "A", "0901234567", null,
            "within_5km", "Giao", false, "addr", true, null, 100_000, 10_000, 0, null, null, 0, 110_000, "VCB", "123", "X", "key", "{}", "{}"),
        Now,
        TimeSpan.FromMinutes(15));

    [Fact]
    public void A_pending_session_expires_after_its_lifetime_and_only_then()
    {
        var session = NewSession();

        Assert.False(session.ExpireIfDue(Now.AddMinutes(14)));
        Assert.Equal(PaymentSessionStatus.Pending, session.Status);

        Assert.True(session.ExpireIfDue(Now.AddMinutes(15)));
        Assert.Equal(PaymentSessionStatus.Cancelled, session.Status);
        Assert.False(session.ExpireIfDue(Now.AddHours(1)));
    }

    [Fact]
    public void Session_lifecycle_pending_to_failed_to_retry_to_success()
    {
        var session = NewSession();

        session.MarkFailed("Chưa thấy tiền");
        Assert.Equal(PaymentSessionStatus.Failed, session.Status);
        Assert.Throws<BusinessRuleValidationException>(() => session.MarkSucceeded(Guid.NewGuid(), "X"));

        session.Retry(Now.AddMinutes(20), TimeSpan.FromMinutes(15));
        Assert.Equal(PaymentSessionStatus.Pending, session.Status);
        Assert.Equal(Now.AddMinutes(35), session.ExpiresAtUtc);

        var orderId = Guid.NewGuid();
        session.MarkSucceeded(orderId, "TH127-1");
        Assert.Equal(orderId, session.OrderId);
        Assert.Throws<BusinessRuleValidationException>(() => session.Cancel());
        Assert.Throws<BusinessRuleValidationException>(() => session.Retry(Now, TimeSpan.FromMinutes(1)));
    }
}
