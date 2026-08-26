# AGENTS.md — FURPMS Backend

Đồ án tốt nghiệp **SU26SE053**, ĐH FPT. Hệ thống quản lý đề tài nghiên cứu khoa học, bám quy định
**QĐ 543/QĐ-ĐHFPT**. .NET 8 + PostgreSQL 16.

File này là điểm vào cho agent mới. Đọc hết trước khi sửa dòng code đầu tiên.

---

## 0. Trước hết — bạn có đang ở đúng thư mục không?

Trên máy chủ dự án có **ba thư mục trông giống nhau**, và chúng là **ba repo GitHub khác nhau**,
không phải bản sao của nhau:

| Thư mục | Repo | Trạng thái |
|---|---|---|
| `D:\capstone\newroot\FURPMS_BEv2` | `trunghq54/FURPMS_BEv2` | ✅ **BE ĐANG DÙNG** — chính là repo này |
| `D:\capstone\newroot\furpms-web` | `immanhdung/FURPMS-Web` | ✅ **FE đang dùng** |
| `D:\Downloads\doc\9 đồ án\FURPMS\FURPMS_BE` | `trunghq54/FURPMS_BE` | ❌ **CHẾT** từ 02/08/2026 |

> Cập nhật 18/08: hai repo đang dùng đã gom về **`D:\capstone\newroot\`** (mở chung một workspace
> VS Code). Đường dẫn `D:\capstone\FURPMS_BEv2` và `D:\Downloads\doc\9 đồ án\core\FURPMS-Web` ghi
> ở các bản doc cũ **không còn đúng**.

Thư mục `FURPMS_BE` cũ vẫn còn `CLAUDE.md` và `docs/` nhưng **nội dung đã lỗi thời** — ví dụ nó
vẫn ghi chuỗi kết nối SQL Server LocalDB, trong khi dự án đã chuyển sang PostgreSQL từ 14/08. Sửa
vào đó là công cốc: không ai lấy code từ repo ấy nữa.

**Cách tự kiểm:** `git remote -v` phải ra `FURPMS_BEv2`. Nếu ra `FURPMS_BE` (không có `v2`) thì
dừng lại, chuyển thư mục.

Nhánh làm việc: **`dev`**. `master` là nhánh chính để mở PR.

---

## 1. Chạy dự án

```bash
docker compose up -d          # PostgreSQL 16 ở cổng 5433 (KHÔNG phải 5432)
dotnet run --project FURPMS.API      # → http://localhost:5068  (Swagger: /swagger)
```

Cổng 5433 là cố ý — né trường hợp máy đã cài sẵn PostgreSQL native chiếm cổng mặc định.

App **tự chạy `Migrate()` + seeder khi khởi động** (qua `DatabaseStartup`, có thử lại vì mạng nội
bộ Railway mất vài giây mới sẵn sàng). Không cần chạy migration bằng tay để có DB dùng được.

Kiểm tra:
```bash
dotnet build            # phải 0 error
dotnet test             # 300 test, phải xanh hết
```

> `dotnet test` dùng **`UseInMemoryDatabase`** (`FURPMS.Tests/Helpers/TestDbContextFactory.cs`) —
> **không cần Docker, không cần PostgreSQL**. Chạy được trong sandbox không có mạng nội bộ.
> Chỉ khi muốn chạy thật `dotnet run` mới cần `docker compose up -d`.

### Tài khoản demo (CHỈ có ở môi trường Development)

| Vai | Email | Mật khẩu |
|---|---|---|
| Quản trị | `admin@furpms.edu.vn` | `password` |
| Cán bộ QLKH | `staff.demo@furpms.edu.vn` | `password` |
| Chủ nhiệm đề tài | `pi.demo@furpms.edu.vn`, `pi2.demo@furpms.edu.vn` | `password` |
| Ủy viên hội đồng | `reviewer1..5.demo@furpms.edu.vn` | `password` |

> Ngoài Development, seeder **không** đổ dữ liệu demo. Mật khẩu quản trị lấy từ biến
> `SeedAdminPassword`; không đặt thì sinh ngẫu nhiên và ghi log **một lần duy nhất**. Biến này chỉ
> có tác dụng khi **chưa có** tài khoản admin — admin đã tồn tại thì seeder bỏ qua hẳn.

### Thứ KHÔNG có trong repo

`appsettings.Development.json` bị gitignore và chứa **`GeminiAI:ApiKey`**. Clone mới sẽ **không có
key AI** → mọi tính năng AI ném lỗi *"Chưa cấu hình GeminiAI:ApiKey"*. Xin key từ chủ dự án.

`appsettings.json` (đã commit) **không chứa bí mật thật**: `JwtSettings.SecretKey` là chuỗi
placeholder dành cho máy cá nhân, `EmailSettings` để rỗng. Bí mật thật nằm ở biến môi trường trên
Railway. **Đừng commit key thật vào đây.**

---

## 2. Kiến trúc

N-tier thẳng: **Controller → Service → Repository → DbContext**. Không MediatR, không AutoMapper —
map tay. Đừng thêm hai thứ đó vào.

| Dự án | Chứa gì |
|---|---|
| `FURPMS.Domain` | Entity thuần, **không logic** |
| `FURPMS.Application` | Interface, DTO, hằng số, hợp đồng service |
| `FURPMS.Infrastructure` | `FURPMSDbContext`, cài đặt service, seeder, migration |
| `FURPMS.API` | Controller, middleware, `Program.cs` |

Repository ở đây **mỏng**, chủ yếu phơi `IQueryable<T>`. Nó giúp test/mock chứ **không** phải lớp
trừu tượng để đổi DB — thứ cho phép đổi DB là EF Core (đổi SQL Server → PostgreSQL chỉ tốn 5 dòng).

### Quy ước đặt tên

| Chỗ | Quy ước |
|---|---|
| Class C# | `PascalCase` |
| Bảng / cột DB | `snake_case` — **tự động** qua `UseSnakeCaseNamingConvention()`, đừng viết `HasColumnName` tay |
| Route | `kebab-case`, danh từ số nhiều: `/api/research-types` |
| DTO | `<Entity><Action>Request` / `<Entity><Action>Response` |

### Khóa chính

- Thực thể nghiệp vụ (`proposals`, `users`, `contracts`…) → **`Guid`**
- Bảng danh mục / cấu hình (`roles`, `research_types`…) → **`int` IDENTITY**
- Nhật ký (`audit_log`, `email_log`) → **`long` IDENTITY**
- **Xóa mềm chỉ ở `users` và `proposals`** (`IsDeleted`/`DeletedAt`/`DeletedBy`), kèm
  `HasQueryFilter(e => !e.IsDeleted)`

---

## 3. Trả lỗi — đọc kỹ, đây là chỗ hay sai nhất

Mọi controller trả `ApiResponse<T>` hoặc `ApiResponse`. Lỗi thì **ném exception**, đừng tự dựng
response lỗi — `GlobalExceptionMiddleware` quy đổi:

| Ném | HTTP | Dùng khi |
|---|---|---|
| `UnauthorizedAccessException` | 401 | **CHỈ** lỗi xác thực (chưa đăng nhập / sai mật khẩu) |
| `ForbiddenException` | 403 | Đã đăng nhập nhưng thiếu quyền với tài nguyên |
| `KeyNotFoundException` | 404 | Không tìm thấy |
| `ArgumentException` | 400 | Dữ liệu vào không hợp lệ |
| `InvalidOperationException` | 409 | Xung đột trạng thái nghiệp vụ |
| còn lại | 500 | |

> ⚠️ **Đừng ném `UnauthorizedAccessException` cho lỗi phân quyền.** FE thấy 401 là **đăng xuất
> người dùng ngay**. Thiếu quyền phải dùng `ForbiddenException` (403).

> ⚠️ **Câu trong `throw` của 400/409 là văn bản NGƯỜI DÙNG ĐỌC.** FE hiện nguyên văn (mã lỗi của
> hai nhóm này chỉ là thùng chứa chung `VALIDATION_FAILED`/`CONFLICT`, không có bản dịch riêng).
> Viết đủ hai ý: **vướng cái gì** (nêu tên/số liệu cụ thể) và **làm gì để thoát**. Ví dụ đạt:
>
> *"Người này đang là chủ nhiệm đề tài nên không xoá được — hãy dùng "Vô hiệu hoá" để khoá đăng
> nhập mà vẫn giữ tên trên hồ sơ đề tài."*
>
> Không nhét mã quy tắc nội bộ ("rule #7") vào câu. **Không viện dẫn điều khoản QĐ543 nếu chưa mở
> file quy định ra kiểm** — xem §6.

---

## 4. Bẫy đã cắn thật — đừng vấp lại

### 4.1 Ngày giờ luôn UTC

Cột PostgreSQL là `timestamptz`. **Npgsql ném lỗi** khi ghi `DateTime` có `Kind = Unspecified` →
500. `UtcDateTimeConverter` (đăng ký ở `Program.cs`) chuẩn hoá mọi `DateTime` đọc từ JSON, nên
service **không cần** tự `ToUniversalTime()`. Dùng `DateTime.UtcNow`, **không bao giờ** `DateTime.Now`.

### 4.2 `BackgroundService` phải nhả luồng trước khi làm việc nặng

.NET 8: `StartAsync` **await `ExecuteAsync` cho tới lần nhả luồng đầu tiên**. Làm việc nặng ngay
đầu `ExecuteAsync` ⇒ web server **không lên nổi cổng**, nhìn như build hỏng. Đã xảy ra thật với
`AiSummaryPregenerationService`: hết hạn mức Gemini là cả API chết theo. Luôn `await Task.Yield();`
ở dòng đầu.

Cũng lưu ý .NET 8 mặc định `BackgroundServiceExceptionBehavior.StopHost` — exception lọt ra khỏi
`ExecuteAsync` là **giết cả tiến trình**. Bọc `try/catch`.

### 4.3 Thứ tự build khi thêm migration

`dotnet ef migrations add` **build TRƯỚC** khi sinh file. Nên phải `dotnet build` lại rồi mới
`dotnet run --no-build`, không thì app báo "database is already up to date" mà bảng mới không có.

```bash
dotnet ef migrations add <Tên> --project FURPMS.Infrastructure --startup-project FURPMS.API
```

### 4.4 EF Core lặt vặt

- Cột tính sẵn: `HasComputedColumnSql("...", stored: true)`
- Ràng buộc check: `b.ToTable(t => t.HasCheckConstraint("tên", "sql"))`
- Unique cho phép null: `HasIndex(x => x.Col).IsUnique().HasFilter("\"col\" IS NOT NULL")`
  — cú pháp PostgreSQL dùng `"` chứ không phải `[ ]` của T-SQL
