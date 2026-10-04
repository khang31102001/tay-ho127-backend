namespace AdminPlatform.Modules.Navigation.Application.Containers;

/// <summary>Scope is "admin" | "site"; Location is "sidebar" | "header" | "footer" | "mobile". Both are fixed at creation.</summary>
public sealed record NavigationMenuResponse(Guid Id, string Code, string Name, bool IsActive, string Scope, string Location);

public sealed record UpdateNavigationMenuRequest(string Name, bool IsActive);
