using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.SharedKernel;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.UnitTests.Media;

public class MediaTests
{
    [Fact]
    public void Create_sets_status_active_by_default()
    {
        var media = MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, "A photo", 1024, MediaStorageProvider.External);

        Assert.Equal(MediaStatus.Active, media.Status);
        Assert.Equal("photo.jpg", media.FileName);
        Assert.Equal(MediaKind.Image, media.Kind);
    }

    [Fact]
    public void Create_rejects_a_negative_size()
    {
        Assert.Throws<BusinessRuleValidationException>(
            () => MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, -1, MediaStorageProvider.External));
    }

    [Fact]
    public void Create_rejects_an_empty_file_name()
    {
        Assert.Throws<BusinessRuleValidationException>(
            () => MediaEntity.Create(" ", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, 1024, MediaStorageProvider.External));
    }

    [Fact]
    public void UpdateMetadata_changes_file_name_url_kind_and_alt_text()
    {
        var media = MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, 1024, MediaStorageProvider.External);

        media.UpdateMetadata("clip.mp4", "https://cdn.example.com/clip.mp4", MediaKind.Video, "A clip");

        Assert.Equal("clip.mp4", media.FileName);
        Assert.Equal("https://cdn.example.com/clip.mp4", media.Url);
        Assert.Equal(MediaKind.Video, media.Kind);
        Assert.Equal("A clip", media.AltText);
    }

    [Fact]
    public void UpdateMetadata_marks_uploaded_media_external_when_url_changes()
    {
        var media = MediaEntity.Create("photo.jpg", "https://res.cloudinary.com/demo/photo.jpg", MediaKind.Image, null, 1024, MediaStorageProvider.Cloudinary);

        media.UpdateMetadata("photo.jpg", " https://res.cloudinary.com/demo/photo.jpg ", MediaKind.Image, "Alt");
        Assert.Equal(MediaStorageProvider.Cloudinary, media.StorageProvider);

        media.UpdateMetadata("photo.jpg", "https://cdn.example.com/other.jpg", MediaKind.Image, "Alt");
        Assert.Equal(MediaStorageProvider.External, media.StorageProvider);
    }

    [Fact]
    public void Deactivate_then_activate_round_trips_status()
    {
        var media = MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, 1024, MediaStorageProvider.External);

        media.Deactivate();
        Assert.Equal(MediaStatus.Inactive, media.Status);

        media.Activate();
        Assert.Equal(MediaStatus.Active, media.Status);
    }
}
