using AdminPlatform.Modules.Media;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Media;

public class MediaStorageProviderTests
{
    private static readonly Dictionary<string, string?> CloudinaryKeys = new()
    {
        ["MediaStorage:Cloudinary:CloudName"] = "demo",
        ["MediaStorage:Cloudinary:ApiKey"] = "key",
        ["MediaStorage:Cloudinary:ApiSecret"] = "secret",
    };

    private static IServiceCollection AddModule(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:Default"] = "Host=localhost;Database=test";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddMediaModule(configuration);
    }

    // So sánh theo đăng ký (không resolve) vì LocalDiskFileStorage cần IWebHostEnvironment của Host.
    private static List<Type?> RegisteredStorages(IServiceCollection services) =>
        services.Where(d => d.ServiceType == typeof(IFileStorage)).Select(d => d.ImplementationType).ToList();

    [Fact]
    public void Local_is_always_registered_and_cloudinary_only_with_keys()
    {
        Assert.Equal([typeof(LocalDiskFileStorage)], RegisteredStorages(AddModule(new())));
        Assert.Equal(
            [typeof(LocalDiskFileStorage), typeof(CloudinaryFileStorage)],
            RegisteredStorages(AddModule(new(CloudinaryKeys))));
    }

    [Fact]
    public void Cloudinary_default_without_keys_fails_validation()
    {
        using var provider = AddModule(new() { ["MediaStorage:Provider"] = "cloudinary" }).BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MediaStorageOptions>>().Value);
    }

    [Fact]
    public void Unknown_default_provider_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => AddModule(new() { ["MediaStorage:Provider"] = "S3" }));
    }
}
