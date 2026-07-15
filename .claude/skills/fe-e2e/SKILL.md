---
name: fe-e2e
description: Boot FURPMS BE (.NET, port 5068) + FE (Vite, port 5173) rồi lái Chrome headless (Playwright) test UI end-to-end — login 4 role demo, đi các luồng nghiệp vụ, chụp screenshot, bắt console error + API >= 400.
---

# FURPMS — chạy app + E2E browser test

## Đường dẫn & port
- BE: `d:\Downloads\doc\9 đồ án\FURPMS\FURPMS_BE` → `dotnet run --project FURPMS.API` → http://localhost:5068 (tự Migrate + seed khi boot; cần SQL Server docker `localhost:1433`, sa / `Furpms@Strong123`).
- FE: `d:\Downloads\doc\9 đồ án\FURPMS\Fefurpmsv0` → `npm run dev` → http://localhost:5173 (VITE_API_URL mặc định trỏ 5068).
- Playwright đã có trong devDependencies của FE; Chromium headless đã tải về `%LOCALAPPDATA%\ms-playwright`. Nếu thiếu: `npx playwright install chromium`.

## Boot (background, poll port — đừng sleep chay)
```bash
cd "$BE" && (dotnet run --project FURPMS.API > /tmp/be.log 2>&1 &)
cd "$FE" && (npm run dev > /tmp/fe.log 2>&1 &)
timeout 40 bash -c 'until curl -sf http://localhost:5173 >/dev/null; do sleep 1; done'
# BE sẵn sàng khi login trả 200:
curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:5068/api/auth/login \
  -H "Content-Type: application/json" -d '{"email":"admin@furpms.edu.vn","password":"Admin@123456"}'
```
Tắt: `taskkill //F //IM FURPMS.API.exe; taskkill //F //IM dotnet.exe; taskkill //F //IM node.exe`

## Tài khoản demo (seeder)
| Role | Email | Mật khẩu |
|---|---|---|
| Admin | admin@furpms.edu.vn | Admin@123456 |
| Staff | staff.demo@furpms.edu.vn | Staff@123456 |
| PI | pi.demo@furpms.edu.vn | Faculty@123456 |
| PI 2 | pi2.demo@furpms.edu.vn | Faculty@123456 |
| Reviewer 1–3 | reviewer{1,2,3}.demo@furpms.edu.vn | Reviewer@123456 |

## Script driver (ESM — PHẢI đặt trong thư mục FE để resolve node_modules)
Đặt file vào `Fefurpmsv0/.e2e/*.mjs` (đã gitignore). Khung chuẩn:
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
await page.fill('input[type="password"]', 'Admin@123456')
await page.click('button[type="submit"]'); await page.waitForTimeout(2500)
await page.screenshot({ path: '.e2e/shots/x.png' })
```
Chạy: `cd Fefurpmsv0 && node .e2e/ten-script.mjs` → **đọc screenshot bằng Read tool** để nhìn UI thật.

## Đặc thù UI đã dò được (đỡ mò lại)
- Nút login/điều hướng đều tiếng Việt: PI có "Điền dữ liệu mẫu" (fill form mẫu), wizard 5 bước nút "Tiếp", cuối là "Lưu nháp"; nộp từ workspace = nút "Nộp duyệt" (có dialog xác nhận CV — bấm nút xác nhận trong dialog).
- Form chấm điểm reviewer dùng **slider `input[type=range]`** (không phải number) — set qua native setter + dispatch `input` event (React controlled).
- Menu admin/staff nằm trong `nav/aside`; PI + reviewer là nút top-bar (locator theo text).
- 2 chỗ 404 pre-existing (đã biết, đừng báo lại như bug mới): menu "Yêu cầu thay đổi" (change-requests chưa có BE) + nút "✨ AI gợi ý" khi chấm (ai-feedback chưa có BE).

## Ngoài ra
- Có Playwright MCP đăng ký scope user (`claude mcp list` → playwright) — session mới có thể điều khiển browser trực tiếp bằng tool MCP thay vì viết script.
