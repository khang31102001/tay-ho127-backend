using AdminPlatform.Modules.Identity.Application.Auth;

namespace AdminPlatform.UnitTests.Identity;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new LoginRequest("user@example.com", "correct-horse", null));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("user@example.com", "")]
    public void Invalid_request_fails(string email, string password)
    {
        var result = _validator.Validate(new LoginRequest(email, password, null));
        Assert.False(result.IsValid);
    }
}
