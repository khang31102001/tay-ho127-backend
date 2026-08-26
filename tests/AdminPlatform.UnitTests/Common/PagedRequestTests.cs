using AdminPlatform.Common.Pagination;

namespace AdminPlatform.UnitTests.Common;

public class PagedRequestTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Page_clamps_to_at_least_one(int input, int expected)
    {
        var request = new PagedRequest { Page = input };
        Assert.Equal(expected, request.Page);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(500, 200)]
    [InlineData(50, 50)]
    public void PageSize_clamps_between_default_and_max(int input, int expected)
    {
        var request = new PagedRequest { PageSize = input };
        Assert.Equal(expected, request.PageSize);
    }

    [Theory]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("asc", false)]
    [InlineData(null, false)]
    public void IsDescending_is_case_insensitive(string? direction, bool expected)
    {
        var request = new PagedRequest { SortDirection = direction ?? "asc" };
        Assert.Equal(expected, request.IsDescending);
    }
}
