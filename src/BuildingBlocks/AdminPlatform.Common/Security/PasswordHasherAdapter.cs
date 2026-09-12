using Microsoft.AspNetCore.Identity;

namespace AdminPlatform.Common.Security;

/// <summary>Thin wrapper over ASP.NET Core Identity's battle-tested PBKDF2 hasher (Microsoft.Extensions.Identity.Core)
/// — reused rather than reinventing password hashing, without pulling in the full Identity/UserManager stack.
/// Lives in Common (not a specific module) so every module that needs local-credential hashing (Identity's
/// admin users, Customer's local accounts, ...) shares one reviewed implementation instead of duplicating it.</summary>
internal sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string hash, string providedPassword) =>
        _inner.VerifyHashedPassword(null!, hash, providedPassword) != PasswordVerificationResult.Failed;
}
