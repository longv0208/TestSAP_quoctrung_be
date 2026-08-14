# HANDOFF — trạng thái hiện hành (cập nhật 14/08/2026)

> **Đọc file này ĐẦU TIÊN** nếu bạn là người/AI mới tiếp nhận dự án. Nó thay cho
> `HANDOFF_Week10.md` (đã lỗi thời ở phần hạ tầng). Sau file này thì đọc `../CLAUDE.md` (quy tắc
> nghiệp vụ đánh số) và `KICH_BAN_DEMO.md` (bấm theo từng bước).

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

---

## 2. Chạy dự án

### Local

```bash
# BE
cd FURPMS_BEv2
docker compose up -d                 # PostgreSQL 16, cổng 5433
dotnet run --project FURPMS.API      # :5068 — tự Migrate + seed

# FE (cửa sổ khác)
cd core/FURPMS-Web && npm run dev    # :5173
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
| FE | Vercel — **chưa deploy**, repo `github.com/immanhdung/FURPMS-Web`, code đầy đủ ở nhánh **`dev`** |
| Nhánh BE đang deploy | `thu-nghiem/postgres` |

Biến môi trường trên Railway (`:` trong JSON → `__`):

```
ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}}
JwtSettings__SecretKey               (bắt buộc — khoá trong repo là khoá mẫu)
EmailSettings__FrontendUrl           ← CHƯA ĐẶT. Link trong email đang trỏ localhost
EmailSettings__SmtpUsername / __SmtpPassword
GeminiAI__ApiKey / __Model
Cloudinary__CloudName / __ApiKey / __ApiSecret / __Folder   (thiếu là tệp bay mỗi lần redeploy)
```

Ứng dụng **tự soi cấu hình** khi khởi động ngoài Development (`ProductionReadinessCheck`) và ghi
cảnh báo cho từng thứ còn thiếu. Đọc Deploy Logs là biết.

---

## 3. Ba cái bẫy đã gặp thật khi deploy — đừng mất thời gian lại

| Triệu chứng | Nguyên nhân thật | Đã xử lý thế nào |
|---|---|---|
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
| Đặt `EmailSettings__FrontendUrl` trên Railway | **Bạn** — chưa đặt, link email đang trỏ localhost |
| Deploy FE lên Vercel (branch `dev`, biến `VITE_API_BASE_URL`) | Chờ chủ repo FE (`immanhdung`) |
| Tạo tài khoản thật cho từng vai trên bản deploy | Bạn — không còn tài khoản demo |
| Đi hết luồng chính trên bản deploy | Bạn |

### Backlog còn treo (`BACKLOG_Uu_tien.md`) — đều P2/P3, không chặn luồng chính

`P2-2` đổi nhãn màn Staff · `P2-3` gợi ý tiêu đề lịch họp · `P2-6` màu ô nhận xét ·
`P2-9` phạm vi Admin sửa hồ sơ người khác *(cần bạn quyết, không phải việc code)* ·
`P3-1` Google Meet · `P3-3` đo token AI · `P3-4` `/ai/search` · `HD-7` hash file bản ký

> Tôi đã khuyên **bỏ hẳn** `P3-2`/`HD-9` (làm lại giao diện hợp đồng) — chụp màn rồi, màn đó là
> màn khá nhất hệ thống. Thầy chê **file Word**, đã sửa theo Nghị định 30/2020.

---

## 6. Cách kiểm tra nhanh mọi thứ còn chạy

```bash
cd FURPMS_BEv2 && dotnet build && dotnet test        # phải 190/190 xanh
cd core/FURPMS-Web && npx tsc -p tsconfig.app.json --noEmit && npm run build
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
> BE .NET 8 + **PostgreSQL** (`D:\capstone\FURPMS_BEv2`, nhánh `thu-nghiem/postgres`).
> FE React 19 + Vite (`core/FURPMS-Web`, nhánh `dev`).
>
> Đọc theo thứ tự: `docs/HANDOFF_HIEN_HANH.md` → `CLAUDE.md` (quy tắc nghiệp vụ #1–29, **không
> được tự đoán**) → `docs/KICH_BAN_DEMO.md`.
>
> Quy ước làm việc: mọi thay đổi phải **build xanh + test xanh** trước khi coi là xong; sửa code
> thì **cập nhật doc cùng lượt**; **không tự commit** trừ khi được yêu cầu; mọi kết luận nghiệp vụ
> phải **tra QĐ543** chứ không đoán.
