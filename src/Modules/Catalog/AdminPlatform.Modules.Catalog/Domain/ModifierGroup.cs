using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>A product-level choice such as "Nước mắm" (spice level) or "Rau" (herbs), with its options.
/// Options are only ever edited together with their group, so the group owns them.</summary>
public sealed class ModifierGroup : AuditableEntity
{
    public const int MaxOptionCount = 50;

    private readonly List<ModifierOption> _options = [];

    public string Name { get; private set; } = string.Empty;
    public ModifierSelectionType SelectionType { get; private set; }

    /// <summary>A multiple-choice group that is required needs at least one option picked; a single-choice
    /// group with a default option always satisfies it.</summary>
    public bool IsRequired { get; private set; }

    public IReadOnlyList<ModifierOption> Options => _options;

    private ModifierGroup()
    {
        // EF Core
    }

    public static ModifierGroup Create(string name, ModifierSelectionType selectionType, bool isRequired, IReadOnlyList<ModifierOptionInput> options)
    {
        var group = new ModifierGroup { Id = Guid.NewGuid() };
        group.Update(name, selectionType, isRequired, options);
        return group;
    }

    /// <summary>Replaces the option list. An input carrying the id of an existing option updates it in
    /// place, keeping its id stable — carts and order snapshots reference option ids.</summary>
    public void Update(string name, ModifierSelectionType selectionType, bool isRequired, IReadOnlyList<ModifierOptionInput> options)
    {
        if (options.Count == 0)
        {
            throw new BusinessRuleValidationException("A modifier group needs at least one option.");
        }

        if (options.Count > MaxOptionCount)
        {
            throw new BusinessRuleValidationException($"A modifier group can have at most {MaxOptionCount} options.");
        }

        if (selectionType == ModifierSelectionType.Single && options.Count(o => o.IsDefault) > 1)
        {
            throw new BusinessRuleValidationException("A single-choice group can have at most one default option.");
        }

        var keptIds = options.Where(o => o.Id.HasValue).Select(o => o.Id!.Value).ToHashSet();
        if (keptIds.Count != options.Count(o => o.Id.HasValue))
        {
            throw new BusinessRuleValidationException("Option ids must be unique.");
        }

        if (keptIds.Any(id => _options.All(o => o.Id != id)))
        {
            throw new BusinessRuleValidationException("An option id does not belong to this modifier group.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        SelectionType = selectionType;
        IsRequired = isRequired;

        _options.RemoveAll(o => !keptIds.Contains(o.Id));
        for (var index = 0; index < options.Count; index++)
        {
            var input = options[index];
            var existing = input.Id is { } id ? _options.Find(o => o.Id == id) : null;
            if (existing is null)
            {
                _options.Add(ModifierOption.Create(Id, input.Label, input.PriceAdjustment, input.IsDefault, index));
            }
            else
            {
                existing.Update(input.Label, input.PriceAdjustment, input.IsDefault, index);
            }
        }
    }
}

/// <summary>One option as sent by the editor; Id is null for a newly added option.</summary>
public sealed record ModifierOptionInput(Guid? Id, string Label, decimal PriceAdjustment, bool IsDefault);
