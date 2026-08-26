using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Navigation.Domain;

/// <summary>Self-referencing tree via ParentId, with Route/Icon/SortOrder for rendering an admin sidebar.</summary>
public sealed class Menu : CatalogEntity
{
    public Guid? ParentId { get; private set; }
    public string? Route { get; private set; }
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }

    private Menu()
    {
        // EF Core
    }

    public static Menu Create(string code, string name, Guid? parentId, string? route, string? icon, int sortOrder)
    {
        return new Menu
        {
            Id = Guid.NewGuid(),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            ParentId = parentId,
            Route = route,
            Icon = icon,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive, Guid? parentId, string? route, string? icon, int sortOrder)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
        ParentId = parentId;
        Route = route;
        Icon = icon;
        SortOrder = sortOrder;
    }
}
