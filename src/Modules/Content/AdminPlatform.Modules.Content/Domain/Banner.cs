using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>A promotional image block on the website (desktop + optional mobile image, text and a call to
/// action). It is live when it is active and the current time is inside its optional start/end window.</summary>
public sealed class Banner : AuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxTextLength = 300;
    public const int MaxUrlLength = 500;
    public const int MaxMediaIdLength = 64;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Opaque media references with no FK (a Media module id, or a legacy key the website resolves
    /// itself) — modules never share tables (ARCHITECTURE.md). Same convention as ProductMedia.</summary>
    public string? DesktopMediaId { get; private set; }

    public string? MobileMediaId { get; private set; }

    public string AltText { get; private set; } = string.Empty;
    public string? Heading { get; private set; }
    public string? Subheading { get; private set; }
    public string? CtaLabel { get; private set; }

    /// <summary>A site-relative path or an absolute http(s) URL — see <see cref="SafeUrl"/>.</summary>
    public string? CtaUrl { get; private set; }

    public BannerPlacement Placement { get; private set; }
    public DateTime? StartAtUtc { get; private set; }
    public DateTime? EndAtUtc { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Banner()
    {
        // EF Core
    }

    public static Banner Create(BannerDetails details)
    {
        var banner = new Banner { Id = Guid.NewGuid() };
        banner.Update(details);
        return banner;
    }

    public void Update(BannerDetails details)
    {
        if (!string.IsNullOrWhiteSpace(details.CtaUrl) && !SafeUrl.IsSafe(details.CtaUrl))
        {
            throw new BusinessRuleValidationException("The call-to-action URL must be a site path (/...) or an http(s) URL.");
        }

        var startAtUtc = ToUtc(details.StartAtUtc);
        var endAtUtc = ToUtc(details.EndAtUtc);
        if (startAtUtc is { } start && endAtUtc is { } end && end <= start)
        {
            throw new BusinessRuleValidationException("The end date must be after the start date.");
        }

        Name = Guard.NotNullOrWhiteSpace(details.Name, nameof(details.Name)).Trim();
        DesktopMediaId = Clean(details.DesktopMediaId);
        MobileMediaId = Clean(details.MobileMediaId);
        AltText = Guard.NotNullOrWhiteSpace(details.AltText, nameof(details.AltText)).Trim();
        Heading = Clean(details.Heading);
        Subheading = Clean(details.Subheading);
        CtaLabel = Clean(details.CtaLabel);
        CtaUrl = Clean(details.CtaUrl);
        Placement = details.Placement;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        DisplayOrder = details.DisplayOrder;
        IsActive = details.IsActive;
    }

    /// <summary>Live = switched on and inside the optional start/end window (both ends inclusive).</summary>
    public bool IsLiveAt(DateTime nowUtc) =>
        IsActive && (StartAtUtc is not { } start || nowUtc >= start) && (EndAtUtc is not { } end || nowUtc <= end);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Unspecified dates (e.g. from a date input) are taken as UTC; Postgres timestamptz only accepts UTC.</summary>
    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } utc => utc,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
    };
}

/// <summary>The editable fields of a banner, grouped so Create/Update stay readable.</summary>
public sealed record BannerDetails(
    string Name,
    string? DesktopMediaId,
    string? MobileMediaId,
    string AltText,
    string? Heading,
    string? Subheading,
    string? CtaLabel,
    string? CtaUrl,
    BannerPlacement Placement,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int DisplayOrder,
    bool IsActive);
