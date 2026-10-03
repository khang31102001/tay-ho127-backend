using AdminPlatform.Modules.Content.Domain;

namespace AdminPlatform.Modules.Content.Application;

/// <summary>Maps <see cref="PublishStatus"/> to/from the lowercase names used on the wire
/// ("draft" | "published" | "archived").</summary>
public static class ContentWireFormat
{
    public static string ToWire(PublishStatus status) => status.ToString().ToLowerInvariant();

    public static string ToWire(SectionKind kind) => kind.ToString().ToLowerInvariant();

    public static bool TryParseSectionKind(string? value, out SectionKind kind) =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out kind) && Enum.IsDefined(kind);

    public static bool TryParseStatus(string? value, out PublishStatus status) =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out status) && Enum.IsDefined(status);
}
