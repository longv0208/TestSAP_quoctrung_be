# FURPMS — API Contract (Backend hiện tại)

> **Đây là contract DUY NHẤT, đã hợp nhất** — thay thế bản cũ `FURPMS_API_Contract_v1.1_Delta.md` (đã xoá). Mọi chi tiết field/nghiệp vụ của bản v1.1 đã gộp vào §13 cuối file.
>
> Tài liệu mô tả **toàn bộ REST API** backend FURPMS đang cung cấp, để bàn giao cho team FE. Cập nhật theo mã nguồn (v2.0: proposal CRUD + budget/member persistence + dev tools + tóm tắt AI Gemini).
>
> **Nguồn chính xác nhất là Swagger đang chạy:** `http://localhost:5068/swagger` — có schema request/response đầy đủ, bấm "Try it out" gọi thử được. Tài liệu này cho cái nhìn tổng thể + quy ước + các luồng chính.
>
> ### ⚠️ Cập nhật 09/07/2026 — DB đổi sang PROJECT-CENTRIC (56 bảng)
> Sau refactor Phase A+B (biên bản Review 2): **`Project` là thực thể gốc**, `Proposal` thành tài liệu có version thuộc project. **API GIỮ NGUYÊN route theo `proposalId`** (FE không phải đổi URL) — service tự resolve proposal → project. Chỉ khác ở **response DTO** (thêm field) và **hành vi**:
> - `ProposalDto` thêm `projectId`, `versionNo`, `projectStatus`. PUT khi đề tài `REVISION_REQUIRED` → **tạo bản version mới** (giữ lịch sử) thay vì sửa tại chỗ.
> - `ContractDto` thêm `projectId`, `scopeTitle`; **1 đề tài có thể có NHIỀU hợp đồng** (bỏ ràng buộc 1-1).
> - `FinalReportDto` neo `projectId` (thay `contractId`); `DeliverableResponse` thêm `projectId`, `contractId` nullable.
> - DTO chấm điểm/quyết định (`SubmitScoreRequest`, `SaveMinutesRequest`, `FinalizeDecisionRequest`, `CloseRoundRequest`) thêm field **optional** `projectId`/`proposalProjectId` — hội đồng chấm 1 đề tài thì bỏ trống (tự suy); chấm nhiều đề tài mới cần chỉ rõ.
> - Một số field DTO tên `proposalId` (council/meeting/order) nay **chứa id bản đề cương hiện hành** của project (đủ để FE điều hướng `/api/proposals/{id}`).
>
> ### ⚠️ Cập nhật 15/07/2026 — Review Board cấp Track + rà soát contract
> - **§8 viết lại**: tách luồng chính **Review Board cấp Track** (§8.1: tạo vòng cho cả lĩnh vực, lập hội đồng trọn gói, xóa vòng, thêm/gỡ đề tài) khỏi luồng **legacy theo proposal** (§8.2). Biên bản Thư ký→Chủ tịch (§8.5) nay đồng bộ cả `project_rounds` + tự đóng round.
> - **§5 bổ sung**: nhóm **research-types CRUD** (`/api/cycles/research-types`) và **tracks theo đợt** (`/api/cycles/{id}/tracks`) — trước đây thiếu trong doc.
> - **§6 sửa**: tài liệu đính kèm (`/api/proposals/{id}/documents`, `/api/documents`) **đã có BE** (bản cũ ghi nhầm là 404).

---

## 1. Tổng quan

| Mục | Giá trị |
|---|---|
| Base URL (dev) | `http://localhost:5068` |
| Swagger UI | `http://localhost:5068/swagger` |
| Định dạng | JSON, UTF-8 |
| Xác thực | JWT Bearer (header `Authorization: Bearer <token>`) |
| CORS | cho phép origin `http://localhost:5173` |
| Stack | ASP.NET Core 8, EF Core, **PostgreSQL 16** (đổi khỏi SQL Server 14/08 — Railway không cung cấp SQL Server) |

---

## 2. Quy ước chung

### 2.1. Response envelope
**Mọi** endpoint trả về cùng một vỏ bọc `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Login successful.",
  "data": { /* payload, có thể là object | array | null */ },
  "errors": null
}
```

Khi lỗi (do middleware sinh ra):
```json
{ "success": false, "message": "Proposal not found.", "data": null, "errors": null }
```

### 2.2. Mapping lỗi → HTTP status
Backend ném exception, `GlobalExceptionMiddleware` map sang status:

| Exception | HTTP | Ý nghĩa |
|---|---|---|
| `UnauthorizedAccessException` | 401 | **Chưa/hết đăng nhập** (sai mật khẩu, tài khoản khoá) |
| `ForbiddenException` | 403 | Đã đăng nhập nhưng **không đủ quyền** với tài nguyên (không phải PI của đề tài, không phải Thư ký hội đồng…) |
| `ArgumentException` | 400 | Dữ liệu vào không hợp lệ |
| `KeyNotFoundException` | 404 | Không tìm thấy |
| `InvalidOperationException` | 409 | Xung đột trạng thái (vd: sửa đề xuất đã nộp) |
| khác | 500 | Lỗi không lường trước |

Ngoài ra ASP.NET tự trả **400** cho lỗi model-binding (sai kiểu dữ liệu JSON), **401** nếu thiếu/không hợp lệ token, **403** nếu sai role ở `[Authorize(Roles=…)]`.

> ⚠️ **Đổi từ 20/07/2026:** trước đây lỗi phân quyền cũng trả 401, khiến FE tưởng hết phiên và **tự đăng xuất người dùng** khi họ bấm vào chức năng không thuộc quyền mình. Nay đã tách: **401 = đăng nhập lại**, **403 = báo lỗi tại chỗ, đừng logout**. FE phải xử lý 403 riêng, không gộp chung với 401.

> ⚠️ **Đổi từ 17/08/2026 — `errorCode` chung KHÔNG được nuốt `message`.** 4 mã `CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `UNEXPECTED` chỉ là **thùng chứa** theo kiểu exception, không phải loại lỗi cụ thể — mọi `InvalidOperationException` toàn hệ thống đều ra `CONFLICT`. FE (`axiosClient.resolveMessage`) trước đây ưu tiên bản dịch của `errorCode` nên **mọi lỗi 409/400 đều hiện đúng một câu chung vô nghĩa**, che mất câu BE viết kỹ. Nay: mã **cụ thể** → dùng bản dịch; mã **thùng chứa** → dùng `message` của BE. ⇒ BE viết `message` cho 400/409 phải coi như **văn bản người dùng đọc trực tiếp**: nêu vướng gì + làm gì để thoát.

### 2.2b. Ngày giờ — luôn gửi/nhận UTC

Mọi cột ngày giờ trong PostgreSQL là `timestamptz`. **Từ 17/08/2026** tầng đọc JSON (`UtcDateTimeConverter`) chuẩn hoá mọi `DateTime` nhận vào về UTC:

| Client gửi | BE hiểu |
|---|---|
| `"2026-08-26T04:04:00Z"` | 04:04 UTC (khuyến nghị — FE dùng `toISOString()`) |
| `"2026-08-26T11:04:00+07:00"` | 04:04 UTC |
| `"2026-08-26T11:04:00"` (không múi giờ) | **11:04 UTC** — giữ nguyên con số, không đoán múi giờ máy chủ |

> ⚠️ Trước đó chuỗi không múi giờ khiến Npgsql ném lỗi → **500 "Hệ thống gặp sự cố ngoài dự kiến"** ở mọi endpoint nhận ngày giờ (đặt lịch họp, khung giờ hội đồng, hạn xác nhận thư mời, mốc hợp đồng…). FE phải quy đổi ô `<input type="datetime-local">` bằng `fromDateTimeLocalInput` / `toDateTimeLocalInput` (`src/utils/format.ts`), **không** cắt chuỗi bằng `.slice(0, 16)` — cắt như vậy lệch đúng bằng chênh múi giờ.

### 2.3. Vai trò (roles)
4 vai trò, khớp với `Role.Name` trong DB: **`Admin`**, **`Staff`**, **`Faculty`**, **`ReviewCommittee`**.
- `[Authorize]` (mặc định ở hầu hết controller) = cần đăng nhập (vai trò bất kỳ).
- Cột "Quyền" bên dưới ghi rõ khi endpoint giới hạn vai trò; "*" = mọi user đã đăng nhập.
- Token chứa claim role; 1 user có thể có nhiều role.

### 2.4. Kiểu ID
- Thực thể nghiệp vụ (proposal, user, contract, council, meeting, deliverable...) → **GUID**.
- Bảng cấu hình/lookup (cycle, track, role type, budget category, rubric...) → **int**.
- `deliverables`, `disbursements`, `settlements`, một số sub-resource → **int**.

### 2.5. Giá trị trạng thái (backend lưu CHỮ HOA)
| Thực thể | Các trạng thái |
|---|---|
| Cycle | `PLANNING` → `OPEN` → `CLOSED` |
| Proposal (tài liệu) | `DRAFT`, `SUBMITTED`, `APPROVED`, `REJECTED`, `REVISION_REQUIRED` |
| Project (vòng đời đề tài) | `PROPOSED`, `UNDER_REVIEW`, `APPROVED`, `IN_PROGRESS`, `ACCEPTANCE`, `COMPLETED`, `CANCELLED`, `TERMINATED` |
| Review round | type: `SCREENING` / `REVIEW` / `ACCEPTANCE`; dimension: `SCIENCE` / `FINANCE`; status: `PENDING`, `OPEN`, `PASSED`, `FAILED` |
| Council decision / round result | `APPROVED` / `REJECTED` / `REVISION_REQUIRED` |
| Deliverable / Acceptance | `PENDING` / `PASSED` / `FAILED` (acceptance submit dùng `PASS` / `FAIL`) |
| Council member | `ASSIGNED` (đã gán, chưa mời) → `INVITED` → `CONFIRMED` / `DECLINED` / `EXPIRED` |

---

## 3. Xác thực — `/api/auth`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/auth/login` | công khai | Đăng nhập, trả token + thông tin user |
| GET | `/api/auth/me` | * | Thông tin user hiện tại |
| POST | `/api/auth/change-password` | * | Đổi mật khẩu |
| POST | `/api/auth/forgot-password` | **Không cần đăng nhập** | Xin mã đặt lại mật khẩu: `{ email }`. **LUÔN trả 200** dù email có tồn tại hay không — trả lời khác nhau là biến màn này thành công cụ dò tài khoản. Mã băm SHA-256 lưu DB, sống **30 phút**, dùng **một lần**. Gửi kèm chuông + email (`PASSWORD_RESET`). |
| POST | `/api/auth/reset-password` | **Không cần đăng nhập** | Đặt lại mật khẩu: `{ token, newPassword }`. **400** nếu mã sai/hết hạn/đã dùng, hoặc mật khẩu < 8 ký tự. Đổi xong xoá mã ngay. |

**Login request**
```json
{ "email": "admin@furpms.edu.vn", "password": "password" }
```
**Login response (`data`)**
```json
{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "user": { "id": "guid", "email": "...", "fullName": "...", "status": "ACTIVE", "roles": ["Admin"], "lastLoginAt": "..." }
}
```
**Change password request**: `{ "currentPassword": "...", "newPassword": "...", "confirmNewPassword": "..." }`

---

## 4. Người dùng & hồ sơ

### Users — `/api/users`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/users` | Admin, Staff | Danh sách user |
| GET | `/api/users/{id}` | * | Chi tiết user |
| POST | `/api/users` | Admin | Tạo user |
| PUT | `/api/users/{id}` | Admin | Cập nhật user |
| PATCH | `/api/users/{id}/toggle-active` | Admin | Khóa/mở tài khoản (đổi `isActive`) |
| DELETE | `/api/users/{id}` | Admin | **Xoá mềm** tài khoản (18/08) |
| POST | `/api/users/{id}/reset-password` | Admin | Reset mật khẩu về mặc định `Furpms@123456` |

**CreateUserRequest**
```json
{ "email": "...", "fullName": "...", "phoneNumber": "...", "department": "...",
  "academicDegree": 1, "roles": [3], "temporaryPassword": "..." }
```
`roles` = mảng **Role.Id** (Admin=1, Staff=2, Faculty=3, ReviewCommittee=4).

⚠️ **Email được kiểm ĐỊNH DẠNG từ 18/08** (`400 VALIDATION_FAILED`). Trước đó chỉ kiểm "có nhập
chưa", nên `abc`, `test@gmail`, `a@localhost` đều tạo được tài khoản rồi thư mời gửi đi đâu mất.
Quy tắc: phải có `@`, tên miền có ít nhất một dấu chấm và đuôi ≥2 ký tự, không khoảng trắng.
Thông báo trả về nêu đúng chuỗi đã nhập.

**DELETE `/api/users/{id}` — xoá mềm, có chốt chặn**
Đặt `is_deleted` + `deleted_at`/`deleted_by`, đồng thời `status = INACTIVE`. Bộ lọc toàn cục khiến
người bị xoá biến mất khỏi mọi danh sách, nhưng **hồ sơ đã ký vẫn giữ nguyên tên** (đề tài, hội
đồng, điểm chấm) — nên không dùng xoá cứng.

Trả `409 CONFLICT` kèm lý do cụ thể + hướng xử lý khi:
| Trường hợp | Thông báo |
|---|---|
| Tự xoá chính mình | *Không thể tự xoá tài khoản của chính mình.* |
| Quản trị viên duy nhất | *…xoá xong sẽ không còn ai quản trị hệ thống.* |
| Đang là chủ nhiệm đề tài | *…hãy dùng "Vô hiệu hoá" để khoá đăng nhập mà vẫn giữ tên trên hồ sơ đề tài.* |
| Đang là ủy viên hội đồng | *…hãy gỡ khỏi hội đồng trước, hoặc dùng "Vô hiệu hoá".* |
| Đang là thành viên tham gia đề tài | *…hãy dùng "Vô hiệu hoá".* |

→ Lối thoát cho mọi trường hợp bị chặn là `PATCH /toggle-active`.

### Academic Profile — `/api/users/{userId}/profile`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/users/{userId}/profile` | * (chủ hồ sơ; Admin/Staff xem mọi người) | Hồ sơ khoa học |
| PUT | `/api/users/{userId}/profile` | như trên | Tạo/cập nhật hồ sơ |

⚠️ **Đổi 14/08 — `PUT` KHÔNG còn nhận 8 ô đếm công trình** (`isiScopusCount`, `intlJournalCount`,
`domesticJournalCount`, `intlConferenceCount`, `domesticConferenceCount`, `patentsCount`,
`phdSupervisedCount`, `masterSupervisedCount`). Chúng nay là **số suy ra** — máy chủ tính lại từ
`academic_works` sau mỗi lần thêm/sửa/xoá công trình. `GET` **vẫn trả** các số này để hiển thị.
Gửi kèm chúng trong `PUT` thì bị bỏ qua (không lỗi).

### Công trình khoa học — `/api/users/{userId}/academic-works`

