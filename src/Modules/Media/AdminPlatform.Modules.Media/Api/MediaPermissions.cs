namespace AdminPlatform.Modules.Media.Api;

public static class MediaPermissions
{
    public const string MediaView = "media.view";
    public const string MediaCreate = "media.create";
    public const string MediaUpdate = "media.update";
    public const string MediaDelete = "media.delete";

    /// <summary>Xóa vĩnh viễn (xóa file trên nơi lưu + bản ghi) — tách khỏi media.delete (chỉ ngừng hoạt động).</summary>
    public const string MediaPurge = "media.purge";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (MediaView, "View media"),
        (MediaCreate, "Create media"),
        (MediaUpdate, "Update media"),
        (MediaDelete, "Delete media"),
        (MediaPurge, "Permanently delete media (file + record)"),
    ];
}
