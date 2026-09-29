namespace AdminPlatform.Modules.Catalog.Application.ModifierGroups;

/// <summary>Options are the full, ordered list. Send an existing option's Id to keep it (its id stays
/// stable for carts/orders); omit Id for a new option; options left out are removed.</summary>
public sealed record ModifierOptionRequest(Guid? Id, string Label, decimal PriceAdjustment, bool IsDefault);

/// <summary>SelectionType: "single" or "multiple" (case-insensitive).</summary>
public sealed record CreateModifierGroupRequest(string Name, string SelectionType, bool IsRequired, IReadOnlyList<ModifierOptionRequest> Options);

public sealed record UpdateModifierGroupRequest(string Name, string SelectionType, bool IsRequired, IReadOnlyList<ModifierOptionRequest> Options);

public sealed record ModifierOptionResponse(Guid Id, string Label, decimal PriceAdjustment, bool IsDefault);

/// <summary>SelectionType is returned lowercase ("single" / "multiple").</summary>
public sealed record ModifierGroupResponse(
    Guid Id,
    string Name,
    string SelectionType,
    bool IsRequired,
    IReadOnlyList<ModifierOptionResponse> Options,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
