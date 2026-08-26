using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Application.Users;
using AdminPlatform.Modules.Identity.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Identity;

file sealed class PassthroughPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string hash, string providedPassword) => hash == $"hashed:{providedPassword}";
}

file sealed class AllowAllUserScopeValidator : IUserScopeValidator
{
    public Task<bool> HasBrandAccessAsync(Guid userId, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<bool> HasFiscalYearAccessAsync(Guid userId, Guid fiscalYearId, CancellationToken cancellationToken) => Task.FromResult(true);
}

public class UserServiceTests
{
    private static IIdentityDbContext NewDb() =>
        new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserService NewSut(IIdentityDbContext db) => new(db, new PassthroughPasswordHasher(), new AllowAllUserScopeValidator());

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_email()
    {
        var db = NewDb();
        var sut = NewSut(db);
        var request = new CreateUserRequest("dup@example.com", "First User", "S3curePassw0rd");
        await sut.CreateAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.CreateAsync(request with { FullName = "Second User" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_normalizes_email_case_for_the_duplicate_check()
    {
        var db = NewDb();
        var sut = NewSut(db);
        await sut.CreateAsync(new CreateUserRequest("Dup@Example.com", "First", "S3curePassw0rd"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.CreateAsync(new CreateUserRequest("dup@example.com", "Second", "S3curePassw0rd"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_toggles_active_status()
    {
        var db = NewDb();
        var sut = NewSut(db);
        var created = await sut.CreateAsync(new CreateUserRequest("a@b.com", "A", "S3curePassw0rd"), CancellationToken.None);

        var updated = await sut.UpdateAsync(created.Id, new UpdateUserRequest("A Renamed", false), CancellationToken.None);

        Assert.False(updated.IsActive);
        Assert.Equal("A Renamed", updated.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_for_unknown_id()
    {
        var sut = NewSut(NewDb());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_rejects_a_wrong_current_password()
    {
        var db = NewDb();
        var sut = NewSut(db);
        var created = await sut.CreateAsync(new CreateUserRequest("a@b.com", "A", "S3curePassw0rd"), CancellationToken.None);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => sut.ChangePasswordAsync(created.Id, new ChangePasswordRequest("wrong-password", "N3wPassw0rd"), CancellationToken.None));
    }
}
