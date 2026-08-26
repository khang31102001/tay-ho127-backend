namespace AdminPlatform.SharedKernel;

/// <summary>
/// Base for entities that track who created/modified them and support
/// Postgres-native optimistic concurrency via the `xmin` system column.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Mapped to Postgres `xmin` via IsRowVersion() — not set manually.</summary>
    public uint RowVersion { get; set; }
}
