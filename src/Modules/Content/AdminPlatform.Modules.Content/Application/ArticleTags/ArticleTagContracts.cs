namespace AdminPlatform.Modules.Content.Application.ArticleTags;

/// <summary>Slug is optional: when blank it is generated from Name (and made unique).</summary>
public sealed record CreateArticleTagRequest(string Name, string? Slug);

/// <summary>A blank slug keeps the current one.</summary>
public sealed record UpdateArticleTagRequest(string Name, string? Slug);

public sealed record ArticleTagResponse(Guid Id, string Name, string Slug, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
