# HANDOFF — trạng thái hiện hành (cập nhật 25/08/2026)

> **Đọc file này ĐẦU TIÊN** nếu bạn là người/AI mới tiếp nhận dự án. Nó thay cho
> `HANDOFF_Week10.md` (đã lỗi thời ở phần hạ tầng). Sau file này thì đọc `../CLAUDE.md` (quy tắc
> nghiệp vụ đánh số) và `KICH_BAN_DEMO.md` (bấm theo từng bước).
>
> 🔴 **25/08 — nhóm bị hội đồng cho BẢO VỆ LẦN 2.** Danh sách việc hiện hành nay nằm ở
> **`KE_HOACH_BAO_VE_LAN2.md`** (7 nhóm việc, có bảng hiện trạng đã xác minh bằng cách đọc code).
> Đọc file này để biết *hệ thống đang chạy thế nào*, rồi sang file kia để biết *phải làm gì tiếp*.

---

## 1. Hai thay đổi lớn nhất gần đây — đọc kỹ, dễ hiểu nhầm

### 1.1 Cơ sở dữ liệu: SQL Server → **PostgreSQL** (14/08)

**Vì sao đổi:** Railway (nơi deploy) không cung cấp SQL Server. Không phải vì Postgres tốt hơn.

**Việc phải sửa chỉ có 5 dòng** — vì thứ cho phép đổi DB là **EF Core**, không phải repository
pattern (repository ở đây mỏng, chỉ phơi `IQueryable<T>`; nó giúp test/mock chứ không giúp đổi DB):

| Sửa gì | Chỗ |
|---|---|
| `UseSqlServer` → `UseNpgsql` | `DependencyInjection.cs` |
| Cú pháp T-SQL `[cột]` → `"cột"` (3 chỗ) | `FURPMSDbContext.cs` — `HasFilter`, `HasComputedColumnSql`, `HasCheckConstraint` |
| Thêm package `Npgsql.EntityFrameworkCore.PostgreSQL` | `FURPMS.Infrastructure.csproj` |

Toàn bộ **19 migration cũ (T-SQL) đã bị xoá** và thay bằng **một migration nền cho Postgres**.
Không sửa tay dòng nào trong đó.

> ⚠️ **Nếu sau này cần đổi DB lần nữa:** ba thứ gây khó là raw SQL (`FromSqlRaw`), ghim kiểu dữ
> liệu (`HasColumnType`), và `DateTime.Now` (Npgsql ném lỗi vì `Kind=Local`). Codebase này
> **không có cái nào** — đã kiểm: 0 / 0 / 0 (cả 168 chỗ đều dùng `DateTime.UtcNow`).

### 1.2 Seeder **không còn** đổ dữ liệu demo ngoài môi trường Development

**Trước đây** seeder chạy y hệt ở mọi môi trường ⇒ deploy lên máy chủ là DB thật có ngay **9 tài
khoản demo, tất cả mật khẩu `password`, kể cả `admin@furpms.edu.vn`** — mà danh sách email đó nằm
trong `docs/` của repo. Ai đọc repo cũng đăng nhập được bằng quyền quản trị.

Nay `SeedAsync()` tách hai phần:

- **Bắt buộc (mọi môi trường):** vai trò · quản trị viên · danh mục · cấu hình · bộ tiêu chí
- **Demo (chỉ Development):** tài khoản mẫu · đề tài mẫu · kịch bản đã duyệt · reset mật khẩu demo

Mật khẩu quản trị ngoài Development lấy từ biến `SeedAdminPassword`; **không đặt thì sinh ngẫu
nhiên và ghi log MỘT LẦN**.

> ⚠️ Biến `SeedAdminPassword` chỉ có tác dụng **khi chưa có** tài khoản admin. Admin đã tồn tại thì
> seeder bỏ qua hẳn — muốn đổi phải xoá dòng admin trong DB rồi khởi động lại.

### 1.3 Hai lỗi Dũng (FE) báo 18/08 — đã sửa, ghi lại vì cả hai đều dễ tái phát

**(a) Tên file hiển thị sai — sửa ở BE.** Màn xem tài liệu của hội đồng và câu *"AI đã đọc file
đính kèm (…)"* hiện `02%20%C4%90e%CC%82%CC%80%20cu%CC%9Bo%CC%9Bng…` thay vì *02 Đề cương nghiên
cứu…*. **Không phải FE hiện sai:** DB lưu đúng chuỗi đó, vì file trên máy người nộp vốn đã mang tên
**bị mã hoá URL** (tải về từ link không kèm `Content-Disposition`) và dấu tiếng Việt ở dạng **NFD**
(macOS). BE chép thẳng `IFormFile.FileName` vào cột `original_file_name`.

