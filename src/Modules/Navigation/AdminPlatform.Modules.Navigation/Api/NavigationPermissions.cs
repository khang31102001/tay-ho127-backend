namespace AdminPlatform.Modules.Navigation.Api;

public static class NavigationPermissions
{
    public const string MenusView = "menus.view";
    public const string MenusCreate = "menus.create";
    public const string MenusUpdate = "menus.update";
    public const string MenusDelete = "menus.delete";
    public const string MenusManagePermissions = "menus.permissions.manage";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (MenusView, "View menus"),
        (MenusCreate, "Create menus"),
        (MenusUpdate, "Update menus"),
        (MenusDelete, "Delete menus"),
        (MenusManagePermissions, "Assign permissions to a menu"),
    ];
}
