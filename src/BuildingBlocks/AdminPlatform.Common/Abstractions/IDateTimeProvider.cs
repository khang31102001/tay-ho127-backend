namespace AdminPlatform.Common.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
