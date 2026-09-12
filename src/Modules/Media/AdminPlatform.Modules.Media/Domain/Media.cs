using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Media.Domain;

/// <summary>Shared media library entry, referenced by id from Product, Customer, Article, Banner,
/// BrandSettings, PaymentMethod and PageSection (stored as a plain id column in those modules — no
/// cross-module ProjectReference, per ARCHITECTURE.md "How modules communicate"). This module owns only
/// the media metadata; actual file storage/upload is a separate boundary — see the module report.</summary>
public sealed class Media : AuditableEntity
{
    public string FileName { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public MediaKind Kind { get; private set; }
    public string? AltText { get; private set; }

    /// <summary>File size in bytes.</summary>
    public int Size { get; private set; }

    public MediaStatus Status { get; private set; } = MediaStatus.Active;

    private Media()
    {
        // EF Core
    }

    public static Media Create(string fileName, string url, MediaKind kind, string? altText, int size)
    {
        if (size < 0)
        {
            throw new BusinessRuleValidationException("Size cannot be negative.");
        }

        return new Media
        {
            Id = Guid.NewGuid(),
            FileName = Guard.NotNullOrWhiteSpace(fileName, nameof(fileName)).Trim(),
            Url = Guard.NotNullOrWhiteSpace(url, nameof(url)).Trim(),
            Kind = kind,
            AltText = NormalizeAltText(altText),
            Size = size,
            Status = MediaStatus.Active,
        };
    }

    public void UpdateMetadata(string fileName, string url, MediaKind kind, string? altText)
    {
        FileName = Guard.NotNullOrWhiteSpace(fileName, nameof(fileName)).Trim();
        Url = Guard.NotNullOrWhiteSpace(url, nameof(url)).Trim();
        Kind = kind;
        AltText = NormalizeAltText(altText);
    }

    public void Activate() => Status = MediaStatus.Active;

    public void Deactivate() => Status = MediaStatus.Inactive;

    private static string? NormalizeAltText(string? altText) => string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
}
