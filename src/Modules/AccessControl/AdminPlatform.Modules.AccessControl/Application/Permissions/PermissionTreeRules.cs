namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

/// <summary>A permission row reduced to what the tree rules need.</summary>
public sealed record PermissionNodeInfo(Guid Id, Guid? ParentId, bool IsGroup, bool IsActive);

/// <summary>Pure tree algorithms over the (small, ~100 rows) permission table, loaded fully into memory.
/// Every walk is bounded so a corrupted cycle in the data cannot loop forever.</summary>
public static class PermissionTreeRules
{
    /// <summary>Length of the chain from the node up to its root; a root is depth 1.</summary>
    public static int Depth(Guid id, IReadOnlyDictionary<Guid, PermissionNodeInfo> byId)
    {
        var depth = 0;
        Guid? current = id;
        while (current is { } currentId && byId.TryGetValue(currentId, out var node) && depth <= byId.Count)
        {
            depth++;
            current = node.ParentId;
        }

        return depth;
    }

    /// <summary>Longest chain below the node counting the node itself (a node without children = 1).</summary>
    public static int SubtreeHeight(Guid id, ILookup<Guid, PermissionNodeInfo> childrenByParent)
    {
        var height = 1;
        var level = new List<Guid> { id };
        var visited = new HashSet<Guid> { id };

        while (true)
        {
            var next = level
                .SelectMany(parent => childrenByParent[parent])
                .Where(child => visited.Add(child.Id))
                .Select(child => child.Id)
                .ToList();
            if (next.Count == 0)
            {
                return height;
            }

            height++;
            level = next;
        }
    }

    /// <summary>True when <paramref name="candidateId"/> is <paramref name="ancestorId"/> itself or sits below it.</summary>
    public static bool IsSelfOrDescendant(Guid candidateId, Guid ancestorId, IReadOnlyDictionary<Guid, PermissionNodeInfo> byId)
    {
        Guid? current = candidateId;
        var steps = 0;
        while (current is { } currentId && steps++ <= byId.Count)
        {
            if (currentId == ancestorId)
            {
                return true;
            }

            current = byId.TryGetValue(currentId, out var node) ? node.ParentId : null;
        }

        return false;
    }

    /// <summary>Every ACTIVE leaf below the given groups, at any depth.</summary>
    public static HashSet<Guid> ActiveLeavesUnder(IEnumerable<Guid> groupIds, IReadOnlyCollection<PermissionNodeInfo> all)
    {
        var childrenByParent = all.Where(n => n.ParentId is not null).ToLookup(n => n.ParentId!.Value);
        var leaves = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        var pending = new Queue<Guid>(groupIds);

        while (pending.Count > 0)
        {
            var parentId = pending.Dequeue();
            if (!visited.Add(parentId))
            {
                continue;
            }

            foreach (var child in childrenByParent[parentId])
            {
                if (child.IsGroup)
                {
                    pending.Enqueue(child.Id);
                }
                else if (child.IsActive)
                {
                    leaves.Add(child.Id);
                }
            }
        }

        return leaves;
    }
}
