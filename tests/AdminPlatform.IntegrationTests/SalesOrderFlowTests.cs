using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Catalog.Application.Promotions;
using AdminPlatform.Modules.Sales.Application.DeliveryMethods;
using AdminPlatform.Modules.Sales.Application.OrderOptions;
using AdminPlatform.Modules.Sales.Application.Orders;
using AdminPlatform.Modules.Sales.Application.PaymentMethods;
using AdminPlatform.Modules.Sales.Application.Payments;
using AdminPlatform.Modules.Sales.Application.PaymentSessions;
using AdminPlatform.Modules.Sales.Application.Settings;

namespace AdminPlatform.IntegrationTests;

/// <summary>The Sales module end to end over HTTP: admin configuration, a guest ordering and tracking, a QR payment
/// confirmed by staff, the discount-code counter and the permission/ownership boundaries.</summary>
[Collection("Api")]
public class SalesOrderFlowTests
{
    private const string Sales = "/api/v1/sales";
    private readonly AdminPlatformApiFactory _factory;

    public SalesOrderFlowTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object body, HttpStatusCode expected = HttpStatusCode.Created)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>A sellable dish plus a delivery method, a COD method and a transfer method, all with unique codes.</summary>
    private sealed record Shop(
        Guid ProductId, decimal Price, string DeliveryCode, string CodCode, string TransferCode, Guid OptionGroupId, Guid OptionValueId);

    private async Task<Shop> SetUpShopAsync(HttpClient admin)
    {
        var suffix = Guid.NewGuid().ToString("n")[..8];
        var category = await PostAsync<CategoryResponse>(admin, "/api/v1/catalog/categories", new CreateCategoryRequest($"Đơn {suffix}", null, 1, true));
        var product = await PostAsync<ProductResponse>(admin, "/api/v1/catalog/products", new CreateProductRequest(
            $"Bánh cuốn {suffix}", null, category.Id, 50_000, null, null, true, null, null, null, null, null));

        var delivery = await PostAsync<DeliveryMethodResponse>(admin, $"{Sales}/delivery-methods", new CreateDeliveryMethodRequest(
            $"giao-{suffix}", "Giao tận nơi", null, "delivery", 15_000, 500_000, 20, 40, null, 1, true, false));
        var cod = await PostAsync<PaymentMethodResponse>(admin, $"{Sales}/payment-methods", new CreatePaymentMethodRequest(
            $"cod-{suffix}", "Tiền mặt", null, null, "cod", null, null, null, null, null, null, 1, true, false, null, null));
        var transfer = await PostAsync<PaymentMethodResponse>(admin, $"{Sales}/payment-methods", new CreatePaymentMethodRequest(
            $"ck-{suffix}", "Chuyển khoản", null, null, "bank_transfer", null, "Ghi mã tham chiếu", "Vietcombank", "0123456789", "CTY TEST", null, 2, true, false, null, null));
        var group = await PostAsync<OrderOptionGroupResponse>(admin, $"{Sales}/order-option-groups", new CreateOrderOptionGroupRequest(
            $"Rau {suffix}", "single", true, [new OrderOptionValueRequest(null, "Tiêu chuẩn", 0, true), new OrderOptionValueRequest(null, "Thêm rau", 5_000, false)]));

        return new Shop(product.Id, 50_000, delivery.Code, cod.Code, transfer.Code, group.Id, group.Options[1].Id);
    }

    private static CreateOrderRequest Request(Shop shop, string payment, int quantity = 2, string? discountCode = null, string? key = null, string phone = "0901234567") =>
        new("Nguyễn Văn A", phone, null, "12 Đinh Tiên Hoàng", shop.DeliveryCode, payment,
            [new OrderItemRequest(shop.ProductId, quantity, null, null)], true, null,
            [new SelectedOptionRequest(shop.OptionGroupId, shop.OptionValueId)], discountCode, key ?? Guid.NewGuid().ToString("n"));

    [Fact]
    public async Task Configuration_is_managed_by_an_admin_and_only_enabled_methods_reach_the_public_checkout()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        using var anonymous = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("n")[..8];

        // Bad input: pickup without an address, a duplicate code, an unknown group.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Sales}/delivery-methods", new CreateDeliveryMethodRequest(
            $"p-{suffix}", "Tự lấy", null, "pickup", 0, null, null, null, null, 1, true, false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Sales}/payment-methods", new CreatePaymentMethodRequest(
            $"x-{suffix}", "X", null, null, "crypto", null, null, null, null, null, null, 1, true, false, null, null))).StatusCode);

        var method = await PostAsync<DeliveryMethodResponse>(admin, $"{Sales}/delivery-methods", new CreateDeliveryMethodRequest(
            $"d-{suffix}", "Giao", null, "delivery", 10_000, null, 10, 20, null, 1, true, true));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Sales}/delivery-methods", new CreateDeliveryMethodRequest(
            $"d-{suffix}", "Trùng", null, "delivery", 10_000, null, null, null, null, 2, true, false))).StatusCode);

        // Saving a second default clears the first one.
        var second = await PostAsync<DeliveryMethodResponse>(admin, $"{Sales}/delivery-methods", new CreateDeliveryMethodRequest(
            $"e-{suffix}", "Giao 2", null, "delivery", 20_000, null, null, null, null, 2, true, true));
        Assert.False((await admin.GetFromJsonAsync<DeliveryMethodResponse>($"{Sales}/delivery-methods/{method.Id}"))!.IsDefault);
        Assert.True(second.IsDefault);

        var publicList = (await anonymous.GetFromJsonAsync<List<PublicDeliveryMethodResponse>>($"{Sales}/public/delivery-methods"))!;
        Assert.Contains(publicList, m => m.Code == method.Code);

        var off = await admin.PutAsJsonAsync($"{Sales}/delivery-methods/{method.Id}", new UpdateDeliveryMethodRequest(
            "Giao", null, "delivery", 10_000, null, 10, 20, null, 1, false, false));
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.DoesNotContain((await anonymous.GetFromJsonAsync<List<PublicDeliveryMethodResponse>>($"{Sales}/public/delivery-methods"))!, m => m.Code == method.Code);

        // The public payment methods never show bank details or the gateway.
        var transfer = await PostAsync<PaymentMethodResponse>(admin, $"{Sales}/payment-methods", new CreatePaymentMethodRequest(
            $"ck-{suffix}", "Chuyển khoản", null, null, "bank_transfer", "vnpay", null, "VCB", "999", "CTY", null, 3, true, false, null, null));
        var publicPayments = await anonymous.GetStringAsync($"{Sales}/public/payment-methods");
        Assert.Contains(transfer.Code, publicPayments);
        Assert.DoesNotContain("999", publicPayments);
        Assert.DoesNotContain("vnpay", publicPayments);

        // Settings: a bad prefix is refused, a good change shows an example code.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"{Sales}/order-settings", new UpdateOrderSettingsRequest("TH 127", "yyMMdd", 5, 15))).StatusCode);
        var settings = await admin.PutAsJsonAsync($"{Sales}/order-settings", new UpdateOrderSettingsRequest("TH127", "yyMMdd", 5, 15));
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        Assert.Matches(@"^TH127-\d{6}-00128$", (await settings.Content.ReadFromJsonAsync<OrderSettingsResponse>())!.ExampleOrderCode);

        // None of this is anonymous.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Sales}/delivery-methods")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Sales}/order-settings")).StatusCode);
    }

    [Fact]
    public async Task A_guest_orders_with_cash_and_the_server_decides_every_amount()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        using var guest = _factory.CreateClient();
        var shop = await SetUpShopAsync(admin);

        var key = Guid.NewGuid().ToString("n");
        var created = await PostAsync<OrderResponse>(guest, $"{Sales}/public/orders", Request(shop, shop.CodCode, key: key));

        Assert.Equal(2 * 50_000 + 5_000, created.Subtotal); // dishes + the whole-order option, once
        Assert.Equal(15_000, created.DeliveryFee);
        Assert.Equal(120_000, created.TotalAmount);
        Assert.Equal("pending", created.OrderStatus);
        Assert.Equal("pending", created.PaymentStatus);
        Assert.Matches(@"^TH127-\d{6}-\d{5}$", created.OrderCode);
        Assert.NotNull(created.PaymentId);

        // Pressing the button twice returns the same order.
        var replay = await PostAsync<OrderResponse>(guest, $"{Sales}/public/orders", Request(shop, shop.CodCode, key: key));
        Assert.Equal(created.Id, replay.Id);

        // Refused: a transfer method cannot be ordered directly, a bad phone, an empty cart.
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync($"{Sales}/public/orders", Request(shop, shop.TransferCode))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync($"{Sales}/public/orders", Request(shop, shop.CodCode, phone: "123"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync($"{Sales}/public/orders", Request(shop, shop.CodCode) with { Items = [] })).StatusCode);

        // Tracking needs the order code AND the phone; a wrong phone is the same 404 as an unknown code.
        var tracked = await PostAsync<OrderResponse>(guest, $"{Sales}/public/orders/lookup", new OrderLookupRequest(created.OrderCode, "+84 901 234 567"), HttpStatusCode.OK);
        Assert.Equal(created.Id, tracked.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsJsonAsync($"{Sales}/public/orders/lookup", new OrderLookupRequest(created.OrderCode, "0909999999"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsJsonAsync($"{Sales}/public/orders/lookup", new OrderLookupRequest("TH127-000000-99999", "0901234567"))).StatusCode);

        // The admin works the order through to completion; an illegal move is a 400.
        string[] steps = ["confirmed", "preparing", "ready", "delivering", "completed"];
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Sales}/orders/{created.Id}/status", new ChangeOrderStatusRequest("completed", null))).StatusCode);
        OrderResponse current = created;
        foreach (var step in steps)
        {
            current = await PostAsync<OrderResponse>(admin, $"{Sales}/orders/{created.Id}/status", new ChangeOrderStatusRequest(step, null), HttpStatusCode.OK);
        }

        Assert.Equal("completed", current.OrderStatus);
        Assert.NotNull(current.CompletedAt);
        Assert.Equal(6, current.StatusHistory.Count);
        Assert.Empty(current.NextStatuses);

        // Payment: the admin records the cash, the order mirrors it, the trail is append-only.
        var paid = await PostAsync<PaymentResponse>(admin, $"{Sales}/payments/{created.PaymentId}/transition", new TransitionPaymentRequest("paid", "đã thu tiền"), HttpStatusCode.OK);
        Assert.Equal("paid", paid.Status);
        Assert.Equal("paid", (await admin.GetFromJsonAsync<OrderResponse>($"{Sales}/orders/{created.Id}"))!.PaymentStatus);
        var trail = (await admin.GetFromJsonAsync<List<PaymentTransactionResponse>>($"{Sales}/payments/{created.PaymentId}/transactions"))!;
        Assert.Equal(["created", "charge"], trail.Select(t => t.Action));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Sales}/payments/{created.PaymentId}/transition", new TransitionPaymentRequest("cancelled", null))).StatusCode);

        // The lists the admin screens use.
        var orders = (await admin.GetFromJsonAsync<PagedResult<OrderResponse>>($"{Sales}/orders?search={created.OrderCode}&status=completed"))!;
        Assert.Contains(orders.Items, o => o.Id == created.Id);
        var payments = (await admin.GetFromJsonAsync<PagedResult<PaymentResponse>>($"{Sales}/payments?search={created.OrderCode}"))!;
        Assert.Contains(payments.Items, p => p.Id == paid.Id);

        // Staff screens are not for guests.
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"{Sales}/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"{Sales}/payments")).StatusCode);
    }

    [Fact]
    public async Task A_discount_code_is_recalculated_by_the_server_and_its_last_use_goes_to_one_order()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        using var guest = _factory.CreateClient();
        var shop = await SetUpShopAsync(admin);
        var code = $"SALES{Guid.NewGuid().ToString("n")[..8]}".ToUpperInvariant();

        var promotion = await PostAsync<PromotionResponse>(admin, "/api/v1/catalog/promotions",
            new CreatePromotionRequest(code, "Giảm 10%", null, "percentage", 10, null, null, null, null, 1, null, null, "active"));

        var first = await PostAsync<OrderResponse>(guest, $"{Sales}/public/orders", Request(shop, shop.CodCode, discountCode: code));
        Assert.Equal(10_500, first.Discount); // 10% of 105.000, computed by the server
        Assert.Equal(code, first.DiscountCode);
        Assert.Equal(1, (await admin.GetFromJsonAsync<PromotionResponse>($"/api/v1/catalog/promotions/{promotion.Id}"))!.UsageCount);

        // The single use is gone: the next order with the code is refused, and no order is created.
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync($"{Sales}/public/orders", Request(shop, shop.CodCode, discountCode: code))).StatusCode);

        // Cancelling the first order gives the use back.
        var cancelled = await PostAsync<OrderResponse>(admin, $"{Sales}/orders/{first.Id}/status", new ChangeOrderStatusRequest("cancelled", "khách đổi ý"), HttpStatusCode.OK);
        Assert.Equal("cancelled", cancelled.PaymentStatus);
        Assert.Equal(0, (await admin.GetFromJsonAsync<PromotionResponse>($"/api/v1/catalog/promotions/{promotion.Id}"))!.UsageCount);
        Assert.Equal(HttpStatusCode.Created, (await guest.PostAsJsonAsync($"{Sales}/public/orders", Request(shop, shop.CodCode, discountCode: code))).StatusCode);

        // A made-up code is refused.
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync($"{Sales}/public/orders", Request(shop, shop.CodCode, discountCode: "KHONGCO"))).StatusCode);
    }

    [Fact]
    public async Task A_qr_payment_session_creates_the_order_only_when_staff_confirm_the_money()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        using var guest = _factory.CreateClient();
        var shop = await SetUpShopAsync(admin);

        var request = Request(shop, shop.TransferCode);
        var session = await PostAsync<PaymentSessionResponse>(guest, $"{Sales}/public/payment-sessions", request);
        Assert.Equal("pending", session.Status);
        Assert.Equal("qr", session.Channel);
        Assert.Equal("0123456789", session.BankAccountNumber);
        Assert.Equal(120_000, session.TotalAmount);
        Assert.Null(session.OrderId);

        // The customer cannot mark it paid: the guest has no confirm endpoint, and staff endpoints refuse them.
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsJsonAsync($"{Sales}/payment-sessions/{session.Id}/confirm", new { })).StatusCode);
        var polled = (await guest.GetFromJsonAsync<PaymentSessionResponse>($"{Sales}/public/payment-sessions/{session.Id}"))!;
        Assert.Equal("pending", polled.Status);

        // Staff confirm: the order exists, paid.
        var confirmed = await PostAsync<PaymentSessionResponse>(admin, $"{Sales}/payment-sessions/{session.Id}/confirm", new { }, HttpStatusCode.OK);
        Assert.Equal("success", confirmed.Status);
        Assert.NotNull(confirmed.OrderCode);

        var tracked = await PostAsync<OrderResponse>(guest, $"{Sales}/public/orders/lookup", new OrderLookupRequest(confirmed.OrderCode!, "0901234567"), HttpStatusCode.OK);
        Assert.Equal("paid", tracked.PaymentStatus);
        Assert.Equal(120_000, tracked.TotalAmount);

        // The customer's page now sees the success and the order code; confirming twice is harmless.
        Assert.Equal(confirmed.OrderCode, (await guest.GetFromJsonAsync<PaymentSessionResponse>($"{Sales}/public/payment-sessions/{session.Id}"))!.OrderCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"{Sales}/payment-sessions/{session.Id}/confirm", new { })).StatusCode);
        var orders = (await admin.GetFromJsonAsync<PagedResult<OrderResponse>>($"{Sales}/orders?search={confirmed.OrderCode}"))!;
        Assert.Single(orders.Items);

        // A cash method does not take a session; rejection + retry + cancel work.
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync($"{Sales}/public/payment-sessions", Request(shop, shop.CodCode))).StatusCode);

        var second = await PostAsync<PaymentSessionResponse>(guest, $"{Sales}/public/payment-sessions", Request(shop, shop.TransferCode));
        var rejected = await PostAsync<PaymentSessionResponse>(admin, $"{Sales}/payment-sessions/{second.Id}/reject", new RejectPaymentSessionRequest("Chưa thấy tiền"), HttpStatusCode.OK);
        Assert.Equal("failed", rejected.Status);
        var retried = await PostAsync<PaymentSessionResponse>(guest, $"{Sales}/public/payment-sessions/{second.Id}/retry", new { }, HttpStatusCode.OK);
        Assert.Equal("pending", retried.Status);
        var cancelled = await PostAsync<PaymentSessionResponse>(guest, $"{Sales}/public/payment-sessions/{second.Id}/cancel", new { }, HttpStatusCode.OK);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Sales}/payment-sessions/{second.Id}/confirm", new { })).StatusCode);

        var sessions = (await admin.GetFromJsonAsync<PagedResult<PaymentSessionResponse>>($"{Sales}/payment-sessions?status=cancelled"))!;
        Assert.Contains(sessions.Items, s => s.Id == second.Id);
    }

    [Fact]
    public async Task A_signed_in_customer_sees_only_their_own_order_history()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        using var anonymous = _factory.CreateClient();
        var shop = await SetUpShopAsync(admin);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        var tokens = await CustomerAuthTestHelper.RegisterAsync(anonymous, "Khách Sales", $"09{Random.Shared.Next(10_000_000, 99_999_999)}", $"sales-{suffix}@example.com", "Passw0rd!Strong");
        using var customer = _factory.CreateClient();
        customer.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var mine = await PostAsync<OrderResponse>(customer, $"{Sales}/public/orders", Request(shop, shop.CodCode));
        var guestOrder = await PostAsync<OrderResponse>(anonymous, $"{Sales}/public/orders", Request(shop, shop.CodCode));
        Assert.NotNull(mine.CustomerId);
        Assert.Null(guestOrder.CustomerId);

        var history = (await customer.GetFromJsonAsync<PagedResult<OrderResponse>>($"{Sales}/customer/orders"))!;
        Assert.Equal(mine.Id, Assert.Single(history.Items).Id);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync($"{Sales}/customer/orders/{mine.OrderCode}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"{Sales}/customer/orders/{guestOrder.OrderCode}")).StatusCode);

        // Realms: an anonymous caller and an admin token are both refused the customer history.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Sales}/customer/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync($"{Sales}/customer/orders")).StatusCode);
        // A customer token is not an admin: it cannot open the staff order list.
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync($"{Sales}/orders")).StatusCode);
    }
}
