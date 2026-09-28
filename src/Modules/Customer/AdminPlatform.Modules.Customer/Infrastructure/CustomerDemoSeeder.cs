using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Application;
using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Customer.Infrastructure;

/// <summary>Idempotent DEMO data (Migrator `seed-demo` only): a few customers with a local (email +
/// password) login and delivery addresses — created the same way CustomerAuthService.RegisterAsync does,
/// so they can log in through /api/v1/customer/auth/login. Upserted by login email.</summary>
public static class CustomerDemoSeeder
{
    private sealed record DemoAddress(string ReceiverName, string Phone, string AddressLine, string Ward, bool IsDefault);

    private sealed record DemoCustomer(string FullName, string Phone, string Email, DemoAddress[] Addresses);

    private static readonly DemoCustomer[] DemoCustomers =
    [
        new("Nguyen Van An", "0901000001", "customer1@demo.tayho127.test",
        [
            new("Nguyen Van An", "0901000001", "127 Tay Ho", "Quang An", IsDefault: true),
            new("Nguyen Thi Hoa", "0901000011", "15 Xuan Dieu", "Quang An", IsDefault: false),
        ]),
        new("Tran Thi Binh", "0901000002", "customer2@demo.tayho127.test",
        [
            new("Tran Thi Binh", "0901000002", "45 Au Co", "Nhat Tan", IsDefault: true),
        ]),
        new("Le Hoang Cuong", "0901000003", "customer3@demo.tayho127.test", []),
    ];

    public static async Task SeedAsync(IServiceProvider services, string password, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ICustomerDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        foreach (var demo in DemoCustomers)
        {
            var exists = await db.CustomerAuthIdentities.AnyAsync(
                i => i.Provider == CustomerAuthProvider.Local && i.Email == demo.Email, cancellationToken);
            if (exists)
            {
                continue;
            }

            var customer = Domain.Customer.Create(demo.FullName, demo.Phone, demo.Email);
            db.Customers.Add(customer);
            db.CustomerAuthIdentities.Add(CustomerAuthIdentity.CreateLocal(customer.Id, demo.Email, passwordHasher.Hash(password)));

            foreach (var address in demo.Addresses)
            {
                db.CustomerAddresses.Add(CustomerAddress.Create(
                    customer.Id, address.ReceiverName, address.Phone, address.AddressLine,
                    address.Ward, "Tay Ho", "Ha Noi", addressNote: null, address.IsDefault));
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
