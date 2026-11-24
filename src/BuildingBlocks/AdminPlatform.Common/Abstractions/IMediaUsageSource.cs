namespace AdminPlatform.Common.Abstractions;

/// <summary>Port dùng chung (định nghĩa ở Common, như cho phép trong ModuleBoundaryTests): mỗi module có lưu
/// tham chiếu tới media (id dạng chữ/uuid, hoặc URL chèn trong HTML) tự cài một source và đăng ký vào DI.
/// Module Media gom mọi source để biết một media còn được dùng ở đâu trước khi xóa vĩnh viễn — Media không
/// cần biết module nào tồn tại, module mới chỉ việc đăng ký thêm source.</summary>
public interface IMediaUsageSource
{
    Task<IReadOnlyList<MediaUsage>> FindUsagesAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken);
}

/// <summary>Quy tắc xóa vĩnh viễn media: nơi nào đang dùng thì tự gỡ tham chiếu (đặt null / bỏ khỏi danh sách) thay vì
/// chặn xóa. Cùng module với <see cref="IMediaUsageSource"/> (thường chung một class) và cũng đăng ký vào DI.
/// Chỉ gỡ các tham chiếu có <see cref="MediaUsage.CanDetach"/> = true; ảnh chèn trong HTML thì không tự sửa được.</summary>
public interface IMediaUsageDetacher
{
    Task DetachAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken);
}

/// <summary>Media cần tra: id (so với cột lưu id) và URL (so với HTML có chèn ảnh).</summary>
public sealed record MediaUsageTarget(Guid MediaId, string Url)
{
    /// <summary>Id ở dạng các module lưu trong cột chữ ("d" — chữ thường, có gạch).</summary>
    public string IdText => MediaId.ToString();
}

/// <param name="Module">Tên module hiển thị cho Admin, vd. "Sản phẩm".</param>
/// <param name="EntityId">Id bản ghi đang dùng (để Admin mở ra gỡ).</param>
/// <param name="EntityName">Tên dễ đọc của bản ghi, vd. tên sản phẩm.</param>
/// <param name="Field">Vị trí dùng, vd. "Ảnh đại diện".</param>
/// <param name="CanDetach">false = không tự gỡ được (vd. URL chèn trong nội dung HTML) nên vẫn chặn xóa vĩnh viễn.</param>
public sealed record MediaUsage(Guid MediaId, string Module, string EntityId, string EntityName, string Field, bool CanDetach = true);
