namespace AdminPlatform.Modules.Content.Domain;

/// <summary>Only Published content is visible on the public website; Draft and Archived are admin-only.</summary>
public enum PublishStatus
{
    Draft,
    Published,
    Archived,
}
