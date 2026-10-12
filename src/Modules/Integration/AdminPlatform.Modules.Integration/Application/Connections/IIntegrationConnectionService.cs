namespace AdminPlatform.Modules.Integration.Application.Connections;

public interface IIntegrationConnectionService
{
    /// <summary>Every known provider, ordered by code. The set is small and fixed, so it is not paged.</summary>
    Task<IReadOnlyList<IntegrationConnectionResponse>> ListAsync(CancellationToken cancellationToken);

    Task<IntegrationConnectionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IntegrationConnectionResponse> UpdateAsync(Guid id, UpdateIntegrationConnectionRequest request, CancellationToken cancellationToken);
}