Sửa bằng `FURPMS.Application/Common/FileNames.cs` — giải mã `%XX` (**chỉ khi tên không có khoảng
trắng**, nên tên thật kiểu `Giải ngân 50% đợt 1.pdf` không bị đụng), gom về NFC, bỏ đường dẫn và ký
tự điều khiển. Áp **cả lúc lưu lẫn lúc đọc**: đọc cũng chuẩn hoá vì các bản ghi đã nằm trên DB
Railway vẫn mang tên hỏng và **không có migration nào đi sửa dữ liệu cũ**.

Tiện thể gộp 3 bản sao validate upload trùng lặp vào `AssertUploadAllowedAsync` — nay trả về
`(tên đã chuẩn hoá, đuôi file)` và dùng chung cho cả 6 luồng upload.

**(b) Đổi vai bị văng ra "Bạn không có quyền" — sửa ở FE.** Triệu chứng **giống hệt** lỗi đã sửa
12/08 (xem `BACKLOG_Uu_tien.md`, mục "Đã sửa xong trong lượt này") nhưng **nguyên nhân khác hẳn**:
lần trước là khoá localStorage dùng chung; lần này là **react-router v7 bọc mọi thay đổi địa chỉ
trong `React.startTransition`**, trong khi zustand (`useSyncExternalStore`) không hoãn được ⇒ React
commit một lượt trung gian *(vai MỚI, địa chỉ CŨ)* và `RoleGuard` của trang cũ bắn
`<Navigate to="/unauthorized">` đè lên điều hướng về dashboard. Sửa bằng
`<BrowserRouter useTransitions={false}>` + đưa `<Suspense>` vào trong `AppLayout`. Chi tiết ở
`AGENTS.md` §3.5 của repo FE.

> ⚠️ Rút ra: thấy lại triệu chứng "đổi vai ăn 403 oan" thì **đừng cho là hồi quy của bản vá cũ** —
> đã có hai nguyên nhân độc lập cùng ra một màn hình.

### 1.4 🔴 Cloudinary CHẶN phát hành file `raw` — mọi tài liệu mới nộp đều không mở được (18/08)

**Triệu chứng:** nộp file xong, mở ra ăn **404 "File không còn trên storage"**. Nhưng file **cũ**
vẫn mở bình thường — nên rất dễ tưởng hệ thống vẫn ổn.

**Nguyên nhân:** Cloudinary chặn *delivery* các tài nguyên kiểu `raw` (mọi tài liệu ở đây đều là
.docx/.pdf ⇒ rơi hết vào diện này). URL CDN mà `CloudinaryFileStorage` dựng ra nay trả `401` kèm
`X-Cld-Error: deny or ACL failure`, BE dịch thành "file không còn trên storage". Tài sản upload từ
trước khi Cloudinary đổi mặc định vẫn phát hành được — đó là lý do lỗi chỉ lộ ra với file mới.

**Đã đo từng cách trước khi sửa** (cloud `dq6dp3g3v`, thư mục `furpms-dev`):

| Cách đọc | Kết quả |
|---|---|
| URL CDN trần | 401 |
| URL CDN + chữ ký `s--…--` (SHA1/SHA256, có/không version) | 401 |
| Upload kèm `access_mode=public` rồi đọc URL CDN | 401 |
| URL CDN của tài sản **không tồn tại** | 404 ⇒ chặn theo TỪNG tài sản, không phải cả tài khoản |
| **`api.cloudinary.com/v1_1/{cloud}/raw/download` có chữ ký** | **200, đúng nội dung** |

**Đã sửa:** `CloudinaryFileStorage.ReadAllBytesAsync` đọc qua endpoint `/download` có chữ ký thay
vì URL CDN (`SignedDownloadUrl`). Cách này **an toàn hơn** bản cũ: tài liệu không còn phát hành
công khai, ai có URL cũng không tải được — muốn lấy phải qua BE có `[Authorize]`.

