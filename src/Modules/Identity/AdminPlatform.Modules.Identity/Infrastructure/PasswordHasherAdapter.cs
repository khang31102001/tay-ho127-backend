using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;

namespace AdminPlatform.Modules.Identity.Infrastructure;

/// <summary>Thin wrapper over ASP.NET Core Identity's battle-tested PBKDF2 hasher (Microsoft.Extensions.Identity.Core)
/// — reused rather than reinventing password hashing, without pulling in the full Identity/UserManager stack.</summary>
internal sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string hash, string providedPassword) =>
        _inner.VerifyHashedPassword(null!, hash, providedPassword) != PasswordVerificationResult.Failed;
}
