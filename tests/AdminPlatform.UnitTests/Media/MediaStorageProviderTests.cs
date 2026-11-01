using AdminPlatform.Modules.Media;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Media;

public class MediaStorageProviderTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:Default"] = "Host=localhost;Database=test";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddMediaModule(configuration).BuildServiceProvider();
    }

    [Fact]
    public void Cloudinary_provider_registers_cloudinary_storage()
    {
        using var provider = BuildProvider(new()
        {
            ["MediaStorage:Provider"] = "Cloudinary",
            ["MediaStorage:Cloudinary:CloudName"] = "demo",
            ["MediaStorage:Cloudinary:ApiKey"] = "key",
            ["MediaStorage:Cloudinary:ApiSecret"] = "secret",
        });

        Assert.IsType<CloudinaryFileStorage>(provider.GetRequiredService<IFileStorage>());
    }

    [Fact]
    public void Cloudinary_provider_without_keys_fails_validation()
    {
        using var provider = BuildProvider(new() { ["MediaStorage:Provider"] = "cloudinary" });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MediaStorageOptions>>().Value);
    }

    [Fact]
    public void Unknown_provider_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => BuildProvider(new() { ["MediaStorage:Provider"] = "S3" }));
    }
}