> ⚠️ **Phải kiểm trên bản deploy.** Railway dùng **cùng tài khoản Cloudinary**, chỉ khác thư mục,
> nên nhiều khả năng đang dính y hệt. Cách kiểm nhanh: đăng nhập bản deploy → nộp một file bất kỳ →
> bấm mở lại. Ra 404 thì đúng lỗi này, deploy bản vá là hết. **Đây là lỗi chặn demo**: hội đồng
> không mở được đề cương thì không chấm được gì.

### 1.5 🔴 Rule #15 đã được VIẾT LẠI (25/08) — đọc trước khi đụng bất cứ chỗ nào có tiền

Câu cũ — *"Tài chính = minh chứng, hệ thống KHÔNG quản tiền… scope = **strip + ẩn nav/UI tiền**"* —
bị hiểu thành **"không được HIỆN tiền"**. Hậu quả: `Contract.TotalAmount` bị giấu khỏi mọi màn dù BE
vẫn trả về, và **hội đồng bảo vệ lần 2 bắt đúng chỗ này** (yêu cầu số 1: *"thể hiện rõ ngân sách
tương ứng cho các đề tài"*).

Nay tách hai khái niệm bị gộp nhầm:

| | |
|---|---|
| **CÓ — hiển thị đầy đủ** | dự toán duyệt (Điều 15) · trần loại đề tài (Điều 14) · **giá trị hợp đồng** · lịch giải ngân theo %/mốc · trạng thái từng đợt · quyết toán. Đây là **hồ sơ hành chính** của đề tài |
| **KHÔNG làm** | thanh toán · nối ngân hàng · thay sổ kế toán. `ActualAmount` là **ghi nhận lại** con số Phòng Tài chính báo, không tự tính |

⇒ Ẩn *cấu hình* tài chính thì đúng; ẩn *số tiền của đề tài* thì sai. Toàn văn ở `CLAUDE.md` #15.

**Hệ quả đang chạy trong code:** màn Hợp đồng có tab **"Ngân sách"**
(`GET /api/projects/{projectId}/budget`, xem `API_CONTRACT.md` §13.2b) · danh sách có cột **Giá trị
hợp đồng** + bộ lọc giai đoạn · đợt giải ngân hiện **% và số tiền** · KPI Thống kê hết 0.

> Một nguyên tắc phải giữ khi làm tiếp: **đợt đã đánh dấu chi mà chưa có `actualAmount` thì tổng là
> TẠM TÍNH theo kế hoạch, và màn hình phải nói ra**. Im lặng là để người đọc tưởng đó là số quyết
> toán — hệ thống không được tự quyết thay kế toán.

**Trạng thái công việc bảo vệ lần 2:** Sprint 0, Nhóm 1 (Ngân sách), Nhóm 2 (Deadline), Nhóm 3
(Lưu trữ quyết định) và Nhóm 4 (Cảnh báo điểm lệch) ✅ xong. Còn lại: Nhóm 5 (chuyên môn người chấm),
Nhóm 6 (AI trùng), Nhóm 7 (tài liệu) — xem `KE_HOACH_BAO_VE_LAN2.md` §12.

### 1.6 🔴 SAU KHI DEPLOY phải chạy backfill hồ sơ quyết định (25/08)

Nhóm 3 thêm bảng `project_decisions` — sổ các quyết định đã ra với một đề tài (yêu cầu số 2 của hội
đồng). **Sổ chỉ bắt đầu ghi từ lúc tính năng lên.** Mọi đề tài đã chạy xong trước đó — gồm cả dữ liệu
thật đang nằm trên Railway — sẽ mở ra **hồ sơ trống trơn**, đúng lúc bảo vệ.

```bash
# 1. Xem trước, KHÔNG ghi gì
POST /api/admin/backfill-decisions?dryRun=true     # Admin
# 2. Chạy thật
POST /api/admin/backfill-decisions
```

**Chạy lại bao nhiêu lần cũng được** — nhận diện theo `sourceEntityType + sourceEntityId +
decisionType`, đã có thì bỏ qua. Đúng cả khi luồng thật đã ghi trước đó (đã kiểm: chạy lại sau khi
nộp một đề cương thật → thêm 0, đếm bản trùng trong DB = 0).

### 1.7 Đếm ngược tới hạn — **chỉ máy chủ được tính** (25/08)

Nhóm 2 làm xong thì lộ ra một lỗi rất dễ tái phát: thẻ *"Hạn sắp tới"* trên bảng điều khiển hiện
**"Còn 7 ngày"** trong khi tab **Sản phẩm** của chi tiết hợp đồng hiện **"Còn 6 ngày"** — cho **cùng
một sản phẩm**, trên cùng một phiên đăng nhập. Nguyên nhân: máy chủ chạy UTC, trình duyệt chạy giờ
máy người dùng (UTC+7); sát nửa đêm là lệch nguyên một ngày.

Cách chữa đã áp dụng: **bỏ hẳn phép trừ ngày phía giao diện.** Mọi DTO có hạn nay trả `daysLeft`
(âm = quá hạn, `null` = chưa đặt hạn), tính tập trung ở `FURPMS.Application/Common/DeadlineMath.cs`
qua `IClock` — nên công cụ tua thời gian tác động đúng vào đây y như bộ quét nhắc hạn qua email.
Khoá bằng `FURPMS.Tests/Deadlines/DaysLeftConsistencyTests.cs`.

> **Ngoại lệ duy nhất, đừng "dọn" nhầm:** `daysUntil()` bên FE vẫn còn và chỉ phục vụ **lịch họp**.
> Buổi họp là *cuộc hẹn*, không phải hạn nộp — nhãn "quá hạn 3 ngày" cho một buổi họp đã diễn ra là
> sai nghĩa, nên `MeetingsAgenda` cố ý **không** dùng `DeadlineBadge`.

---

## 2. Chạy dự án

### Local

```bash
# BE
cd FURPMS_BEv2
docker compose up -d                 # PostgreSQL 16, cổng 5433
dotnet run --project FURPMS.API      # :5068 — tự Migrate + seed

# FE (cửa sổ khác)
cd ../furpms-web && npm run dev     # :5173
```

Tài khoản demo (chỉ có ở Development): `admin@furpms.edu.vn` · `staff.demo@…` · `pi.demo@…` ·
`pi2.demo@…` · `reviewer1..5.demo@…` — **mật khẩu tất cả là `password`**.

> ⚠️ Container tên `furpms-db-1` trùng tên với container SQL Server cũ. Máy còn container cũ thì
> `docker rm -f furpms-db-1` trước, không thì port map sai do Docker giữ cấu hình cũ.

Làm lại từ dữ liệu sạch:
```bash
docker exec -i furpms-db-1 psql -U postgres -c "DROP DATABASE IF EXISTS furpms"
docker exec -i furpms-db-1 psql -U postgres -c "CREATE DATABASE furpms"
```

### Bản deploy

| | |
|---|---|
| BE | Railway — `https://furpmsbev2-production.up.railway.app` (project `abundant-endurance`) |
| DB | PostgreSQL service **cùng project** (bắt buộc — mạng nội bộ `.railway.internal` không thông giữa hai project) |
| FE | Vercel — `https://furpms-web.vercel.app` (project `furpms-web`, tài khoản `trunghq54`, deploy từ **fork**) |
| Nhánh BE đang deploy | **`master`** (đổi 18/08; trước là `thu-nghiem/postgres`). Commit ở `dev` **không tự lên Railway** — phải merge sang `master` |
| Nhánh FE đang deploy | **`dev`** — repo gốc `github.com/immanhdung/FURPMS-Web`. `main` đi sau `dev` **154 commit** (dừng ở 15/07), deploy nhầm `main` là ra bản của tháng trước |

Fork mặc định lấy nhánh `main`, nên sau khi fork **phải đổi Production Branch sang `dev`** (GitHub →
Settings → Default branch, hoặc Vercel → Settings → Git). Biến trên Vercel: `VITE_API_BASE_URL` =
URL Railway + `/api` — không đặt cũng chạy vì `.env.production` đã commit sẵn giá trị đó, đặt ở
dashboard thì **đè lên file** (đổi URL BE khỏi phải commit).

Biến môi trường trên Railway (`:` trong JSON → `__`):

```
ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}}
JwtSettings__SecretKey               (bắt buộc — khoá trong repo là khoá mẫu)
EmailSettings__FrontendUrl           = https://furpms-web.vercel.app   ← ĐẶT NGAY nếu chưa
                                       (mọi nút "bấm vào đây" trong email ghép từ biến này;
                                        bỏ trống là rơi về localhost:5173 ⇒ link chết với người nhận)
EmailSettings__SmtpUsername / __SmtpPassword
GeminiAI__ApiKey / __Model
Cloudinary__CloudName / __ApiKey / __ApiSecret / __Folder   (thiếu là tệp bay mỗi lần redeploy)
```

Ứng dụng **tự soi cấu hình** khi khởi động ngoài Development (`ProductionReadinessCheck`) và ghi
cảnh báo cho từng thứ còn thiếu. Đọc Deploy Logs là biết.

---

## 3. Bốn cái bẫy đã gặp thật khi deploy — đừng mất thời gian lại

| Triệu chứng | Nguyên nhân thật | Đã xử lý thế nào |
|---|---|---|
| FE: bấm menu thì chạy, **F5 giữa chừng hoặc dán link `/proposals/my` cho người khác → 404** | App dùng `BrowserRouter`; các đường dẫn đó chỉ tồn tại trong trình duyệt, trên đĩa không có file nào tên vậy. Rất dễ bỏ sót vì tự test toàn vào từ trang chủ | `vercel.json` rewrite mọi đường dẫn về `/index.html`. Rewrite chạy **sau** bước tìm file thật nên `/assets/*.js` vẫn được phục vụ đúng |
| `Couldn't set data source` + `KeyNotFoundException` | Biến vẫn là chuỗi **SQL Server**, Npgsql không hiểu `Server=`/`Data Source=` | `PostgresConnectionString` bắt sớm, báo rõ và chỉ chỗ sửa |
| `SocketException: Name or service not known` | Postgres ở **project khác** ⇒ DNS nội bộ không phân giải được | Phải đặt DB **cùng project** |
| Vẫn `Name or service not known` dù cùng project | Mạng nội bộ Railway mất **vài giây** mới sẵn sàng; app gọi `Migrate()` ngay dòng đầu | `DatabaseStartup` thử lại 10 lần × 3 giây; chỉ thử lại lỗi **mạng** |

`PostgresConnectionString.Resolve` chấp nhận **cả ba** dạng: URI (`postgresql://…` của
Railway/Render/Heroku), khoá=giá trị của Npgsql, và bộ biến rời `PGHOST/PGPORT/…`. Tự chọn SSL theo
tên máy chủ (`.internal` → tắt, công khai → bật).

---

## 4. Kiến trúc — điểm cần biết để không phá

- **N-tier:** Controller → Service → Repository → `FURPMSDbContext`. Không MediatR, không AutoMapper.
- **Logic nghiệp vụ nằm ở Service**, không nhét trong controller. Controller chỉ lấy danh tính từ
  claim rồi gọi service.
- **Mã lỗi ổn định:** mọi phản hồi thất bại kèm `errorCode` (xem `ErrorCodes`). Giao diện tra
  `errors.<mã>` trong bảng dịch, không có thì rơi về câu tiếng Việt máy chủ gửi kèm. Nhờ vậy chuyển
  đổi được từng phần.
  - Dùng `AppException(code, message, status, details)` khi cần mã **cụ thể**; ngoại lệ chuẩn
    (`KeyNotFoundException`…) vẫn chạy và nhận mã chung do middleware suy ra.
  - ⚠️ **401 = chưa/hết đăng nhập ⇒ giao diện TỰ ĐĂNG XUẤT.** Đừng dùng 401 cho lỗi nghiệp vụ.
    (Đã từng sai: gõ sai mật khẩu hiện tại khi đổi mật khẩu trả 401 ⇒ người dùng bị đá ra ngoài.)
- **Văn bản xuất ra ghim culture `vi-VN`** (`DocumentExportService.Vi`). Dấu `/` trong
  `"dd/MM/yyyy"` là *chỗ dành cho dấu phân cách của culture*, không phải ký tự cố định — không ghim
  thì cùng một hợp đồng xuất ở hai máy ra hai kiểu ngày.
- **Ngày tháng ở giao diện dùng `DD/MM/YYYY` dạng số**, không dùng `MMM` (tên tháng theo locale
  dayjs, mặc định là `en`).

---

## 5. Việc còn lại

### Bắt buộc trước khi bảo vệ

| Việc | Ai làm |
|---|---|
| ~~Đặt `EmailSettings__FrontendUrl` trên Railway~~ | ✅ **Xong** — kiểm 18/08 trên Railway: đã đặt `https://furpms.vercel.app`. ⚠️ Lưu ý có **hai** tên miền cùng sống: `furpms.vercel.app` và `furpms-web.vercel.app`. Biến đang trỏ cái thứ nhất; chốt hẳn một cái rồi sửa mọi doc theo, không thì link trong email dẫn về bản người dùng không dùng |
| ~~Deploy FE lên Vercel~~ | ✅ **Xong 14/08** — `furpms-web.vercel.app`, nhánh `dev`. Đã thử thật: đăng nhập admin vào được `/dashboard`, F5 giữa chừng vẫn đúng, không lỗi console, không API ≥ 400 |
| Tạo tài khoản thật cho từng vai trên bản deploy | Bạn — không còn tài khoản demo. DB deploy đang **rỗng**: 0 đợt, 0 đề cương, 0 hội đồng, 0 hợp đồng |
| Đi hết luồng chính trên bản deploy | Bạn |

### Backlog còn treo (`BACKLOG_Uu_tien.md`) — đều P2/P3, không chặn luồng chính

`P2-3` gợi ý tiêu đề lịch họp · `P2-6` màu ô nhận xét ·
`P2-9` phạm vi Admin sửa hồ sơ người khác *(cần bạn quyết, không phải việc code)* ·
`P3-1` Google Meet · `P3-3` đo token AI · `P3-4` `/ai/search` · `HD-7` hash file bản ký

> Tôi đã khuyên **bỏ hẳn** `P3-2`/`HD-9` (làm lại giao diện hợp đồng) — chụp màn rồi, màn đó là
> màn khá nhất hệ thống. Thầy chê **file Word**, đã sửa theo Nghị định 30/2020.

---

## 6. Cách kiểm tra nhanh mọi thứ còn chạy

```bash
cd FURPMS_BEv2 && dotnet build && dotnet test        # phải 300/300 xanh
cd furpms-web  && npx tsc -p tsconfig.app.json --noEmit && npm run build
```

> ⚠️ `npx tsc --noEmit` ở thư mục gốc FE **không kiểm gì cả** (`tsconfig.json` có `"files": []` +
> references). **Phải** dùng `-p tsconfig.app.json`.

### Quét giao diện bằng trình duyệt

Script nằm ở `.e2e/` trong repo FE (thư mục này **gitignore**, phải viết lại nếu clone mới).
Cách làm đúng — đã trả giá để rút ra:

1. **Lấy danh sách route từ `src/constants/nav.ts`**, đừng tự gõ. Tôi từng quét `/cycles`,
   `/tracks`, `/statistics`, `/proposals/my` — **không route nào tồn tại**, và trang 404 render
   sạch nên báo "OK". Kết quả sạch giả.
2. Khi bóc `nav.ts`, **bỏ qua dòng comment** — vài mục bị ẩn cố ý (`budget-categories`,
   `financial-config` theo rule #15; `documents` chưa dùng). Không bỏ thì báo 404 giả.
3. **Xoá `localStorage` giữa các vai.** Còn token cũ thì vào `/login` bị chuyển thẳng về
   `/dashboard`, kịch bản kẹt ở vai đầu tiên.
4. **Phân biệt bốn kết cục**, đừng chỉ hỏi "có lỗi không": *ok* · *404 route không tồn tại* ·
   *bị chặn quyền* · *lỗi thật*. Gộp lại là mất hẳn loại 2 và 3.
5. **Đừng `await r.text()` trong handler `response`** của Playwright — nó chặn luồng, làm mọi
   điều hướng sau đó treo.

Kết quả lần chạy 14/08: **40 route × 4 vai — 0 vấn đề** (local, PostgreSQL).

---

## 7. Nếu bàn giao cho AI khác — nói đúng những câu này

> Dự án FURPMS, capstone SU26SE053, quản lý đề tài NCKH theo **QĐ 543/QĐ-ĐHFPT**.
> BE .NET 8 + **PostgreSQL** (`D:\capstone\newroot\FURPMS_BEv2`, nhánh `thu-nghiem/postgres`).
> FE React 19 + Vite (`furpms-web`, nhánh `dev`).
>
> Đọc theo thứ tự: `docs/HANDOFF_HIEN_HANH.md` → `CLAUDE.md` (quy tắc nghiệp vụ #1–29, **không
> được tự đoán**) → `docs/KICH_BAN_DEMO.md`.
>
> Quy ước làm việc: mọi thay đổi phải **build xanh + test xanh** trước khi coi là xong; sửa code
> thì **cập nhật doc cùng lượt**; **không tự commit** trừ khi được yêu cầu; mọi kết luận nghiệp vụ
> phải **tra QĐ543** chứ không đoán.
