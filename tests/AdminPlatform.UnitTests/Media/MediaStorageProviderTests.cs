using AdminPlatform.Modules.Media;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Domain;
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

    private static readonly Dictionary<string, string?> S3Keys = new()
    {
        ["MediaStorage:S3:ServiceUrl"] = "https://s3.example.vn",
        ["MediaStorage:S3:Bucket"] = "tayho127",
        ["MediaStorage:S3:AccessKey"] = "key",
        ["MediaStorage:S3:SecretKey"] = "secret",
        ["MediaStorage:S3:PublicBaseUrl"] = "https://s3.example.vn/tayho127",
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
    public void S3_is_registered_only_with_keys_and_public_url()
    {
        Assert.Equal(
            [typeof(LocalDiskFileStorage), typeof(S3FileStorage)],
            RegisteredStorages(AddModule(new(S3Keys))));

        var withoutPublicUrl = new Dictionary<string, string?>(S3Keys) { ["MediaStorage:S3:PublicBaseUrl"] = "" };
        Assert.Equal([typeof(LocalDiskFileStorage)], RegisteredStorages(AddModule(withoutPublicUrl)));
    }

    [Fact]
    public void S3_default_without_keys_fails_validation()
    {
        using var provider = AddModule(new() { ["MediaStorage:Provider"] = "s3" }).BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MediaStorageOptions>>().Value);
    }

    [Fact]
    public void S3_default_with_keys_resolves_the_s3_storage()
    {
        var services = AddModule(new(S3Keys) { ["MediaStorage:Provider"] = "S3" });
        services.AddSingleton<S3FileStorage>();
        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<MediaStorageOptions>>().Value.S3.IsConfigured);
        Assert.Equal(MediaStorageProvider.S3, provider.GetRequiredService<S3FileStorage>().Provider);
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
        Assert.Throws<InvalidOperationException>(() => AddModule(new() { ["MediaStorage:Provider"] = "Ftp" }));
    }
}
