using AdminPlatform.Modules.Customer.Application.Addresses;
using AdminPlatform.Modules.Customer.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Customer;

public class CustomerAddressServiceTests
{
    private static CustomerDbContext NewDb() =>
        new(new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateAsync_forces_the_first_address_to_be_default()
    {
        var db = NewDb();
        var sut = new CustomerAddressService(db);
        var customerId = Guid.NewGuid();

        var created = await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: false), CancellationToken.None);

        Assert.True(created.IsDefault);
    }

    [Fact]
    public async Task CreateAsync_unsets_the_previous_default_when_a_new_default_is_added()
    {
        var db = NewDb();
        var sut = new CustomerAddressService(db);
        var customerId = Guid.NewGuid();

        var first = await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true), CancellationToken.None);
        var second = await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "456 Other St", null, null, null, null, IsDefault: true), CancellationToken.None);

        var all = await sut.ListAsync(customerId, CancellationToken.None);

        Assert.True(second.IsDefault);
        Assert.Single(all, a => a.IsDefault);
        Assert.False(all.Single(a => a.Id == first.Id).IsDefault);
    }

    [Fact]
    public async Task SetDefaultAsync_moves_the_default_flag_to_the_chosen_address()
    {
        var db = NewDb();
        var sut = new CustomerAddressService(db);
        var customerId = Guid.NewGuid();

        var first = await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true), CancellationToken.None);
        var second = await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "456 Other St", null, null, null, null, IsDefault: false), CancellationToken.None);

        var updated = await sut.SetDefaultAsync(customerId, second.Id, CancellationToken.None);
        var all = await sut.ListAsync(customerId, CancellationToken.None);

        Assert.True(updated.IsDefault);
        Assert.Single(all, a => a.IsDefault);
        Assert.False(all.Single(a => a.Id == first.Id).IsDefault);
    }

    [Fact]
    public async Task DeleteAsync_promotes_a_remaining_address_when_the_default_is_removed()
    {
        var db = NewDb();
        var sut = new CustomerAddressService(db);
        var customerId = Guid.NewGuid();

        var first = await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true), CancellationToken.None);
        await sut.CreateAsync(customerId, new CreateCustomerAddressRequest("Jane", "0901234567", "456 Other St", null, null, null, null, IsDefault: false), CancellationToken.None);

        await sut.DeleteAsync(customerId, first.Id, CancellationToken.None);

        var remaining = await sut.ListAsync(customerId, CancellationToken.None);
        Assert.Single(remaining);
        Assert.True(remaining[0].IsDefault);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_for_another_customers_address()
    {
        var db = NewDb();
        var sut = new CustomerAddressService(db);
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var address = await sut.CreateAsync(ownerId, new CreateCustomerAddressRequest("Jane", "0901234567", "123 Main St", null, null, null, null, IsDefault: true), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.UpdateAsync(attackerId, address.Id, new UpdateCustomerAddressRequest("X", "0900000000", "Other", null, null, null, null), CancellationToken.None));
    }
}
