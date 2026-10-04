namespace AdminPlatform.Modules.Organization.Application.Brands;

/// <summary>How customers reach one branch. OpenTime/CloseTime are "HH:mm" and must be given together (or both left empty).</summary>
public sealed record BrandContactDto(
    string? Phone,
    string? Hotline,
    string? Email,
    string? AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string? OpenTime,
    string? CloseTime,
    string? BusinessHoursNote);

public sealed record CreateBrandRequest(Guid OrganizationId, string Code, string Name);

/// <summary>Contact: when sent, replaces the contact details of the branch; when omitted (null) they are left untouched.
/// IsPrimary: when true this branch becomes THE primary branch the website presents (the flag is cleared on every other
/// branch; the branch must be active); when omitted nothing changes; false clears it.</summary>
public sealed record UpdateBrandRequest(string Name, bool IsActive, BrandContactDto? Contact = null, bool? IsPrimary = null);

public sealed record BrandResponse(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    bool IsPrimary,
    BrandContactDto Contact);
