using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Sales.Domain;

public enum OptionSelectionType
{
    /// <summary>Customer picks exactly one value (radio).</summary>
    Single,

    /// <summary>Customer picks any number of values (checkbox).</summary>
    Multiple,
}

/// <summary>A choice that applies to the WHOLE order (e.g. fish sauce, herbs), unlike a product modifier
/// that belongs to one dish. The surcharge of a picked value is added once to the order, never multiplied
/// by a quantity. Values keep their ids when edited — order snapshots refer to them.</summary>
public sealed class OrderOptionGroup : AuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxValueCount = 50;

    private readonly List<OrderOptionValue> _values = [];

    public string Name { get; private set; } = string.Empty;
    public OptionSelectionType SelectionType { get; private set; }
    public bool IsRequired { get; private set; }
    public IReadOnlyList<OrderOptionValue> Values => _values;

    private OrderOptionGroup()
    {
        // EF Core
    }

    public static OrderOptionGroup Create(string name, OptionSelectionType selectionType, bool isRequired, IReadOnlyList<OrderOptionValueInput> values)
    {
        var group = new OrderOptionGroup { Id = Guid.NewGuid() };
        group.Update(name, selectionType, isRequired, values);
        return group;
    }

    /// <summary>Replaces the value list. An input carrying the id of an existing value updates it in place.</summary>
    public void Update(string name, OptionSelectionType selectionType, bool isRequired, IReadOnlyList<OrderOptionValueInput> values)
    {
        if (values.Count == 0)
        {
            throw new BusinessRuleValidationException("An order option group needs at least one value.");
        }

        if (values.Count > MaxValueCount)
        {
            throw new BusinessRuleValidationException($"An order option group can have at most {MaxValueCount} values.");
        }

        if (selectionType == OptionSelectionType.Single && values.Count(v => v.IsDefault) > 1)
        {
            throw new BusinessRuleValidationException("A single-choice group can have at most one default value.");
        }

        var keptIds = values.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
        if (keptIds.Count != values.Count(v => v.Id.HasValue))
        {
            throw new BusinessRuleValidationException("Value ids must be unique.");
        }

        if (keptIds.Any(id => _values.All(v => v.Id != id)))
        {
            throw new BusinessRuleValidationException("A value id does not belong to this group.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        SelectionType = selectionType;
        IsRequired = isRequired;

        _values.RemoveAll(v => !keptIds.Contains(v.Id));
        for (var index = 0; index < values.Count; index++)
        {
            var input = values[index];
            var existing = input.Id is { } id ? _values.Find(v => v.Id == id) : null;
            if (existing is null)
            {
                _values.Add(OrderOptionValue.Create(Id, input.Label, input.PriceAdjustment, input.IsDefault, index));
            }
            else
            {
                existing.Update(input.Label, input.PriceAdjustment, input.IsDefault, index);
            }
        }
    }
}

public sealed class OrderOptionValue : Entity
{
    public Guid OrderOptionGroupId { get; private set; }
    public string Label { get; private set; } = string.Empty;

    /// <summary>Added once to the order subtotal when picked.</summary>
    public decimal PriceAdjustment { get; private set; }

    public bool IsDefault { get; private set; }
    public int SortOrder { get; private set; }

    private OrderOptionValue()
    {
        // EF Core
    }

    internal static OrderOptionValue Create(Guid groupId, string label, decimal priceAdjustment, bool isDefault, int sortOrder)
    {
        var value = new OrderOptionValue { Id = Guid.NewGuid(), OrderOptionGroupId = groupId };
        value.Update(label, priceAdjustment, isDefault, sortOrder);
        return value;
    }

    internal void Update(string label, decimal priceAdjustment, bool isDefault, int sortOrder)
    {
        Label = Guard.NotNullOrWhiteSpace(label, nameof(label)).Trim();
        PriceAdjustment = priceAdjustment;
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }
}

/// <summary>One value as sent by the editor; Id is null for a newly added value.</summary>
public sealed record OrderOptionValueInput(Guid? Id, string Label, decimal PriceAdjustment, bool IsDefault);
