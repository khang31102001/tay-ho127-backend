namespace AdminPlatform.Modules.Navigation.Application;

/// <summary>Pure tree algorithms over one menu's items, held in memory as an id → parent-id map. Every walk is
/// bounded so a corrupted cycle in the data cannot loop forever.</summary>
public static class NavigationTreeRules
{
    /// <summary>Deepest allowed nesting for any menu, counting a root item as level 1.</summary>
    public const int MaxDepth = 3;

    /// <summary>Length of the chain from the item up to its root; a root is level 1.</summary>
    public static int Depth(Guid id, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        var depth = 0;
        Guid? current = id;
        while (current is { } currentId && parentById.TryGetValue(currentId, out var parent) && depth <= parentById.Count)
        {
            depth++;
            current = parent;
        }

        return depth;
    }

    /// <summary>Longest chain below the item counting the item itself (a leaf item = 1).</summary>
    public static int SubtreeHeight(Guid id, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        var childrenByParent = parentById
            .Where(pair => pair.Value is not null)
            .ToLookup(pair => pair.Value!.Value, pair => pair.Key);

        var height = 1;
        var level = new List<Guid> { id };
        var visited = new HashSet<Guid> { id };

        while (true)
        {
            var next = level.SelectMany(parent => childrenByParent[parent]).Where(visited.Add).ToList();
            if (next.Count == 0)
            {
                return height;
            }

            height++;
            level = next;
        }
    }

    /// <summary>True when <paramref name="candidateId"/> is <paramref name="ancestorId"/> itself or sits below it.</summary>
    public static bool IsSelfOrDescendant(Guid candidateId, Guid ancestorId, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        Guid? current = candidateId;
        var steps = 0;
        while (current is { } currentId && steps++ <= parentById.Count)
        {
            if (currentId == ancestorId)
            {
                return true;
            }

            current = parentById.TryGetValue(currentId, out var parent) ? parent : null;
        }

        return false;
    }
}
