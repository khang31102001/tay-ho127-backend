using System.Text.Json;
using AdminPlatform.Modules.Sales.Application;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Application.Settings;
using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Sales.Infrastructure;

/// <summary>A dish the demo orders can contain — handed in by the Migrator (the only project allowed to read the
/// Catalog), because Sales never references another module.</summary>
public sealed record DemoProduct(Guid Id, string Name, decimal Price, string? MediaId);

/// <summary>DEMO data for test/dev databases only (`migrator seed-demo`, never part of `all`): switches the sample bank
/// transfer methods on and creates orders in every status, payments in every status and payment sessions in every
/// status, so every admin screen has rows to show. Idempotent: each row is keyed by a fixed idempotency key.</summary>
public static class SalesDemoSeeder
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <returns>how many orders were created.</returns>
    public static async Task<int> SeedAsync(IServiceProvider services, IReadOnlyList<DemoProduct> products, CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return 0;
        }

        var db = services.GetRequiredService<ISalesDbContext>();
        var codes = services.GetRequiredService<IOrderCodeGenerator>();
        var settings = await OrderSettingsStore.GetOrCreateAsync(db, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var method in await db.PaymentMethods.Where(m => m.Group == PaymentMethodGroup.BankTransfer && !m.IsActive).ToListAsync(cancellationToken))
        {
            method.Update(new PaymentMethodDetails(
                method.Name, method.Description, method.IconMediaId, method.Group, method.Gateway, method.Instructions, method.BankName,
                method.BankAccountNumber, method.BankAccountHolder, method.BankBranch, method.DisplayOrder, true, method.IsDefault,
                method.MinOrderAmount, method.MaxOrderAmount));
        }

        var delivery = await db.DeliveryMethods.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);
        var home = delivery.First(m => m.Type == DeliveryMethodType.Delivery);
        var pickup = delivery.FirstOrDefault(m => m.Type == DeliveryMethodType.Pickup) ?? home;
        var cod = await db.PaymentMethods.AsNoTracking().FirstAsync(m => m.Group == PaymentMethodGroup.Cod, cancellationToken);
        var transfer = await db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Group == PaymentMethodGroup.BankTransfer, cancellationToken) ?? cod;

        // (key, delivery, payment, customer, phone, target order status, payment status)
        var plans = new (string Key, DeliveryMethod Delivery, PaymentMethod Payment, string Name, string Phone, OrderStatus Status, PaymentStatus Paid)[]
        {
            ("demo-order-1", home, cod, "Nguyễn Thị Hoa", "0901234567", OrderStatus.Pending, PaymentStatus.Pending),
            ("demo-order-2", home, cod, "Trần Văn Minh", "0912345678", OrderStatus.Confirmed, PaymentStatus.Pending),
            ("demo-order-3", home, transfer, "Lê Thu Trang", "0923456789", OrderStatus.Preparing, PaymentStatus.Paid),
            ("demo-order-4", pickup, cod, "Phạm Quốc Bảo", "0934567890", OrderStatus.Ready, PaymentStatus.Pending),
            ("demo-order-5", home, transfer, "Võ Ngọc Anh", "0945678901", OrderStatus.Completed, PaymentStatus.Paid),
            ("demo-order-6", home, cod, "Đặng Hữu Phước", "0956789012", OrderStatus.Cancelled, PaymentStatus.Cancelled),
        };

        var created = 0;
        for (var index = 0; index < plans.Length; index++)
        {
            var plan = plans[index];
            if (await db.Orders.AnyAsync(o => o.IdempotencyKey == plan.Key, cancellationToken))
            {
                continue;
            }

            var product = products[index % products.Count];
            var item = OrderItem.Create(product.Id, product.Name, product.MediaId, product.Price, index % 3 + 1, null, []);
            var fee = plan.Delivery.ResolveFee(item.LineTotal);
            var order = Order.Create(
                new OrderDraft(
                    await codes.NextAsync(settings, cancellationToken), plan.Key, null, plan.Name, plan.Phone, null,
                    plan.Delivery.PickupAddress ?? "12 Đinh Tiên Hoàng, Đa Kao, Quận 1, TP. Hồ Chí Minh",
                    plan.Payment.Code, plan.Payment.Name, plan.Delivery.Code, plan.Delivery.Name, plan.Delivery.Type == DeliveryMethodType.Pickup,
                    [item], [], 0, null, null, 0, fee, true, null, "Khách hàng"),
                now.AddHours(-(plans.Length - index)));

            var payment = Payment.CreateFor(order, plan.Payment.Gateway, null, now, "Hệ thống", alreadyPaid: false);
            if (plan.Paid == PaymentStatus.Paid)
            {
                payment.Transition(PaymentStatus.Paid, now, "demo", "Dữ liệu demo.");
            }

            foreach (var step in StepsTo(plan.Status, order.IsPickup))
            {
                order.ChangeStatus(step, now, "demo", null, null);
            }

            if (plan.Paid == PaymentStatus.Cancelled)
            {
                payment.Transition(PaymentStatus.Cancelled, now, "demo", "Đơn hàng đã bị hủy.");
            }

            order.SyncPaymentStatus(payment.Status);
            db.Orders.Add(order);
            db.Payments.Add(payment);
            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await SeedSessionsAsync(db, transfer, home, products, settings, now, cancellationToken);
        return created;
    }

    /// <summary>The statuses an order passes through from "pending" to <paramref name="target"/>.</summary>
    private static IEnumerable<OrderStatus> StepsTo(OrderStatus target, bool isPickup)
    {
        OrderStatus[] path = isPickup
            ? [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Completed]
            : [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Delivering, OrderStatus.Completed];

        if (target == OrderStatus.Pending)
        {
            return [];
        }

        if (target == OrderStatus.Cancelled)
        {
            return [OrderStatus.Cancelled];
        }

        var last = Array.IndexOf(path, target);
        return path.Take(last + 1);
    }

    private static async Task SeedSessionsAsync(
        ISalesDbContext db, PaymentMethod transfer, DeliveryMethod delivery, IReadOnlyList<DemoProduct> products,
        OrderSettings settings, DateTime now, CancellationToken cancellationToken)
    {
        (string Key, string Phone, string Name, PaymentSessionStatus Status)[] plans =
        [
            ("demo-session-1", "0967890123", "Bùi Minh Khang", PaymentSessionStatus.Pending),
            ("demo-session-2", "0978901234", "Hồ Thanh Thảo", PaymentSessionStatus.Failed),
            ("demo-session-3", "0989012345", "Ngô Gia Huy", PaymentSessionStatus.Cancelled),
            ("demo-session-4", "0990123456", "Đỗ Khánh Linh", PaymentSessionStatus.Success),
        ];

        for (var index = 0; index < plans.Length; index++)
        {
            var plan = plans[index];
            if (await db.PaymentSessions.AnyAsync(s => s.IdempotencyKey == plan.Key, cancellationToken))
            {
                continue;
            }

            var product = products[index % products.Count];
            var request = new CreateOrderRequest(
                plan.Name, plan.Phone, null, "45 Lê Lợi, Bến Thành, Quận 1", delivery.Code, transfer.Code,
                [new OrderItemRequest(product.Id, 1, null, null)], true, null, null, null, null);
            var subtotal = product.Price;
            var fee = delivery.ResolveFee(subtotal);
            var line = new { items = new[] { new { productId = product.Id, productName = product.Name, productImage = product.MediaId, unitPrice = product.Price, quantity = 1, lineTotal = product.Price, note = (string?)null, modifiers = Array.Empty<object>() } }, options = Array.Empty<object>() };

            var session = PaymentSession.Create(
                new PaymentSessionDraft(
                    $"PAYDEMO{index + 1:00}", PaymentChannel.Qr, transfer.Code, transfer.Name, null, plan.Name, plan.Phone, null, delivery.Code, delivery.Name,
                    false, request.DeliveryAddress!, true, null, subtotal, fee, 0, null, null, 0, subtotal + fee,
                    transfer.BankName, transfer.BankAccountNumber, transfer.BankAccountHolder, plan.Key,
                    JsonSerializer.Serialize(request, Json), JsonSerializer.Serialize(line, Json)),
                now,
                TimeSpan.FromMinutes(settings.PaymentSessionMinutes));

            switch (plan.Status)
            {
                case PaymentSessionStatus.Failed:
                    session.MarkFailed("Chưa nhận được tiền chuyển khoản.");
                    break;
                case PaymentSessionStatus.Cancelled:
                    session.Cancel();
                    break;
                case PaymentSessionStatus.Success:
                    var order = await db.Orders.AsNoTracking().FirstAsync(o => o.IdempotencyKey == "demo-order-3", cancellationToken);
                    session.MarkSucceeded(order.Id, order.OrderCode);
                    break;
            }

            db.PaymentSessions.Add(session);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

}
