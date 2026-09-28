using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Identity.Application.Auth;
using AdminPlatform.Modules.Organization.Application.Organizations;
using AdminPlatform.Modules.Platform.Application.AuditLogs;
using AdminPlatform.Modules.Platform.Application.FiscalYears;
using AdminPlatform.Modules.Platform.Application.SystemSettings;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class PlatformCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public PlatformCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Fiscal_year_can_be_created_read_and_updated_and_rejects_an_inverted_date_range()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var organizationId = await CreateOrganizationAsync(client);
        var code = AuthTestHelper.UniqueCode("fy");

        var createResponse = await client.PostAsJsonAsync("/api/v1/fiscal-years",
            new CreateFiscalYearRequest(organizationId, code, "FY Test", new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31)));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var fiscalYear = (await createResponse.Content.ReadFromJsonAsync<FiscalYearResponse>())!;

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/fiscal-years",
            new CreateFiscalYearRequest(organizationId, code, "Dup", new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31)));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var invertedResponse = await client.PostAsJsonAsync("/api/v1/fiscal-years",
            new CreateFiscalYearRequest(organizationId, AuthTestHelper.UniqueCode("fy"), "Bad", new DateOnly(2030, 12, 31), new DateOnly(2030, 1, 1)));
        Assert.Equal(HttpStatusCode.BadRequest, invertedResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/fiscal-years/{fiscalYear.Id}",
            new UpdateFiscalYearRequest("FY Renamed", false, new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31)));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.False((await updateResponse.Content.ReadFromJsonAsync<FiscalYearResponse>())!.IsActive);
    }

    [Fact]
    public async Task System_setting_can_be_created_read_updated_and_deleted()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var code = $"test.{Guid.NewGuid():n}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/system-settings", new CreateSystemSettingRequest(code, "Test Setting", "1", null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var setting = (await createResponse.Content.ReadFromJsonAsync<SystemSettingResponse>())!;

        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/v1/system-settings", new CreateSystemSettingRequest(code, "Dup", "1", null))).StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/system-settings/{setting.Id}", new UpdateSystemSettingRequest("Test Setting", "2", true));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("2", (await updateResponse.Content.ReadFromJsonAsync<SystemSettingResponse>())!.Value);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/system-settings/{setting.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/system-settings/{setting.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_change_is_audit_logged_with_the_acting_user_as_actor()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");
        var organizationId = await CreateOrganizationAsync(client);

        var page = await client.GetFromJsonAsync<PagedResultDto<AuditLogResponse>>(
            $"/api/v1/audit-logs?page=1&pageSize=100&entityName=Organization&actorUserId={me!.Id}");

        Assert.Contains(page!.Items, log => log.EntityId == organizationId.ToString() && log.ActorUserId == me.Id);
    }

    private static async Task<Guid> CreateOrganizationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/organizations", new CreateOrganizationRequest(AuthTestHelper.UniqueCode("org"), "Platform Test Org"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganizationResponse>())!.Id;
    }
}
