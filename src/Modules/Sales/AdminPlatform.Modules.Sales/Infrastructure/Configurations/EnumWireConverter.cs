using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AdminPlatform.Modules.Sales.Infrastructure.Configurations;

/// <summary>Stores an enum as its wire text ("bank_transfer"), so the database, the API and the code share one spelling.</summary>
internal sealed class EnumWireConverter<T> : ValueConverter<T, string> where T : struct, Enum
{
    public EnumWireConverter()
        : base(value => EnumWire.ToWire(value), text => EnumWire.Parse<T>(text, typeof(T).Name))
    {
    }
}
