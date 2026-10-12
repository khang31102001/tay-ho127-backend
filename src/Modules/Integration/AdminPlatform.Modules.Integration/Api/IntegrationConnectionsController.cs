using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Integration.Application.Connections;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Integration.Api;

[ApiController]
[Route("api/v1/integrations")]
public sealed class IntegrationConnectionsController : ControllerBase
{
    private readonly IIntegrationConnectionService _connections;

    public IntegrationConnectionsController(IIntegrationConnectionService connections)
    {
        _connections = connections;
    }

    /// <summary>All known providers. Secrets are never returned — only which ones are still missing from the server.</summary>
    [HttpGet]
    [RequirePermission(IntegrationPermissions.IntegrationsView)]
    [ProducesResponseType<IReadOnlyList<IntegrationConnectionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IntegrationConnectionResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await _connections.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(IntegrationPermissions.IntegrationsView)]
    [ProducesResponseType<IntegrationConnectionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationConnectionResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _connections.GetByIdAsync(id, cancellationToken));

    /// <summary>400 for an unknown setting key, a bad environment, or enabling a provider whose secrets are not set.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(IntegrationPermissions.IntegrationsUpdate)]
    [ProducesResponseType<IntegrationConnectionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationConnectionResponse>> Update(
        Guid id, [FromBody] UpdateIntegrationConnectionRequest request, CancellationToken cancellationToken) =>
        Ok(await _connections.UpdateAsync(id, request, cancellationToken));
}
