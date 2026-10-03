namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>Admin-chosen status. "Expired" is not stored: it is derived from the end date
/// (see <see cref="Promotion.IsExpired"/>), so an admin never has to flip it by hand.</summary>
public enum PromotionStatus
{
    Draft,
    Active,
    Inactive,
}
