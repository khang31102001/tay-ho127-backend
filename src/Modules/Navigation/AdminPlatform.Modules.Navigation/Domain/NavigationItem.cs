using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>One node of a navigation tree (self-referencing via ParentId). Shared by the admin sidebar and the
/// website menus — what only the website needs (target type, page id, open-in-new-tab) lives in
/// <see cref="NavigationItemSiteDetail"/>. A group (IsGroup) is a heading without a link; Name is the label shown.</summary>
public sealed class NavigationItem : CatalogEntity
{
    public Guid MenuId { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsGroup { get; private set; }
    public string? Url { get; private set; }
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }

    private NavigationItem()
    {
        // EF Core
    }

    public static NavigationItem Create(
        Guid menuId, string code, string label, Guid? parentId, bool isGroup, string? url, string? icon, int sortOrder)
    {
        return new NavigationItem
        {
            Id = Guid.NewGuid(),
            MenuId = Guard.NotEmpty(menuId, nameof(menuId)),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(label, nameof(label)).Trim(),
            ParentId = parentId,
            IsGroup = isGroup,
            Url = Normalize(url),
            Icon = Normalize(icon),
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    public void Update(string label, bool isActive, Guid? parentId, bool isGroup, string? url, string? icon, int sortOrder)
    {
        Name = Guard.NotNullOrWhiteSpace(label, nameof(label)).Trim();
        IsActive = isActive;
        ParentId = parentId;
        IsGroup = isGroup;
        Url = Normalize(url);
        Icon = Normalize(icon);
        SortOrder = sortOrder;
    }

    /// <summary>Moves the item within the tree. Callers validate parent/cycle/depth rules first.</summary>
    public void Place(Guid? parentId, int sortOrder)
    {
        ParentId = parentId;
        SortOrder = sortOrder;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