- Khóa ngoại vòng → đặt `OnDelete(DeleteBehavior.NoAction)` ở phía không sở hữu

### 4.5 Seeder phải idempotent

`DatabaseSeeder.SeedAsync()` chạy mỗi lần khởi động. **Luôn kiểm tra tồn tại trước khi chèn.** Và
mỗi khối phải có chốt idempotent **riêng** — đã có lần một lệnh `return` sớm khiến khối phía sau
không bao giờ chạy trên DB cũ.

---

## 5. Tài liệu — đọc theo thứ tự này

| Thứ tự | File | Vì sao |
|---|---|---|
| 1 | `docs/HANDOFF_HIEN_HANH.md` | Trạng thái hiện hành, hai thay đổi lớn gần nhất |
| 2 | `CLAUDE.md` | **Quy tắc nghiệp vụ đánh số #1–#24** — thứ không suy ra được từ code |
| 3 | `docs/Process_Spec_v2.md` | Nghiệp vụ đầy đủ — đọc trước khi code bất kỳ luồng nào |
| 4 | `docs/API_CONTRACT.md` | Hợp đồng FE↔BE. Nguồn chính xác nhất vẫn là Swagger `:5068/swagger` |
| 5 | `docs/ERD_v3_Project_Centric.dbml` | Sơ đồ DB duy nhất (~56 bảng) |
| 6 | `docs/PROGRESS.md` | % từng nhóm chức năng + việc nên làm tiếp |
| — | `docs/QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx` | **Văn bản quy định gốc** + toàn bộ biểu mẫu BM01–BM15 |

