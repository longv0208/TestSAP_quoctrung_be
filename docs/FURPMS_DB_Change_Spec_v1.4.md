# FURPMS — DB Change Spec v1.3 → v1.4
**Lý do:** cập nhật sau buổi họp với thầy Trí về module Proposal + đối chiếu 2 file mẫu thật (Thuyết minh khoa học – Mẫu 1, Dự toán kinh phí – Mẫu 3).
**Áp lên:** schema v1.3 (file `scriptdb.sql`) — codebase code-first BE.
**Dành cho:** Phát & Chinh (BE) đọc để nắm thay đổi + dùng làm spec cho AI code.

> Quy ước: ✅ = thêm mới · ✏️ = sửa bảng cũ · ⚠️ = phá ràng buộc cũ, cần chú ý khi migrate.

---

## 0. Tóm tắt thay đổi (changelog)

| # | Hạng mục | Loại | Mức độ |
|---|---|---|---|
| 1 | Review Rounds + tách review **Khoa học / Kinh phí** chạy tuần tự | ✅ bảng mới + ✏️ | **Lớn** |
| 2 | Phương thức khoán (từng phần / toàn phần) trên proposal | ✏️ | Trung bình |
| 3 | Chức danh + hệ số cho thành viên | ✅ config + ✏️ | Trung bình |
| 4 | Tính tiền công theo **ngày công × hệ số** | ✏️ + ✅ config | Trung bình |
| 5 | Dự toán theo **12 khoản chi + nguồn vốn** (line items) | ✅ bảng mới + ⚠️ | Trung bình |
| 6 | Sản phẩm: deadline theo mốc + nghiệm thu từng phần | ✏️ | Nhỏ |
| 7 | Công bố / kết quả đào tạo | ✅ (tùy chọn, để sau) | Nhỏ |

Hai phần thầy yêu cầu mà DB **đã có sẵn**, không cần đổi: `notifications` (nhắc deadline) và `amendment_requests` (gửi request gia hạn). Chỉ cần code logic.

---

## 1. ✅ Review Rounds — tách Khoa học / Kinh phí (quan trọng nhất)

**Vấn đề:** hiện `review_councils` gắn thẳng vào proposal, không có khái niệm "vòng", không phân biệt chấm khoa học vs chấm kinh phí, không ép thứ tự "khoa học đạt → mới mở kinh phí".

**Cách làm:** thêm bảng `review_rounds` đứng giữa proposal ↔ council. Mỗi vòng có rubric riêng, dimension riêng, và có thể phụ thuộc vòng trước.

```sql
CREATE TABLE review_rounds (
    id INT IDENTITY(1,1) PRIMARY KEY,
    proposal_id UNIQUEIDENTIFIER NOT NULL REFERENCES proposals(id),
    round_number INT NOT NULL,                 -- 1, 2, 3...
    dimension NVARCHAR(20) NOT NULL,           -- SCIENCE / FINANCE
    round_type NVARCHAR(30) NOT NULL,          -- SCREENING / REVIEW / ACCEPTANCE
    rubric_template_id INT NULL REFERENCES rubric_templates(id),
    sequence INT NOT NULL,                     -- thứ tự chạy
    prerequisite_round_id INT NULL REFERENCES review_rounds(id), -- chỉ mở khi vòng này PASSED
    status NVARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING/OPEN/PASSED/FAILED
    opened_at DATETIME2 NULL,
    closed_at DATETIME2 NULL,
    result NVARCHAR(30) NULL,
    CONSTRAINT UQ_review_rounds UNIQUE (proposal_id, round_number)
);
GO

-- Gắn council vào round (mỗi round có hội đồng riêng -> reviewer KH ≠ reviewer KP một cách tự nhiên)
ALTER TABLE review_councils ADD round_id INT NULL REFERENCES review_rounds(id);
GO
```

