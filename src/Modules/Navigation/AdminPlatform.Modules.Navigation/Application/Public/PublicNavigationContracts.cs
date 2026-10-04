namespace AdminPlatform.Modules.Navigation.Application.Public;

/// <summary>One website menu node. TargetType is "route" | "page" | "external"; it is null (and Url null) for a group
/// heading. For a "page" target the website resolves the real path from TargetId (the Page id) and falls back to Url.</summary>
public sealed record PublicNavigationNode(
    Guid Id, string Label, string? Url, string? TargetType, Guid? TargetId, bool OpenInNewTab, string? Icon, int SortOrder,
    IReadOnlyList<PublicNavigationNode> Children);

public sealed record PublicNavigationResponse(string Code, string Name, string Location, IReadOnlyList<PublicNavigationNode> Items);
