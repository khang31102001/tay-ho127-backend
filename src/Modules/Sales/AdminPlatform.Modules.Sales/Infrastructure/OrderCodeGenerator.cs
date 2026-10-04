using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Sales.Application.Ports;
using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AdminPlatform.Modules.Sales.Infrastructure;

/// <summary>Draws the next running number with ONE atomic upsert, so two orders placed at the same instant can
/// never receive the same code (a read-then-write would). The counter bucket is prefix + date part, so
/// numbering restarts by itself each new day (or never, when the format has no date part).</summary>
internal sealed class OrderCodeGenerator : IOrderCodeGenerator
{
    private const string NextNumberSql = """
        INSERT INTO sales.order_code_counters (counter_key, last_number)
        VALUES (@key, 1)
        ON CONFLICT (counter_key) DO UPDATE SET last_number = sales.order_code_counters.last_number + 1
        RETURNING last_number
        """;

    private readonly SalesDbContext _db;
    private readonly IDateTimeProvider _clock;

    public OrderCodeGenerator(SalesDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<string> NextAsync(OrderSettings settings, CancellationToken cancellationToken)
    {
        // Vietnam is UTC+7 all year; a fixed offset needs no time-zone data (absent in slim containers).
        var localNow = _clock.UtcNow.AddHours(7);

        var connection = _db.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = NextNumberSql;
            command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

            var key = command.CreateParameter();
            key.ParameterName = "key";
            key.Value = settings.CounterKey(localNow);
            command.Parameters.Add(key);

            var next = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return settings.BuildCode(localNow, next);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