Mô hình dữ liệu là **v3 Project-centric** (sau Review 2): `Project` là thực thể gốc, `Proposal` là
**tài liệu có phiên bản** thuộc Project. Đừng nhầm hai cái.

---

## 6. Quy tắc nghiệp vụ — KHÔNG được tự suy đoán

`CLAUDE.md` có 24 quy tắc đánh số, kèm nguồn (buổi họp nào chốt). Chúng **không suy ra được từ
code**. Vài cái hay bị đoán sai:

- **#7** — 1 đợt = đúng 1 loại đề tài. Mở cả hai loại nghĩa là tạo **2 đợt độc lập**.
  Nhưng **không có giới hạn nào về số đợt mỗi năm** — đừng bịa ra.
- **#11** — Chủ nhiệm (PI) là actor **duy nhất** tương tác thay cho đề tài. Thành viên đề tài chỉ
  là dữ liệu, không đăng nhập.
- **#12** — Kết quả là **quyết định của Chủ tịch**, không phải đếm phiếu. Hệ thống hiện điểm chỉ để
  tham khảo. Thư ký soạn biên bản → Chủ tịch duyệt = khóa → mới cập nhật trạng thái đề tài.
- **#15** — **Hệ thống KHÔNG quản tiền.** Kế toán chi tiền ngoài hệ thống; hệ thống chỉ theo dõi
  mốc giải ngân + lưu minh chứng. Quy tắc #2/#3/#6 cũ đã bị thay thế bởi #15/#16.
