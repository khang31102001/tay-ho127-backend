namespace AdminPlatform.Modules.Sales.Application.OrderOptions;

/// <summary>Values are the full, ordered list. Send an existing value's Id to keep it (its id stays stable for
/// order snapshots); omit Id for a new value; values left out are removed. The surcharge of a picked value is
/// added once to the whole order.</summary>
public sealed record OrderOptionValueRequest(Guid? Id, string Label, decimal PriceAdjustment, bool IsDefault);

/// <summary>SelectionType: "single" or "multiple" (case-insensitive).</summary>
public sealed record CreateOrderOptionGroupRequest(string Name, string SelectionType, bool IsRequired, IReadOnlyList<OrderOptionValueRequest> Options);

public sealed record UpdateOrderOptionGroupRequest(string Name, string SelectionType, bool IsRequired, IReadOnlyList<OrderOptionValueRequest> Options);

public sealed record OrderOptionValueResponse(Guid Id, string Label, decimal PriceAdjustment, bool IsDefault);

/// <summary>Same shape for the admin and for the public checkout (there is nothing internal in a group).</summary>
public sealed record OrderOptionGroupResponse(
    Guid Id,
    string Name,
    string SelectionType,
    bool IsRequired,
    IReadOnlyList<OrderOptionValueResponse> Options,
    DateTime CreatedAt,
    DateTime UpdatedAt);
