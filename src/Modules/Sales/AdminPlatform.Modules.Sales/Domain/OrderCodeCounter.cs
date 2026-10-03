namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>The last running number handed out for one order-code bucket (prefix + date part). Only ever
/// advanced by a single atomic database statement (see OrderCodeGenerator), never edited through the entity.</summary>
public sealed class OrderCodeCounter
{
    public string CounterKey { get; private set; } = string.Empty;
    public int LastNumber { get; private set; }

    private OrderCodeCounter()
    {
        // EF Core
    }
}
