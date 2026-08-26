using AdminPlatform.Modules.Identity.Application.Users;

namespace AdminPlatform.UnitTests.Identity;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new CreateUserRequest("user@example.com", "Jane Doe", "S3curePassw0rd"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Short_password_fails()
    {
        var result = _validator.Validate(new CreateUserRequest("user@example.com", "Jane Doe", "short"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Fact]
    public void Missing_full_name_fails()
    {
        var result = _validator.Validate(new CreateUserRequest("user@example.com", "", "S3curePassw0rd"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequest.FullName));
    }
}
