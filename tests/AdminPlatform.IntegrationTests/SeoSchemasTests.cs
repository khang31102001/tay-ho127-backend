using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Seo.Application.Schemas;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class SeoSchemasTests
{
    private const string ValidJsonLd = """{"@context":"https://schema.org","@type":"Product","name":"Bánh cuốn"}""";

    private readonly AdminPlatformApiFactory _factory;

    public SeoSchemasTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seo_schemas_are_upserted_by_an_admin_and_only_active_ones_reach_the_public_site()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var entityId = $"p-{Guid.NewGuid():n}"[..12];
        var encoded = Uri.EscapeDataString(entityId);
        var query = $"entityType=product&entityId={encoded}&schemaType=Product";

        // Invalid input is rejected: unknown schema type, missing id, broken JSON, Advanced Mode without JSON.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/schemas", Body("product", entityId, "Movie"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/schemas", Body("product", null, "Product"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/schemas", Body("product", entityId, "Product", json: "{ nope"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/seo/schemas", Body("product", entityId, "Product", custom: true))).StatusCode);

        // Upsert twice: the second call replaces, it does not duplicate.
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/v1/seo/schemas",
            Body("product", entityId, "Product", config: new() { ["sku"] = "A-1" }))).StatusCode);
        var second = await admin.PutAsJsonAsync("/api/v1/seo/schemas",
            Body("product", entityId, "Product", config: new() { ["sku"] = "B-2", ["brand"] = "Tây Hồ" }, json: ValidJsonLd, custom: true));
        var saved = (await second.Content.ReadFromJsonAsync<SeoSchemaResponse>())!;
        Assert.Equal("B-2", saved.Config!["sku"]);
        Assert.True(saved.IsCustomOverride);

        var read = (await admin.GetFromJsonAsync<SeoSchemaResponse>($"/api/v1/seo/schemas/lookup?{query}"))!;
        Assert.Equal(saved.Id, read.Id);

        // The public site reads the active row without signing in, but not the admin endpoint.
        using var anonymous = _factory.CreateClient();
        var publicRow = (await anonymous.GetFromJsonAsync<SeoSchemaResponse>($"/api/v1/seo/public/schema?{query}"))!;
        Assert.Equal(ValidJsonLd, publicRow.CustomJsonLd);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/seo/schemas/lookup?{query}")).StatusCode);

        // Switching it off hides it from the public site (the admin still sees it).
        await admin.PutAsJsonAsync("/api/v1/seo/schemas", Body("product", entityId, "Product", json: ValidJsonLd, custom: true, active: false));
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/seo/public/schema?{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/seo/schemas/lookup?{query}")).StatusCode);

        // Reset removes it; resetting again is still a success.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/seo/schemas?{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/seo/schemas?{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/seo/schemas/lookup?{query}")).StatusCode);
    }

    private static UpsertSeoSchemaRequest Body(
        string type, string? id, string schema, Dictionary<string, string>? config = null, string? json = null,
        bool custom = false, bool active = true) =>
        new(type, id, schema, config, json, custom, active);
}