QĐ543 **Biểu mẫu 02** đòi **cả hai**: số lượng (mục 14.1–14.5, 15, 19.1/19.3) *và* danh sách chi
tiết (14.6, 16.3, 17, 19.4). Trước 14/08 hệ thống chỉ có ô đếm nhập tay ⇒ hồ sơ thiếu so với biểu
mẫu, hội đồng xét năng lực chủ nhiệm (Điều 7) không tra được nguồn.

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/users/{userId}/academic-works` | chủ hồ sơ; **Admin/Staff xem được** (thẩm định) | Danh sách, sắp theo mục → thứ tự → năm mới nhất |
| POST | `/api/users/{userId}/academic-works` | **CHỈ chủ hồ sơ** | Thêm |
| PUT | `/api/users/{userId}/academic-works/{workId}` | **CHỈ chủ hồ sơ** | Sửa |
| DELETE | `/api/users/{userId}/academic-works/{workId}` | **CHỈ chủ hồ sơ** | Xoá |

> ⚠️ Ghi thì **kể cả Admin cũng bị 403** — lý lịch khoa học là lời khai có trách nhiệm của người
> đứng tên, khai hộ là làm hỏng giá trị pháp lý của nó.

**AcademicWorkRequest**: `{ workType, category, title, venue?, authors?, role?, year?, startYear?,
identifier?, volume?, pages?, status?, url?, note?, sortOrder }`

- `workType` ∈ `BOOK`(13) · `PUBLICATION`(14.6) · `PATENT`(15) · `APPLICATION`(16.3) ·
  `PROJECT`(17) · `AWARD`(18) · `SUPERVISION`(19.4)
- `category` phải **thuộc đúng `workType`**, nếu không → 400 kèm danh sách giá trị hợp lệ:
  - `PUBLICATION` → `ISI_SCOPUS` · `JOURNAL_INTL` · `JOURNAL_DOMESTIC` · `CONFERENCE_INTL` · `CONFERENCE_DOMESTIC`
  - `PROJECT` → `PROJECT_LEAD`(17.1) · `PROJECT_MEMBER`(17.2)
  - `BOOK` → `BOOK_MONOGRAPH` · `BOOK_TEXTBOOK` · `PATENT` → `PATENT_GRANTED`
  - `APPLICATION` → `APPLIED_ABROAD` · `APPLIED_DOMESTIC` · `AWARD` → `AWARD_GENERAL`
  - `SUPERVISION` → `PHD` · `MASTER`
- `role` ∈ `MAIN_AUTHOR` · `CO_AUTHOR` · `CORRESPONDING` · `LEAD` · `MEMBER` · `MAIN_SUPERVISOR` · `CO_SUPERVISOR`
- `status` ∈ `ACCEPTED` · `IN_PROGRESS` · `FAILED` (nguyên văn 3 giá trị BM02 mục 17 liệt kê)
- `volume` / `pages` — BM02 mục 14.6 **đòi đích danh** *"tên tạp chí, volume, trang số"*
- Chặn: năm ngoài 1900–2100 · năm công bố vượt năm hiện tại · `startYear > year`

---

## 5. Master data (cấu hình)

### Loại đề tài (Research Types) — `/api/cycles/research-types`
> Cấu hình loại đề tài (Ứng dụng / Cơ bản / …) + trần kinh phí. `ResearchType.Id` chính là giá trị `researchType` khi tạo đề xuất (§6). Có thùng rác (deactivate/reactivate) trước khi xóa vĩnh viễn.

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/cycles/research-types` (`?includeInactive=`) | * | Danh sách loại đề tài |
| POST | `/api/cycles/research-types` | Admin | Tạo (`{ code?, name, maxBudgetCap, requireOrderingUnit? }`) |
| PUT | `/api/cycles/research-types/{id}` | Admin | Sửa (`{ name?, maxBudgetCap?, requireOrderingUnit? }`) |
| PATCH | `/api/cycles/research-types/{id}/deactivate` | Admin | Chuyển vào thùng rác (`isActive=false`) |
| PATCH | `/api/cycles/research-types/{id}/reactivate` | Admin | Khôi phục |
| DELETE | `/api/cycles/research-types/{id}` | Admin | Xóa vĩnh viễn |

### Cycles & Tracks — `/api/cycles`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/cycles` | * | Danh sách đợt nộp (kèm `trackCount`) |
| GET | `/api/cycles/{id}` | * | Chi tiết đợt (kèm `tracks[]` đã gắn) |
| POST | `/api/cycles` | Admin, Staff | Tạo đợt — **409 nếu trùng TÊN trong cùng năm**. Cùng năm + cùng loại nhưng khác tên thì **cho phép** (đợt bổ sung): QĐ543 không giới hạn số đợt/năm |
| PUT | `/api/cycles/{id}` | Admin, Staff | Sửa đợt — kiểm cùng bộ luật với tạo (409 khi đổi thành tên đã có trong năm đó) |
| DELETE | `/api/cycles/{id}` | Admin, Staff | Xoá đợt lỡ tạo nhầm — **409 nếu đã có đề tài / vòng chấm / danh mục đặt hàng do Staff tạo**. Lĩnh vực đã gắn, danh mục mặc định và **log gia hạn** bị xoá theo (không còn là lý do chặn từ 17/08). Đợt đã dùng thật thì **đóng**, không xoá |
| POST | `/api/cycles/{id}/open` | Admin, Staff | Mở đợt (→ `OPEN`) |
| POST | `/api/cycles/{id}/close` | Admin, Staff | Đóng đợt (→ `CLOSED`) |
| POST | `/api/cycles/{id}/extend-deadline` | Admin, Staff | **Gia hạn deadline đợt** (rule tuần 10) `{ newDeadline: "yyyy-MM-dd", reason? }` — ghi log, **KHÔNG ghi đè** `SubmissionDeadline` gốc; phải sau deadline hiện tại (else 400) |
| GET | `/api/cycles/{id}/deadline-extensions` | * | Lịch sử gia hạn (mới nhất trước) — deadline hiệu lực = `newDeadline` bản đầu list |

> Response cycle (`GET /api/cycles`, `GET /api/cycles/{id}`) nay trả **`submissionDeadline` = hạn HIỆU LỰC** (sau gia hạn), kèm **`originalDeadline`** (hạn gốc, chỉ khi đã gia hạn) + **`extensionCount`**. Trước đây luôn trả hạn gốc → FE hiện hạn cũ sau khi gia hạn.
| GET | `/api/cycles/tracks` | * | **Toàn bộ** lĩnh vực + `cycleCount` (số đợt đang mở) |
| POST | `/api/cycles/tracks` | Admin, Staff | Tạo lĩnh vực toàn cục (không gắn đợt) |
| GET | `/api/cycles/{id}/tracks` | * | Lĩnh vực **đã gắn vào đợt** `{id}` (bảng nối `cycle_track`) |
| POST | `/api/cycles/{id}/tracks` | Admin, Staff | Tạo lĩnh vực **và gắn vào đợt** `{id}` (nếu chưa gắn) |
| POST | `/api/cycles/{cycleId}/tracks/{trackId}` | Admin, Staff | **Gắn** lĩnh vực toàn cục có sẵn vào đợt (409 nếu đã gắn) |
| DELETE | `/api/cycles/{cycleId}/tracks/{trackId}` | Admin, Staff | **Gỡ** lĩnh vực khỏi đợt (409 nếu đã có đề tài dùng lĩnh vực đó trong đợt) |
| PUT | `/api/cycles/tracks/{id}` | Admin, Staff | Sửa lĩnh vực (tên/mô tả/chủ; đổi tên trùng → 409) |
| PATCH | `/api/cycles/tracks/{id}/owner` | Admin, Staff | Gán chủ lĩnh vực (FE hiện ẩn tính năng này) |
| PATCH | `/api/cycles/tracks/{id}/deactivate` | Admin, Staff | Vô hiệu lĩnh vực |

> ⚠️ Đợt nộp phải ở trạng thái `OPEN` thì PI mới tạo được đề xuất. FE lấy đợt đang mở bằng cách `GET /api/cycles` rồi lọc `status == "Open"`.
> **Đợt vs Lĩnh vực (Phase B):** 1 đợt chứa nhiều lĩnh vực qua `cycle_track`. Màn "Đợt & Lĩnh vực" dùng `GET/POST /api/cycles/{id}/tracks` (theo đợt); dropdown nộp đề tài dùng `GET /api/cycles/tracks` (toàn cục).
> **Lĩnh vực dùng lại nhiều đợt (rule #6):** lĩnh vực là **master data toàn cục** — tạo 1 lần (`POST /api/cycles/tracks`), rồi mỗi đợt **tự gắn/gỡ** (`POST`/`DELETE /api/cycles/{cycleId}/tracks/{trackId}`) để kiểm soát đợt nào mở lĩnh vực nào. Đợt 1 mở AI+IT, đợt 2 chỉ mở AI (gỡ IT) — PI chỉ chọn được lĩnh vực **đã gắn** vào đợt đang nộp. Không gỡ được nếu trong đợt đã có đề tài dùng lĩnh vực đó.
> **`cycleCount` (18/08):** `GET /api/cycles/tracks` trả kèm số đợt đang mở mỗi lĩnh vực. Vì tạo
> lĩnh vực xong nó **chưa thuộc đợt nào**, màn quản lý lĩnh vực trước đây im lặng hoàn toàn: người
> tạo thấy lĩnh vực nằm trong danh sách là yên tâm, rồi bên PI trống trơn — đúng lỗi báo 18/08.
> Nay `cycleCount = 0` hiện cảnh báo "Chưa đợt nào mở" kèm chỉ dẫn sang Đợt → Quản lý lĩnh vực.

### Các lookup khác (đều CRUD theo cùng mẫu: GET list / GET {id} / POST / PUT; ghi/sửa = Admin)
| Resource | Base path | Đọc | Ghi |
|---|---|---|---|
| Personnel role types | `/api/personnel-role-types` | * | Admin |
| Budget expense categories | `/api/budget-expense-categories` | * | Admin |  <!-- 06 hạng mục QĐ543 Điều 15, có `maxPercentage` -->
| System financial configs | `/api/financial-configs` | * | Admin |
| Product categories | `/api/product-categories` (`?activeOnly=`) | * | Admin |
| Organizational units | `/api/organizational-units` | * | Admin |
| Rubric criteria | `/api/rubric-criteria` (`?roundType=`) | * | Admin (có DELETE) |
| Amendment categories | `/api/amendment-categories` (`?activeOnly=`) | * | — (chỉ đọc, seed sẵn) |

### Cấu hình vận hành — `/api/system-settings`
Key-value do Admin chỉnh trong app, có hiệu lực ngay (không cần restart). Khác `/api/financial-configs` — bảng kia chỉ chứa hệ số tài chính.

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/system-settings` | Admin | Danh sách cấu hình: `{ id, key, value, recommendedValue, description, updatedAt }` |
| GET | `/api/system-settings/upload-policy` | mọi user đăng nhập | Giới hạn upload đã giải mã sẵn — FE validate trước khi gửi file |
| GET | `/api/system-settings/council-policy` | mọi user đăng nhập | `{ allowRespondOnBehalf }` — Staff dùng để ẩn thao tác trả lời thư mời thay khi Admin đã tắt; không lộ danh sách cấu hình đầy đủ |
| PUT | `/api/system-settings/{key}` | Admin | Body `{ "value": "25" }` → 400 nếu ngoài khoảng cho phép |

`upload-policy` trả:
```json
{ "maxFileSizeMb": 10, "recommendedMaxFileSizeMb": 10, "minAllowedMb": 1, "maxAllowedMb": 100,
  "allowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg"] }
```

Key hiện có:

| Key | Mặc định / khuyến cáo | Ràng buộc |
|---|---|---|
| `UPLOAD_MAX_FILE_SIZE_MB` | `10` | số nguyên 1–100; ngoài khoảng → 400 |
| `UPLOAD_ALLOWED_EXTENSIONS` | `.pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg` | danh sách ngăn bằng dấu phẩy, tối thiểu 1 mục |

> Upload tài liệu (§6 "Tài liệu đề xuất") đọc chính sách này mỗi lần gọi. Giá trị hỏng/thiếu trong DB → rơi về mức khuyến cáo.

---

## 6. Đề xuất nghiên cứu — `/api/proposals`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/proposals` | * (Admin/Staff xem hết; còn lại chỉ của mình) | Danh sách (filter `?cycleId&trackId&status&type&search`) |
| GET | `/api/proposals/my` | * | **Đề cương của tôi** — LUÔN chỉ của người gọi (`PiUserId == caller`), **kể cả Admin/Staff** (đa vai đang "làm PI"). Khác `/api/proposals` (Admin/Staff xem hết). |
| GET | `/api/proposals/{id}` | Staff/Admin · PI chủ đề cương · thành viên hội đồng được gán chấm | Chi tiết (kèm members, budgetItems). **Kiểm quyền (chống IDOR):** người ngoài 3 nhóm này → **403** (trước đây ai đăng nhập cũng đọc được). |
| POST | `/api/proposals` | * | Tạo (trạng thái `DRAFT`) |
| PUT | `/api/proposals/{id}` | chủ nhiệm (PI) | Sửa khi `DRAFT`; khi `REVISION_REQUIRED` → **tạo bản version mới** (v2, v3…) giữ lịch sử |
| POST | `/api/proposals/{id}/submit` | PI | Nộp duyệt (`DRAFT` → `SUBMITTED`; project → `UNDER_REVIEW`) |
| PATCH | `/api/proposals/{id}/withdraw` | PI | Rút lại (`SUBMITTED` → `DRAFT`) |

> **ProposalDto** (response) nay có thêm: `projectId`, `versionNo`, `projectStatus` (vòng đời tổng của đề tài: `PROPOSED / UNDER_REVIEW / APPROVED / IN_PROGRESS / ACCEPTANCE / COMPLETED / CANCELLED / TERMINATED`). Tạo đề tài = tạo `Project` gốc + `Proposal` v1 cùng lúc; PI **không tự chọn order** → hệ thống tự gán order mặc định "Nghiên cứu tự do" của đợt (100% đề tài thuộc 1 order).

**CreateProposalRequest / (PUT dùng cùng body)**
```json
{
  "trackId": "1",
  "titleVI": "Tên đề tài",
  "titleEN": "Title",
  "researchType": 1,            // 1 = Ứng dụng, 2 = Cơ bản (ResearchType.Id)
  "durationMonths": 18,
  "objectives": "Mục tiêu...",
  "methodology": "Phương pháp...",
  "expectedOutput": "",
  "members": [
    { "fullName": "Nguyễn Văn A", "email": "a@fpt.edu.vn", "department": "SE", "role": "TVC", "workMonths": 5 }
  ],
  "budgetItems": [
    { "category": "Công lao động trực tiếp", "amount": 50000000, "note": "Thù lao nhóm" }
  ]
}
```
- `budgetItems[].category` = **tên hoặc mã hạng mục** lấy từ `GET /api/budget-expense-categories` (khớp tên trước, rồi tới mã; không khớp → "Văn phòng phẩm, chi khác"). **Từ 12/08 chỉ còn 06 hạng mục** theo QĐ543 Điều 15: `LABOR` 100% · `EQUIPMENT` 60% · `OUTSOURCED` 60% · `CONFERENCE` 30% · `OFFICE_OTHER` 20% · `INCIDENTAL_IP` 10%. Bộ 12 hạng mục cũ (mẫu cấp Bộ) chuyển `isActive=false`, **không xoá** — dự toán cũ vẫn đọc được.
- ⚠️ **400 nếu hạng mục vượt tỷ lệ tối đa** (Điều 15). Lỗi liệt kê **tất cả** hạng mục vi phạm trong một lần, kèm số tiền, tỷ lệ hiện tại và mức tối đa quy ra tiền.
- Response `budgetItems[]` có thêm **`categoryCode`** (mới 12/08) — FE nạp lại form đối chiếu bằng **mã**, vì tên hạng mục đổi theo quy định còn mã thì giữ.
- Backend lưu đầy đủ members (kèm email/đơn vị) + budget items (kèm ghi chú) và **tự tính lại tổng kinh phí**.
- **`totalBudget`** (mới 12/08): tổng dự toán khi chủ nhiệm chưa tách hạng mục — wizard FE gửi trường này. Có `budgetItems` thì tổng **luôn** lấy từ tổng hạng mục, `totalBudget` bị bỏ qua.
- ⚠️ **400 nếu vượt trần kinh phí** (QĐ543 **Điều 14**: cơ bản ≤ 100tr · ứng dụng ≤ 150tr). Áp cho `POST /proposals`, `PUT /proposals/{id}`, tạo bản chỉnh sửa, `PUT /proposals/{id}/budget`, và kiểm lại ở `POST /proposals/{id}/submit`. Thông báo lỗi nêu rõ trần + cách xử lý khi được duyệt cấp vượt trần (Điều 14.3).
- ❌ **`fundingMethod` (WHOLE/PARTIAL) không còn được dùng** — lịch giải ngân do loại đề tài quyết định (Điều 16). Trường vẫn nhận để tương thích ngược nhưng không ảnh hưởng gì; FE đã gỡ khỏi form.

