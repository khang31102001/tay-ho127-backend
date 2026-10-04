using AdminPlatform.Modules.Organization.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Organization;

public class BrandBranchAndProfileTests
{
    private static BrandContact Contact(string? open = "06:00", string? close = "21:30", string? email = null) =>
        new("0900 127 127", null, email, "127 Đinh Tiên Hoàng", "Đa Kao", "Quận 1", "TP. Hồ Chí Minh", open, close, null);

    [Fact]
    public void A_branch_keeps_its_own_contact_and_hours()
    {
        var branch = Brand.Create(Guid.NewGuid(), "main", "Chi nhánh chính");

        branch.UpdateContact(Contact());

        Assert.Equal("127 Đinh Tiên Hoàng", branch.AddressLine);
        Assert.Equal("06:00", branch.OpenTime);
        Assert.Equal("21:30", branch.CloseTime);
    }

    [Theory]
    [InlineData("06:00", null)]
    [InlineData(null, "21:30")]
    [InlineData("6am", "9pm")]
    [InlineData("25:00", "21:30")]
    public void Opening_hours_come_as_a_valid_HH_mm_pair(string? open, string? close)
    {
        var branch = Brand.Create(Guid.NewGuid(), "main", "Main");

        Assert.Throws<BusinessRuleValidationException>(() => branch.UpdateContact(Contact(open, close)));
    }

    [Fact]
    public void Hours_may_be_left_out_entirely_and_a_bad_email_is_refused()
    {
        var branch = Brand.Create(Guid.NewGuid(), "main", "Main");

        branch.UpdateContact(Contact(null, null));
        Assert.Null(branch.OpenTime);
        Assert.Throws<BusinessRuleValidationException>(() => branch.UpdateContact(Contact(email: "not-an-email")));
    }

    [Fact]
    public void Only_an_active_branch_can_be_primary_and_deactivating_drops_the_flag()
    {
        var branch = Brand.Create(Guid.NewGuid(), "main", "Main");
        branch.SetPrimary(true);
        Assert.True(branch.IsPrimary);

        branch.Update("Main", isActive: false);
        Assert.False(branch.IsPrimary);
        Assert.Throws<BusinessRuleValidationException>(() => branch.SetPrimary(true));
    }

    private static BrandProfileDetails Profile(params SocialLink[] links) =>
        new("Bánh Cuốn Tây Hồ 127", "Slogan", null, null, null, links, null, null, null, null, null);

    [Fact]
    public void Profile_normalizes_and_orders_social_links()
    {
        var profile = BrandProfile.Create(Profile(
            new SocialLink("Facebook", "https://facebook.com/x", 2, true),
            new SocialLink("website", "https://tayho127.com", 1, true)));

        Assert.Equal(["website", "facebook"], profile.SocialLinks.Select(l => l.Platform));
    }

    [Theory]
    [InlineData("myspace", "https://x.com")]
    [InlineData("facebook", "javascript:alert(1)")]
    [InlineData("facebook", "not a url")]
    public void Profile_refuses_unknown_platforms_and_unsafe_links(string platform, string url)
    {
        Assert.Throws<BusinessRuleValidationException>(() => BrandProfile.Create(Profile(new SocialLink(platform, url, 1, true))));
    }

    [Fact]
    public void Profile_needs_a_name()
    {
        Assert.Throws<BusinessRuleValidationException>(() =>
            BrandProfile.Create(new BrandProfileDetails(" ", "x", null, null, null, [], null, null, null, null, null)));
    }
}
