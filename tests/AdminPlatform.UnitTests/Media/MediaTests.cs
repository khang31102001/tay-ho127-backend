using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.SharedKernel;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.UnitTests.Media;

public class MediaTests
{
    [Fact]
    public void Create_sets_status_active_by_default()
    {
        var media = MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, "A photo", 1024);

        Assert.Equal(MediaStatus.Active, media.Status);
        Assert.Equal("photo.jpg", media.FileName);
        Assert.Equal(MediaKind.Image, media.Kind);
    }

    [Fact]
    public void Create_rejects_a_negative_size()
    {
        Assert.Throws<BusinessRuleValidationException>(
            () => MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, -1));
    }

    [Fact]
    public void Create_rejects_an_empty_file_name()
    {
        Assert.Throws<BusinessRuleValidationException>(
            () => MediaEntity.Create(" ", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, 1024));
    }

    [Fact]
    public void UpdateMetadata_changes_file_name_url_kind_and_alt_text()
    {
        var media = MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, 1024);

        media.UpdateMetadata("clip.mp4", "https://cdn.example.com/clip.mp4", MediaKind.Video, "A clip");

        Assert.Equal("clip.mp4", media.FileName);
        Assert.Equal("https://cdn.example.com/clip.mp4", media.Url);
        Assert.Equal(MediaKind.Video, media.Kind);
        Assert.Equal("A clip", media.AltText);
    }

    [Fact]
    public void Deactivate_then_activate_round_trips_status()
    {
        var media = MediaEntity.Create("photo.jpg", "https://cdn.example.com/photo.jpg", MediaKind.Image, null, 1024);

        media.Deactivate();
        Assert.Equal(MediaStatus.Inactive, media.Status);

        media.Activate();
        Assert.Equal(MediaStatus.Active, media.Status);
    }
}
