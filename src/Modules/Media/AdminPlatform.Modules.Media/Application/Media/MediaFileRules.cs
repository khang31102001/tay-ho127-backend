using AdminPlatform.Modules.Media.Domain;

namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Các loại file được phép upload. Kiểm tra cả đuôi file lẫn chữ ký (magic bytes) ở đầu nội dung,
/// để không tin vào đuôi/Content-Type do client tự khai.</summary>
public static class MediaFileRules
{
    private sealed record Rule(MediaKind Kind, Func<byte[], bool> HasValidSignature);

    private static readonly Dictionary<string, Rule> RulesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new(MediaKind.Image, h => StartsWith(h, 0xFF, 0xD8, 0xFF)),
        [".jpeg"] = new(MediaKind.Image, h => StartsWith(h, 0xFF, 0xD8, 0xFF)),
        [".png"] = new(MediaKind.Image, h => StartsWith(h, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)),
        [".gif"] = new(MediaKind.Image, h => StartsWith(h, (byte)'G', (byte)'I', (byte)'F', (byte)'8')),
        [".webp"] = new(MediaKind.Image, h => StartsWith(h, (byte)'R', (byte)'I', (byte)'F', (byte)'F') && h.Length >= 12 && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P'),
        [".mp4"] = new(MediaKind.Video, h => h.Length >= 8 && h[4] == 'f' && h[5] == 't' && h[6] == 'y' && h[7] == 'p'),
        [".webm"] = new(MediaKind.Video, h => StartsWith(h, 0x1A, 0x45, 0xDF, 0xA3)),
        [".pdf"] = new(MediaKind.Document, h => StartsWith(h, (byte)'%', (byte)'P', (byte)'D', (byte)'F')),
    };

    public const int SignatureLength = 16;

    public static string AllowedExtensionsText => string.Join(", ", RulesByExtension.Keys.Order());

    public static bool TryGetKind(string extension, out MediaKind kind)
    {
        if (RulesByExtension.TryGetValue(extension, out var rule))
        {
            kind = rule.Kind;
            return true;
        }

        kind = default;
        return false;
    }

    public static bool HasValidSignature(string extension, byte[] header) =>
        RulesByExtension.TryGetValue(extension, out var rule) && rule.HasValidSignature(header);

    private static bool StartsWith(byte[] header, params byte[] prefix) =>
        header.Length >= prefix.Length && header.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}
