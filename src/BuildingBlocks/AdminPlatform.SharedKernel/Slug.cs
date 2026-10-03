using System.Text;
using System.Text.RegularExpressions;

namespace AdminPlatform.SharedKernel;

/// <summary>URL slug rules for public pages (/thuc-don/{slug}, /bai-viet/{slug}): lowercase ASCII words joined by
/// single hyphens. Vietnamese text is transliterated ("Bánh cuốn đặc biệt" → "banh-cuon-dac-biet").</summary>
public static partial class Slug
{
    public const int MaxLength = 200;

    /// <summary>Explicit Vietnamese → ASCII map. Unicode decomposition (string.Normalize) is not used on
    /// purpose: the app runs with InvariantGlobalization, where normalization leaves "á" untouched.</summary>
    private static readonly Dictionary<char, char> VietnameseToAscii = BuildVietnameseMap();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex ValidSlugRegex();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharactersRegex();

    public static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxLength && ValidSlugRegex().IsMatch(value);

    /// <summary>Best-effort slug from free text; empty when the text has no letter or digit.</summary>
    public static string FromText(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Trim().ToLowerInvariant())
        {
            builder.Append(VietnameseToAscii.GetValueOrDefault(character, character));
        }

        var slug = NonSlugCharactersRegex().Replace(builder.ToString(), "-").Trim('-');
        return slug.Length <= MaxLength ? slug : slug[..MaxLength].TrimEnd('-');
    }

    private static Dictionary<char, char> BuildVietnameseMap()
    {
        var groups = new Dictionary<char, string>
        {
            ['a'] = "àáảãạăằắẳẵặâầấẩẫậ",
            ['e'] = "èéẻẽẹêềếểễệ",
            ['i'] = "ìíỉĩị",
            ['o'] = "òóỏõọôồốổỗộơờớởỡợ",
            ['u'] = "ùúủũụưừứửữự",
            ['y'] = "ỳýỷỹỵ",
            ['d'] = "đ",
        };

        var map = new Dictionary<char, char>();
        foreach (var (ascii, accented) in groups)
        {
            foreach (var character in accented)
            {
                map[character] = ascii;
            }
        }

        return map;
    }
}
