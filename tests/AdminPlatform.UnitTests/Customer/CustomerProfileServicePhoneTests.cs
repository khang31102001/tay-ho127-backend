using AdminPlatform.Modules.Customer.Application.Customers;
using AdminPlatform.Modules.Customer.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Customer;

public class CustomerProfileServicePhoneTests
{
    private static CustomerDbContext NewDb() =>
        new(new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateCustomerRequest Create(string phone, string email) => new("Jane", phone, email, null, null, null);

    [Fact]
    public async Task A_phone_already_taken_is_found_whatever_way_it_is_written()
    {
        var db = NewDb();
        var sut = new CustomerProfileService(db);
        await sut.CreateAsync(Create("0901234567", "a@example.com"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(Create("+84 901 234 567", "b@example.com"), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(Create(" 0901.234.567 ", "c@example.com"), CancellationToken.None));
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task A_phone_saved_by_an_admin_is_stored_in_the_canonical_form()
    {
        var db = NewDb();
        var sut = new CustomerProfileService(db);

        var created = await sut.CreateAsync(Create("+84901234567", "a@example.com"), CancellationToken.None);

        Assert.Equal("0901234567", created.Phone);
    }
}
