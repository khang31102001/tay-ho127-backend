using AdminPlatform.Modules.Sales.Application;
using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Sales.Infrastructure;

/// <summary>The starting configuration taken from the frontend's former mock: delivery methods, payment methods,
/// general order options and the order settings. Each group is created only while ITS table is empty, so it is safe in
/// the every-deploy `seed` step and never recreates something an admin deleted or overwrites an edit.
/// Only cash on delivery starts enabled: the transfer/wallet methods carry placeholder bank details and no gateway
/// exists, so an admin must enter real details and switch them on — a customer must never be shown a fake account.</summary>
public static class SalesSeeder
{
    private static readonly (string Code, DeliveryMethodDetails Details)[] DeliveryMethods =
    [
        ("within_5km", new("Giao trong bán kính 5km", "Miễn phí giao hàng trong bán kính 5km từ cửa hàng.", DeliveryMethodType.Delivery, 0, null, 20, 35, null, 1, true, true)),
        ("over_5km", new("Giao ngoài bán kính 5km", "Áp dụng phí giao hàng cố định cho khoảng cách xa hơn 5km.", DeliveryMethodType.Delivery, 10_000, null, 35, 55, null, 2, true, false)),
        ("pickup", new("Tự đến lấy tại cửa hàng", "Khách tự đến nhận hàng, không mất phí giao.", DeliveryMethodType.Pickup, 0, null, 10, 15,
            "127 Đinh Tiên Hoàng, Đa Kao, Quận 1, TP. Hồ Chí Minh", 3, true, false)),
    ];

    private static readonly (string Code, PaymentMethodDetails Details)[] PaymentMethods =
    [
        ("cod", new("Tiền mặt khi nhận hàng", "Khách thanh toán trực tiếp cho nhân viên giao hàng.", null, PaymentMethodGroup.Cod, null,
            "Vui lòng chuẩn bị đúng số tiền để thuận tiện khi giao hàng.", null, null, null, null, 1, true, true, null, null)),
        ("bank_transfer", new("Chuyển khoản ngân hàng", "Chuyển khoản trước, đơn được xác nhận sau khi nhận được tiền.", null, PaymentMethodGroup.BankTransfer, null,
            "Nội dung chuyển khoản: mã tham chiếu thanh toán.", "Vietcombank", "0123456789", "CONG TY TNHH BANH CUON TAY HO 127", "Chi nhánh Quận 1, TP. Hồ Chí Minh", 2, false, false, null, null)),
        ("bank_transfer_mb", new("Chuyển khoản MB Bank", "Chuyển khoản trước, đơn được xác nhận sau khi nhận được tiền.", null, PaymentMethodGroup.BankTransfer, null,
            "Nội dung chuyển khoản: mã tham chiếu thanh toán.", "MB Bank", "0987651234", "CONG TY TNHH BANH CUON TAY HO 127", "Chi nhánh Quận 3, TP. Hồ Chí Minh", 3, false, false, null, null)),
        ("vnpay", new("VNPay", "Thanh toán qua cổng VNPay (thẻ ATM/Visa/Mastercard/QR).", null, PaymentMethodGroup.Card, "vnpay", null, null, null, null, null, 4, false, false, 20_000, null)),
        ("momo", new("Ví MoMo", "Thanh toán qua ví điện tử MoMo.", null, PaymentMethodGroup.EWallet, "momo", null, null, null, null, null, 5, false, false, null, null)),
        ("apple_pay", new("Apple Pay", "Thanh toán nhanh bằng Apple Pay.", null, PaymentMethodGroup.Card, "apple_pay", null, null, null, null, null, 6, false, false, null, null)),
        ("google_pay", new("Google Pay", "Thanh toán nhanh bằng Google Pay.", null, PaymentMethodGroup.EWallet, "google_pay", null, null, null, null, null, 7, false, false, null, null)),
    ];

    private static readonly OrderSettingsDetails DefaultSettings = new("TH127", "yyMMdd", 5, 15);

    /// <returns>true when anything was created.</returns>
    public static async Task<bool> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ISalesDbContext>();
        var seededAnything = false;

        if (!await db.DeliveryMethods.AnyAsync(cancellationToken))
        {
            foreach (var (code, details) in DeliveryMethods)
            {
                db.DeliveryMethods.Add(DeliveryMethod.Create(code, details));
            }

            seededAnything = true;
        }

        if (!await db.PaymentMethods.AnyAsync(cancellationToken))
        {
            foreach (var (code, details) in PaymentMethods)
            {
                db.PaymentMethods.Add(PaymentMethod.Create(code, details));
            }

            seededAnything = true;
        }

        if (!await db.OrderOptionGroups.AnyAsync(cancellationToken))
        {
            db.OrderOptionGroups.Add(OrderOptionGroup.Create("Nước mắm", OptionSelectionType.Single, true,
            [
                new(null, "Không cay", 0, true),
                new(null, "Cay", 0, false),
                new(null, "Cay nhiều", 0, false),
                new(null, "Thêm nước mắm", 5_000, false),
            ]));
            db.OrderOptionGroups.Add(OrderOptionGroup.Create("Rau", OptionSelectionType.Single, true,
            [
                new(null, "Rau tiêu chuẩn", 0, true),
                new(null, "Thêm rau", 5_000, false),
                new(null, "Không rau", 0, false),
            ]));
            seededAnything = true;
        }

        if (!await db.OrderSettings.AnyAsync(cancellationToken))
        {
            db.OrderSettings.Add(OrderSettings.Create(DefaultSettings));
            seededAnything = true;
        }

        if (seededAnything)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return seededAnything;
    }
}
