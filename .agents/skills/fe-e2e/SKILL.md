---
name: fe-e2e
description: Boot FURPMS BE (.NET, port 5068) + FE (Vite, port 5173) rồi lái Chrome headless (Playwright) test UI end-to-end — login các vai demo, đi các luồng nghiệp vụ, chụp screenshot, bắt console error + API >= 400.
---

# FURPMS — chạy app + E2E browser test

## Đường dẫn & port

> ⚠️ **Trước 18/08 file này trỏ SAI cả ba thứ**: repo BE đã chết, SQL Server (đã bỏ từ 14/08), và
> mật khẩu demo cũ. Ai chạy theo bản cũ sẽ dựng nhầm repo lên một DB không tồn tại rồi không hiểu
> vì sao login hỏng. Bảng dưới đã đối chiếu code thật.

- **BE: `D:\capstone\FURPMS_BEv2`** ← repo `trunghq54/FURPMS_BEv2`, KHÔNG phải
  `…\FURPMS\FURPMS_BE` (repo đó chết từ 02/08/2026).
  `dotnet run --project FURPMS.API` → http://localhost:5068 (Swagger `/swagger`).
  Tự `Migrate()` + seed khi khởi động. Cần **PostgreSQL 16 docker ở cổng `5433`**
  (`postgres` / `Furpms@Strong123`) — `docker compose up -d` trong chính thư mục BE.
- **FE: `D:\Downloads\doc\9 đồ án\core\FURPMS-Web`** → `npm run dev` → http://localhost:5173.
  **Không cần tạo `.env`** — `.env.development` đã commit và trỏ sẵn về `:5068`.
- Playwright có sẵn trong devDependencies của FE; Chromium headless ở `%LOCALAPPDATA%\ms-playwright`.
  Thiếu thì `npx playwright install chromium`.

## Boot (chạy nền, poll cổng — đừng `sleep` chay)
```bash
BE="D:/capstone/FURPMS_BEv2"
FE="d:/Downloads/doc/9 đồ án/core/FURPMS-Web"

cd "$BE" && docker compose up -d && (dotnet run --project FURPMS.API > /tmp/be.log 2>&1 &)
cd "$FE" && (npm run dev > /tmp/fe.log 2>&1 &)
timeout 40 bash -c 'until curl -sf http://localhost:5173 >/dev/null; do sleep 1; done'

# BE sẵn sàng khi login trả 200:
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5068/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@furpms.edu.vn","password":"password"}'
```

> BE mất khá lâu mới lên vì lúc khởi động nó **quét sinh sẵn tóm tắt AI**. Log đầy dòng gọi Gemini
> là bình thường, không phải treo. Nếu thật sự không lên nổi cổng, xem `AGENTS.md` §4.2.

Tắt: `taskkill //F //IM FURPMS.API.exe; taskkill //F //IM dotnet.exe; taskkill //F //IM node.exe`

## Tài khoản demo (seeder, chỉ có ở môi trường Development)

**Tất cả cùng mật khẩu `password`.** (Bản cũ của file này ghi `Admin@123456` / `Staff@123456` /
`Faculty@123456` — sai hết, đó là mật khẩu từ trước lần siết seeder 14/08.)

| Vai | Email |
|---|---|
| Quản trị | `admin@furpms.edu.vn` |
| Cán bộ QLKH | `staff.demo@furpms.edu.vn` |
| Chủ nhiệm đề tài | `pi.demo@furpms.edu.vn`, `pi2.demo@furpms.edu.vn` |
| Ủy viên hội đồng | `reviewer1.demo@` … `reviewer5.demo@furpms.edu.vn` (**5 người**, không phải 3) |

## Script driver (ESM — PHẢI đặt trong thư mục FE để resolve `node_modules`)
Đặt file vào `core/FURPMS-Web/.e2e/*.mjs` (đã gitignore). Khung chuẩn:
```js
import { chromium } from 'playwright'
const browser = await chromium.launch({ args: ['--no-sandbox'] })
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } })
const page = await ctx.newPage()
const issues = []
page.on('console', m => { if (m.type() === 'error') issues.push('CONSOLE: ' + m.text()) })
page.on('response', r => { if (r.status() >= 400 && r.url().includes('/api/')) issues.push(`HTTP ${r.status()} ${r.url()}`) })
page.on('dialog', d => d.accept())
// login: input[type=email] + input[type=password] + button[type=submit], chờ ~2.5s
await page.goto('http://localhost:5173', { waitUntil: 'networkidle' })
await page.fill('input[type="email"]', 'admin@furpms.edu.vn')
await page.fill('input[type="password"]', 'password')
await page.click('button[type="submit"]'); await page.waitForTimeout(2500)
await page.screenshot({ path: '.e2e/shots/x.png' })
```
Chạy: `cd "$FE" && node .e2e/ten-script.mjs` → **đọc screenshot bằng Read tool** để nhìn UI thật.

## Đặc thù UI đã dò được (đỡ mò lại)
- **Giao diện SONG NGỮ (vi/en)** — locator theo text sẽ gãy khi đổi ngôn ngữ. Ưu tiên
  `role`/`placeholder`/`type`. Buổi bảo vệ demo bằng **tiếng Anh**.
- PI có nút "Điền dữ liệu mẫu"; wizard 5 bước nút "Tiếp", cuối là "Lưu nháp"; nộp từ workspace =
  "Nộp duyệt" (có hộp thoại xác nhận lý lịch — phải bấm nút xác nhận trong hộp thoại).
- Form chấm điểm dùng **slider `input[type=range]`** (không phải number) — set qua native setter +
  dispatch `input` event (React controlled). Có nút điền nhanh toàn bộ tiêu chí.
- Menu admin/staff nằm trong `nav/aside`; PI + reviewer là nút top-bar.
- Vòng **NGHIỆM THU** khác vòng xét duyệt: chỉ **phản biện** (`memberRole = "Opponent"`) mới chấm
  được (BM10); cột trái là hồ sơ nghiệm thu chứ không phải file đề cương + thẻ AI.

## Ngoài ra
- Có Playwright MCP đăng ký ở scope user (`Codex mcp list` → playwright) — có thể điều khiển
  trình duyệt trực tiếp bằng tool MCP thay vì viết script.
- Chi tiết kiến trúc/bẫy: `AGENTS.md` ở gốc mỗi repo.
