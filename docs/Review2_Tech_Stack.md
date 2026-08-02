# FURPMS — Product / Tech (Review 2)

> Trả lời mục **2. Product/Tech** trong checklist Khoa. Đối chiếu từ `package.json` (FE), `FURPMS.*.csproj` (BE), `Program.cs`, `.github/workflows/ci.yml`, `Dockerfile`.
> Chỗ ghi *[xác nhận]* = nhóm điền cho chính xác.

## 1. Dịch vụ bên thứ 3 (Third-party Services)

| Hạng mục | Dịch vụ | Dùng để làm gì |
|---|---|---|
| **AI API** | Google **Gemini API** (`generativelanguage.googleapis.com`, model `gemini-flash-latest`) | Trích xuất đề cương Word/PDF → field cấu trúc; gợi ý nhận xét chấm điểm |
| **Email** | **Brevo** (Sendinblue) — SMTP relay | Gửi thông báo / thư mời hội đồng / nhắc deadline |
| **Database hosting** | **site4now** — SQL Server free hosting | DB production |
| **App hosting** | **Render** (Docker web service) | Chạy BE production |
| **Authentication** | *Tự xây* — JWT Bearer + BCrypt hash mật khẩu | KHÔNG dùng dịch vụ ngoài (không Auth0/Firebase) |
| **Storage** | *Đĩa local* `App_Data/uploads` trên server | Lưu file đính kèm đề cương. *(Ghi chú: có thể nâng lên Azure Blob/S3 sau)* |

## 2. Công nghệ phát triển (Development)

| Lớp | Công nghệ |
|---|---|
| **Back-end** | .NET 8 · ASP.NET Core Web API · Entity Framework Core 8 (Code-First, Migrations) · kiến trúc **N-tier** (Controller → Service → Repository → DbContext) · BCrypt.Net (hash) · DocumentFormat.OpenXml (đọc .docx) · ClosedXML (xuất Excel) |
| **Front-end (Web)** | React 18 · TypeScript · Vite 6 · Tailwind CSS 4 · axios · react-router 7 · recharts (biểu đồ) · lucide-react (icon) · xlsx + docx-preview (xem tài liệu) · ESLint 9 + Prettier |
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
