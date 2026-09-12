using AdminPlatform.Modules.Customer.Domain;
using AdminPlatform.SharedKernel;
using CustomerEntity = AdminPlatform.Modules.Customer.Domain.Customer;

namespace AdminPlatform.UnitTests.Customer;

public class CustomerTests
{
    [Fact]
    public void Create_normalizes_email_and_trims_fields()
    {
        var customer = CustomerEntity.Create(" Jane Doe ", " 0901234567 ", " Jane@Example.COM ");

        Assert.Equal("Jane Doe", customer.FullName);
        Assert.Equal("0901234567", customer.Phone);
        Assert.Equal("jane@example.com", customer.Email);
        Assert.Equal(CustomerStatus.Active, customer.Status);
        Assert.StartsWith("KH", customer.CustomerCode);
    }

    [Fact]
    public void Create_allows_a_null_phone_for_Google_first_signup()
    {
        var customer = CustomerEntity.Create("Google Customer", phone: null, email: "g@example.com");

        Assert.Null(customer.Phone);
    }

    [Fact]
    public void Create_rejects_empty_full_name()
    {
        Assert.Throws<BusinessRuleValidationException>(() => CustomerEntity.Create("", "0901234567", "a@b.com"));
    }

    [Fact]
    public void UpdateProfile_replaces_profile_fields()
    {
        var customer = CustomerEntity.Create("Jane", "0901234567", "jane@example.com");
        var dob = new DateOnly(1995, 5, 20);

        customer.UpdateProfile("Jane Updated", "0909999999", "new@example.com", dob, CustomerGender.Female);

        Assert.Equal("Jane Updated", customer.FullName);
        Assert.Equal("0909999999", customer.Phone);
        Assert.Equal("new@example.com", customer.Email);
        Assert.Equal(dob, customer.DateOfBirth);
        Assert.Equal(CustomerGender.Female, customer.Gender);
    }

    [Fact]
    public void UpdateAvatar_clears_when_blank()
    {
        var customer = CustomerEntity.Create("Jane", "0901234567", "jane@example.com");
        customer.UpdateAvatar("media-1");
        Assert.Equal("media-1", customer.AvatarMediaId);

        customer.UpdateAvatar("  ");
        Assert.Null(customer.AvatarMediaId);
    }

    [Fact]
    public void Deactivate_then_Activate_round_trips()
    {
        var customer = CustomerEntity.Create("Jane", "0901234567", "jane@example.com");

        customer.Deactivate();
        Assert.Equal(CustomerStatus.Inactive, customer.Status);

        customer.Activate();
        Assert.Equal(CustomerStatus.Active, customer.Status);
    }
}
