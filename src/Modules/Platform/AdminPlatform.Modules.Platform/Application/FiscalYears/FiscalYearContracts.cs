namespace AdminPlatform.Modules.Platform.Application.FiscalYears;

public sealed record CreateFiscalYearRequest(Guid OrganizationId, string Code, string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record UpdateFiscalYearRequest(string Name, bool IsActive, DateOnly StartDate, DateOnly EndDate);

public sealed record FiscalYearResponse(
    Guid Id, Guid OrganizationId, string Code, string Name, bool IsActive, DateOnly StartDate, DateOnly EndDate);
