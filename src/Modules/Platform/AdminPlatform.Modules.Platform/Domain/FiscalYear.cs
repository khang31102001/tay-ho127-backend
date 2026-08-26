using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Platform.Domain;

/// <summary>A working/fiscal year, scoped to an Organization (cross-module by id — architecture
/// assumption #6). Deliberately not called "Year": task constraint forbids a generic Year table.</summary>
public sealed class FiscalYear : CatalogEntity
{
    public Guid OrganizationId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    private FiscalYear()
    {
        // EF Core
    }

    public static FiscalYear Create(Guid organizationId, string code, string name, DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            throw new BusinessRuleValidationException("EndDate must be after StartDate.");
        }

        return new FiscalYear
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guard.NotEmpty(organizationId, nameof(organizationId)),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive, DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            throw new BusinessRuleValidationException("EndDate must be after StartDate.");
        }

        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
        StartDate = startDate;
        EndDate = endDate;
    }
}
