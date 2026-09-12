namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Request/response property is named "Type", not "Kind": the FE contract
/// (frontend/src/features/media/types/media.types.ts, ManagedMedia.type) already depends on this field
/// name. The domain entity keeps the business name "Kind" (see database.dbml note) — mapping between the
/// two happens in MediaService's ToResponse/create/update calls, never by renaming the domain field.</summary>
public sealed record CreateMediaRequest(
    string FileName,
    string Url,
    string Type,
    string? AltText,
    int Size);

public sealed record UpdateMediaRequest(
    string FileName,
    string Url,
    string Type,
    string? AltText);

public sealed record MediaResponse(
    Guid Id,
    string FileName,
    string Url,
    string Type,
    string? AltText,
    int Size,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
