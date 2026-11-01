namespace AdminPlatform.Modules.Media.Domain;

/// <summary>Nơi chứa file của một media. Local/Cloudinary = file do Admin upload qua IFileStorage tương ứng;
/// External = chỉ ghi lại URL do Admin nhập, file nằm ở nơi khác (hệ thống không quản lý).</summary>
public enum MediaStorageProvider
{
    Local,
    Cloudinary,
    External,
}
