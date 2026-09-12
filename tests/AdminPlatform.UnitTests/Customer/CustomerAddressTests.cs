using AdminPlatform.Modules.Customer.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Customer;

public class CustomerAddressTests
{
    [Fact]
    public void Create_trims_required_fields_and_keeps_optional_ones()
    {
        var customerId = Guid.NewGuid();

        var address = CustomerAddress.Create(
            customerId, " Jane ", " 0901234567 ", " 123 Main St ", "Ward 1", "District 1", "HCMC", "Leave at gate", isDefault: true);

        Assert.Equal(customerId, address.CustomerId);
        Assert.Equal("Jane", address.ReceiverName);
        Assert.Equal("0901234567", address.Phone);
        Assert.Equal("123 Main St", address.AddressLine);
        Assert.True(address.IsDefault);
    }

    [Fact]
    public void Create_rejects_empty_address_line()
    {
        Assert.Throws<BusinessRuleValidationException>(
            () => CustomerAddress.Create(Guid.NewGuid(), "Jane", "0901234567", "", null, null, null, null, false));
    }

    [Fact]
    public void MarkAsDefault_and_UnmarkAsDefault_toggle_the_flag()
    {
        var address = CustomerAddress.Create(Guid.NewGuid(), "Jane", "0901234567", "123 Main St", null, null, null, null, isDefault: false);

        address.MarkAsDefault();
        Assert.True(address.IsDefault);

        address.UnmarkAsDefault();
        Assert.False(address.IsDefault);
    }

    [Fact]
    public void Update_replaces_editable_fields_but_not_default_flag()
    {
        var address = CustomerAddress.Create(Guid.NewGuid(), "Jane", "0901234567", "123 Main St", null, null, null, null, isDefault: true);

        address.Update("Jane B", "0909999999", "456 Other St", "Ward 2", "District 2", "Hanoi", "Note");

        Assert.Equal("Jane B", address.ReceiverName);
        Assert.Equal("456 Other St", address.AddressLine);
        Assert.True(address.IsDefault);
    }
}
