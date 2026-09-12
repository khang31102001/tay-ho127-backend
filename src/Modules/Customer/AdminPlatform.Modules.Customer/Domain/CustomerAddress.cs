using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Customer.Domain;

public sealed class CustomerAddress : Entity
{
    public Guid CustomerId { get; private set; }
    public string ReceiverName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;
    public string? Ward { get; private set; }
    public string? District { get; private set; }
    public string? Province { get; private set; }
    public string? AddressNote { get; private set; }
    public bool IsDefault { get; private set; }

    private CustomerAddress()
    {
        // EF Core
    }

    public static CustomerAddress Create(
        Guid customerId,
        string receiverName,
        string phone,
        string addressLine,
        string? ward,
        string? district,
        string? province,
        string? addressNote,
        bool isDefault)
    {
        return new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = Guard.NotEmpty(customerId, nameof(customerId)),
            ReceiverName = Guard.NotNullOrWhiteSpace(receiverName, nameof(receiverName)).Trim(),
            Phone = Guard.NotNullOrWhiteSpace(phone, nameof(phone)).Trim(),
            AddressLine = Guard.NotNullOrWhiteSpace(addressLine, nameof(addressLine)).Trim(),
            Ward = ward,
            District = district,
            Province = province,
            AddressNote = addressNote,
            IsDefault = isDefault,
        };
    }

    public void Update(string receiverName, string phone, string addressLine, string? ward, string? district, string? province, string? addressNote)
    {
        ReceiverName = Guard.NotNullOrWhiteSpace(receiverName, nameof(receiverName)).Trim();
        Phone = Guard.NotNullOrWhiteSpace(phone, nameof(phone)).Trim();
        AddressLine = Guard.NotNullOrWhiteSpace(addressLine, nameof(addressLine)).Trim();
        Ward = ward;
        District = district;
        Province = province;
        AddressNote = addressNote;
    }

    public void MarkAsDefault() => IsDefault = true;

    public void UnmarkAsDefault() => IsDefault = false;
}
