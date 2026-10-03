using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application;

/// <summary>Slug rules shared by every Content entity. An explicit slug must be free (409 otherwise); a
/// generated one gets a numeric suffix until it is unique ("tin-tuc", "tin-tuc-2", ...).</summary>
internal static class SlugResolver
{
    /// <param name="otherSlugs">The slugs of every OTHER row of the entity (the one being edited excluded).</param>
    public static async Task<string> ResolveAsync(
        string? requestedSlug, string name, string entityLabel, IQueryable<string> otherSlugs, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedSlug))
        {
            var slug = requestedSlug.Trim();
            if (await otherSlugs.AnyAsync(existing => existing == slug, cancellationToken))
            {
                throw new ConflictException($"Another {entityLabel} already uses the slug '{slug}'.");
            }

            return slug;
        }

        var baseSlug = Slug.FromText(name);
        if (baseSlug.Length == 0)
        {
            throw new BusinessRuleValidationException("A slug cannot be generated from this name; provide one explicitly.");
        }

        var taken = (await otherSlugs
                .Where(existing => existing == baseSlug || existing.StartsWith(baseSlug + "-"))
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var candidate = baseSlug;
        for (var suffix = 2; taken.Contains(candidate); suffix++)
        {
            candidate = $"{baseSlug}-{suffix}";
        }

        return candidate;
    }
}
