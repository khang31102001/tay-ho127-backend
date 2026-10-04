using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>Website-only details of a link item (1 : 0..1 with <see cref="NavigationItem"/>): what it points to.
/// TargetId is a CMS Page id, kept as a plain Guid — no cross-module foreign key. Admin items and group
/// headings have no row here.</summary>
public sealed class NavigationItemSiteDetail
{
    public Guid ItemId { get; private set; }
    public NavigationTargetType TargetType { get; private set; }
    public Guid? TargetId { get; private set; }
    public bool OpenInNewTab { get; private set; }

    private NavigationItemSiteDetail()
    {
        // EF Core
    }

    public static NavigationItemSiteDetail Create(Guid itemId, NavigationTargetType targetType, Guid? targetId, bool openInNewTab)
    {
        return new NavigationItemSiteDetail
        {
            ItemId = Guard.NotEmpty(itemId, nameof(itemId)),
            TargetType = targetType,
            TargetId = targetType == NavigationTargetType.Page ? targetId : null,
            OpenInNewTab = openInNewTab,
        };
    }

    public void Update(NavigationTargetType targetType, Guid? targetId, bool openInNewTab)
    {
        TargetType = targetType;
        TargetId = targetType == NavigationTargetType.Page ? targetId : null;
        OpenInNewTab = openInNewTab;
    }
}
