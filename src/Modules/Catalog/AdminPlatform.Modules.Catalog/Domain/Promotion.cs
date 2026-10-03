using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A discount code the customer enters at checkout. Business rules that depend only on the
/// promotion itself live here; the ones that need the cart (discount amounts) live in
/// PromotionValidationService. <see cref="UsageCount"/> is read-only for now: it is incremented by the
/// Orders module once it exists.</summary>
public sealed partial class Promotion : AuditableEntity
{
    public const int MaxCodeLength = 64;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;

    private readonly List<PromotionProduct> _products = [];
    private readonly List<PromotionCategory> _categories = [];

    /// <summary>Upper-case, unique, the identity customers type at checkout.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PromotionType Type { get; private set; }
    public decimal Value { get; private set; }

    /// <summary>Cap on the discount amount; only meaningful for percentage-based types.</summary>
    public decimal? MaxDiscountAmount { get; private set; }

    public decimal? MinimumOrderAmount { get; private set; }
    public DateTime? StartAtUtc { get; private set; }
    public DateTime? EndAtUtc { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }
    public PromotionStatus Status { get; private set; } = PromotionStatus.Draft;

    public IReadOnlyList<PromotionProduct> Products => _products;
    public IReadOnlyList<PromotionCategory> Categories => _categories;

    [GeneratedRegex("^[A-Z0-9_-]+$")]
    private static partial Regex CodeRegex();

    private Promotion()
    {
        // EF Core
    }

    public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsValidCode(string? code)
    {
        var normalized = NormalizeCode(code);
        return normalized.Length is > 0 and <= MaxCodeLength && CodeRegex().IsMatch(normalized);
    }

    public static Promotion Create(PromotionDetails details)
    {
        var promotion = new Promotion { Id = Guid.NewGuid() };
        promotion.Update(details);
        return promotion;
    }

    public void Update(PromotionDetails details)
    {
        if (!IsValidCode(details.Code))
        {
            throw new BusinessRuleValidationException($"'{details.Code}' is not a valid promotion code (letters, digits, '-' and '_').");
        }

        if (details.Type == PromotionType.FixedAmount ? details.Value <= 0 : details.Value is <= 0 or > 100)
        {
            throw new BusinessRuleValidationException(details.Type == PromotionType.FixedAmount
                ? "A fixed-amount promotion needs a value greater than 0."
                : "A percentage-based promotion needs a value greater than 0 and at most 100.");
        }

        if (details.MaxDiscountAmount < 0 || details.MinimumOrderAmount < 0)
        {
            throw new BusinessRuleValidationException("Amounts cannot be negative.");
        }

        if (details.UsageLimit < 1)
        {
            throw new BusinessRuleValidationException("The usage limit must be at least 1.");
        }

        var startAtUtc = ToUtc(details.StartAtUtc);
        var endAtUtc = ToUtc(details.EndAtUtc);
        if (startAtUtc is { } start && endAtUtc is { } end && end <= start)
        {
            throw new BusinessRuleValidationException("The end date must be after the start date.");
        }

        Code = NormalizeCode(details.Code);
        Name = Guard.NotNullOrWhiteSpace(details.Name, nameof(details.Name)).Trim();
        Description = string.IsNullOrWhiteSpace(details.Description) ? null : details.Description.Trim();
        Type = details.Type;
        Value = details.Value;
        MaxDiscountAmount = details.MaxDiscountAmount;
        MinimumOrderAmount = details.MinimumOrderAmount;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        UsageLimit = details.UsageLimit;
        Status = details.Status;
    }

    /// <summary>Replaces the product scope. Links still present keep their row so EF never deletes and
    /// re-inserts the same composite key in one save.</summary>
    public void ReplaceProducts(IReadOnlyCollection<Guid> productIds)
    {
        var ids = productIds.Distinct().ToList();

        _products.RemoveAll(link => !ids.Contains(link.ProductId));
        foreach (var id in ids.Where(id => !_products.Exists(link => link.ProductId == id)))
        {
            _products.Add(PromotionProduct.Create(Id, id));
        }
    }

    /// <summary>Same keep-or-add strategy as <see cref="ReplaceProducts"/>.</summary>
    public void ReplaceCategories(IReadOnlyCollection<Guid> categoryIds)
    {
        var ids = categoryIds.Distinct().ToList();

        _categories.RemoveAll(link => !ids.Contains(link.CategoryId));
        foreach (var id in ids.Where(id => !_categories.Exists(link => link.CategoryId == id)))
        {
            _categories.Add(PromotionCategory.Create(Id, id));
        }
    }

    public bool IsExpired(DateTime nowUtc) => EndAtUtc is { } end && nowUtc > end;

    public bool HasStarted(DateTime nowUtc) => StartAtUtc is not { } start || nowUtc >= start;

    public bool IsUsageExhausted => UsageLimit is { } limit && UsageCount >= limit;

    /// <summary>Unspecified dates (e.g. from a datetime-local input) are taken as UTC; Postgres
    /// timestamptz only accepts UTC values.</summary>
    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } utc => utc,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
    };
}

/// <summary>The editable scalar fields of a promotion, grouped so Create/Update stay readable.</summary>
public sealed record PromotionDetails(
    string Code,
    string Name,
    string? Description,
    PromotionType Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimit,
    PromotionStatus Status);
