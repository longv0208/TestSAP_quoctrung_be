# FURPMS — Export Test Guide (Phase 4)

**Date:** 2026-06-10

---

## Prerequisites

The demo proposal `DEMO-2026-001` is seeded automatically on first startup. It includes:
- PI user: `pi.demo@furpms.edu.vn` / `Faculty@123456`
- 3 team members (PI + secretary + 1 member) with role codes and salary coefficients
- 3 research contents with 5 activities (Gantt spans months 1–18)
- 3 expected products
- Budget total: 900,000,000 VNĐ with 4 budget items across categories
- 3 labor details with `WorkDays`, `Coefficient`, `DailyRate` (BASE_DAILY_SALARY = 1,490,000)

---

## Step 1 — Start the API

```bash
cd FURPMS.API
dotnet run
```

Swagger UI: `https://localhost:<port>/swagger`

---

## Step 2 — Authenticate

**POST /api/auth/login**

```json
{
  "email": "pi.demo@furpms.edu.vn",
  "password": "Faculty@123456"
}
```

Copy the `token` from the response. In Swagger, click **Authorize** and enter `Bearer <token>`.

*Alternatively use Admin credentials:* `admin@furpms.edu.vn` / `Admin@123456`

---

## Step 3 — Get the Demo Proposal ID

**GET /api/proposals** *(if such an endpoint exists)* or query the DB directly:

```sql
SELECT id, proposal_code FROM proposals WHERE proposal_code = 'DEMO-2026-001';
```

Note the GUID (e.g. `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`).

---

## Step 4 — Download Scientific Thuyết Minh (.docx)

```
GET /api/proposals/{id}/export/scientific
```

In Swagger: click **Try it out**, enter the proposal ID, click **Execute**.
The browser will download `ThuyetMinh_DEMO-2026-001.docx`.

**Verify the document contains:**
| Section | Expected content |
|---|---|
| Title | "Nghiên cứu ứng dụng học máy trong phân tích cảm xúc văn bản tiếng Việt" |
| PI name | Nguyễn Văn An (TS) |
| Duration | 18 tháng |
| Funding method | ☑ Khoán từng phần (PARTIAL) |
| Team table | 3 rows: PI, secretary (Trần Thị Bình), Lê Minh Cường |
| Research contents | 3 contents with descriptions |
| Gantt chart | 5 activities, months 1–18 marked with X |
| Expected products | 3 rows |

---

## Step 5 — Download Budget Dự Toán (.xlsx)

```
GET /api/proposals/{id}/export/budget
```

Downloads `DuToan_DEMO-2026-001.xlsx`.

**Sheet "Tong hop" — verify:**
| Row | Category | Total (VNĐ) |
|---|---|---|
| 1 | Công lao động trực tiếp | 849,747,000 |
| 5 | Chi văn phòng phẩm, in ấn | 1,003,000 |
| 7 | Chi Hội đồng tư vấn | 7,250,000 |
| 12 | Chi quản lý phí cơ quan chủ trì | 42,000,000 |
| **Tổng** | | **900,000,000** |

**Sheet "Tong hop tien cong" — verify:**
| Row | Name | Role | Days | Coeff | Daily rate | Total |
|---|---|---|---|---|---|---|
| 1 | Nguyễn Văn An | CNNV | 255 | 0.79 | 1,177,100 | 300,160,500 |
| 2 | Trần Thị Bình | TKKH | 215 | 0.49 | 730,100 | 156,971,500 |
| 3 | Lê Minh Cường | TVC | 141 | 0.49 | 730,100 | 102,944,100 |

---

## Step 6 — Authorization check

The export endpoints require the caller to be:
- **Admin** or **Staff** role, OR
- The **PI** of the proposal (token's user ID == `proposal.pi_user_id`)

Test the negative case: log in as `admin@furpms.edu.vn`, create a second proposal (if any), then try to download it as the demo PI user — expect **401 Unauthorized**.

---

## Step 7 — Create your own proposal via Swagger (optional)

To test with custom data:

1. POST `/api/proposals` (if endpoint exists, or insert directly via SQL)
2. POST `/api/proposals/{id}/team-members` — add members with `memberRoleCode`
3. PUT `/api/proposals/{id}/budget` — provide `totalAmount` and `items[]` (sum must equal totalAmount)
4. PUT `/api/proposals/{id}/budget/labor/{detailId}` — set `workDays` and `coefficient`
5. Download both exports and verify data matches.

---

## Known limitations (v1.4)

- Word document uses basic formatting only (no colors, no merged cells, no headers/footers from original template).
- Gantt chart columns capped at `min(durationMonths, 24)`.
- No page breaks or section breaks in the Word output.
- `TotalAmount` on labor details is a computed column (`totalResearchHours × hourlyRate`) — demo seeds this as 0 since no hourly rate is set; the Excel sheet shows `workDays × dailyRate` instead which is the correct Phase 3 formula.
