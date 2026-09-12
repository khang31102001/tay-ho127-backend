namespace AdminPlatform.Modules.Customer.Application.Addresses;

public sealed record CreateCustomerAddressRequest(
    string ReceiverName,
    string Phone,
    string AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string? AddressNote,
    bool IsDefault);

public sealed record UpdateCustomerAddressRequest(
    string ReceiverName,
    string Phone,
    string AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string? AddressNote);

public sealed record CustomerAddressResponse(
    Guid Id,
    string ReceiverName,
    string Phone,
    string AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string? AddressNote,
    bool IsDefault);
