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
| Stack | ASP.NET Core 8, EF Core, SQL Server |

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

**Login request**
```json
{ "email": "admin@furpms.edu.vn", "password": "Admin@123456" }
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
| POST | `/api/users/{id}/reset-password` | Admin | Reset mật khẩu về mặc định `Furpms@123456` |

**CreateUserRequest**
```json
{ "email": "...", "fullName": "...", "phoneNumber": "...", "department": "...",
  "academicDegree": 1, "roles": [3], "temporaryPassword": "..." }
```
`roles` = mảng **Role.Id** (Admin=1, Staff=2, Faculty=3, ReviewCommittee=4).

### Academic Profile — `/api/users/{userId}/profile`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/users/{userId}/profile` | * (chủ hồ sơ; Admin/Staff xem mọi người) | Hồ sơ khoa học |
| PUT | `/api/users/{userId}/profile` | như trên | Tạo/cập nhật hồ sơ |

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
| POST | `/api/cycles` | Admin, Staff | Tạo đợt |
| PUT | `/api/cycles/{id}` | Admin, Staff | Sửa đợt |
| POST | `/api/cycles/{id}/open` | Admin, Staff | Mở đợt (→ `OPEN`) |
| POST | `/api/cycles/{id}/close` | Admin, Staff | Đóng đợt (→ `CLOSED`) |
| POST | `/api/cycles/{id}/extend-deadline` | Admin, Staff | **Gia hạn deadline đợt** (rule tuần 10) `{ newDeadline: "yyyy-MM-dd", reason? }` — ghi log, **KHÔNG ghi đè** `SubmissionDeadline` gốc; phải sau deadline hiện tại (else 400) |
| GET | `/api/cycles/{id}/deadline-extensions` | * | Lịch sử gia hạn (mới nhất trước) — deadline hiệu lực = `newDeadline` bản đầu list |

> Response cycle (`GET /api/cycles`, `GET /api/cycles/{id}`) nay trả **`submissionDeadline` = hạn HIỆU LỰC** (sau gia hạn), kèm **`originalDeadline`** (hạn gốc, chỉ khi đã gia hạn) + **`extensionCount`**. Trước đây luôn trả hạn gốc → FE hiện hạn cũ sau khi gia hạn.
| GET | `/api/cycles/tracks` | * | **Toàn bộ** lĩnh vực (dropdown khi PI nộp đề tài) |
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

### Các lookup khác (đều CRUD theo cùng mẫu: GET list / GET {id} / POST / PUT; ghi/sửa = Admin)
| Resource | Base path | Đọc | Ghi |
|---|---|---|---|
| Personnel role types | `/api/personnel-role-types` | * | Admin |
| Budget expense categories | `/api/budget-expense-categories` | * | Admin |
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
- `budgetItems[].category` = **tên hạng mục** lấy từ `GET /api/budget-expense-categories` (khớp theo tên; không khớp → "Chi khác").
- Backend lưu đầy đủ members (kèm email/đơn vị) + budget items (kèm ghi chú) và **tự tính lại tổng kinh phí**.

