using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Identity.Application;
using AdminPlatform.Modules.Identity.Application.Users;

namespace AdminPlatform.Migrator;

/// <summary>The Migrator never issues tokens or switches a working context — it only applies migrations
/// and runs seeders — but IdentityModule's AuthService/UserService still need these ports satisfied for
/// the DI container to build (Host.CreateApplicationBuilder validates the graph in Development). The
/// real implementations are the Host's CrossModuleAdapters; these are inert stand-ins for this tool only.</summary>
internal sealed class NullUserPermissionsProvider : IUserPermissionsProvider
{
    public Task<UserPermissionsSnapshot> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(UserPermissionsSnapshot.Empty);
}

internal sealed class NullUserScopeValidator : IUserScopeValidator
{
    public Task<bool> HasBrandAccessAsync(Guid userId, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<bool> HasFiscalYearAccessAsync(Guid userId, Guid fiscalYearId, CancellationToken cancellationToken) => Task.FromResult(true);
}
