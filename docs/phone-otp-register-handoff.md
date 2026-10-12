# Bàn giao: xác minh SĐT bằng OTP khi đăng ký (Zalo → SMS qua eSMS)

> File này là ghi chú để **làm tiếp** công việc ở các phiên sau. Cập nhật nó khi hoàn thành từng bước.
> **Trạng thái: Bước 1–5 (toàn bộ Backend) XONG; còn Bước 6 (Frontend) và Bước 7 (thử với eSMS).**
> Bước 1–4 đã nằm trong `main` backend (commit `02d781c`). Bước 5 nằm ở nhánh `feat/phone-otp-register-customer` (từ `main` `fbaa366`).

## 1. Mục tiêu

Khách tạo tài khoản ở Site Customer **buộc phải xác minh số điện thoại bằng mã OTP** (gửi qua Zalo, SMS dự phòng) thì tài khoản mới được tạo. Về sau dùng lại hạ tầng này để gửi thông báo đơn hàng qua Zalo (Phase 2).

Phân biệt ba thứ (đã chốt): **OTP xác thực** (nghiệp vụ này), **Zalo Login** (OAuth, KHÔNG làm, không trả SĐT đáng tin), **ZNS/ZBS template** (kênh gửi tin).

## 2. Quyết định kiến trúc đã chốt

- Hai module mới, mỗi module một schema và một DbContext, đúng quy ước repo:
  - **Integration** (`integration`): cấu hình kết nối bên thứ ba. **Secret đọc từ biến môi trường, KHÔNG lưu DB** (có thể chuyển sang mã hoá DB sau mà không đổi nơi gọi).
  - **Notification** (`notification`): OTP, cổng gửi tin, nhật ký gửi.
- Module không tham chiếu nhau (test `ModuleBoundaryTests` ép). Notification khai báo port `IIntegrationConnectionProvider`; adapter nằm ở `Host/CrossModuleAdapters/NotificationAdapters.cs`.
- Đăng ký **hai bước, không có bảng "đăng ký chờ"**: (1) xin OTP → nhận `challengeId`; (2) gọi lại `POST register` kèm `otpChallengeId` + `otpCode`, server kiểm OTP rồi mới tạo tài khoản. Không lưu mật khẩu chờ trên server, không tạo tài khoản "chiếm chỗ" SĐT/email người khác.
- Cờ `Customer:PhoneVerification:Required` (mặc định **tắt**): tắt thì `register` và test cũ chạy y như cũ.
- Nhà cung cấp: eSMS API `MultiChannelMessage` (Zalo ZNS rồi SMS dự phòng, một request).

## 3. Đã làm (Bước 1–5)

| Bước | Nội dung | Vị trí chính |
|---|---|---|
| 1 | Khung 2 module, migration `InitialCreate`, nối vào sln/Host/Migrator/Dockerfile/test kiến trúc | `src/Modules/Integration`, `src/Modules/Notification` |
| 2 | Integration: danh mục provider (`esms`), API Admin `GET/PUT /api/v1/integrations`, quyền `integrations.view/update`, seeder, `IIntegrationConnectionQueryService` (snapshot có secret, ẩn khi `ToString`) | `Integration/Application/{Providers,Connections}`, `Api/` |
| 3 | `IOtpService` (HMAC-SHA256, 5 phút, 5 lần đoán, xmin chống đoán song song, cooldown 60s, 5/giờ/SĐT, 20/giờ/IP, trần 500/ngày), `IMessageSender`, `LoggingMessageSender`, lỗi 429/503 | `Notification/Application/{Otp,Ports}`, SharedKernel `Exceptions.cs`, `GlobalExceptionHandler.cs` |
| 4 | `EsmsMessageSender`, `IIntegrationConnectionProvider` + adapter Host | `Notification/Infrastructure/EsmsMessageSender.cs` |
| 5 | Customer: đăng ký 2 bước + cờ `Customer:PhoneVerification:Required` (mặc định TẮT), `VietnamesePhone` (SharedKernel), cột `customers.phone_verified_at_utc`, port `ICustomerPhoneVerifier` + adapter Host, rate limit riêng `customer-otp-request` | `Customer/Application/Auth`, `Customer/Domain/Customer.cs`, `Host/CrossModuleAdapters/CustomerAdapters.cs`, `SharedKernel/VietnamesePhone.cs` |

Tại thời điểm commit: build 0 lỗi/0 cảnh báo, test kiến trúc (52) và unit test (485) đều đạt, Host khởi động được (swagger có `/register/otp`), `has-pending-model-changes` sạch.
**Test tích hợp (Postgres) CHƯA từng chạy** (máy dev không có Docker/Postgres): gồm `IntegrationConnectionsTests`, `CustomerPhoneVerificationFlowTests` và việc áp migration thật. Chạy ở CI hoặc máy có Postgres trước khi merge.

