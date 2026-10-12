using AdminPlatform.Modules.Integration.Application.Providers;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Integration.Application.Connections;

public sealed class IntegrationConnectionService : IIntegrationConnectionService
{
    private const int MaxConfigValueLength = 500;

    private readonly IIntegrationDbContext _db;
    private readonly IIntegrationSecretSource _secrets;

    public IntegrationConnectionService(IIntegrationDbContext db, IIntegrationSecretSource secrets)
    {
        _db = db;
        _secrets = secrets;
    }

    public async Task<IReadOnlyList<IntegrationConnectionResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var connections = await _db.IntegrationConnections.AsNoTracking().OrderBy(c => c.ProviderCode).ToListAsync(cancellationToken);
        return connections.Select(ToResponse).ToList();
    }

    public async Task<IntegrationConnectionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var connection = await _db.IntegrationConnections.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.IntegrationConnection), id);
        return ToResponse(connection);
    }

    public async Task<IntegrationConnectionResponse> UpdateAsync(Guid id, UpdateIntegrationConnectionRequest request, CancellationToken cancellationToken)
    {
        var connection = await _db.IntegrationConnections.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.IntegrationConnection), id);

        var provider = IntegrationProviders.Find(connection.ProviderCode)
            ?? throw new BusinessRuleValidationException($"'{connection.ProviderCode}' is not a supported provider.");

        if (!IntegrationConnectionMapper.TryParseEnvironment(request.Environment, out var environment))
        {
            throw new BusinessRuleValidationException("Environment must be 'sandbox' or 'live'.");
        }

        var config = NormalizeConfig(provider, request.Config);

        if (request.IsEnabled)
        {
            var missing = provider.SecretConfigPaths.Keys.Where(name => !_secrets.GetSecrets(provider).ContainsKey(name)).Order(StringComparer.Ordinal).ToList();
            if (missing.Count > 0)
            {
                throw new BusinessRuleValidationException(
                    $"'{provider.Code}' cannot be enabled until its secrets are set in the server environment: {string.Join(", ", missing)}.");
            }
        }

        connection.Update(request.DisplayName, request.IsEnabled, environment, IntegrationConnectionMapper.WriteConfig(config));
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(connection);
    }

    private IntegrationConnectionResponse ToResponse(Domain.IntegrationConnection connection)
    {
        var provider = IntegrationProviders.Find(connection.ProviderCode);
        var setSecrets = provider is null ? new Dictionary<string, string>() : _secrets.GetSecrets(provider);
        return IntegrationConnectionMapper.ToResponse(connection, provider, setSecrets);
    }

    /// <summary>Keeps only known keys (anything else is a mistake worth surfacing), trims values and drops empty ones.</summary>
    private static Dictionary<string, string> NormalizeConfig(IntegrationProviderDefinition provider, Dictionary<string, string>? config)
    {
        var normalized = new Dictionary<string, string>();
        if (config is null)
        {
            return normalized;
        }

        var unknownKeys = config.Keys.Where(key => !provider.ConfigKeys.Contains(key, StringComparer.Ordinal)).Order(StringComparer.Ordinal).ToList();
        if (unknownKeys.Count > 0)
        {
            throw new BusinessRuleValidationException(
                $"Unknown setting(s) for '{provider.Code}': {string.Join(", ", unknownKeys)}. Allowed: {string.Join(", ", provider.ConfigKeys)}.");
        }

        foreach (var (key, value) in config)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (trimmed.Length > MaxConfigValueLength)
            {
                throw new BusinessRuleValidationException($"Setting '{key}' is longer than {MaxConfigValueLength} characters.");
            }

            normalized[key] = trimmed;
        }

        return normalized;
    }
}