**Ghi chú cho team:**
- `dimension = SCIENCE` → vòng sơ loại khoa học. `dimension = FINANCE` → vòng kinh phí, đặt `prerequisite_round_id` trỏ về vòng khoa học → BE chặn mở vòng kinh phí khi vòng khoa học chưa PASSED.
- `review_councils.proposal_id` vẫn giữ (lấy nhanh), nhưng nguồn sự thật về "vòng" là `round_id`.
- Rubric do Admin định nghĩa → đã có `rubric_templates` + `rubric_criteria`, chỉ cần gán `rubric_template_id` vào round.

---

## 2. ✏️ Phương thức khoán trên Proposal

Mục số 8 trong mẫu khoa học: *Khoán đến sản phẩm cuối cùng* (toàn phần) vs *Khoán từng phần*. Field này quyết định cách sinh đợt giải ngân + nghiệm thu.

```sql
ALTER TABLE proposals ADD funding_method NVARCHAR(20) NULL; -- WHOLE / PARTIAL
GO
```
- `PARTIAL` → giải ngân theo từng mốc nghiệm thu.
- `WHOLE` → chỉ lấy nội dung đầu + cuối (đầu kỳ / cuối kỳ).

---

## 3. ✅✏️ Chức danh + hệ số thành viên

`proposal_team_members` hiện chỉ có `is_pi` / `is_secretary` (boolean) → không biểu diễn được TVC / TV / KTV.

```sql
-- Bảng config chức danh (theo phong cách config-driven của v1.3)
CREATE TABLE personnel_role_types (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(20) UNIQUE NOT NULL,         -- CNNV / TKKH / TVC / TV / KTV
    name NVARCHAR(200) NOT NULL,
    default_coefficient DECIMAL(4,2) NULL,     -- vd 0.79 / 0.49 / 0.25
    is_active BIT NOT NULL DEFAULT 1
);
GO

ALTER TABLE proposal_team_members ADD member_role_code NVARCHAR(20) NULL;     -- map personnel_role_types.code
ALTER TABLE proposal_team_members ADD salary_coefficient DECIMAL(4,2) NULL;   -- hệ số tiền công
GO
```
- Giữ `is_pi` / `is_secretary` để không phá code đang chạy; `member_role_code` là cái dùng chính từ giờ.
- ⚠️ Thầy nói rõ: **nhóm không cần quan tâm công thức/giá trị hệ số**, chỉ cần CÓ field để nhập.

---

## 4. ✏️ Tính tiền công theo ngày công × hệ số

File kinh phí thật tính: **số ngày công × hệ số × lương cơ bản ngày**. Bảng `proposal_budget_labor_details` đang để `hourly_rate` (giờ) → lệch.

```sql
-- Config hằng số tài chính (lương cơ bản: 1.490.000đ trong file mẫu)
CREATE TABLE system_financial_configs (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) UNIQUE NOT NULL,         -- BASE_DAILY_SALARY
    value DECIMAL(15,2) NOT NULL,
    description NVARCHAR(300) NULL,
    effective_date DATE NOT NULL,
    is_active BIT NOT NULL DEFAULT 1
);
GO

ALTER TABLE proposal_budget_labor_details ADD work_days DECIMAL(6,1) NULL;     -- số ngày công
ALTER TABLE proposal_budget_labor_details ADD coefficient DECIMAL(4,2) NULL;   -- hệ số
ALTER TABLE proposal_budget_labor_details ADD daily_rate DECIMAL(12,2) NULL;   -- = hệ số × lương cơ bản (tính ở service)
GO
```
- Cột tính `total_amount` cũ (`hours × hourly_rate`) → đổi công thức sang `work_days × daily_rate`, hoặc tính ở Service layer rồi lưu. `total_research_hours`/`hourly_rate` cũ có thể giữ tạm rồi bỏ sau.
- Phần **bảng giải trình chi tiết** (ngày công từng người theo từng nội dung) là **phase 2** — chưa làm bây giờ.

---

## 5. ✅⚠️ Dự toán theo 12 khoản chi + nguồn vốn

`proposal_budget` hiện chỉ có 6 cột cố định. Form thật có **12 khoản chi**, mỗi khoản tách theo **nguồn vốn** (Khoán chi / Ngoài khoán / NSNN / Khác).

