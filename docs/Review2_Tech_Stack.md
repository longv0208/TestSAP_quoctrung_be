# FURPMS — Product / Tech (Review 2)

> Trả lời mục **2. Product/Tech** trong checklist Khoa. Đối chiếu từ `package.json` (FE), `FURPMS.*.csproj` (BE), `Program.cs`, `.github/workflows/ci.yml`, `Dockerfile`.
> Chỗ ghi *[xác nhận]* = nhóm điền cho chính xác.

## 1. Dịch vụ bên thứ 3 (Third-party Services)

| Hạng mục | Dịch vụ | Dùng để làm gì |
|---|---|---|
| **AI API** | Google **Gemini API** (`generativelanguage.googleapis.com`, model `gemini-flash-latest`) — xác thực bằng header `X-goog-api-key`, nhận cả key cũ `AIza…` lẫn key mới `AQ.…` | **5 tính năng (đã chạy thật 05/08):** trích xuất đề cương Word/PDF → prefill form · **đối chiếu form ↔ file đính kèm** · tóm tắt đề cương · góp ý đề cương · **gợi ý điểm theo từng tiêu chí** cho hội đồng. Kết quả cache ở `llm_outputs`. **Free tier là đủ** (gọi theo yêu cầu, không chạy nền). ⚠️ Gemini **không nhận `.docx` inline** → `GeminiFileInput` bóc text bằng OpenXml trước |
| **Email** | **Brevo** (Sendinblue) — SMTP relay, cổng 587 STARTTLS | Thư mời hội đồng · nhắc hạn · **kết quả xét duyệt** · **sản phẩm đạt/không đạt**. Có công tắc Admin `EMAIL_ENABLED` + `CatchFakeMailInbox`/`RedirectDomains` (dev hứng mail của **miền giả** về 1 hộp thư; địa chỉ thật đi thẳng). ⚠️ `FromEmail` là `@gmail.com` gửi qua relay ⇒ SPF/DKIM không khớp ⇒ **mail vào Spam**; muốn sạch phải có domain riêng |
| **Database hosting** | **site4now** — SQL Server free hosting | DB production |
| **App hosting** | **Render** (Docker web service) | Chạy BE production. ⚠️ **filesystem TẠM** (mất file khi redeploy) + **ngủ sau ~15 phút** không dùng |
| **FE hosting** | **Vercel** | ⚠️ Vite nhúng env lúc **build** ⇒ đổi URL API phải **redeploy** |
| **Authentication** | *Tự xây* — JWT Bearer + BCrypt hash mật khẩu | KHÔNG dùng dịch vụ ngoài (không Auth0/Firebase) |
| **Storage** | **Cloudinary** (upload `resource_type=raw` cho .docx/.pdf) — tự động về **đĩa local** nếu không cấu hình `Cloudinary:*` | Lưu mọi file đính kèm: đề cương · minh chứng giải ngân · báo cáo tiến độ (BM06) · báo cáo tổng kết (BM09) · hồ sơ hợp đồng đã ký. 🔒 **URL Cloudinary KHÔNG bao giờ trả cho FE** — đã kiểm chứng là tải được không cần đăng nhập; người dùng luôn qua `/documents/{id}/download` để BE kiểm quyền. Đổi chỗ lưu chỉ sửa `IFileStorage`, không migration |
| **Lịch / họp trực tuyến** | *chưa dùng* — Staff **dán link thủ công** | Muốn tự sinh link Meet thì phải qua **Google Calendar API** (Meet không có API riêng tiện dụng) + OAuth. Xem `PLAN_Week12.md` §Q4 |

## 2. Công nghệ phát triển (Development)

| Lớp | Công nghệ |
|---|---|
| **Back-end** | .NET 8 · ASP.NET Core Web API · Entity Framework Core 8 (Code-First, Migrations) · kiến trúc **N-tier** (Controller → Service → Repository → DbContext) · BCrypt.Net (hash) · DocumentFormat.OpenXml (đọc .docx) · ClosedXML (xuất Excel) |
| **Front-end (Web)** | **React 19** · TypeScript · Vite · Tailwind CSS 4 · axios · react-router 7 · **TanStack Query** (cache/đồng bộ server state) · **react-i18next** (vi/en, parity 1367 key) · **zod + react-hook-form** (validate — *mới phủ 12/34 nhóm màn, xem §Q3 của PLAN_Week12*) · **motion/react** (animation) · recharts · lucide-react · xlsx + docx-preview · ESLint 9 + Prettier |
| **Mobile** | Repo riêng — *[React Native — xác nhận]*; scope chỉ **PI + Staff**: xem trạng thái hồ sơ, lịch họp, nhận **Notification** nhắc deadline (không đưa chấm điểm hội đồng lên mobile) |
| **Database** | **SQL Server** (LocalDB / Docker khi dev, site4now khi production) |
| **Tài liệu/Swagger** | Swashbuckle/Swagger UI cho API docs |

## 3. Quản lý source code & DevOps

| Hạng mục | Công cụ |
|---|---|
| **Source control** | **GitHub** — BE repo `FURPMS_BE`, FE repo `FURPMS-Web`, Mobile repo riêng |
| **CI (tự động)** | **GitHub Actions** — BE: `dotnet restore/build/test` (Release, EF InMemory, không cần SQL); FE: `npm ci` + ESLint + `tsc --noEmit` + `vite build`. Chạy mỗi push & PR |
| **Đóng gói** | **Docker** (multi-stage .NET 8) cho BE deploy |
| **Quy trình** | Branch + Pull Request → review → merge; add Collaborator (không fork trong nội bộ team) |

## 4. Môi trường Deploy (Deployment Environments)

| Môi trường | BE | FE | Database |
|---|---|---|---|
| **Production** | Render (Docker) — `https://furpms-be.onrender.com` | *[Vercel / Netlify / khác — xác nhận]* | site4now SQL Server |
| **Development / Local** | localhost (Kestrel, cổng 8080) | Vite dev server (`localhost:5173`) | SQL Server LocalDB / Docker |

> **Secret/config** (connection string, JWT secret, Gemini key, SMTP) để ở **biến môi trường** (Render dashboard cho prod, `appsettings.Development.json` gitignored cho local) — KHÔNG commit vào repo.