### Sub-resources của đề xuất
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET/PUT | `/api/proposals/{id}/budget` | * | Bảng kinh phí tổng hợp (`BudgetResponse`). **PUT: 400 nếu vượt trần Điều 14** (mới 12/08) |
| GET | `/api/proposals/{id}/budget/cap` | * | **Trần kinh phí đang áp** (mới 12/08): `{ researchTypeName, typeCap, orderCap, effectiveCap }`. `effectiveCap` = trần **nghiêm ngặt hơn** giữa trần loại đề tài (Điều 14) và trần riêng của đơn đặt hàng; `null` = chưa cấu hình ⇒ không chặn. FE hiện giới hạn ngay trên form thay vì để chủ nhiệm điền xong mới ăn lỗi |
| GET | `/api/proposals/{id}/budget/labor` | * | Chi tiết công lao động |
| PUT | `/api/proposals/{id}/budget/labor/{detailId}` | * | Sửa 1 dòng công lao động |
| GET/POST | `/api/proposals/{id}/team-members` | * | Liệt kê / thêm thành viên |
| GET/POST | `/api/proposals/{id}/research-contents` | * | Nội dung nghiên cứu |
| PUT/DELETE | `/api/proposals/{id}/research-contents/{contentId}` | * | Sửa/xoá nội dung |
| POST | `/api/proposals/{id}/research-contents/{contentId}/activities` | * | Thêm hoạt động |
| PUT/DELETE | `/api/proposals/{id}/activities/{activityId}` | * | Sửa/xoá hoạt động |
| GET/POST | `/api/proposals/{id}/expected-products` | * | Sản phẩm dự kiến (nay lưu ở `project_deliverables`) |
| PUT/DELETE | `/api/proposals/{id}/expected-products/{productId}` | * | Sửa/xoá sản phẩm |
| GET | `/api/proposals/{id}/export/scientific` | * | Xuất thuyết minh (file) |
| GET | `/api/proposals/{id}/export/budget` | * | Xuất dự toán (file) |

### Yêu cầu thay đổi đề tài — `/api/proposals/{proposalId}/change-requests`, `/api/change-requests/...`
> Thay đổi ở mức **ĐỀ TÀI** (gia hạn / nội dung / nhân sự / kinh phí / tạm dừng) — khác `amendments` (điều chỉnh theo hợp đồng). PI gửi → Staff/Admin duyệt.

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/proposals/{proposalId}/change-requests` | PI | Gửi yêu cầu `{ type: 1..5, description, newValue? }` |
| GET | `/api/proposals/{proposalId}/change-requests` | * | Danh sách yêu cầu của đề tài |
| GET | `/api/change-requests/pending` | Admin, Staff | Hàng đợi chờ duyệt |
| PATCH | `/api/change-requests/{id}/review` | Admin, Staff | Duyệt `{ approved: bool, adminNote? }` (duyệt 2 lần → 409) |

`type`: 1=ExtendTime · 2=ContentChange · 3=PersonnelChange · 4=BudgetChange · 5=Suspend. Response trả `type`/`status` dạng **tên** (`ExtendTime` / `Pending` / `Approved` / `Rejected`).

### Tài liệu đề xuất — `/api/proposals/{proposalId}/documents`, `/api/documents`
> ✅ **Đã có BE.** Attachment cho cả 2 đường nộp (nhập tay / upload+AI). Theo **QĐ 543 Điều 6.4**, hồ sơ đăng ký gồm **đề cương (BM01) + lý lịch khoa học (BM02)** → đây là chỗ nộp BM02 và bản đề cương gốc.
>
> **Lưu trữ & giới hạn (đã enforce ở BE):**
> - File nằm trên **đĩa server**: `App_Data/uploads/proposals/{proposalId}/…` (đổi qua config `DocumentStorage:RootPath`). **DB chỉ lưu metadata** (tên gốc, dung lượng, MIME, blob name, người upload, cờ mật).
> - **Dung lượng & định dạng do Admin cấu hình** trong `/api/system-settings` (§5) — mặc định **10 MB/file** và `.pdf .doc .docx .xls .xlsx .png .jpg .jpeg`; vượt/sai đuôi trả **400**. FE nên gọi `GET /api/system-settings/upload-policy` để chặn sớm và hiện đúng mức giới hạn.
> - ⚠️ **Khi deploy Render/container: đĩa là ephemeral** — redeploy sẽ **mất file đã upload**. Muốn giữ lâu dài phải gắn volume hoặc chuyển sang blob storage (S3/Azure).

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/proposals/{proposalId}/documents` | * | Danh sách tài liệu của đề xuất |
| POST | `/api/proposals/{proposalId}/documents` | * | Upload (`multipart/form-data`: `file`, `documentType`) |
| GET | `/api/proposals/{proposalId}/documents/{documentId}/download` | * | Tải file |
| DELETE | `/api/proposals/{proposalId}/documents/{documentId}` | * | Xoá tài liệu |
| GET | `/api/documents` | Admin, Staff | Danh sách tài liệu toàn hệ thống (kèm tên đề tài + PI) |

- `documentType` gợi ý: `Thuyết minh` | `Lý lịch khoa học` | `Khác`.

---

## 7. Đặt hàng nghiên cứu — `/api/research-orders`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/research-orders` (`?cycleId&status`) | * | Danh sách |
| GET | `/api/research-orders/{id}` | * | Chi tiết |
| POST | `/api/research-orders` | * | Tạo đơn đặt hàng |
| POST | `/api/research-orders/{id}/match` | Admin, Staff | Ghép với 1 đề xuất (`{ proposalId }`) |

---

## 8. Phản biện, Hội đồng & Chấm

### 8.0 Mô hình dữ liệu (Phase B + redesign Review Board)

- **Vòng phản biện** (`review_round`) thuộc **cycle_track** (lĩnh vực-trong-đợt), KHÔNG thuộc từng đề tài. Nhiều đề tài cùng lĩnh vực **dùng chung 1 vòng**; đề tài tham gia vòng qua bảng nối `project_rounds` (M-N).
- **Hội đồng** (`review_council`) thuộc **round**, chấm 1 **nhóm đề tài** qua `council_project_assignments`. 1 round có thể có **nhiều hội đồng song song**.
- **Trạng thái/kết quả tách theo TỪNG đề tài**: `project_rounds.status` (`PENDING|OPEN|PASSED|FAILED`) + biên bản/điểm gắn `project_id`. DTO chấm nhận field **optional** `projectId` — hội đồng 1 đề tài thì bỏ trống (tự suy), nhiều đề tài thì bắt buộc.
- **2 nhóm endpoint song song, cùng backing store:**
  - **§8.1 Review Board cấp Track** — *luồng chính, FE đang dùng*. Thao tác ở cấp lĩnh vực: tạo vòng cho cả nhóm, lập hội đồng trọn gói, không có nút chốt trực tiếp (kết quả chỉ qua biên bản).
  - **§8.2 Vòng theo proposal** — *legacy, giữ tương thích ngược*. FE không còn gọi; giữ cho tích hợp cũ + test.

### 8.1 Review Board cấp Track — `/api/cycles/{cycleId}/tracks/{trackId}/...`, `/api/rounds/...`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/cycles/{cycleId}/tracks/{trackId}/review-board` | Admin, Staff | 1 phát: đề tài của lĩnh vực + toàn bộ vòng (kèm hội đồng, thành viên, trạng thái từng đề tài) |
| POST | `/api/cycles/{cycleId}/tracks/{trackId}/rounds` | Staff, Admin | Tạo/tái dùng vòng cấp track |
| DELETE | `/api/rounds/{roundId}` | Staff, Admin | Xóa vòng |
| POST | `/api/rounds/{roundId}/projects` | Staff, Admin | Thêm 1 đề tài vào vòng |
| DELETE | `/api/rounds/{roundId}/projects/{projectId}` | Staff, Admin | Gỡ 1 đề tài khỏi vòng |
| POST | `/api/rounds/{roundId}/councils` | Staff, Admin | Tạo hội đồng TRỌN GÓI (đề tài + thành viên trong 1 lần) |

**Request — tạo vòng** (`POST .../tracks/{trackId}/rounds`):
```jsonc
{
  "dimension": "SCIENCE",          // SCIENCE | FINANCE (bắt buộc)
  "roundType": "REVIEW",           // SCREENING | REVIEW | ACCEPTANCE (bắt buộc)
  "rubricTemplateId": 2,           // optional — mặc định template active
  "prerequisiteRoundId": "guid",   // optional — vòng tiên quyết (rule #2)
  "projectIds": ["guid", ...]      // optional — RỖNG/null ⇒ tự gom mọi đề tài
                                   //            SUBMITTED/REVISION_REQUIRED của track
                                   //            chưa nằm trong vòng này
}
```
- Nếu track đã có vòng cùng `dimension`+`roundType` đang `PENDING|OPEN` → **tái dùng** vòng đó, chỉ thêm đề tài (không tạo trùng).
- Với `roundType=ACCEPTANCE`: **409** nếu còn bất kỳ đề tài/vòng REVIEW nào chưa có kết quả cuối cùng. Mặc định chỉ gom đề tài đã `PASSED` REVIEW, project đang `ACCEPTANCE` và final report `ACCEPTED|ARCHIVED`; danh sách `projectIds` gửi tay cũng bị kiểm cùng điều kiện. Khi mở vòng, server kiểm lại toàn bộ để không lách bằng API.
- Trả về `ReviewRoundResponse` (`councilId` luôn null ở path này).