### Sub-resources của đề xuất
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET/PUT | `/api/proposals/{id}/budget` | * | Bảng kinh phí tổng hợp (`BudgetResponse`) |
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
    { "projectId": "guid", "proposalId": "guid", "titleVi": "…", "projectStatus": "UNDER_REVIEW" }
  ],
  "rounds": [
    {
      "id": "guid", "roundNumber": 1, "dimension": "SCIENCE", "roundType": "REVIEW",
      "status": "OPEN", "result": null,
      "canDelete": false,          // server tính: chỉ true khi CHƯA có hội đồng & mọi đề tài PENDING
      "projects": [                // trạng thái TỪNG đề tài trong vòng (từ project_rounds)
        { "projectId": "guid", "titleVi": "…", "status": "PENDING", "result": null }
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
| GET | `/api/councils/my-memberships` | Authenticated | Hội đồng mà tôi tham gia. `MyMembershipDto` **enrich (tuần 10)**: thêm `piName`, `trackName`, `createdAt`, `nextMeetingAt` (FE tìm kiếm/sắp xếp + hiện PI/lĩnh vực/ngày họp) |
| POST | `/api/councils` | Staff, Admin | Lập 1 hội đồng cho 1 đề tài trong round |
| GET | `/api/councils/{councilId}/members` | Authenticated | Thành viên hội đồng |
| DELETE | `/api/councils/{councilId}` | Staff, Admin | **Xóa hội đồng** — chỉ khi **chưa có phiếu chấm / biên bản / nghiệm thu** (có → 409). Tự gỡ thành viên + lịch họp + điểm danh + gán đề tài. |
| POST | `/api/councils/{councilId}/members` | Staff, Admin | Thêm thành viên `{ userId, memberRole, isExternal }` (COI rule #5) |
| POST | `/api/councils/{councilId}/send-invitations` | Staff, Admin | **Gửi thư mời đồng loạt** cho member `ASSIGNED` (rule #13) `{ confirmDeadline? }`. **Gate (rule tuần 10):** phải đủ **Chủ tịch + Thư ký + đã có lịch họp** — thiếu → **409** |
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
| POST | `/api/councils/{councilId}/meetings` | Admin, Staff | Tạo lịch họp |
| PUT | `/api/meetings/{id}` | Admin, Staff | **Sửa lịch họp (mới 05/08)** — `UpdateMeetingRequest` (cùng bộ trường với lúc tạo). Rule #17 cho đổi lịch **bất kỳ lúc nào** nên không khoá theo trạng thái; buổi đã diễn ra thì bỏ ràng buộc "phải ở tương lai" (vẫn sửa được địa điểm/link ghi nhầm). Offline mà trống địa điểm → 400; `durationMinutes <= 0` → 400. Đổi sang online thì BE **tự xoá** `location`, và ngược lại. |
| DELETE | `/api/meetings/{id}` | Admin, Staff | **Xoá buổi họp (mới 05/08)** — chỉ khi `status = SCHEDULED`, ngược lại **409**. Đã có điểm danh (`ActuallyAttended != null`) cũng **409**. Xoá thì dọn dòng điểm danh và **gỡ slot đề tài** đang trỏ tới buổi họp (`CouncilProjectAssignment.MeetingId/SlotStartAt` về null). |
| POST | `/api/meetings/{id}/start` | Admin, Staff | Bắt đầu họp |
| POST | `/api/meetings/{id}/end` | Admin, Staff | Kết thúc họp |

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
| GET | `/api/councils/{councilId}/acceptance/my` | Thành viên hội đồng | **Phiếu của CHÍNH tôi** (null nếu chưa chấm) — form chấm nghiệm thu dùng endpoint này |
| POST | `/api/councils/{councilId}/acceptance` | Thành viên | Nộp **hoặc CẬP NHẬT** đánh giá của mình (`{ result: "PASS"|"FAIL", failReason? }`). Biên bản đã Chủ tịch chốt → **409** (rule #12). *Trước đây nộp lần 2 luôn 409 "already submitted" dù UI ghi "Cập nhật".* |

> **Nghiệm thu chốt vòng đời đề tài:** Chủ tịch duyệt biên bản vòng **ACCEPTANCE** → project `COMPLETED` (Đạt) hoặc quay lại `IN_PROGRESS` (chưa đạt) — khác vòng REVIEW (`APPROVED`/`CANCELLED`). Trước đây mọi vòng đều set APPROVED nên nghiệm thu xong đề tài vẫn "Đã duyệt" → **đứt mạch cuối**.

> **An ninh (§8.1):** `GET .../review-board` (trả danh tính ủy viên toàn lĩnh vực) đã siết `[Authorize(Roles="Admin,Staff")]` — reviewer xem phần của mình qua `/api/councils/my-memberships` (§8.3), không qua board.

---

## 9. Hợp đồng & sau hợp đồng

### Contracts — `/api/contracts`
| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/contracts` | * | Danh sách hợp đồng. Staff/Admin xem hết; PI xem của mình. **`?mine=true`** → LUÔN chỉ HĐ mình là PI (kể cả tài khoản đa vai Staff/Admin đang "làm PI") — dùng cho trang PI (báo cáo tiến độ/sản phẩm/tổng kết). |
| GET | `/api/contracts/{id}` | * | Chi tiết |
| POST | `/api/contracts` | Admin, Staff | Tạo hợp đồng. `maxExtensionMonths` bị chặn theo **QĐ543 Điều 10.4** (≤ 1/2 `DurationMonths` của đề cương) — FE tự điền sẵn đúng trần khi Staff chọn đề tài. |
| PUT | `/api/contracts/{id}` | Admin, Staff | **Sửa hợp đồng (mới 05/08)** — `UpdateContractRequest` { contractNumber, scopeTitle?, startDate, endDate, maxExtensionMonths, sideARepresentative?, econtractUrl? }. KHÔNG đổi được `proposalId` và `totalAmount`. Trùng số HĐ → 409; `endDate <= startDate` hoặc `maxExtensionMonths` vượt **1/2 thời gian thực hiện của đề cương** → 400 (QĐ543 Điều 10.4). Hợp đồng **chưa từng gia hạn** thì sửa `endDate` dời luôn `originalEndDate` (là sửa cho đúng, không phải gia hạn); đã gia hạn rồi thì giữ nguyên hạn gốc. |
| DELETE | `/api/contracts/{id}` | Admin, Staff | **Xoá hợp đồng (mới 05/08)** — chỉ khi `status = PENDING_SIGNATURE`, ngược lại **409** (ký rồi thì dùng chấm dứt). Còn chặn 409 nếu đã có: sản phẩm được nộp · báo cáo tiến độ được nộp · báo cáo tổng kết · quyết toán · đơn điều chỉnh. Xoá thành công thì **gỡ** sản phẩm khỏi hợp đồng (giữ lại cho đề tài) và **dọn** lịch giải ngân + kỳ báo cáo tự sinh. |
| POST | `/api/contracts/{id}/sign` | Admin, Staff | Ký |
| GET | `/api/contracts/{id}/export-word` | Admin, Staff | **BM05 — tự sinh Word hợp đồng** (rule tuần 10): bốc CN/đề tài/kinh phí/thời gian → `.docx` để ký ngoài |
| GET/POST | `/api/contracts/{id}/documents` | Admin, Staff | **Hồ sơ hợp đồng đã ký**: list / upload (multipart `file`) bản ký — Document polymorphic EntityType="Contract" |
| GET | `/api/contracts/{id}/documents/{documentId}/download` | Admin, Staff | Tải/mở bản hợp đồng đã ký (Bearer) |
| GET | `/api/contracts/{contractId}/disbursements` | * | Đợt giải ngân |
| POST | `/api/contracts/{contractId}/disbursements/generate` | Admin, Staff | Sinh lịch giải ngân |
| GET | `/api/contracts/{contractId}/deliverables` | * | Sản phẩm phải nộp |
| POST | `/api/contracts/{contractId}/deliverables` | Admin, Staff | **Staff thêm 1 sản phẩm** cho hợp đồng `{ productName, categoryId?, dueDate?, description? }` (đề cương không có trường sản phẩm cấu trúc → nhập tay; PI sau đó nộp file). |
| GET | `/api/contracts/{contractId}/amendments` | * | Điều chỉnh hợp đồng |
| POST | `/api/contracts/{contractId}/amendments` | * | Tạo yêu cầu điều chỉnh. **409 khi đề tài đã đóng** (`COMPLETED`/`CANCELLED`/`TERMINATED`) hoặc hợp đồng đã chấm dứt (mới 06/08). |

**CreateContractRequest**: `{ proposalId, contractNumber, scopeTitle?, startDate, endDate, maxExtensionMonths=6, sideARepresentative?, econtractUrl? }` — nhận `proposalId` nhưng hợp đồng neo **project**; ký HĐ → project sang `IN_PROGRESS`. **1 đề tài có thể ký nhiều hợp đồng** (từng giai đoạn/phần sản phẩm). `ContractDto` trả thêm `projectId`, `scopeTitle`.
**CreateAmendmentRequest**: `{ categoryId, changeDescription, justification, changePercentage?, oldValue?, newValue?, requiresRectorApproval, reviewerComments? }`

### Giải ngân — `/api/disbursements`
| POST | `/api/disbursements/{id}/confirm` | Admin, Staff | **Đánh dấu đã giải ngân** (rule tuần 10 — không quản tiền): `{ actualAmount?, bankReference?, notes? }` — **tất cả optional**, chỉ đổi status→DISBURSED + `disbursedAt` |
| PUT | `/api/disbursements/{id}/deliverable` | Admin, Staff | **Gắn/gỡ sản phẩm minh chứng cho đợt** (P5): `{ deliverableId: int \| null }` — `null` = gỡ. **400** nếu sản phẩm không thuộc cùng hợp đồng; **409** nếu đợt đã giải ngân. Gắn sản phẩm đã `PASSED` ⇒ set luôn `conditionMetAt` |
| GET | `/api/disbursements/{id}/evidence` | Admin, Staff | **Minh chứng giải ngân** (rule tuần 10) — list file HĐ/chứng từ của đợt |
| POST | `/api/disbursements/{id}/evidence` | Admin, Staff | Upload minh chứng (multipart `file`) — tái dùng `Document` polymorphic (EntityType="Disbursement"); siết dung lượng/đuôi theo `system_settings` |
| GET | `/api/disbursements/{id}/evidence/{documentId}/download` | Admin, Staff | Tải/mở file minh chứng (cần Bearer) |

### Sản phẩm — `/api/deliverables`
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
| PUT | `/api/progress-reports/{id}` | * (PI) | **Sửa nội dung khi còn DRAFT** (nộp rồi → 409) — `UpdateProgressReportRequest`. **Tuần 12 thêm `reportFileUrl`**: PI dán LINK báo cáo thay cho upload khi file quá lớn (bỏ trống → giữ giá trị cũ). Trả kèm trong summary + detail để Staff/hội đồng mở xem. |
| POST | `/api/progress-reports/{id}/submit` | * (PI) | Nộp |
| PATCH | `/api/progress-reports/{id}/schedule` | Admin, Staff | Đặt lịch báo cáo `{ dueDate?, scheduledMeetingAt?, meetingLink?, roundName? }`. **`dueDate` đặt lại = GIA HẠN** hạn nộp (thầy 29/07: đánh giá trúng ngày cuối thì gia hạn được). **`roundName`** = tên đợt Staff đặt (vd "Giữa kỳ"); null → FE hiện "Kỳ {số}". |
| POST | `/api/progress-reports/{id}/evaluate` | Admin, Staff | Đánh giá — `evaluationResult` ∈ **`PASS` / `FAIL` / `CONDITIONAL`** (QĐ543 Điều 10/BM06: Đạt / Không đạt / Có điều kiện). ⚠️ **Đổi tuần 12:** trước BE nhận `SATISFACTORY/…` còn FE gửi `APPROVED/…` → Staff bấm đánh giá **luôn 400**, không chấm được. |
| GET | `/api/progress-reports/{id}/documents` | * | **File báo cáo (BM06)** PI đã nộp |
| POST | `/api/progress-reports/{id}/documents` | PI của đề tài (hoặc Admin/Staff) | **PI upload file PDF/Word** báo cáo (multipart `file`). Người khác → 403 |
| GET | `/api/progress-reports/{id}/documents/{documentId}/download` | * | Tải/mở file báo cáo — **Staff phải xem file rồi mới đánh giá được** (FE khóa nút khi chưa có file) |

### Bộ tiêu chí chấm — `/api/rubric-templates`
> **"Bộ tiêu chí"** = `RubricTemplate` + các `RubricCriterion` bên trong (thầy 29/07: tiêu chí chia theo **loại đề tài** + **lĩnh vực**, phải linh hoạt). 1 bộ **dùng lại cho nhiều đợt**.
> **Thứ tự ưu tiên khi chấm:** ① bộ gắn RIÊNG cho vòng → ② bộ theo (đợt + lĩnh vực + loại vòng) → ③ bộ mặc định chung.
> **Ràng buộc:** mỗi **(đợt + lĩnh vực + LOẠI VÒNG)** chỉ 1 bộ — nhưng cùng lĩnh vực vẫn có bộ riêng cho **Xét duyệt** và bộ riêng cho **Nghiệm thu** (tiêu chí khác hẳn).

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
| POST | `/api/final-reports/{contractId}/submit` | * (PI) | Nộp báo cáo cuối (project → `ACCEPTANCE`) |
| GET | `/api/final-reports/{contractId}/documents` | * | **File báo cáo tổng kết (BM09)** đã upload |
| POST | `/api/final-reports/{contractId}/documents` | * | **Upload file PDF/Word** (multipart `file`) → trả `downloadUrl` để nộp kèm. ⚠️ **Tuần 12:** thay ô dán URL bằng upload file thật (thầy 29/07) |
| GET | `/api/final-reports/{contractId}/documents/{documentId}/download` | * | Tải/mở file |
| POST | `/api/final-reports/{id}/request-revision` | Admin, Staff | Yêu cầu sửa |
| POST | `/api/final-reports/{id}/accept` | Admin, Staff | Chấp nhận |
| POST | `/api/final-reports/{id}/archive` | Admin, Staff | Lưu trữ |

### Quyết toán — `/api/contracts/{contractId}/settlement`, `/api/settlements/...`
| GET | `/api/contracts/{contractId}/settlement` | * | Xem quyết toán |
| POST | `/api/contracts/{contractId}/settlement` | Admin, Staff | Tạo |
| POST | `/api/settlements/{id}/sign` | Admin, Staff | Ký |
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

### Hình thức họp — chỉ còn 2 giá trị (mới 06/08)
`platform` nay chỉ nhận/trả **`IN_PERSON`** hoặc **`ONLINE`**. Giá trị cũ `GOOGLE_MEET`/`TEAMS`/`ZOOM` **vẫn nhận được** ở request và được map về `ONLINE`; khi đọc cũng quy về `ONLINE` — **không cần migration**. Offline vẫn bắt buộc `location`, online giữ `meetingLink`.

### Lịch chấm theo đề tài — phải nằm trong buổi họp (mới 06/08)
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

> ✅ **Đã sửa 04/08:** `POST /ai/extract` — FE gọi sai đường dẫn suốt (BE là **`POST /api/proposals/extract`**) ⇒ nút "phân tích bằng AI" ở wizard **chưa từng chạy**, tức Đường B (upload+AI, rule #10/#20) coi như chưa có. Kèm theo `AiExtractionResult` ở FE cũng lệch hẳn field so với `ExtractedProposalDto` — nay đã khớp, prefill đủ 7 trường.

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