### Hợp đồng API của Bước 5 (cho Frontend)

- `POST /api/v1/customer/auth/register/otp` (ẩn danh) body `{ "phone": "0901234567", "email": "a@b.com" }` → `200 { challengeId, expiresAtUtc, resendAfterSeconds }`.
  Lỗi: `400` cờ tắt hoặc SĐT không phải số VN (`errors.Phone`), `409` SĐT/email đã đăng ký, `429` quá giới hạn (header `Retry-After`, giây), `503` nhà cung cấp không gửi được.
- `POST /api/v1/customer/auth/register` body như cũ **+ tuỳ chọn** `otpChallengeId` (guid), `otpCode` (chuỗi 6 số). Khi cờ BẬT mà thiếu hoặc sai mã → `400` (thông điệp tiếng Việt: không đúng/đã dùng, hết hạn, sai quá số lần); trùng SĐT/email → `409` (kiểm trước khi tiêu thụ mã). Thành công → `201` token như cũ.
- `GET /api/v1/customers/me` và admin `CustomerProfileResponse` có thêm `phoneVerified` (bool).
- Cờ TẮT: `register` không cần mã (client cũ chạy bình thường); `register/otp` trả `400`. FE nên biết cờ qua việc gọi `register/otp` (400 "chưa được bật" → hiện form cũ) hoặc thêm một endpoint đọc cờ nếu cần.

## 4. Giả định tạm — cần eSMS xác nhận (đổi bằng cấu hình/1 dòng, không đổi thiết kế)

1. Template Zalo OTP chỉ có **1 biến = mã OTP** (`Template.ParameterNames` trong `EsmsMessageSender.cs`).
2. SMS dự phòng = cài đặt `otpRegisterSmsContent` với `{code}` được thay bằng mã; `SmsType` mặc định `"2"` (CSKH), đổi qua cài đặt `smsType`; nội dung phải khớp template SMS đã đăng ký ở eSMS (OTP Vina dùng nội dung khác Viettel/Mobi, chưa biết có đưa vào cùng request được không).
3. SĐT gửi dạng `0901234567` (eSMS có thể muốn `84…`; sandbox sẽ cho biết).
4. Chưa biết trong 4 template `540610, 540618, 540646, 540679` cái nào là OTP đăng ký (OA "SVoucher" là OA test của eSMS).
5. Kết nối dùng chung một request cho cả hai kênh nên **thiếu cấu hình SMS thì không gửi được cả Zalo** (từ chối `not-configured`), theo tài liệu eSMS.

## 5. Việc còn lại

### Bước 5 — Customer (backend) — XONG
Đã làm theo thiết kế; chi tiết hợp đồng ở mục 3. Các quyết định đáng nhớ:
- SĐT chuẩn hoá "lenient" trong `Customer` (dạng VN → `0xxxxxxxxx`, còn lại chỉ trim) để không phá dữ liệu/test cũ (test tích hợp cũ dùng SĐT dạng hex như `09a3f1c2e4`); bắt buộc đúng định dạng VN **chỉ khi cờ BẬT**.
- Đổi SĐT thì `PhoneVerifiedAtUtc` bị xoá (xác minh gắn với một số). Kiểm trùng SĐT của service hồ sơ Admin cũng so theo dạng chuẩn (`+84…` = `0…`).
- Chưa làm: SQL kiểm `customers.phone` hiện có không khớp `^0\d{9}$` trước khi bật cờ ở môi trường thật, ví dụ
  `SELECT id, phone FROM customer.customers WHERE phone IS NOT NULL AND phone !~ '^0[0-9]{9}$';` (chỉ kiểm tra, không tự sửa dữ liệu). Khách cũ có `phone_verified_at_utc` = NULL (chưa xác minh) — luồng xác minh cho khách đã có sẵn là việc riêng, chưa làm.