**Request — thêm đề tài** (`POST /api/rounds/{roundId}/projects`): `{ "projectId": "guid" }`
**Request — tạo hội đồng trọn gói** (`POST /api/rounds/{roundId}/councils`):
```jsonc
{
  "councilType": "REVIEW",         // optional — mặc định = round.roundType
  "projectIds": ["guid", ...],     // bắt buộc ≥1; mọi đề tài phải đã ở trong vòng
  "members": [                     // bắt buộc ≥1
    { "userId": "guid", "memberRole": "Chair", "isExternal": false },
    { "userId": "guid", "memberRole": "Secretary" },
    { "userId": "guid", "memberRole": "Opponent" }
  ]
}
```
- `memberRole` ∈ `Chair | Secretary | Opponent | Member`.
- **Bắt buộc có ít nhất 1 `Chair` và 1 `Secretary`** (thiếu → `400`). Vì kết quả chốt qua biên bản Thư ký soạn → Chủ tịch duyệt (rule #12); thiếu 1 trong 2 thì hội đồng không bao giờ chốt được.
- **1 người chỉ giữ 1 vị trí / hội đồng** — `members` trùng `userId` (vd cùng người vừa Chair vừa Secretary) → `400`.
- **COI (rule #5)** kiểm cho **từng member × từng đề tài** TRƯỚC khi ghi: PI/thành viên đề tài không được là ủy viên → `400`, **không tạo hội đồng nửa vời**.
- Thành viên tạo ở trạng thái `ASSIGNED` (chưa gửi thư mời) — gửi mời qua `POST /api/councils/{councilId}/send-invitations` (§8.3, rule #13).
- Thay ủy viên bị `DECLINED` (rule #4): dùng `DELETE /api/council-members/{memberId}` rồi `POST /api/councils/{councilId}/members` (§8.3) — không phải tạo lại hội đồng.

**Response — `GET .../review-board`** (`ReviewBoardDto`):
```jsonc
{
  "projects": [                    // đề tài của lĩnh vực (loại DRAFT/REJECTED)
    { "projectId": "guid", "proposalId": "guid", "titleVi": "…", "projectStatus": "UNDER_REVIEW",
      "piUserId": "guid" }        // ⬅ 18/08 — FE loại chủ nhiệm khỏi dropdown chọn ủy viên (COI)
  ],
  "rounds": [
    {
      "id": "guid", "roundNumber": 1, "dimension": "SCIENCE", "roundType": "REVIEW",
      "status": "OPEN", "result": null,
      "canDelete": false,          // server tính: chỉ true khi CHƯA có hội đồng & mọi đề tài PENDING
      "projects": [                // trạng thái TỪNG đề tài trong vòng (từ project_rounds)
        { "projectId": "guid", "titleVi": "…", "status": "PENDING", "result": null,
          "piUserId": "guid" }   // ⬅ 18/08 — như trên
      ],
      "councils": [
        {
          "id": "guid", "status": "FORMING", "projectIds": ["guid", ...],
          "members": [ /* CouncilMemberResponse (xem §8.3) */ ]
        }
      ]
    }
  ]
}
```

> **`piUserId` (18/08) — ai được vào danh sách chọn ủy viên.** COI (rule #5) vẫn được **chặn ở BE**
> (`AssertNoCoiAsync`, cả khi thêm ủy viên lẫn khi gán đề tài vào hội đồng) — đó là hàng rào thật và
> không đổi. Thêm `piUserId` chỉ để FE **không bày ra** cái tên bấm vào là báo lỗi: trước đó dropdown
> đổ thẳng toàn bộ `/api/users`, nên Staff thấy cả tài khoản quản trị lẫn chính chủ nhiệm đề tài.
> FE lọc bằng `utils/council-eligibility.ts`: giữ người có vai **Faculty hoặc ReviewCommittee**
> (QĐ543 Điều 8.2/12.2 — hội đồng là nhà khoa học; lọc theo "đủ tư cách" chứ **không** cấm riêng vai
> Admin, để người vừa quản trị vừa là giảng viên vẫn vào hội đồng được), bỏ tài khoản đã khoá, bỏ
> chủ nhiệm các đề tài trong vòng.

**Trạng thái / mã lỗi:**
| Tình huống | Mã | Ghi chú |
|---|---|---|
| Xóa vòng đã có hội đồng, hoặc có đề tài đã chấm/chốt | 409 | `DeleteRoundAsync` |
| Gỡ đề tài đã có kết quả, hoặc đã gán hội đồng | 409 | `RemoveProjectFromRoundAsync` |
| Thêm đề tài khác lĩnh vực với vòng | 400 | |
| Thêm đề tài FINANCE chưa ĐẠT vòng tiên quyết | 409 | rule #2, chỉ path thêm-lẻ (xem ⚠ dưới) |
| COI khi tạo hội đồng | 400 | rule #5 |
| Lĩnh vực/vòng/đề tài không tồn tại | 404 | |

> ⚠ **Khác biệt rule #2 giữa 2 path (đang có, cần biết):** `POST /api/rounds/{roundId}/projects` (thêm lẻ) chặn đề tài chưa ĐẠT vòng tiên quyết bằng **409**. Nhưng `POST .../rounds` (tạo/gom hàng loạt) chỉ **bỏ qua âm thầm** (không đưa vào vòng, vẫn trả `200`) và **chỉ khi** round có `prerequisiteRoundId`. Vòng FINANCE tạo **không kèm** `prerequisiteRoundId` sẽ **không** bị kiểm tiên quyết. → Xem backlog `docs/README.md` mục B3.

### 8.2 Vòng theo proposal (legacy — giữ tương thích) — `/api/proposals/{proposalId}/rounds`, `/api/rounds/...`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/proposals/{proposalId}/rounds` | Authenticated | Danh sách vòng của đề xuất (resolve proposal→project→cycle_track) |
| POST | `/api/proposals/{proposalId}/rounds` | Staff, Admin | Tạo vòng `{ dimension, roundType, rubricTemplateId?, prerequisiteRoundId? }` |
| POST | `/api/rounds/{roundId}/open` | Staff, Admin | Mở vòng (`PENDING`→`OPEN`; 409 nếu vòng tiên quyết chưa `PASSED`) |
| POST | `/api/rounds/{roundId}/close` | Staff, Admin | Chốt vòng trực tiếp `{ result, proposalProjectId? }` — **bỏ qua biên bản** (chỉ dùng thủ công/khẩn cấp, không phải luồng chuẩn rule #12) |
| GET | `/api/rounds/{roundId}/members` | Authenticated | Thành viên hội đồng của vòng |
| POST | `/api/rounds/{roundId}/members` | Staff, Admin | Thêm 1 phản biện (tự tạo hội đồng nếu vòng chưa có) |
| DELETE | `/api/rounds/{roundId}/members/{memberId}` | Staff, Admin | Bỏ 1 phản biện |

### 8.3 Hội đồng — `/api/councils`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/councils/my-memberships` | Authenticated | Công việc hội đồng của tôi, **một dòng cho mỗi (hội đồng × đề tài)**. Một hội đồng chấm nhiều đề tài sẽ trả đủ nhiều dòng nhưng cùng `memberId`; FE màn lời mời gộp theo `memberId`, màn chấm giữ từng dòng. `MyMembershipDto` có `projectId`, `proposalId`, `piName`, `cycleId`, `cycleCode`, `trackId`, `trackName`, `createdAt`, `nextMeetingAt` để màn Reviewer tìm/lọc theo đợt · lĩnh vực · trạng thái · vòng. |
| POST | `/api/councils` | Staff, Admin | Lập 1 hội đồng cho 1 đề tài trong round |
| GET | `/api/councils/{councilId}/members` | Authenticated | Thành viên hội đồng |
| DELETE | `/api/councils/{councilId}` | Staff, Admin | **Xóa hội đồng** — chỉ khi **chưa có phiếu chấm / biên bản / nghiệm thu** (có → 409). Tự gỡ thành viên + lịch họp + điểm danh + gán đề tài. |
| POST | `/api/councils/{councilId}/members` | Staff, Admin | Thêm thành viên `{ userId, memberRole, isExternal }` (COI rule #5) |
| POST | `/api/councils/{councilId}/send-invitations` | Staff, Admin | **Gửi thư mời đồng loạt** cho member `ASSIGNED` (rule #13) `{ confirmDeadline? }`. Gate: phải đủ **Chủ tịch + Thư ký + lịch họp + ít nhất 1 đề tài** — thiếu → **409**. Một lời mời xác nhận tư cách cho toàn hội đồng; nội dung liệt kê đầy đủ mọi đề tài hội đồng được giao. |
| GET | `/api/councils/{councilId}/schedule-conflicts` | Staff, Admin | **Cảnh báo trùng lịch** (rule tuần 10): TV hội đồng này còn dự hội đồng khác họp **giao giờ**. Trả `[{ memberUserId, memberName, otherCouncilId, otherCouncilType?, thisMeetingAt, otherMeetingAt }]` (rỗng = không trùng). |
| GET | `/api/councils/{councilId}/slots` | Staff, Admin | **Lịch chấm theo đề tài** (rule tuần 10): slot con từng đề tài trong buổi họp. Trả `[{ projectId, projectTitle, meetingId?, slotStartAt?, slotDurationMinutes?, slotOrder? }]` |
| PUT | `/api/councils/{councilId}/slots` | Staff, Admin | Gán slot: `{ entries: [{ projectId, slotStartAt?, slotDurationMinutes?, slotOrder? }] }` — meetingId auto = buổi họp sớm nhất của hội đồng |
| POST | `/api/councils/{councilId}/projects` | Staff, Admin | **Gán 1 đề tài vào hội đồng có sẵn** `{ projectId }` (dropdown ở màn Hội đồng & Chấm). Đề tài phải đã tham gia round + COI rule #5. Mỗi đề tài ↔ 1 hội đồng/round → tự gỡ khỏi hội đồng khác của round. |
| DELETE | `/api/councils/{councilId}/projects/{projectId}` | Staff, Admin | Gỡ đề tài khỏi hội đồng (chỉ khi chưa có điểm) |
| PATCH | `/api/council-members/{memberId}/respond` | Thành viên | Chấp nhận/từ chối lời mời `{ accept, declineReason? }` — **chỉ chính chủ** (không phải → 403) |
| POST | `/api/council-members/{memberId}/confirm-on-behalf` | Staff, Admin | **Xác nhận thay** (reviewer đồng ý ngoài hệ thống / tiện demo): → `CONFIRMED`. Đã `DECLINED` → 409 |
| DELETE | `/api/council-members/{memberId}` | Staff, Admin | Xoá thành viên |

> Ghi chú: `POST /api/rounds/{roundId}/councils` (tạo hội đồng trọn gói) nay cho phép `projectIds` **rỗng** → tạo hội đồng chỉ có thành viên, gán đề tài sau qua 2 endpoint trên. FE mới (màn "Hội đồng & Chấm") lập hội đồng trước rồi dropdown-gán đề tài.

**CreateCouncilRequest**: `{ proposalId, roundId, councilType, establishmentDecisionNo?, establishedAt?, meetingDeadline?, minMembersRequired=3, maxMembersAllowed=5 }`
**CouncilMemberResponse**: `{ id, councilId, userId, reviewerName, reviewerEmail, memberRole, isExternal, status, invitationSentAt?, confirmedAt?, declinedAt? }` — `status` ∈ `ASSIGNED | INVITED | CONFIRMED | DECLINED | EXPIRED`.

### 8.4 Lịch họp — `/api/meetings`, `/api/councils/{councilId}/meetings`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/meetings` | Admin, Staff | Toàn bộ lịch họp |
| GET | `/api/meetings/my` | * (PI) | **Lịch họp hội đồng chấm đề tài của tôi** — PI **trình bày trước hội đồng** (Process_Spec) nên cần biết ngày/giờ + địa điểm hoặc link. Trước đây chỉ Staff/Reviewer xem được |
| GET | `/api/councils/{councilId}/meetings` | Authenticated | Lịch họp của hội đồng |
| POST | `/api/councils/{councilId}/meetings` | Admin, Staff | Tạo lịch họp. **409 và không ghi dữ liệu** nếu giao giờ với lịch khác của chính hội đồng hoặc lịch hội đồng khác có chung thành viên. Hai lịch nối đuôi đúng giờ kết thúc/bắt đầu được phép. |
| PUT | `/api/meetings/{id}` | Admin, Staff | **Sửa lịch họp (mới 05/08)** — `UpdateMeetingRequest` (cùng bộ trường với lúc tạo), áp dụng cùng ràng buộc chống trùng lịch như POST. Rule #17 cho đổi lịch **bất kỳ lúc nào** nên không khoá theo trạng thái; buổi đã diễn ra thì bỏ ràng buộc "phải ở tương lai" (vẫn sửa được địa điểm/link ghi nhầm). Offline mà trống địa điểm → 400; `durationMinutes <= 0` → 400. Đổi sang online thì BE **tự xoá** `location`, và ngược lại. |
| DELETE | `/api/meetings/{id}` | Admin, Staff | **Xoá buổi họp (mới 05/08)** — chỉ khi `status = SCHEDULED`, ngược lại **409**. Đã có điểm danh (`ActuallyAttended != null`) cũng **409**. Xoá thì dọn dòng điểm danh và **gỡ slot đề tài** đang trỏ tới buổi họp (`CouncilProjectAssignment.MeetingId/SlotStartAt` về null). |
| POST | `/api/meetings/{id}/start` | Admin, Staff | Bắt đầu họp — ⚠️ **đã gỡ khỏi giao diện 17/08** (endpoint giữ lại) |
| POST | `/api/meetings/{id}/end` | Admin, Staff | Kết thúc họp — ⚠️ **đã gỡ khỏi giao diện 17/08** (endpoint giữ lại) |

> ⚠️ **Bỏ nút Bắt đầu/Kết thúc họp trên UI (17/08).** Không luồng nào chờ trạng thái `IN_PROGRESS` (điểm danh, chấm điểm, biên bản đều không kiểm), và **Chủ tịch chốt biên bản thì `ReviewScoringService` tự đóng mọi buổi họp của hội đồng** → hai nút này chỉ tạo bẫy: bấm Bắt đầu rồi Kết thúc là buổi họp "xong" trước cả khi họp thật. Endpoint vẫn còn cho kịch bản test/tự động.

**ScheduleMeetingRequest**: `{ title?, platform="IN_PERSON", meetingLink?, location?, scheduledAt, durationMinutes=120, agenda? }`

| GET | `/api/meetings/{id}/attendance` | * (thành viên HĐ) | **Điểm danh** (rule tuần 10) — list theo DS hội đồng: `[{ memberId, memberName, memberRole, attended?, absenceReason? }]` |
| PUT | `/api/meetings/{id}/attendance` | Thư ký / Admin, Staff | Lưu điểm danh `{ entries: [{ memberId, attended, absenceReason? }] }` — upsert `MeetingAttendance`; lý do chỉ giữ khi vắng | — `platform` ∈ `IN_PERSON | GOOGLE_MEET | TEAMS | ZOOM`. **Offline (`IN_PERSON`) bắt buộc `location`** (thiếu → 400); online thì bỏ `location`, giữ `meetingLink`. `MeetingDto` trả thêm `location`.

### 8.5 Chấm điểm, biên bản & quyết định — `/api/review-scoring`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/review-scoring/rubrics` | Authenticated | Danh sách mẫu rubric |
| GET | `/api/review-scoring/rubrics/{id}` | Authenticated | Chi tiết 1 rubric |
| POST | `/api/review-scoring/councils/{councilId}/scores` | Thành viên | Nộp/sửa phiếu chấm của mình |
| GET | `/api/review-scoring/councils/{councilId}/scores/my` | Authenticated | Phiếu của tôi (null nếu chưa chấm) |
| GET | `/api/review-scoring/councils/{councilId}/scores` | Admin, Staff, **thành viên hội đồng** | Tất cả phiếu (tham khảo) — Thư ký cần để lập biên bản |
| POST | `/api/review-scoring/councils/{councilId}/minutes` | Thư ký | **Soạn/sửa biên bản (nháp)** — chưa khóa, chưa đổi status |
| POST | `/api/review-scoring/councils/{councilId}/minutes/approve` | Chủ tịch | **Duyệt = khóa biên bản** + cập nhật status |

| POST | `/api/review-scoring/councils/{councilId}/minutes/request-revision` | **Chủ tịch** hội đồng | **Trả biên bản cho Thư ký sửa** kèm ghi chú. Body `{ note }` — bắt buộc, rỗng → 400. Không đổi trạng thái khoá; biên bản **đã chốt** → 409. Thư ký nhận thông báo `MINUTES_REVISION_REQUESTED`. Căn cứ QĐ543 Điều 8.3.c |
| GET | `/api/review-scoring/councils/{councilId}/decision` | Authenticated | Xem quyết định/biên bản |
| ~~POST~~ | ~~`/api/review-scoring/councils/{councilId}/decision`~~ | — | **NGỪNG DÙNG** → luôn trả `409`. Dùng luồng biên bản (minutes) bên dưới |

**SubmitScoreRequest**: `{ templateId, projectId?, generalComments?, otherRecommendations?, scoreDetails: [ { criterionId, givenScore, comments? } ] }`
**SaveMinutesRequest**: `{ projectId?, result, councilComments?, recommendations?, qaEntries?, memberOpinions? }` — `result` ∈ `APPROVED | REJECTED | REVISION_REQUIRED`. `qaEntries` = biên bản dạng **Hỏi–Đáp** (BM04/BM12 II.1), mỗi phần tử `{ askedBy?, question, answer?, order }`. `memberOpinions` = **ý kiến từng TV** (BM04 II.1), mỗi phần tử `{ memberName, academicComment?, budgetComment?, order }` (chuyên môn / kinh phí). Cả `qaEntries` và `memberOpinions` gửi lại **thay TOÀN BỘ** danh sách cũ (rỗng = xóa hết). `councilComments` = cách ghi tự do — Thư ký chọn phong cách.
**CouncilDecisionDto**: `{ …, finalizedAt?, qaEntries[], memberOpinions[] }` — `finalizedAt=null` ⇒ còn nháp; `qaEntries`/`memberOpinions` khóa cùng biên bản khi Chủ tịch duyệt.

**Luồng chuẩn (rule #12) — kết quả = QUYẾT ĐỊNH Chủ tịch, không tự đếm phiếu:**
1. Thành viên chấm điểm (`POST .../scores`) — hệ thống hiển thị điểm/phiếu **chỉ để tham khảo**.
2. **Thư ký** `POST .../minutes` ghi kết quả họp kín (nháp; proposal/project/round **chưa đổi**).
3. **Chủ tịch** `POST .../minutes/approve` → khóa (`finalizedAt`) và cập nhật đồng bộ theo `result`:
   | `result` | proposal.status | project.status | project_round | round |
   |---|---|---|---|---|
   | `APPROVED` | `APPROVED` | `APPROVED` | `PASSED` + khóa | tự đóng khi mọi đề tài đã chốt |
   | `REJECTED` | `REJECTED` | `CANCELLED` | `FAILED` + khóa | ↑ |
   | `REVISION_REQUIRED` | `REVISION_REQUIRED` | *(giữ nguyên)* | `OPEN`, **KHÔNG khóa** — chờ PI sửa & nộp lại (rule #1) | **không** đóng |
   - Hội đồng → `DECIDED` khi mọi đề tài được gán đã có biên bản khóa.
   - Round chung chỉ đóng khi **mọi** `project_round` đã terminal (`PASSED`/`FAILED`); một đề tài `REVISION_REQUIRED` còn treo → round vẫn mở. `round.status`/`round.result` suy nhất quán từ "tất cả PASSED?" (không phụ thuộc thứ tự duyệt).

4. **Nộp lại bản REVISION (rule #1):** PI sửa (`PUT` tạo version mới) rồi `POST /api/proposals/{id}/submit` (§6) → hệ thống **tự mở lại hội đồng đã chốt "cần chỉnh sửa"**: council `DECIDED`→`FORMING`, biên bản về nháp, `project_round`→`OPEN` (bỏ result cũ) — **GIỮ nguyên điểm cũ** để chấm lại/bổ sung, không phải lập hội đồng mới.

> ⚠ **Đường chốt trực tiếp:** `POST .../councils/{councilId}/decision` **đã khóa (409)** — luôn chốt qua biên bản. `POST /api/rounds/{roundId}/close` (§8.2) vẫn mở như **công cụ tay/khẩn cấp** (đồng bộ `project_round` đúng qua cùng logic), FE không dùng.

### 8.6 Phản hồi phản biện — `/api/councils/{councilId}/feedback`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/councils/{councilId}/feedback` | Admin, Staff, **thành viên hội đồng** | Xem tổng hợp phản hồi |
| POST | `/api/councils/{councilId}/feedback` | Thành viên | Gửi phản hồi |

**SubmitReviewerFeedbackRequest**: `{ urgencyScore?, scientificContributionScore?, practicalSignificanceScore?, actualVsExpectedScore?, overallAssessment?, otherComments? }` (điểm thang 1–5)

### 8.7 Nghiệm thu — `/api/councils/{councilId}/acceptance`

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/councils/{councilId}/acceptance` | Admin, Staff, **thành viên hội đồng** | Tất cả phiếu nghiệm thu (tổng hợp — Thư ký lập biên bản cần xem). Người ngoài hội đồng → **403**. *Trước đây khóa cứng `Admin,Staff` → reviewer 403, không chấm được.* |
| GET | `/api/councils/{councilId}/acceptance/my?projectId={projectId}` | Thành viên hội đồng | **Phiếu của CHÍNH tôi cho đúng đề tài** (null nếu chưa chấm). `projectId` bắt buộc vì một hội đồng có thể chấm nhiều đề tài. |
| POST | `/api/councils/{councilId}/acceptance` | Thành viên đã `CONFIRMED` | Nộp **hoặc CẬP NHẬT** phiếu riêng cho đề tài `{ projectId, result: "PASS"|"FAIL", failReason? }`. Đề tài không thuộc hội đồng hoặc biên bản của chính đề tài đã chốt → **409**. Mỗi `(councilId, projectId, evaluatorMemberId)` là một phiếu độc lập. |

> **Nghiệm thu chốt vòng đời đề tài:** Chủ tịch duyệt biên bản vòng **ACCEPTANCE** → project `COMPLETED` (Đạt) hoặc quay lại `IN_PROGRESS` (chưa đạt) — khác vòng REVIEW (`APPROVED`/`CANCELLED`). Trước đây mọi vòng đều set APPROVED nên nghiệm thu xong đề tài vẫn "Đã duyệt" → **đứt mạch cuối**.

> **An ninh (§8.1):** `GET .../review-board` (trả danh tính ủy viên toàn lĩnh vực) đã siết `[Authorize(Roles="Admin,Staff")]` — reviewer xem phần của mình qua `/api/councils/my-memberships` (§8.3), không qua board.

---

## 9. Hợp đồng & sau hợp đồng

### Contracts — `/api/contracts`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/contracts` | * | Danh sách hợp đồng. Staff/Admin xem hết; PI xem của mình. **`?mine=true`** → LUÔN chỉ HĐ mình là PI (kể cả tài khoản đa vai Staff/Admin đang "làm PI") — dùng cho trang PI (báo cáo tiến độ/sản phẩm/tổng kết). |
| GET | `/api/contracts/{id}` | * | Chi tiết. DTO có cả `status` của hợp đồng và `projectStatus` của đề tài để không đánh đồng nghiệm thu Đạt với đã thanh lý. |
| POST | `/api/contracts` | Admin, Staff | Tạo hợp đồng. `maxExtensionMonths` bị chặn theo **QĐ543 Điều 10.4** (≤ 1/2 `DurationMonths` của đề cương) — FE tự điền sẵn đúng trần khi Staff chọn đề tài. |
| PUT | `/api/contracts/{id}` | Admin, Staff | **Sửa hợp đồng (mới 05/08)** — `UpdateContractRequest` { contractNumber, scopeTitle?, startDate, endDate, maxExtensionMonths, sideARepresentative?, econtractUrl? }. KHÔNG đổi được `proposalId` và `totalAmount`. Trùng số HĐ → 409; `endDate <= startDate` hoặc `maxExtensionMonths` vượt **1/2 thời gian thực hiện của đề cương** → 400 (QĐ543 Điều 10.4). Hợp đồng **chưa từng gia hạn** thì sửa `endDate` dời luôn `originalEndDate` (là sửa cho đúng, không phải gia hạn); đã gia hạn rồi thì giữ nguyên hạn gốc. |
| DELETE | `/api/contracts/{id}` | Admin, Staff | **Xoá hợp đồng (mới 05/08)** — chỉ khi `status = PENDING_SIGNATURE`, ngược lại **409** (ký rồi thì dùng chấm dứt). Còn chặn 409 nếu đã có: sản phẩm được nộp · báo cáo tiến độ được nộp · báo cáo tổng kết · quyết toán · đơn điều chỉnh. Xoá thành công thì **gỡ** sản phẩm khỏi hợp đồng (giữ lại cho đề tài) và **dọn** lịch giải ngân + kỳ báo cáo tự sinh. |
| POST | `/api/contracts/{id}/sign` | Admin, Staff | Ký |
| POST | `/api/contracts/{id}/terminate` | Admin, Staff | Chấm dứt bất thường `{ reason }`; chỉ hợp đồng đã ký, chưa thanh lý/chấm dứt và đề tài chưa `COMPLETED`. Ghi `terminatedAt/by/reason`, đặt Contract + Project = `TERMINATED`. |
| GET | `/api/contracts/{id}/export-word` | Admin, Staff | **BM05 — tự sinh Word hợp đồng** (rule tuần 10): bốc CN/đề tài/kinh phí/thời gian → `.docx` để ký ngoài |
| GET/POST | `/api/contracts/{id}/documents` | Admin, Staff | **Hồ sơ hợp đồng đã ký**: list / upload (multipart `file`) bản ký — Document polymorphic EntityType="Contract" |
| GET | `/api/contracts/{id}/documents/{documentId}/download` | Admin, Staff | Tải/mở bản hợp đồng đã ký (Bearer) |
| GET | `/api/contracts/{contractId}/disbursements` | * | Đợt giải ngân |
| POST | `/api/contracts/{contractId}/disbursements/generate` | Admin, Staff | **Sinh lịch giải ngân theo LOẠI ĐỀ TÀI** (QĐ543 Điều 16 — đổi 11/08). Đọc `disbursement_templates` của `researchTypeId`: **Ứng dụng 4 đợt 30–30–30–10**, **Cơ bản 1 đợt 100% sau nghiệm thu**. Chưa cấu hình mốc ⇒ lùi về 1 đợt 100%. Đợt cuối lấy phần còn lại nên tổng luôn khớp giá trị hợp đồng. **409** nếu hợp đồng đã có đợt. <br>⚠️ Trước đây chia theo `Proposal.FundingMethod` (WHOLE ⇒ 3 đợt / PARTIAL ⇒ 1 đợt mỗi sản phẩm) — **bỏ**, vì "phương thức khoán chi" không có trong QĐ543. |
| GET | `/api/contracts/{contractId}/deliverables` | * | Sản phẩm phải nộp |
| POST | `/api/contracts/{contractId}/deliverables` | Admin, Staff | **Staff thêm 1 sản phẩm** cho hợp đồng `{ productName, categoryId?, dueDate?, description? }` (đề cương không có trường sản phẩm cấu trúc → nhập tay; PI sau đó nộp file). |
| GET | `/api/contracts/{contractId}/amendments` | * | Điều chỉnh hợp đồng |
| POST | `/api/contracts/{contractId}/amendments` | * | Tạo yêu cầu điều chỉnh. **409 khi đề tài đã đóng** (`COMPLETED`/`CANCELLED`/`TERMINATED`) hoặc hợp đồng đã chấm dứt (mới 06/08). |

**CreateContractRequest**: `{ proposalId, contractNumber, scopeTitle?, startDate, endDate, maxExtensionMonths=6, sideARepresentative?, econtractUrl? }` — nhận `proposalId` nhưng hợp đồng neo **project**; ký HĐ → project sang `IN_PROGRESS`. **1 đề tài có thể ký nhiều hợp đồng** (từng giai đoạn/phần sản phẩm). `ContractDto` trả thêm `projectId`, `scopeTitle`.
**CreateAmendmentRequest**: `{ categoryId, changeDescription, justification, changePercentage?, oldValue?, newValue?, requiresRectorApproval, reviewerComments? }`. Theo BM07, UI chỉ yêu cầu `changeDescription + justification`; riêng `EXTENSION` gửi `newValue` là số tháng. `oldValue/newValue` giữ tương thích dữ liệu cũ, không ép người dùng nhập cho nội dung/kinh phí/thay đổi khác.

### Giải ngân — `/api/disbursements`
| POST | `/api/disbursements/{id}/confirm` | Admin, Staff | **Đánh dấu đã giải ngân** (rule tuần 10 — không quản tiền): `{ actualAmount?, bankReference?, notes? }` — các trường nhập optional, nhưng **bắt buộc đã upload ít nhất 1 file minh chứng**. Gate theo QĐ543 Điều 16: hợp đồng phải đã ký; đề tài ứng dụng đợt 2/3 cần báo cáo tiến độ giai đoạn 1/2 được đánh giá `PASS`; đợt cuối (kể cả đề tài cơ bản chỉ có 1 đợt) cần project `COMPLETED` sau nghiệm thu Đạt. Vi phạm → 409. |
| PUT | `/api/disbursements/{id}/deliverable` | Admin, Staff | **Gắn/gỡ sản phẩm minh chứng cho đợt** (P5): `{ deliverableId: int \| null }` — `null` = gỡ. **400** nếu sản phẩm không thuộc cùng hợp đồng; **409** nếu đợt đã giải ngân. Gắn sản phẩm đã `PASSED` ⇒ set luôn `conditionMetAt` |
| GET | `/api/disbursements/{id}/evidence` | Admin, Staff | **Minh chứng giải ngân** (rule tuần 10) — list file HĐ/chứng từ của đợt |
| POST | `/api/disbursements/{id}/evidence` | Admin, Staff | Upload minh chứng (multipart `file`) — tái dùng `Document` polymorphic (EntityType="Disbursement"); siết dung lượng/đuôi theo `system_settings` |
| GET | `/api/disbursements/{id}/evidence/{documentId}/download` | Admin, Staff | Tải/mở file minh chứng (cần Bearer) |

> **Phụ lục hợp đồng (17/08):** `GET /api/amendments/{id}/export-word` (**Admin/Staff, hoặc chính chủ nhiệm** của hợp đồng) — xuất **phụ lục** ra Word để ký ngoài, **409** nếu đề nghị chưa duyệt. Endpoint có từ trước nhưng **giao diện chưa hề gọi**; nay có nút ở tab "Điều chỉnh" (Staff) và ở màn **"Điều chỉnh & gia hạn"** của PI — chỉ hiện khi trạng thái ĐÃ DUYỆT.
>
> **Quyền xuất văn bản hợp đồng nới cho PI (17/08):** `GET /api/contracts/{id}/export-word`, `…/export-settlement-word` và `…/amendments/{id}/export-word` **bỏ chặn cứng theo vai**, thay bằng **kiểm quyền sở hữu**: Admin/Staff xem mọi hợp đồng, chủ nhiệm chỉ xem hợp đồng của mình, người khác → **403**. Lý do: PI chính là bên ký, trước đó phải nhắn chuyên viên gửi hộ. `DocumentExportService` **không** tự kiểm quyền — kiểm nằm ở controller, đừng bỏ khi thêm endpoint xuất mới. Căn cứ **BM05 Điều 6.1**: sửa đổi phải *"lập thành văn bản phụ lục có đầy đủ chữ ký của các bên"* — hợp đồng gốc **không bị sinh lại**.

### Sản phẩm — `/api/deliverables`

> **Mới 17/08:** `DeliverableResponse` thêm **`scientificRequirements`** và **`notes`** — hai trường chủ nhiệm đã khai từ lâu nhưng DTO không trả nên không màn nào hiện được. FE dùng chúng trong panel chi tiết sản phẩm (dùng chung Staff ↔ hội đồng, chỉ xem).
| POST | `/api/deliverables/{id}/submit` | * (PI) | Nộp sản phẩm (`{ fileUrl, trialEvidenceUrl?, description? }`). **Chuẩn hoá URL (tuần 12):** chỉ nhận đường dẫn nội bộ (`/…` do BE sinh sau upload) hoặc link http(s); thiếu scheme thì BE **tự thêm `https://`**, không parse được → 400 — trước đây nhận nguyên xi mọi chuỗi (vd `abc.com`) nên người nghiệm thu bấm vào ra trang trống. **409 nếu sản phẩm đã nghiệm thu ĐẠT (mới 06/08)** — nộp lại sẽ đặt `acceptanceStatus` về `PENDING`, tức **xoá mất kết quả nghiệm thu** và khoá lại đợt giải ngân vốn đã mở nhờ sản phẩm đó. |
| POST | `/api/deliverables/{id}/evaluate` | Admin, Staff | Đánh giá (`{ acceptanceStatus: "PASSED"|"FAILED", qualityAssessment? }`) |

### Điều chỉnh — `/api/amendments`
| GET | `/api/amendments/{id}` | * | Chi tiết |
| POST | `/api/amendments/{id}/approve` | Admin, Staff | Duyệt (`ReviewAmendmentRequest`). **409 khi đề tài đã đóng** — chặn cả ở đây chứ không chỉ lúc gửi, vì đơn có thể nằm chờ từ trước khi đề tài được nghiệm thu. Gia hạn vẫn giới hạn ≤ `MaxExtensionMonths` (QĐ543 Điều 10.4). |
| POST | `/api/amendments/{id}/reject` | Admin, Staff | Từ chối |

### Báo cáo tiến độ — `/api/progress-reports`
| GET | `/api/progress-reports?contractId=` | * | Theo hợp đồng |
| GET | `/api/progress-reports/{id}` | * | Chi tiết |
| POST | `/api/progress-reports/generate?contractId=&roundCount=` | Admin, Staff | **Sinh sẵn các kỳ báo cáo** — chia đều theo mốc hợp đồng, bỏ qua kỳ đã có. **`roundCount` (1–12) để Staff tự chọn số kỳ**; bỏ trống → mặc định theo loại (QĐ543 Điều 10.1: Ứng dụng 2 / Cơ bản 1). ⚠️ **Tuần 12: không còn fix cứng số kỳ** (thầy 29/07). |
| POST | `/api/progress-reports` | * (PI) | Tạo 1 kỳ. *(Tuần 12 bỏ chặn cứng theo loại — Staff/PI thêm kỳ được.)* Luồng chuẩn: Staff `generate` → PI điền. |
| PUT | `/api/progress-reports/{id}` | * (PI) | **Sửa nội dung khi chưa có kết quả đánh giá** (`DRAFT` hoặc `SUBMITTED`) — `UpdateProgressReportRequest`. `reportFileUrl` chỉ nhận link http(s); bỏ trống → giữ giá trị cũ. Trả kèm trong summary + detail để Staff/hội đồng mở xem. |
| POST | `/api/progress-reports/{id}/submit` | * (PI) | Nộp/nộp lại trước khi Staff đánh giá. **409** khi kỳ **trước** chưa nộp hoặc Staff chưa đánh giá. Nguồn: QĐ543 Điều 10.1 — báo cáo **định kỳ**, kỳ sau chỉ có nghĩa khi kỳ trước đã chốt. Chặn theo **KỲ**, không chặn PI nộp sớm vì QĐ543 không quy định khoảng cách ngày. |
| PATCH | `/api/progress-reports/{id}/schedule` | Admin, Staff | Đặt lịch báo cáo `{ dueDate?, scheduledMeetingAt?, meetingLink?, roundName? }`. **`dueDate` đặt lại = GIA HẠN** hạn nộp (thầy 29/07: đánh giá trúng ngày cuối thì gia hạn được). Máy chủ chặn hạn nộp/buổi họp ở quá khứ, ngoài thời gian hợp đồng, trước đầu kỳ báo cáo; buổi họp cũng không được trước hạn nộp. **`roundName`** = tên đợt Staff đặt (vd "Giữa kỳ"); null → FE hiện "Kỳ {số}". |
| POST | `/api/progress-reports/{id}/evaluate` | Admin, Staff | Đánh giá — `evaluationResult` ∈ **`PASS` / `FAIL` / `CONDITIONAL`** (QĐ543 Điều 10/BM06: Đạt / Không đạt / Có điều kiện). ⚠️ **Đổi tuần 12:** trước BE nhận `SATISFACTORY/…` còn FE gửi `APPROVED/…` → Staff bấm đánh giá **luôn 400**, không chấm được. |
| GET | `/api/progress-reports/{id}/documents` | * | **File báo cáo (BM06)** PI đã nộp |
| POST | `/api/progress-reports/{id}/documents` | PI của đề tài (hoặc Admin/Staff) | **PI upload file PDF/Word** báo cáo (multipart `file`). Người khác → 403 |
| GET | `/api/progress-reports/{id}/documents/{documentId}/download` | * | Tải/mở file báo cáo — **Staff phải xem file rồi mới đánh giá được** (FE khóa nút khi chưa có file) |

### Bộ tiêu chí chấm — `/api/rubric-templates`
> **"Bộ tiêu chí"** = `RubricTemplate` + các `RubricCriterion` bên trong (thầy 29/07: tiêu chí chia theo **loại đề tài** + **lĩnh vực**, phải linh hoạt). 1 bộ **dùng lại cho nhiều đợt**.
> **Thứ tự ưu tiên khi chấm:** ① bộ gắn RIÊNG cho vòng → ② bộ theo (đợt + lĩnh vực + loại vòng) → ③ bộ mặc định chung.
> **Ràng buộc:** mỗi **(đợt + lĩnh vực + LOẠI VÒNG)** chỉ 1 bộ — nhưng cùng lĩnh vực vẫn có bộ riêng cho **Xét duyệt** và bộ riêng cho **Nghiệm thu** (tiêu chí khác hẳn).
> **Thang điểm:** mọi bộ dùng để tổng hợp trên thang **100**. Riêng BM10, phản biện vẫn chọn mức 1–5 đúng biểu mẫu; FE quy đổi thành 5/10/15/20/25 cho mỗi tiêu chí (4 tiêu chí = 100). Seed cũ 20 điểm được nâng tỷ lệ cả tiêu chí lẫn phiếu đã lưu, không đổi mức đánh giá tương đối.

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/rubric-templates` | * | Danh sách bộ + tiêu chí + phạm vi đã gắn |
| GET | `/api/rubric-templates/resolve?cycleId=&trackId=&templateType=` | * | **Bộ áp dụng** cho (đợt, lĩnh vực, loại vòng). Không có bộ riêng → trả **bộ mặc định** (bộ chưa gắn phạm vi nào) nên không bao giờ kẹt không chấm được |
| GET | `/api/rubric-templates/for-council/{councilId}` | * | **Bộ áp dụng cho 1 hội đồng** — BE tự suy (đợt, lĩnh vực, loại vòng) từ hội đồng → form chấm điểm chỉ cần councilId. Fallback bộ mặc định |
| PATCH | `/api/rubric-templates/rounds/{roundId}` | Admin, Staff | **Gắn/gỡ bộ tiêu chí RIÊNG cho 1 vòng** `{ templateId }`. 1 bộ gắn được nhiều vòng; mỗi vòng dùng bộ khác nhau. `null` → bỏ gắn riêng, quay về bộ theo (đợt+lĩnh vực) |
| PATCH | `/api/rubric-templates/{id}` | Admin, Staff | Đổi `name` / `appliesBasic` / `appliesApplied` / `isActive`. Bỏ tick cả 2 loại → **400** |
| PUT | `/api/rubric-templates/{id}/scopes` | Admin, Staff | Lưu danh sách `{ entries: [{ cycleId, trackId }] }`. Trùng bộ khác **cùng loại vòng** → **409** kèm tên bộ đang giữ |
| POST | `/api/rubric-templates/{id}/duplicate` | Admin, Staff | **Sao chép bộ** (kèm tiêu chí) → tên `Sao chép "<tên gốc>"`. **KHÔNG copy phạm vi** (vì mỗi lĩnh vực chỉ 1 bộ → copy sẽ đụng ngay) |

### Báo cáo tổng kết — `/api/final-reports`
> Nghiệm thu cuối nay là **của ĐỀ TÀI** (1 `final_report` / project), không phải từng hợp đồng — route vẫn nhận `contractId` (resolve → project). `FinalReportDto` trả `projectId`.

| GET | `/api/final-reports/{contractId}` | * | Theo hợp đồng (→ báo cáo cuối của đề tài) |
| POST | `/api/final-reports/{contractId}/submit` | * (PI) | Nộp báo cáo cuối (project → `ACCEPTANCE`). Bản đầy đủ và tóm tắt nhận **file nội bộ `/api/...` hoặc link HTTP(S)**; chuỗi tương đối kiểu `ab` bị 400 để không mở nhầm route FE. `language` chỉ nhận `VI`/`EN`. Nếu đang `SUBMITTED` hoặc `REVISION_REQUIRED`, PI được cập nhật/nộp lại cùng bản ghi; `ACCEPTED/ARCHIVED` thì khoá. |
| GET | `/api/final-reports/{contractId}/documents` | * | **File báo cáo tổng kết (BM09)** đã upload |
| POST | `/api/final-reports/{contractId}/documents` | * | **Upload file PDF/Word** (multipart `file`) → trả `downloadUrl` để nộp kèm. FE đồng thời giữ lựa chọn dán link ngoài để PI linh hoạt khi tài liệu đã nằm trên Drive/kho cơ quan. |
| GET | `/api/final-reports/{contractId}/documents/{documentId}/download` | * | Tải/mở file |
| POST | `/api/final-reports/{id}/request-revision` | Admin, Staff | Yêu cầu sửa |
| POST | `/api/final-reports/{id}/accept` | Admin, Staff | Chấp nhận |
| POST | `/api/final-reports/{id}/archive` | Admin, Staff | Lưu trữ; 409 nếu chưa có bản tóm tắt (QĐ543 Điều 13 yêu cầu báo cáo đầy đủ + tóm tắt) |

### Quyết toán — `/api/contracts/{contractId}/settlement`, `/api/settlements/...`
| GET | `/api/contracts/{contractId}/settlement` | * | Xem quyết toán |
| POST | `/api/contracts/{contractId}/settlement` | Admin, Staff | Tạo; **409** nếu đề tài chưa nghiệm thu Đạt (`projectStatus != COMPLETED`), còn đợt giải ngân chưa chi hoặc hợp đồng đã chấm dứt. |
| POST | `/api/settlements/{id}/sign` | Admin, Staff | Ký BM13; **409** nếu chưa xác nhận cả kế toán + tài sản. Thành công đổi hợp đồng sang `SETTLED`. |
| POST | `/api/settlements/{id}/accounting-cleared` | Admin, Staff | Xác nhận đã quyết toán kế toán |
| POST | `/api/settlements/{id}/assets-cleared` | Admin, Staff | Xác nhận đã thanh lý tài sản |

---

## 10. Thống kê, thông báo, dev tools

### Analytics — `/api/analytics`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/analytics/overview` | Admin, Staff | Tổng quan |
| GET | `/api/analytics/by-track?cycleId=` | Admin, Staff | Theo track |
| GET | `/api/analytics/funnel?cycleId=` | Admin, Staff | Phễu trạng thái |
| GET | `/api/analytics/dashboard/staff` | Admin, Staff | Dashboard Staff: `{ kpis[], reviewProgress[], councilPerformance[], activity[] }` |
| GET | `/api/analytics/dashboard/faculty` | Faculty, Admin | Dashboard PI (theo user đăng nhập): `{ kpis[], proposalStatus[], upcomingDeadlines[], aiSuggestions[], activity[] }` |
| GET | `/api/analytics/dashboard/reviewer` | ReviewCommittee, Admin | Dashboard reviewer (theo user đăng nhập): `{ kpis[], reviewCompletionTrend[], reviewDecisions[], activity[] }` |

- `KpiDatum`: `{ id, label, value, format?, deltaLabel? }` · `ActivityItem`: `{ id, message, actor, timestamp, type }` với `type` ∈ `proposal|review|council|meeting|contract|system`.
- 3 endpoint dashboard thêm 15/07 theo yêu cầu FE mới (trước đó FE phải mock). Lưu ý authz: nhiều `[Authorize]` là **AND** — controller giữ `[Authorize]` chung, role đặt ở từng endpoint.

### Notifications — `/api/notifications`
| GET | `/api/notifications` | * | Thông báo của tôi |
| GET | `/api/notifications/count` | * | Số chưa đọc |
| PATCH | `/api/notifications/{id}/read` | * | Đánh dấu đã đọc |
| PATCH | `/api/notifications/read-all` | * | Đọc tất cả |

### Admin / Dev tools — `/api/admin` (Admin)
| GET | `/api/admin/system-clock` | Xem mốc thời gian hệ thống (đã tua) |
| POST | `/api/admin/system-clock` | Đặt offset ngày (`{ offsetDays }`) — tua nhanh để test mốc hạn |
| POST | `/api/admin/run-deadline-scan` | Chạy ngay tác vụ quét nhắc hạn |

---

## 11. Bảng tóm tắt quyền theo vai trò

| Nhóm chức năng | Faculty (PI) | ReviewCommittee | Staff | Admin |
|---|:---:|:---:|:---:|:---:|
| Đề xuất của mình (tạo/sửa/nộp/rút) | ✅ | – | xem | xem |
| Xem mọi đề xuất | – | – | ✅ | ✅ |
| Master data (cycle/track/lookup) | đọc | đọc | sửa* | sửa |
| Lập hội đồng / phân công / lịch họp | – | – | ✅ | ✅ |
| Chấm điểm / phản hồi / nghiệm thu | – | ✅ | xem | xem |
| Chốt quyết định hội đồng | – | – | ✅ | ✅ |
| Hợp đồng / giải ngân / điều chỉnh | nộp SP, tạo CR | – | ✅ | ✅ |
| Báo cáo tiến độ / tổng kết | nộp | – | đánh giá | đánh giá |
| Thống kê, người dùng, dev-tools | – | – | thống kê | ✅ |

(*) cycle/track: Staff sửa được; các lookup khác (budget category, role type, financial config, product category, org unit, rubric) chỉ **Admin** ghi.

---

## 12. Ghi chú cho team FE
- Luôn đọc `success` trước, dùng `message` để hiển thị lỗi cho người dùng.
- Gửi `Content-Type: application/json; charset=utf-8` cho body có tiếng Việt.
- Lưu token vào storage, gắn `Authorization: Bearer` cho mọi request (trừ login).
- Trạng thái backend là **CHỮ HOA**; nếu UI muốn nhãn tiếng Việt thì tự map ở FE.
- Một số sub-resource (research-contents, expected-products, labor details) BE đã có nhưng FE cũ chưa dùng hết — Swagger có đủ.
- Khi cần biết chính xác field của 1 request/response: mở **Swagger**, mục tương ứng có schema + ví dụ.

### Kết quả bỏ phiếu chi tiết — BM12 mục 10.1 (mới 06/08)
`GET /api/review-scoring/councils/{councilId}/ballot-tally?projectId=` → `BallotTallyDto`:
`totalMembers` (= **số phiếu phát ra**) · `ballotsReturned` · `validBallots` · `invalidBallots` ·
`passCount` / `failCount` (vòng NGHIỆM THU) · `averageScore` · `isAcceptanceRound` ·
`ballots[]` gồm `{ memberId, memberName, memberRole, hasSubmitted, isValidBallot, totalScore, maxScore, result, comments, submittedAt }`.

Nguồn: QĐ543 **BM12 mục 10.1** *"Số phiếu phát ra … thu về … hợp lệ … không hợp lệ; Kết quả đánh giá: Đạt … Không đạt …"*. Trước đây biên bản chỉ có 4 ô nên Thư ký không có số để điền vào biểu mẫu, và không ai biết điểm nào của ai. Vòng XÉT DUYỆT trả `totalScore` (BM03 thang 100), vòng NGHIỆM THU trả `result` Đạt/Không đạt (BM11 không có thang điểm).

⚠️ **Sửa 08/08 — phiếu nghiệm thu nay được tính vào quorum và vào số liệu biên bản.** Phiếu chấm điểm nằm ở `review_scores`, phiếu Đạt/Không đạt nằm ở `acceptance_evaluations`; trước đây **chỉ bảng đầu được đếm**, nên hội đồng nghiệm thu dù đủ 5/5 phiếu vẫn bị `POST /minutes` trả **409** *"mới có 0/5 phiếu"* và biên bản luôn hiện `validBallots = 0`. Nay:
- **Quorum** (`POST …/minutes`, `POST …/minutes/approve`) đếm **hợp** hai bảng, distinct theo thành viên; rule *"nghiệm thu phải có phản biện dự"* (Điều 12.3.b) cũng nhận phiếu BM11.
- **`CouncilDecisionDto`** của vòng nghiệm thu: `attendingMembers`/`validBallots`/`invalidBallots` tính cả phiếu Đạt/Không đạt; `averageScore` vẫn **`null`** vì nghiệm thu không chấm điểm.

### Khoá bộ tiêu chí đã dùng để chấm — mới 09/08 (rule #13)

Một phiếu chấm chỉ lưu `(criterionId, givenScore)`. **Sửa tên tiêu chí là đổi nghĩa phiếu đã ký** —
biên bản in hôm nay khác biên bản in hôm qua từ cùng một dữ liệu. **Hạ điểm tối đa còn tệ hơn**:
phiếu cũ chấm 20 trên tiêu chí nay trần chỉ còn 10 ⇒ tổng sai mà không ai biết vì sao.

Xoá bộ và xoá tiêu chí vốn đã bị chặn; chỗ hở là **SỬA** — trước đây không kiểm gì. Nay bộ **đã có
người chấm bằng nó** thì khoá nội dung, **409** ở cả 4 đường:

| Endpoint | Chặn khi |
|---|---|
| `PATCH /api/rubric-templates/{id}` | đổi **tên** hoặc loại đề tài áp dụng *(bật/tắt bộ vẫn cho — tắt chỉ ngăn dùng cho vòng MỚI)* |
| `POST /api/rubric-templates/{id}/criteria` | thêm tiêu chí |
| `PUT /api/rubric-templates/{id}/criteria/{cid}` | sửa tiêu chí |
| `DELETE /api/rubric-templates/{id}/criteria/{cid}` | xoá **và cả tắt** tiêu chí |

**Đường thoát:** `POST /api/rubric-templates/{id}/duplicate` → sửa bản sao → gắn cho vòng chấm mới.
Vòng đang dùng bộ cũ giữ nguyên. Đúng rule #13: *"đổi active chỉ áp đề tài mới"*.

`GET /api/rubric-templates` trả thêm **`ballotCount`** và **`isLocked`** để màn quản lý làm mờ nút
Sửa/Xoá kèm lời giải thích, thay vì để người dùng bấm rồi mới ăn 409.

> **Vì sao không đánh số version thật:** "khoá + nhân bản" giữ nguyên lịch sử, **không cần
> migration**, và dùng lại đúng nút "Nhân bản" đã có. Đánh version thật chỉ đáng làm khi cần so
> sánh giữa các bản — chưa ai yêu cầu.

### Cảnh báo quỹ giờ buổi họp — mới 09/08

`GET /api/councils/{id}/slots` → `CouncilSlotBoardDto` thêm 4 trường:
`projectCount` · `unscheduledCount` · `remainingMinutes` (âm = đã vượt) · **`warning`** (`null` khi
không có gì bất thường).

Có vì thứ tự thao tác thực tế: Staff đặt lịch họp trước, **rồi mới** gán thêm đề tài 2, 3 vào cùng
hội đồng. Trước đây không có gì nhắc, nên buổi họp 90 phút gán 5 đề tài vẫn lưu được và chỉ vỡ ra
vào đúng hôm họp.

**Hai** trường hợp `warning` khác `null` (từ 17/08):
1. Tổng khung đã chia **vượt** thời lượng buổi họp
2. Còn đề tài **chưa có khung giờ** mà buổi họp đã kín

> ⚠️ **Bỏ trường hợp 3 (17/08):** *"Còn N đề tài chưa có khung giờ, buổi họp còn trống X phút"* — nó bật ngay khi vừa mở bảng, lúc Staff chưa kịp nhập gì, tức là báo động cho một trạng thái **hoàn toàn bình thường**. Cảnh báo nào cũng kêu thì thành tiếng ồn và người dùng bỏ qua luôn hai cảnh báo thật ở trên. Số liệu vẫn còn ở `unscheduledCount` / `remainingMinutes` nếu FE muốn tự bày.

⚠️ **Cảnh báo chứ KHÔNG chặn** — rule #17 cho đổi lịch bất kỳ lúc nào, khoá cứng sẽ cản đúng thao
tác hợp lệ. Và cố ý **không tự đặt ra "mỗi đề tài tối thiểu bao nhiêu phút"**: QĐ543 không quy định
con số đó, bịa ra là cắm một tham số nghiệp vụ vào code. Chỉ nói bằng phép tính có thật, Staff tự cân.

### XOÁ master data — mới 09/08

Năm màn Admin trước đây chỉ có thêm/sửa. Nay có `DELETE`, quyền **Admin**, theo đúng khuôn đã dùng
cho loại đề tài: **xoá vĩnh viễn chỉ khi KHÔNG ai tham chiếu**; còn dùng thì báo 409 và bảo
**vô hiệu hoá** (`isActive = false`) — dữ liệu cũ không được để mồ côi.

| Endpoint | Chặn khi đang được… |
|---|---|
| `DELETE /api/organizational-units/{id}` | người dùng · đề tài · danh mục đặt hàng · đơn vị con |
| `DELETE /api/product-categories/{id}` | sản phẩm của đề tài |
| `DELETE /api/personnel-role-types/{id}` | thành viên đề tài (khớp theo **`memberRoleCode`**, không phải khoá ngoại) |
| `DELETE /api/budget-expense-categories/{id}` | dòng dự toán của đề tài |
| `DELETE /api/financial-configs/{id}` | *(không bảng nào trỏ tới — cấu hình chỉ được **đọc** lúc tính dự toán, số đã tính nằm sẵn trong đề tài, nên cho xoá thẳng)* |

Thông báo nêu **tên** bản ghi và **liệt kê chỗ đang dùng**, để Admin biết phải gỡ ở đâu thay vì
đoán mò.

### Cấu hình bước nhảy điểm — sửa 09/08

`GET /api/system-settings/scoring-policy` → `{ scoreDecimalPlaces }`. **Mọi user đã đăng nhập**
đọc được (như `upload-policy`).

Vì sao phải tách: màn chấm điểm của hội đồng cần biết bước nhảy để dựng ô nhập, nhưng trước đó nó
gọi `GET /system-settings` — endpoint **chỉ cho Admin**. Reviewer luôn ăn **403**, lỗi bị nuốt, rơi
về mặc định số nguyên. Cộng thêm `SCORE_DECIMAL_PLACES` **chưa từng được seed thành dòng** nên Admin
sửa cũng **404**. Kết quả: tính năng A10 *"Admin đặt bước nhảy điểm"* **chưa bao giờ chạy** dù chốt
chặn phía máy chủ vẫn đúng.

Nay: thêm endpoint công khai + seed dòng cấu hình. `GET /system-settings` (danh sách đầy đủ) **vẫn
chỉ Admin** — reviewer không cần và không nên thấy toàn bộ cấu hình vận hành.

### Bổ sung SỬA/XOÁ còn thiếu — mới 09/08

Rà CRUD 05/08 phát hiện ba chỗ **có thêm mà không có sửa/xoá**. Nay bổ sung, kèm cửa khoá:

| Endpoint | Quyền | Chặn khi |
|---|---|---|
| `PUT /api/deliverables/{id}` | Admin/Staff | sản phẩm đã nghiệm thu **Đạt** → 409 |
| `DELETE /api/deliverables/{id}` | Admin/Staff | đã nghiệm thu Đạt · **đã nộp minh chứng** · **đang là điều kiện của một đợt giải ngân** (kèm số đợt) → 409 |
| `DELETE /api/progress-reports/{id}` | PI của đề tài, hoặc Admin/Staff | báo cáo **không còn là bản nháp** → 409. Xoá kèm các dòng `progress_report_items` |
| `PUT /api/proposals/{proposalId}/team-members/{memberId}` | **chỉ chủ nhiệm** | đề cương không còn `DRAFT`/`REVISION_REQUIRED` → 409 |
| `DELETE /api/proposals/{proposalId}/team-members/{memberId}` | **chỉ chủ nhiệm** | đề cương đã nộp → 409 · thành viên là **chủ nhiệm** → 409 · thành viên **đang có dòng thuê khoán trong dự toán** → 409 |

Hai điểm cố ý:
- **`isPi` không sửa được qua `PUT` thành viên** — chủ nhiệm là cột neo của đề tài
  (`Project.PiUserId`); đổi người phải đi qua đề nghị thay đổi nhân sự (BM07), không sửa lén trong
  danh sách.
- **Báo cáo đã nộp không xoá khỏi lịch sử** — nó là căn cứ mở đợt giải ngân và là một mục trong hồ
  sơ nghiệm thu; xoá đi thì hội đồng thấy đề tài "nhảy cóc" một kỳ mà không ai giải thích được.

### Rà validate toàn hệ thống — thay đổi hành vi 09/08 (F1)

| Thay đổi | Trước | Nay |
|---|---|---|
| `dimension` khi tạo vòng chấm | nhận `SCIENCE` hoặc `FINANCE` | **chỉ `SCIENCE`** — bỏ phương diện tài chính (rule #16). Gửi `FINANCE` → **400** |
| Gán đề tài vào vòng có **vòng tiên quyết** | chỉ kiểm khi `dimension == FINANCE` ⇒ **không bao giờ chạy** | luôn kiểm: đề tài phải **ĐẠT** vòng tiên quyết; **chưa từng tham gia cũng là chưa đạt** → **409** |
| `POST /progress-reports/{id}/evaluate` | đánh giá được báo cáo **không có file lẫn link** | phải có **file đính kèm hoặc link** mới đánh giá được → **409** |
| Thông báo lỗi nghiệp vụ | 88 câu tiếng Anh | **toàn bộ tiếng Việt** |

`evaluationResult` chỉ nhận **`PASS` / `CONDITIONAL` / `FAIL`** (QĐ543 Điều 10, BM06) — giá trị
`ACHIEVED` từng xuất hiện trong dữ liệu demo là **sai**, đã sửa và seed lại.

📘 Toàn bộ quy tắc nghiệp vụ tra ở **`docs/BUSINESS_RULES.md`** (luật → căn cứ QĐ543 → dòng code → mã lỗi).

### Thông tin định danh Bên B để lập hợp đồng — mới 08/08 (C3)

`GET /api/users/me/contract-identity` → `ContractIdentityResponse`
`PUT /api/users/me/contract-identity` ← `UpdateContractIdentityRequest`

Chỉ có đường **`/me`** — **chính chủ tự khai**, Staff/Admin không gõ hộ kể cả khi đang lập hợp đồng
cho người đó. Căn cứ thu thập: BM05 **Điều 7.2** — Bên B *"ủy quyền cho Trường ĐH FPT khai báo thông
tin định danh để **cấp chứng thư số**"*.

- Đọc ra **luôn bị che**: `bankAccountNumberMasked` / `nationalIdMasked` = `****1234`. Số đầy đủ
  **không đi ra khỏi máy chủ qua API**; nó chỉ được đổ thẳng vào **file Word hợp đồng** lúc xuất,
  đúng chỗ mà bản giấy vốn để trống.
- `hasBankAccount` / `hasNationalId` để giao diện biết hiện "Khai thông tin" hay "Cập nhật".
- `missingForContract[]` liệt kê phần còn thiếu — **chỉ để nhắc**, không phải điều kiện chặn:
  **TUỲ CHỌN**, chưa khai thì hợp đồng vẫn lập được và bản Word để dấu chấm lửng như bản giấy.
- Trong `PUT`: trường `null` = **giữ nguyên**, chuỗi rỗng = **xoá**. Số tài khoản/CCCD tự bỏ khoảng
  trắng và dấu chấm (người dùng hay gõ theo nhóm); tên ngân hàng và nơi cấp giữ nguyên khoảng trắng.
- Kiểm tra: số tài khoản chỉ chữ số · CCCD 12 chữ số (hoặc 9 nếu là CMND cũ) · ngày cấp không ở tương lai.
- Mỗi lần đổi ghi một dòng `audit_logs` (`UPDATE_CONTRACT_IDENTITY`) — trong đó `old_values` là bản
  **đã che** và `new_values` chỉ ghi **tên trường đã đổi**, không nhân bản số thật sang bảng khác.

⚠️ **Đổi shape 08/08:** `GET/PUT /api/users/{userId}/profile` nay trả `AcademicProfileResponse`
thay vì entity `AcademicProfile`. Tên trường **giữ y hệt** nên giao diện không phải sửa; mục đích là
để 5 cột mới (tài khoản, CCCD) **không lọt ra** ở endpoint mà Admin/Staff cũng gọi được.

### Phụ lục hợp đồng — mới 08/08 (F4)

`GET /api/amendments/{id}/export-word` → file `.docx` (`PhuLucHopDong_<số HĐ>_<ngày>.docx`).
Quyền **Admin/Staff**. Trả **409** nếu đề nghị điều chỉnh chưa ở trạng thái `APPROVED`.

Hợp đồng đã ký **không sửa đè lên bản gốc** — mỗi thay đổi phải có văn bản riêng dẫn chiếu hợp đồng
gốc và ghi rõ *trước → sau*. Trước đây duyệt điều chỉnh xong chỉ đổi vài dòng trong cơ sở dữ liệu,
không có giấy tờ nào đem ký, nên hồ sơ quyết toán không giải thích được vì sao thời gian/nội dung
khác với hợp đồng gốc.

Nội dung bản Word: căn cứ QĐ543 + hợp đồng gốc + **Điều 6.1** (thông báo trước 15 ngày) · Bên A /
Bên B · Điều 1 (đề tài & hợp đồng được điều chỉnh) · Điều 2 (**bảng trước → sau**) · Điều 3 (lý do)
· Điều 4 (hiệu lực, có thêm khoản Hiệu trưởng phê duyệt khi `requiresRectorApproval`) · ô ký hai bên.

Riêng loại **gia hạn**: DB lưu `newValue` là **số tháng**, nên bản Word quy ra **mốc thời gian thật**
(`01/03/2026 – 01/03/2027` → `01/03/2026 – 01/06/2027`) kèm dòng *"Số tháng gia hạn: 3 tháng"* —
in trần `0 → 3` vào văn bản đem ký là vô nghĩa.

### Giải ngân đợt CUỐI & quyết toán — khoá mới 08/08 (C6)

`POST /api/disbursements/{id}/confirm` → **409** khi đợt đang xác nhận là **đợt cuối** của hợp đồng
**và** đề tài chưa được công nhận Đạt (`Project.Status != COMPLETED`). Căn cứ QĐ543 **BM05 Điều
4.2** — *"Đợt cuối: giải ngân kinh phí còn lại **sau khi đề tài được công nhận kết quả Đạt**"*.
Không chặn thì chi hết tiền xong mới họp nghiệm thu, mất đòn bẩy cuối cùng của mốc giải ngân.

- Chỉ áp khi hợp đồng có **≥ 2 đợt**. Hợp đồng 1 đợt thì đợt đó vừa đầu vừa cuối — chặn là cấm luôn
  khoản tạm ứng sau khi ký, đề tài không có tiền bắt đầu.
- Các đợt **trước** đợt cuối không bị ảnh hưởng; luật cũ (đợt gắn sản phẩm minh chứng thì sản phẩm
  phải nghiệm thu Đạt) vẫn giữ nguyên.

`POST /api/contracts/{contractId}/settlement` → **409** khi còn đợt giải ngân chưa đánh dấu đã chi,
kèm danh sách số đợt còn treo. Quyết toán là bước **đóng** hợp đồng nên phải đi sau mọi mốc giải ngân.

### Hồ sơ nghiệm thu — mở rộng 08/08 (C4 + C5)

`GET /api/councils/{councilId}/proposals/{proposalId}/dossier` → `AcceptanceDossierDto`.
Quyền: **Admin/Staff hoặc thành viên của chính hội đồng đó** (403 nếu người ngoài) — không mở các
endpoint hợp đồng cho reviewer vì như thế họ thấy hợp đồng của mọi đề tài.

Trước đây chỉ trả báo cáo tiến độ (% + đánh giá), sản phẩm, báo cáo tổng kết — kèm cờ `hasFile`
**không có đường nào mở file ra xem**. Nay bổ sung:

- **`project`** — thông tin đề tài đầy đủ để đối chiếu với cái đã đăng ký: mã · tên VI/EN · PI (tên,
  email) · đơn vị chủ trì · loại · lĩnh vực · đợt · thời gian · tổng kinh phí · **mục tiêu / phương
  pháp / sản phẩm dự kiến** · `members[]` · `proposalFiles[]`.
- **`contractStartDate` / `contractEndDate` / `contractSignedAt` / `contractTotalAmount` /
  `contractFiles[]`** (bản hợp đồng đã ký làm minh chứng).
- **`progressReports[]`** thêm `completedContent` · `pendingContent` · `nextPeriodPlan` ·
  `piRecommendations` · **`evaluatedByName`** · **`evaluatedByRole`** · `evaluatedAt` ·
  `reportFileUrl` · **`files[]` của CHÍNH kỳ đó** (xem lại được tất cả các kỳ trước, không chỉ kỳ cuối).
- **`deliverables[]`** thêm `scientificRequirements` · `fileUrl` (link PI dán) ·
  `trialEvidenceUrl` (minh chứng thử nghiệm, Điều 13.1) · `files[]`.
- **`finalReport`** thêm `reportFileUrl` · `summaryFileUrl` · `files[]`.

Mỗi phần tử `files[]` = `{ id, fileName, category, sizeBytes, uploadedAt, downloadUrl }`;
`downloadUrl` lấy từ `Document.StorageUrl`, các endpoint tải về chỉ yêu cầu đăng nhập nên thành viên
hội đồng bấm là xem được.

⚠️ **Không có cột điểm cho báo cáo tiến độ.** Note của nhóm ghi *"ai chấm · role gì · **bao nhiêu
điểm**"*, nhưng theo rule #16 (chốt tuần 10) báo cáo tiến độ giữa kỳ **Staff duyệt trực tiếp, không
lập hội đồng** nên không có thang điểm — chỉ có `evaluationResult` + `overallCompletionPct`. DTO trả
kèm `progressReportNote` nói rõ điều này để người chấm không đi tìm cột điểm không tồn tại.

### Hình thức họp — chỉ còn 2 giá trị (mới 06/08)
`platform` nay chỉ nhận/trả **`IN_PERSON`** hoặc **`ONLINE`**. Giá trị cũ `GOOGLE_MEET`/`TEAMS`/`ZOOM` **vẫn nhận được** ở request và được map về `ONLINE`; khi đọc cũng quy về `ONLINE` — **không cần migration**. Offline vẫn bắt buộc `location`, online giữ `meetingLink`.

### Lịch chấm theo đề tài — phải nằm trong buổi họp (mới 06/08)
⚠️ **Đổi shape 06/08:** `GET /api/councils/{id}/slots` nay trả `CouncilSlotBoardDto`
(`{ meetingId, meetingStartAt, meetingDurationMinutes, assignedMinutes, slots[] }`) thay vì mảng
slot trần — để màn lịch chấm hiện "đã xếp 60/90 phút" thay vì bắt Staff tự cộng rồi tới lúc lưu
mới ăn 400 vì tràn giờ.
`PUT /api/councils/{id}/slots` nay **400** khi:
- Hội đồng **chưa có buổi họp** nào (phải đặt lịch họp trước).
- Khung giờ chấm nằm **ngoài** `[meeting.scheduledAt, scheduledAt + durationMinutes]` — kể cả khi bắt đầu đúng giờ nhưng **tràn ra ngoài** vì thời lượng quá dài.
- Hai đề tài có khung giờ **chồng nhau** — hội đồng chỉ chấm được một đề tài tại một thời điểm.
- `slotDurationMinutes <= 0` khi đã đặt `slotStartAt`.

Trước đây lưu nguyên xi mọi giá trị: slot 7h sáng cho buổi họp 14h chiều, hay slot 3 tiếng trong buổi họp 2 tiếng, đều lọt.

### Biên bản hội đồng — điều kiện họp hợp lệ (mới 06/08)
`PUT /api/review-scoring/councils/{id}/minutes` (Thư ký lưu nháp) và
`POST /api/review-scoring/councils/{id}/minutes/approve` (Chủ tịch chốt) nay **409** khi:
- Số phiếu đã nộp **< 2/3 số thành viên** hội đồng (làm tròn LÊN — hội đồng 5 người cần 4 phiếu). Nguồn: **QĐ543 Điều 8.3.b** / **Điều 12.3.b** — *"tham dự của ít nhất 2/3 số thành viên"*, và *"các thành viên tham dự họp cần đánh giá thẩm định"* nên số phiếu = số người dự.
- Hội đồng **NGHIỆM THU** mà **chưa có phiếu nào của thành viên phản biện** (Điều 12.3.b: *"và sự tham dự của thành viên phản biện"*).

Chốt biên bản còn **tự đóng mọi buổi họp** của hội đồng (`status → COMPLETED`, set `actualEndAt`) — trước đây buổi họp kẹt ở `SCHEDULED` vĩnh viễn nên lịch vẫn hiện như sắp họp dù đề tài đã có kết quả.

### Bộ tiêu chí — ràng buộc tổng điểm (mới 06/08)
`GET /api/rubric-templates` nay trả thêm **`totalCriteriaScore`** (tổng điểm các tiêu chí đang bật)
và **`isTotalValid`** (`totalCriteriaScore === maxTotalScore`).

- `POST`/`PUT` tiêu chí → **400** nếu tổng vượt `maxTotalScore` (QĐ543 **BM03**: 10+20+40+20+10 = *"Cộng 100"*). Sửa một tiêu chí thì **trừ điểm cũ của chính nó** ra trước khi cộng điểm mới.
- **Không** chặn khi tổng còn thiếu — bộ phải xây dần từng mục mới đủ.
- `POST /api/review-scoring/councils/{id}/scores` → **409** nếu bộ tiêu chí chưa cộng đúng tổng. Đây mới là cổng chặn thật: bộ 60 hay 125 điểm thì điểm trung bình và mọi tỷ lệ % suy ra sau đó đều vô nghĩa.

### Tóm tắt AI đề xuất (Gemini) — ✅ ĐÃ CÓ
| Method | Path | Mô tả |
|---|---|---|
| GET | `/api/proposals/{id}/summary` | Lấy tóm tắt AI hiện có (null nếu chưa tạo) |
| POST | `/api/proposals/{id}/generate-summary` | Sinh tóm tắt mới bằng Gemini. ⚠️ **Nguồn = CÁC TRƯỜNG PI ĐÃ NHẬP** (tên đề tài, loại NC, thời gian, mục tiêu, phương pháp, sản phẩm dự kiến, thành viên, tổng kinh phí) — **KHÔNG đọc file đính kèm** (`AiSummaryService.BuildPrompt`, DTO trả `source: "textFields"`). Đề cương nộp theo Đường A (nhập tay) thì tóm tắt đủ; nộp theo Đường B mà form sơ sài, nội dung chỉ nằm trong file Word/PDF thì tóm tắt sẽ nghèo nàn. |
| PATCH | `/api/proposals/{id}/summary` | Sửa lại nội dung tóm tắt (`{ editedText }`) |

> Cần cấu hình **`GeminiAI:ApiKey`** (đặt trong `appsettings.Development.json` đã gitignore, hoặc biến môi trường `GeminiAI__ApiKey`). Kết quả lưu ở bảng `llm_outputs`. Chưa cấu hình key → trả lỗi rõ ("Hết hạn mức/Key bị từ chối…").

### AI trợ lý (P8, 04/08) — ✅ ĐÃ CÓ
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/ai/proposals/{id}/feedback` | chủ đề cương / Staff / Admin | Góp ý **đã sinh trước đó** (null nếu chưa có) — đọc cache `llm_outputs`, **không tốn quota** |
| POST | `/api/ai/proposals/{id}/feedback` | như trên | Sinh góp ý mới: mảng `{ category, suggestion }`, nhóm ∈ Mục tiêu / Phương pháp / Sản phẩm dự kiến / Tính khả thi / Kinh phí / Trình bày |
| POST | `/api/ai/proposals/{id}/consistency-check` | chủ đề cương / Staff / Admin | **Đối chiếu form ↔ FILE đề cương** (thầy 29/07). AI đọc **file đính kèm mới nhất** rồi so với thông tin đã điền. Trả `{ hasFile, fileName, issues[] }`; `issues[i].kind` ∈ `MISSING` (form thiếu) / `MISMATCH` (lệch) / `EXTRA` (form có, file không nhắc). **Chưa đính kèm file → `hasFile=false`, KHÔNG phải lỗi** |
| POST | `/api/ai/councils/{councilId}/proposals/{proposalId}/score-suggestion` | **thành viên hội đồng** hoặc Admin/Staff (khác → **403**) | **AI gợi ý điểm từng tiêu chí**: `{ criterionId, criterionName, maxScore, suggestedScore, comment }`. Luôn trả **đủ** tiêu chí của bộ đang áp; AI thiếu thì `suggestedScore=0` + ghi chú. Điểm bị kẹp `[0, maxScore]`. **404** nếu hội đồng chưa có bộ tiêu chí nào |

> Bộ tiêu chí dùng để gợi ý chọn bằng **cùng** thứ tự ưu tiên với màn chấm (`IRubricResolver`: vòng → đợt+lĩnh vực → mặc định) — không có 2 bản logic.
> **AI chỉ GỢI Ý** — người chấm tự nhập điểm cuối (rule #12). FE hiện gợi ý dưới từng tiêu chí kèm nút "Áp dụng", không tự ghi đè.

### ⚠️ FE đang gọi nhưng BE CHƯA implement (trả 404) — cần làm BE hoặc ẩn ở FE
| FE gọi | Mục đích | Trạng thái BE |
|---|---|---|
| `POST /api/ai/search` | Tìm kiếm ngữ nghĩa (trang PI) | ❌ Chưa làm — cần embedding + `semantic_search_vector` |
| `POST /api/ai/suggest-reviewers` | Gợi ý thành viên hội đồng | ❌ Chưa làm — **nên làm bằng truy vấn thuần**, không cần AI |
| `POST /api/ai/similarity-check` | Trùng lặp với đặt hàng | ❌ Chưa làm — **không màn nào dùng**, nên xoá cả 2 đầu |

> ✅ **Cập nhật 19/08:** `POST /api/proposals/extract` nhận multipart field `file` (**PDF hoặc DOCX**, theo giới hạn dung lượng/đuôi file Admin cấu hình). Gemini trích xuất để prefill tên VI/EN, tóm tắt, mục tiêu, phương pháp, sản phẩm dự kiến, tính cấp thiết, tính mới, khả năng ứng dụng/chuyển giao, cơ sở vật chất, thời gian, tổng kinh phí, `budgetItems[]` theo đúng 06 mã hạng mục và `teamMembers[]`. FE chỉ điền **ô trống**, không ghi đè nội dung PI đã nhập. Nếu file chỉ có tổng kinh phí mà không có phân bổ, tổng chỉ hiện tham chiếu để PI tự phân bổ; hệ thống không tự bịa hạng mục. Thành viên thiếu email vẫn được điền tên và thông tin đọc được nhưng PI phải bổ sung email hợp lệ trước khi nộp. Field không thấy → `null`/mảng rỗng; AI lỗi/chưa cấu hình → trả `warning` thân thiện để FE tiếp tục đường nhập tay, không lộ lỗi raw của provider.

> ✅ **Đã bổ sung** (trước đây 404): tài liệu đính kèm `/api/proposals/{id}/documents` + `/api/documents` (§6), 4 endpoint change-requests (§6), `toggle-active` + `reset-password` (§4).

> Ngoài ra: muốn **chấm điểm** được, Admin phải thêm **tiêu chí chấm** (rubric criteria) cho loại vòng
> ở `/api/rubric-criteria`; nếu chưa có, màn chấm điểm hiện thông báo "Chưa cấu hình tiêu chí chấm".

---

## 13. Chi tiết field & quy tắc nghiệp vụ (gộp từ contract v1.1 Delta)

> Phần bổ sung field-level cho các nhóm quan trọng. Nguồn chính xác vẫn là Swagger.

### 13.1 Đề xuất — field mở rộng
- `fundingMethod`: `PARTIAL` (khoán từng phần) | `WHOLE` (khoán toàn phần). Quyết định cách sinh đợt giải ngân.
- Thành viên nhóm: ngoài `fullName/email/department/role/workMonths` còn có `memberRoleCode` (`CNNV | TKKH | TVC | TV | KTV`) và `salaryCoefficient` (hệ số tiền công, nhập tay). `isPi/isSecretary` giữ để tương thích ngược.

### 13.2 Kinh phí — line items + nguồn vốn
`GET/PUT /api/proposals/{id}/budget` dùng dạng line items:
```json
{
  "totalAmount": 900000000,
  "items": [
    { "id": 1, "categoryId": 1, "categoryCode": "LABOR", "categoryName": "Công lao động trực tiếp",
      "amount": 849747000, "sourceKhoan": 849747000, "sourceNgoaiKhoan": 0,
      "sourceNsnn": 849747000, "sourceOther": 0, "sequence": 1, "note": "..." }
  ]
}
```
- PUT validate: tổng `items[].amount` = `totalAmount`.
- `categoryId` lấy từ `/api/budget-expense-categories` (12 khoản seed sẵn theo Mẫu 3).

### 13.3 Tiền công (labor details) — `/api/proposals/{id}/budget/labor`
```json
{ "teamMemberId": 3, "workDays": 215, "coefficient": 0.49,
  "dailyRate": 730100, "totalAmount": 156971500 }
```
- `dailyRate` = `coefficient` × `BASE_DAILY_SALARY` (lấy từ `/api/financial-configs`, mặc định 1.490.000 đ).
- `totalAmount` = `workDays` × `dailyRate`.

### 13.4 Vòng phản biện — field & quy tắc
`GET /api/proposals/{id}/rounds` trả mỗi vòng kèm:
`{ id, roundNumber, dimension, roundType, rubricTemplateId, sequence, prerequisiteRoundId, status, openedAt, closedAt, result, councilId }`
- `dimension`: `SCIENCE` (khoa học) | `FINANCE` (kinh phí).
- `roundType`: `SCREENING | REVIEW | ACCEPTANCE`.
- `status`: `PENDING | OPEN | PASSED | FAILED`; `result`: `APPROVED | REJECTED | REVISION_REQUIRED`.
- **Quy tắc:** `POST /api/rounds/{id}/open` trả **409** nếu `prerequisiteRound.status != PASSED` (chặn mở vòng kinh phí khi vòng khoa học chưa đạt).
- **Phase B:** 1 vòng thuộc **cycle_track**, chứa **nhiều đề tài** (`project_rounds`) và có thể có **nhiều hội đồng song song** (mỗi hội đồng chấm 1 nhóm đề tài). Kết quả từng đề tài nằm ở `project_rounds`, không phải ở round chung. Luồng thao tác chuẩn ở màn **Review Board cấp Track** (§8.1); route theo `proposalId` (§8.2) giữ cho tương thích.

### 13.5 Xuất hồ sơ — `/api/proposals/{id}/export`
- `/scientific` → file **Word .docx** (Mẫu 1 — Thuyết minh khoa học).
- `/budget` → file **Excel .xlsx** (Mẫu 3 — Dự toán kinh phí).
- Response là file: `Content-Disposition: attachment; filename="...".` (FE tải bằng Blob).

### 13.6 Sau duyệt — quy tắc giải ngân & sản phẩm
- `POST /api/contracts/{id}/disbursements/generate` sinh đợt theo `fundingMethod`:
  - `PARTIAL` → mỗi mốc nghiệm thu sản phẩm 1 đợt.
  - `WHOLE` → 2–3 đợt (đầu/giữa/cuối).
- **P5 (04/08):** đợt giải ngân **nào cũng** gắn được sản phẩm minh chứng (không chỉ PARTIAL) qua `PUT /api/disbursements/{id}/deliverable`. `GET .../disbursements` nay trả kèm `deliverableName`, `deliverableAcceptanceStatus`, `deliverableSubmittedAt`, `isBlockedByDeliverable`.
- **Gate:** `POST /api/disbursements/{id}/confirm` trả **409** nếu đợt **có gắn** sản phẩm mà sản phẩm chưa nghiệm thu `PASSED`. Đợt **không gắn** sản phẩm (vd tạm ứng khởi động HĐ) vẫn xác nhận bình thường.
- `POST /api/deliverables/{id}/evaluate`:
  - `PASSED` → mở khoá **mọi** đợt giải ngân trỏ tới sản phẩm này (theo LIÊN KẾT, không còn theo `fundingMethod`); `IsCompleted=true`.
  - `FAILED` → hợp đồng chuyển trạng thái xem xét.
- Nhắc hạn (job nội bộ, không phải endpoint): quét `deliverables.dueDate` gửi email mốc **T-30 / T-14 / T-7 / quá hạn**. Test bằng dev-tools tua thời gian (§10).

### 13.7 Bảng status (tổng hợp)
| Thực thể | Field | Giá trị |
|---|---|---|
| cycle | status | `PLANNING` → `OPEN` → `CLOSED` |
| proposal (tài liệu) | status | `DRAFT, SUBMITTED, APPROVED, REJECTED, REVISION_REQUIRED` |
| project (đề tài) | status | `PROPOSED, UNDER_REVIEW, APPROVED, IN_PROGRESS, ACCEPTANCE, COMPLETED, CANCELLED, TERMINATED` |
| proposal | fundingMethod | `PARTIAL, WHOLE` |
| review_round | status / dimension | `PENDING, OPEN, PASSED, FAILED` / `SCIENCE, FINANCE` |
| project_round (kết quả từng đề tài) | status / result | `PENDING, OPEN, PASSED, FAILED` / `APPROVED, REJECTED, REVISION_REQUIRED` |
| council decision / round result | result | `APPROVED, REJECTED, REVISION_REQUIRED` |
| council | status | `FORMING, DECIDED` |
| contract | status | `PENDING_SIGNATURE, ACTIVE, UNDER_REVIEW` |
| disbursement | status | `PENDING, DISBURSED` |
| deliverable / acceptance | acceptanceStatus | `PENDING, PASSED, FAILED` (acceptance submit dùng `PASS/FAIL`) |
| council member | status | `ASSIGNED, INVITED, CONFIRMED, DECLINED, EXPIRED` |
