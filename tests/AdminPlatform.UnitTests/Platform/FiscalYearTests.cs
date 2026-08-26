using AdminPlatform.Modules.Platform.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Platform;

public class FiscalYearTests
{
    [Fact]
    public void Create_rejects_end_date_not_after_start_date()
    {
        var start = new DateOnly(2026, 1, 1);

        Assert.Throws<BusinessRuleValidationException>(
            () => FiscalYear.Create(Guid.NewGuid(), "fy2026", "FY2026", start, start));
    }

    [Fact]
    public void Create_succeeds_with_valid_range()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 12, 31);

        var fiscalYear = FiscalYear.Create(Guid.NewGuid(), "fy2026", "FY2026", start, end);

        Assert.Equal(start, fiscalYear.StartDate);
        Assert.Equal(end, fiscalYear.EndDate);
        Assert.True(fiscalYear.IsActive);
    }

    [Fact]
    public void Update_rejects_end_date_not_after_start_date()
    {
        var fiscalYear = FiscalYear.Create(Guid.NewGuid(), "fy2026", "FY2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.Throws<BusinessRuleValidationException>(
            () => fiscalYear.Update("FY2026", true, new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1)));
    }
}
