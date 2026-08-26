using AdminPlatform.Modules.Identity.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Identity;

public class UserTests
{
    [Fact]
    public void Create_normalizes_email_to_lowercase_and_trims()
    {
        var user = User.Create("  Someone@Example.COM ", "hash", " Someone ");

        Assert.Equal("someone@example.com", user.Email);
        Assert.Equal("Someone", user.FullName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Create_rejects_empty_password_hash()
    {
        Assert.Throws<BusinessRuleValidationException>(() => User.Create("a@b.com", "", "A"));
    }

    [Fact]
    public void Deactivate_then_Activate_round_trips()
    {
        var user = User.Create("a@b.com", "hash", "A");

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void SetWorkingContext_stores_both_ids()
    {
        var user = User.Create("a@b.com", "hash", "A");
        var brandId = Guid.NewGuid();
        var fiscalYearId = Guid.NewGuid();

        user.SetWorkingContext(brandId, fiscalYearId);

        Assert.Equal(brandId, user.CurrentBrandId);
        Assert.Equal(fiscalYearId, user.CurrentFiscalYearId);
    }
}