```sql
-- Config 12 khoản chi
CREATE TABLE budget_expense_categories (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) UNIQUE NOT NULL,
    name NVARCHAR(300) NOT NULL,               -- "Công lao động trực tiếp", "Quản lý phí cơ quan chủ trì"...
    sequence INT NOT NULL,
    is_active BIT NOT NULL DEFAULT 1
);
GO

-- Dòng chi tiết (thay 6 cột cứng)
CREATE TABLE proposal_budget_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    proposal_id UNIQUEIDENTIFIER NOT NULL REFERENCES proposals(id),
    category_id INT NOT NULL REFERENCES budget_expense_categories(id),
    amount DECIMAL(15,2) NOT NULL DEFAULT 0,
    source_khoan DECIMAL(15,2) NOT NULL DEFAULT 0,        -- Khoán chi
    source_ngoai_khoan DECIMAL(15,2) NOT NULL DEFAULT 0,  -- Ngoài khoán
    source_nsnn DECIMAL(15,2) NOT NULL DEFAULT 0,         -- NSNN
    source_other DECIMAL(15,2) NOT NULL DEFAULT 0,        -- Khác
    sequence INT NOT NULL
);
GO
```
- ⚠️ Ràng buộc cũ `CK_budget_sum` (cộng 6 cột = total) sẽ **không còn đúng** khi chuyển sang line items → **DROP** nó, chuyển sang validate ở Service (tổng items = total_amount). 6 cột cũ trong `proposal_budget` giữ lại làm cache tổng hợp hoặc bỏ dần.
- Kế hoạch phân bổ theo đợt (Đợt 1/2/3) → dùng `disbursement_templates` / `contract_disbursements` đã có; chỉ cần map vào.

---

## 6. ✏️ Sản phẩm theo mốc + nghiệm thu từng phần

Để chạy được flow nhắc deadline + nghiệm thu từng phần:
```sql
ALTER TABLE product_deliverables ADD due_date DATE NULL;            -- deadline tính từ contract.start_date + tháng
ALTER TABLE product_deliverables ADD acceptance_status NVARCHAR(20) NULL; -- PENDING/PASSED/FAILED
GO
```
- `notifications` + job nhắc trước 1 tháng / 2 tuần / 1 tuần đã đủ bảng, chỉ cần scheduler.
- Gia hạn: dùng `amendment_requests` (category = EXTENSION).

---

## 7. ✅ (Tùy chọn, để sau) Công bố + đào tạo

Mẫu khoa học có bảng công bố (tạp chí, SJR, IF) và kết quả đào tạo mà `proposal_expected_products` chưa cover. Khi cần export đúng mẫu thì thêm 2 bảng `proposal_publications` và `proposal_training_outputs`. **Chưa ưu tiên.**

---

## Hướng dẫn cho AI code (code-first BE)

1. Tạo entity mới + cấu hình EF cho: `ReviewRound`, `PersonnelRoleType`, `SystemFinancialConfig`, `BudgetExpenseCategory`, `ProposalBudgetItem`.
2. Thêm property vào entity cũ: `ReviewCouncil.RoundId`, `Proposal.FundingMethod`, `ProposalTeamMember.{MemberRoleCode, SalaryCoefficient}`, `ProposalBudgetLaborDetail.{WorkDays, Coefficient, DailyRate}`, `ProductDeliverable.{DueDate, AcceptanceStatus}`.
3. Bỏ ràng buộc `CK_budget_sum`; validate tổng dự toán ở tầng Service.
4. Tạo **1 migration** tên `V1_4_ProposalReview_Budget_Update`.
5. Seed dữ liệu config: `personnel_role_types` (5 dòng), `budget_expense_categories` (12 dòng), `system_financial_configs` (BASE_DAILY_SALARY).
6. Logic nghiệp vụ: chặn mở round FINANCE khi round SCIENCE chưa PASSED (dựa `prerequisite_round_id`).

> Lưu ý chung: con số cụ thể (mức trần, số đợt, % giải ngân) bám theo **QĐ 543**. 2 file mẫu của thầy chỉ để học **cấu trúc tài liệu cần export**, không phải luật FPT.