### Bước 6 — Frontend (repo `fontend`) — NEXT
> Làm trên nhánh tách từ `main` FE SAU KHI PR sửa build (`fix/revert-broken-refactor`, PR #37) được merge; đừng làm trên `main` đang không biên dịch được.
- `src/features/auth/{components/AuthModal.tsx, hooks/useAuthModal.ts, services/auth.service.ts, types/auth.types.ts}`: thêm bước nhập OTP 6 số sau form đăng ký, đếm ngược gửi lại, hiển thị lỗi 429/503.
- BFF: thêm `app/api/customer/auth/register/otp/route.ts`; `register/route.ts` chuyển tiếp thêm `otpChallengeId`, `otpCode`; ghi cookie như hiện tại.
- `main` FE vừa refactor lớn (`src/core`, `src/shared`...); **đọc lại cấu trúc trước khi sửa**. `main` đang có lỗi cú pháp sẵn ở `src/features/payments/services/admin-payments.service.ts:14` (dấu backtick/nháy lệch) làm `tsc` dừng sớm.

### Bước 7 — Thử với eSMS
1. Có `ESMS_API_KEY`, `ESMS_SECRET_KEY`, `NOTIFICATION_OTP_HMAC_KEY` (`openssl rand -base64 32`) trong `.env` (**không dán vào chat/commit**).
2. `PUT /api/v1/integrations/{id}` cho `esms`: `isEnabled`, `environment: sandbox`, config `oaId`, `brandname`, `otpRegisterTemplateId`, `otpRegisterSmsContent` (có `{code}`).
3. Thử sandbox (`Sandbox=1`, không gửi thật, không tính tiền) → rồi live tới số Viettel/Mobi; sau đó bật `Customer__PhoneVerification__Required=true`.

### Phase 2 — Thông báo đơn hàng qua Zalo
`OutboxOrderNotifier` ở Host thay `LoggingOrderNotifier` (đăng ký trước `AddSalesModule`, Sales không phải sửa); mở rộng `OrderNotification` (chỉ thêm trường cuối: món, trạng thái...); bảng `notification_outbox` (dedupe key `order:{id}:placed`, `FOR UPDATE SKIP LOCKED`, backoff 1p/5p/30p tối đa 5 lần); `OutboxWorker` (BackgroundService); thêm kind `order-placed` + `orderPlacedTemplateId` vào `EsmsMessageSender`; chỉ gửi mã đơn, vài tên món, tổng tiền, trạng thái, link tra cứu — **không** gửi địa chỉ/email/ghi chú; SĐT trên đơn khách vãng lai có thể là của người khác → giới hạn theo SĐT.

### Phase 3 / backlog
- Trang Admin "Tích hợp" + mục menu (seed `NavigationSeeder`); API xem `delivery_logs` (quyền `notifications.view`).
- Job dọn `otp_challenges` / `delivery_logs` theo thời gian (chưa có — bảng sẽ phình dần).
- Callback trạng thái từ eSMS (`CallbackUrl`) + webhook xác thực chữ ký; thông báo đổi trạng thái đơn.
- Quên mật khẩu bằng OTP (FE hiện là mock `app/api/auth/forgot-password`).
- Giới hạn theo IP thật: BFF chuyển tiếp `X-Forwarded-For` + Backend cấu hình forwarded headers với proxy tin cậy (hiện IP thấy được là IP máy chủ Next.js nên giới hạn theo IP gần như vô hiệu; phòng thủ thật là theo SĐT + trần ngày).
- Cập nhật `README.md`/`docs/ARCHITECTURE.md`/`CHANGELOG.md` cho hai module mới (chưa làm).
- Cân nhắc phương án A (gọi thẳng Zalo OpenAPI) về sau: chỉ cần thêm một `IMessageSender` mới.

## 6. Chính sách Zalo cần nhớ (xác minh ở phiên trước)

- Xác minh được từ trang chính thức `oa.zalo.me`: từ 01/01/2026 ZBS Template Message gộp UID Giao dịch + UID Truyền thông + ZNS; template phải đăng ký và được duyệt; OA phải liên kết ZBS Account để trả phí.
- **Chưa xác minh** (trang developer là SPA không đọc được): hạn mức gửi, bảng mã lỗi, hành vi khi SĐT không có Zalo, yêu cầu consent, gói OA từ 01/06/2026. Giá chỉ có từ nguồn bên thứ ba (≈300–400 đ/tin) — đối chiếu lại trước khi lập ngân sách.

## 7. Cách chạy lại trên máy này

```powershell
$env:Path = "C:\Program Files\dotnet;" + $env:Path + ";$env:USERPROFILE\.dotnet\tools"
$env:DOTNET_ROLL_FORWARD = "Major"        # repo nhắm net8.0, máy chỉ có runtime 9
dotnet build AdminPlatform.sln -c Debug
dotnet test tests/AdminPlatform.ArchitectureTests/AdminPlatform.ArchitectureTests.csproj --no-build
dotnet test tests/AdminPlatform.UnitTests/AdminPlatform.UnitTests.csproj --no-build
# migration mới (ví dụ Customer):
dotnet ef migrations add <Ten> --project src/Modules/Customer/AdminPlatform.Modules.Customer/AdminPlatform.Modules.Customer.csproj --context AdminPlatform.Modules.Customer.Infrastructure.CustomerDbContext --output-dir Infrastructure/Migrations
```

- .NET SDK 9.0.318 đã cài bằng winget; `dotnet-ef` toàn cục 9.0.9. Máy không có Docker/Postgres.
- Windows giới hạn độ dài đường dẫn: tạo git worktree ở thư mục ngắn (vd `C:\Users\Admin\wt-xxx`).
- Thư mục làm việc chung có thể bị phiên khác thay đổi (đã gặp: module Media). Khi commit, chỉ `git add` đúng file của mình.
