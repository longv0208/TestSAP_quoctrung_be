# FURPMS — Hướng dẫn chạy & demo

Tài liệu giúp test nhanh hệ thống mà không phải mò: tài khoản sẵn, luồng đi đầy đủ theo vai trò, và các "mẹo" để đỡ phải nhập tay khi demo.

---

## 1. Khởi động

**Database** (Docker — SQL Server):
```bash
docker compose up -d db        # hoặc dùng container db-1 sẵn có (port 1433)
```

**Backend**:
```bash
dotnet run --project FURPMS.API     # chạy ở http://localhost:5068
```
- Khi khởi động, BE tự **chạy migration** + **seed** (idempotent). Swagger: `http://localhost:5068/swagger`.

**Frontend** (repo Fefurpmsv0): `npm run dev` → `http://localhost:5173` (đã trỏ `VITE_API_URL=http://localhost:5068`).

> Chỉ chạy **một** instance BE tại một thời điểm (tránh giành port 5068).

---

## 2. Tài khoản mẫu (seed sẵn)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@furpms.edu.vn` | `Admin@123456` |
| Staff (Phòng QLKH) | `staff.demo@furpms.edu.vn` | `Staff@123456` |
| Hội đồng 1 | `reviewer1.demo@furpms.edu.vn` | `Reviewer@123456` |
| Hội đồng 2 | `reviewer2.demo@furpms.edu.vn` | `Reviewer@123456` |
| Hội đồng 3 | `reviewer3.demo@furpms.edu.vn` | `Reviewer@123456` |
| Giảng viên (PI 1) | `pi.demo@furpms.edu.vn` | `Faculty@123456` |
| Giảng viên (PI 2) | `pi2.demo@furpms.edu.vn` | `Faculty@123456` |

**Dữ liệu seed sẵn** (thuộc PI 1, đợt SU26 đang **OPEN**):
- `DEMO-2026-001` — đề xuất **đã nộp** (để demo phân công + chấm điểm).
- `DEMO-2026-002` — đề xuất **đã DUYỆT** kèm **hợp đồng `HD-2026-002`** (ACTIVE, 450tr): có sẵn 3 đợt **giải ngân** (đợt 1 đã chi) và 2 **sản phẩm** (1 đã nghiệm thu PASSED) → mở khoá demo các màn sau duyệt.
- **Bộ tiêu chí chấm (rubric)** vòng xét duyệt (5 tiêu chí, 100 điểm) → reviewer chấm điểm được ngay.

---

## 3. Luồng demo đầu-cuối (happy path)

> Vòng đời 1 đề tài đi qua nhiều vai trò. Tất cả các bước **chuyển trạng thái bằng nút bấm**, không phải chờ theo lịch — nên demo được trọn vẹn trong vài phút.

**B1 — PI nộp đề xuất** (đăng nhập `pi.demo`)
1. "Tạo đề xuất mới" → điền 5 bước (mẹo nhập nhanh ở mục 4) → "Lưu nháp".
2. "Đề xuất của tôi" → bản nháp → **Gửi duyệt** (`DRAFT` → `SUBMITTED`).
3. Cần sửa? Bấm **Rút lại** → **Sửa** → "Cập nhật" → Gửi duyệt lại.

**B2 — Staff lập hội đồng & vòng phản biện** (đăng nhập `staff.demo`)
1. Mở đề xuất đã nộp → tạo **Vòng phản biện** (Xét duyệt) → **Phân công** 3 thành viên hội đồng (reviewer1–3).
2. Lập **lịch họp** cho hội đồng nếu cần.

**B3 — Hội đồng chấm điểm** (đăng nhập từng `reviewer*`)
1. Vào hội đồng mình tham gia → **chấm điểm** theo rubric → có thể gửi **phản hồi phản biện**.

**B4 — Staff chốt kết quả** (`staff.demo`)
1. Xem tổng hợp điểm/phiếu → **Chốt** vòng `Đạt`/`Từ chối` (hoặc chốt quyết định hội đồng `APPROVED/REJECTED/REVISION_REQUIRED`).

**B5 — Hợp đồng & giải ngân** (`staff.demo`)
1. Tạo **hợp đồng** cho đề tài đã duyệt → **Ký** → **Sinh lịch giải ngân**.
2. **Xác nhận giải ngân** từng đợt.

**B6 — Thực hiện & nghiệm thu** (PI + Staff)
1. PI **nộp sản phẩm** / **báo cáo tiến độ** → Staff **đánh giá**.
2. Vòng **nghiệm thu** → **báo cáo tổng kết** → **quyết toán**.

**Admin** xem **Thống kê** (overview/track/funnel), quản lý người dùng, master data, và **Công cụ test** (mục 5).

---

## 4. Mẹo nhập nhanh khi test

- **Nút "Điền dữ liệu mẫu"** ở form tạo đề xuất (góc trên): bấm 1 phát là điền sẵn tiêu đề, mục tiêu, phương pháp, 2 thành viên, 2 khoản kinh phí hợp lệ — chỉ việc bấm tiếp đến bước cuối và "Lưu nháp".
- **Bản nháp tự lưu trên trình duyệt**: gõ dở, F5 không mất chữ.
- Hạng mục kinh phí giờ là **dropdown** (không phải gõ tay) → chọn nhanh, lưu/đọc lại khớp.

---

## 5. Công cụ test thời gian (Admin → "Công cụ test")

Phần lớn hệ thống đi theo nút bấm nên **không cần chờ**. Riêng **nhắc hạn / quá hạn sản phẩm** phụ thuộc thời gian thật — dùng công cụ này:

- **Tua đồng hồ hệ thống** `+7/+14/+30/+90/+180 ngày` (hoặc nhập tuỳ ý) để mô phỏng "đến hạn"; **Đặt lại** về thời gian thật.
- **Chạy quét nhắc hạn**: kích hoạt ngay tác vụ gửi nhắc hạn (bình thường chạy mỗi 24h) → kiểm tra email/thông báo của PI.

Tương ứng API: `GET/POST /api/admin/system-clock`, `POST /api/admin/run-deadline-scan` (xem `API_CONTRACT.md` §10).

---

## 6. Cách "chuyên nghiệp" thường làm với dữ liệu mẫu

Tham khảo nếu sau này muốn nâng cấp khâu test/demo:

1. **Seed data (đang dùng)** — `DatabaseSeeder` tạo sẵn tài khoản + 1 đề tài + master data, idempotent. Phù hợp demo cố định.
2. **Demo-scenario seed** — viết thêm hàm seed 1 đề tài ở **mỗi trạng thái** (đã nộp / đang chấm / đã duyệt / có hợp đồng…) để mở UI là thấy ngay mọi màn hình, không phải bấm từ đầu.
3. **Autofill ở FE (đang có nút "Điền dữ liệu mẫu")** — gắn dữ liệu giả ngay trong form, chỉ bật khi DEV. Nhanh nhất khi test thủ công.
4. **Faker/Factory** — sinh dữ liệu giả hàng loạt (vd Bogus cho .NET) cho test khối lượng lớn / phân trang.
5. **Bộ sưu tập request (Postman/Bruno/.http)** — lưu sẵn các request theo luồng, "Run collection" để dựng trạng thái bằng API thay vì click. Kết hợp với Swagger là đủ cho FE tự test.

> Khi bàn giao FE cho team khác: gửi kèm **`API_CONTRACT.md` + Swagger URL + file này**. Đó là bộ đủ để họ bắt tay làm mà không cần hỏi lại nhiều.
