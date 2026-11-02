using System.Text.RegularExpressions;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Content.Domain;

/// <summary>A website page the admin manages (home, menu, landing pages). The page itself only holds its
/// identity and publication state; its content blocks are <see cref="PageSection"/> rows.</summary>
public sealed partial class Page : AuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxPathLength = 200;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Unique URL PATH of the page, not a bare slug: "/" for the home page, otherwise lowercase
    /// words joined by hyphens per segment ("/thuc-don", "/ve-chung-toi/lich-su"). The navigation module
    /// uses this value directly as the link target.</summary>
    public string Slug { get; private set; } = string.Empty;

    public PublishStatus Status { get; private set; } = PublishStatus.Draft;

    /// <summary>Stable identifier the website looks the page up by ("home", "gioi-thieu") — see <see cref="ContentKey"/>.
    /// Unique; null for a page the website does not place itself (e.g. a landing page reached only by its path).</summary>
    public string? PageKey { get; private set; }

    /// <summary>Position of the page in admin lists (ascending).</summary>
    public int SortOrder { get; private set; }

    /// <summary>Set the first time the page is published and kept afterwards (un-publishing does not erase it).</summary>
    public DateTime? PublishedAtUtc { get; private set; }

    [GeneratedRegex("^(?:/[a-z0-9]+(?:-[a-z0-9]+)*)+$")]
    private static partial Regex PathRegex();

    private Page()
    {
        // EF Core
    }

    public static bool IsValidPath(string? path) =>
        !string.IsNullOrEmpty(path) && path.Length <= MaxPathLength && (path == "/" || PathRegex().IsMatch(path));

    /// <summary>Best-effort path from a page name ("Về chúng tôi" → "/ve-chung-toi"); null when the name has no letter or digit.</summary>
    public static string? PathFromName(string name)
    {
        var slug = SharedKernel.Slug.FromText(name);
        return slug.Length == 0 ? null : "/" + slug;
    }

    public static Page Create(string name, string slug, PublishStatus status, DateTime nowUtc, string? pageKey = null, int sortOrder = 0)
    {
        var page = new Page { Id = Guid.NewGuid() };
        page.Update(name, slug, status, nowUtc);
        page.SetKeyAndOrder(pageKey, sortOrder);
        return page;
    }

    /// <summary>Sets the page key and admin list position. Uniqueness of the key is checked by the caller (and the
    /// unique index).</summary>
    public void SetKeyAndOrder(string? pageKey, int sortOrder)
    {
        var key = ContentKey.Clean(pageKey);
        if (!ContentKey.IsValid(key))
        {
            throw new BusinessRuleValidationException($"'{key}' is not a valid page key (lowercase words joined by hyphens).");
        }

        PageKey = key;
        SortOrder = sortOrder;
    }

    public void Update(string name, string slug, PublishStatus status, DateTime nowUtc)
    {
        if (!IsValidPath(slug))
        {
            throw new BusinessRuleValidationException($"'{slug}' is not a valid page path (\"/\" or lowercase /segments joined by hyphens).");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        Slug = slug;
        Status = status;

        if (status == PublishStatus.Published && PublishedAtUtc is null)
        {
            PublishedAtUtc = nowUtc;
        }
    }

    /// <summary>Seeding only: lets imported content keep the date it was originally published.</summary>
    public void SetPublishedAt(DateTime? publishedAtUtc) => PublishedAtUtc = publishedAtUtc;
}
