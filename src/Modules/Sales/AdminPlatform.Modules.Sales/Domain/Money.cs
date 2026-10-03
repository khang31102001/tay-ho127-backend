namespace AdminPlatform.Modules.Sales.Domain;

/// <summary>Every amount is Vietnamese dong, whole units — all money is rounded the same way, in one place.</summary>
public static class Money
{
    public static decimal Round(decimal amount) => Math.Round(amount, 0, MidpointRounding.AwayFromZero);
}
