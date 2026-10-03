namespace AdminPlatform.Modules.Content.Application.Pages;

/// <summary>Slug is the page's URL PATH ("/" for home, otherwise "/thuc-don", "/ve-chung-toi/lich-su"). When blank on
/// create it is generated from Name (and made unique); a blank slug on update keeps the current one.
/// Status: "draft" | "published" | "archived"; PublishedAt is set automatically the first time the page is published.</summary>
public sealed record CreatePageRequest(string Name, string? Slug, string Status);

public sealed record UpdatePageRequest(string Name, string? Slug, string Status);

public sealed record PageResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
