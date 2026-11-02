namespace AdminPlatform.Modules.Content.Application.Pages;

/// <summary>Slug is the page's URL PATH ("/" for home, otherwise "/thuc-don", "/ve-chung-toi/lich-su"). When blank on
/// create it is generated from Name (and made unique); a blank slug on update keeps the current one.
/// Status: "draft" | "published" | "archived"; PublishedAt is set automatically the first time the page is published.
/// PageKey is the stable identifier the website looks the page up by ("home", "gioi-thieu"), unique; SortOrder is the
/// position in admin lists. On update a null PageKey / SortOrder keeps the current value (the key is an identity the
/// website depends on, so it is never cleared by a client that does not know it).</summary>
public sealed record CreatePageRequest(string Name, string? Slug, string Status, string? PageKey = null, int SortOrder = 0);

public sealed record UpdatePageRequest(string Name, string? Slug, string Status, string? PageKey = null, int? SortOrder = null);

public sealed record PageResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? PageKey,
    int SortOrder);
