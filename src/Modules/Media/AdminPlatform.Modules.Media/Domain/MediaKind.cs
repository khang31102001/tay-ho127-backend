namespace AdminPlatform.Modules.Media.Domain;

/// <summary>Matches the FE's MEDIA_TYPE_OPTIONS values (frontend/src/features/media/types/media.types.ts).
/// The FE field name is "type" — see MediaContracts.cs for the API-boundary mapping to this enum.</summary>
public enum MediaKind
{
    Image,
    Video,
    Document,
}