- **#16** — **Chỉ 2 hội đồng**: Xét duyệt đề cương + Nghiệm thu. Báo cáo giữa kỳ do Staff duyệt
  trực tiếp, không lập hội đồng.
- **#24** — Vòng nghiệm thu: **chỉ phản biện** viết BM10 (4 tiêu chí × 1–5 điểm); **mọi thành viên
  có mặt** bỏ phiếu BM11 (chỉ Đạt / Không đạt, không có thang điểm).
- **COI (#5)** — Chủ nhiệm và thành viên đề tài **không được** là ủy viên hội đồng chấm chính đề
  tài đó. Đã chặn ở `ReviewShared.AssertNoCoiAsync`, gọi ở **cả hai** đường vào (thêm ủy viên · gán
  đề tài vào hội đồng). Giữ nguyên cả hai — bỏ một là thủng.

> ### ⚠️ Không bịa dẫn chứng quy định
> Đã có lần một quy tắc bị bịa ra rồi **viện dẫn "QĐ543 Điều 6"** trong chính thông báo lỗi hiện
> cho người dùng — trong khi điều đó nói chuyện khác hẳn. Nếu định viện dẫn điều khoản: **mở file
> `docs/QD_543_...docx` ra đọc trước**. Không kiểm được thì đừng viện dẫn.

---

## 7. Thế nào là "xong"

1. **`dotnet build` sạch + `dotnet test` xanh** (nêu rõ số test pass) trước khi coi là xong.
   Test đỏ thì báo kèm output, không giấu.
2. **Chạy thử thật** với endpoint vừa sửa nếu có thể (curl vào `:5068`), đừng chỉ tin build.
3. **Cập nhật tài liệu NGAY trong cùng lượt** — đừng để người khác phát hiện tài liệu lệch:

   | Sửa gì | Cập nhật |
   |---|---|
   | Thêm/sửa/xóa endpoint · đổi request/response · đổi `[Authorize]` · đổi enum | `docs/API_CONTRACT.md` |
   | Xong/bỏ một tính năng lớn | `docs/PROGRESS.md` |
   | Xong việc backlog · thêm/xóa file trong `docs/` | `docs/README.md` |
   | Đổi bảng/cột/quan hệ | `docs/ERD_v3_Project_Centric.dbml` |
   | Đổi luồng nghiệp vụ / state machine | `docs/Process_Spec_v2.md` |
   | Thầy hướng dẫn chốt quy tắc mới | `CLAUDE.md` — đánh số + ghi nguồn |

4. **KHÔNG tự commit** trừ khi chủ dự án yêu cầu trong đúng lượt đó.

---

## 8. Viết code cho khớp codebase

Codebase này **chú thích bằng tiếng Việt**, và chú thích giải thích **vì sao** chứ không mô tả lại
code. Nhiều chú thích ghi cả ngày tháng và lỗi cụ thể đã gặp. Giữ đúng phong cách đó:

```csharp
// NHẢ LUỒNG NGAY — bắt buộc, không phải cho đẹp.
//
// .NET 8: `BackgroundService.StartAsync` await ExecuteAsync cho tới lần nhả luồng đầu tiên.
// Trước 17/08 hàm này gọi thẳng SweepMissingAsync, nên cả vòng quét chạy TRƯỚC khi web server
// kịp lắng nghe cổng: Visual Studio treo ở "Waiting for the web server to listen on port 7003"
// và tưởng như build hỏng.
await Task.Yield();
```

Đừng viết chú thích kiểu `// tăng biến đếm`. Nếu chỗ đó không có gì đáng giải thích thì bỏ trống.

---

## 9. Frontend

FE nằm ở repo riêng: **`D:\capstone\newroot\FURPMS-Web`** (`immanhdung/FURPMS-Web`).
Xem `AGENTS.md` trong đó. Chạy `npm run dev` → `http://localhost:5173`, tự trỏ về `:5068`.

Sửa endpoint hay đổi hình dạng DTO ở BE thì **phải kiểm luôn FE** — không có sinh code tự động
giữa hai bên, kiểu TypeScript ở FE là chép tay và **sẽ không báo lỗi** khi BE đổi.
