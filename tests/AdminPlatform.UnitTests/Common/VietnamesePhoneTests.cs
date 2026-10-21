using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Common;

public class VietnamesePhoneTests
{
    [Theory]
    [InlineData("0901234567", "0901234567")]
    [InlineData(" 0901 234 567 ", "0901234567")]
    [InlineData("+84901234567", "0901234567")]
    [InlineData("+84 901.234.567", "0901234567")]
    [InlineData("(0901)-234-567", "0901234567")]
    public void Normalize_gives_the_one_stored_form(string input, string expected)
    {
        Assert.Equal(expected, VietnamesePhone.Normalize(input));
        Assert.True(VietnamesePhone.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("090123456")]
    [InlineData("09012345678")]
    [InlineData("84901234567")]
    [InlineData("0901234abc")]
    [InlineData("+1 202 555 0100")]
    public void A_number_that_is_not_a_vietnamese_number_is_refused(string input)
    {
        Assert.Throws<BusinessRuleValidationException>(() => VietnamesePhone.Normalize(input));
        Assert.False(VietnamesePhone.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_phone_is_refused_by_Normalize_and_TryNormalize_and_is_null_when_lenient(string? input)
    {
        Assert.ThrowsAny<Exception>(() => VietnamesePhone.Normalize(input));
        Assert.False(VietnamesePhone.TryNormalize(input, out _));
        Assert.Null(VietnamesePhone.NormalizeLenient(input));
    }

    [Theory]
    [InlineData("+84 901 234 567", "0901234567")]
    [InlineData("  +1 202 555 0100  ", "+1 202 555 0100")]
    [InlineData("12345", "12345")]
    public void Lenient_normalizes_what_it_can_and_otherwise_only_trims(string input, string expected)
    {
        Assert.Equal(expected, VietnamesePhone.NormalizeLenient(input));
    }
}
