using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A menu that sells products (e.g. the main website menu, "favourites" carousel, cross-sell list).
/// Code is the stable identity the website looks menus up by, so it cannot change after creation.</summary>
public sealed partial class SalesMenu : AuditableEntity
{
    public const int MaxCodeLength = 64;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex CodeRegex();

    private SalesMenu()
    {
        // EF Core
    }

    public static bool IsValidCode(string? code) =>
        !string.IsNullOrEmpty(code) && code.Length <= MaxCodeLength && CodeRegex().IsMatch(code);

    public static SalesMenu Create(string code, string name, bool isActive)
    {
        if (!IsValidCode(code))
        {
            throw new BusinessRuleValidationException($"'{code}' is not a valid menu code (lowercase letters, digits and hyphens).");
        }

        return new SalesMenu
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            IsActive = isActive,
        };
    }

    public void Update(string name, bool isActive)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
    }
}
