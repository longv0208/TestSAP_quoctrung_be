# FURPMS — Hướng dẫn chạy & demo

Tài liệu giúp test nhanh hệ thống mà không phải mò: tài khoản sẵn, luồng đi đầy đủ theo vai trò, và các "mẹo" để đỡ phải nhập tay khi demo.

---

## 1. Khởi động

**Database** (Docker — PostgreSQL 16):
```bash
docker compose up -d           # container furpms-db-1, PostgreSQL 16, cổng 5433
```

**Backend**:
```bash
dotnet run --project FURPMS.API     # chạy ở http://localhost:5068
```
- Khi khởi động, BE tự **chạy migration** + **seed** (idempotent). Swagger: `http://localhost:5068/swagger`.

**Frontend** (repo `core/FURPMS-Web`): `npm run dev` → `http://localhost:5173` (`.env` trỏ `VITE_API_BASE_URL=http://localhost:5068/api`).

> Chỉ chạy **một** instance BE tại một thời điểm (tránh giành port 5068).

---

## 2. Tài khoản mẫu (seed sẵn)

Mọi tài khoản dùng chung mật khẩu **`password`** (E8 — một chuỗi để khỏi gõ nhầm khi demo).

| Vai trò | Họ tên hiển thị | Email |
|---|---|---|
| Admin | Quản trị hệ thống | `admin@furpms.edu.vn` |
| Staff (Phòng QLKH) | Trần Thị Mai Lan | `staff.demo@furpms.edu.vn` — **có 2 vai** (Cán bộ + Giảng viên) để demo đổi vai ở dropdown avatar, rule #23 |
| Hội đồng — **Chủ tịch** | PGS.TS. Lê Quang Minh | `reviewer1.demo@furpms.edu.vn` |
| Hội đồng — **Thư ký** | TS. Phạm Thu Hương | `reviewer2.demo@furpms.edu.vn` |
| Hội đồng — **Phản biện** | TS. Vũ Đình Nam | `reviewer3.demo@furpms.edu.vn` |
| Hội đồng — Thành viên | TS. Đặng Hoài Anh | `reviewer4.demo@furpms.edu.vn` |
| Hội đồng — Thành viên | ThS. Bùi Thanh Hà | `reviewer5.demo@furpms.edu.vn` |
| Giảng viên (PI 1) | Nguyễn Văn An | `pi.demo@furpms.edu.vn` |
| Giảng viên (PI 2) | Hoàng Văn Bình | `pi2.demo@furpms.edu.vn` |

> Vai trong hội đồng là **field khi gán**, không phải vai trò đăng nhập (rule #11) — nhưng bộ dữ liệu
> demo luôn gán reviewer1 = Chủ tịch, reviewer2 = Thư ký, reviewer3 = Phản biện cho **mọi** hội đồng,
> nên cứ nhớ đúng 3 người này là đủ diễn.

⚠️ `password` là mật khẩu **demo**, cố tình dễ. Trên bản deploy công khai phải **đổi mật khẩu admin**
hoặc tắt `DEMO_DATA_ENABLED` để seeder không đặt lại.

---

## 3. Dữ liệu demo — 8 đề tài, mỗi cái đứng ở một bước

Mục đích **không phải** "màn nào cũng có chữ", mà là **mỗi bước của quy trình có sẵn một đề tài
đứng ngay TRƯỚC bước đó** — mở màn nào là bấm thật được ngay màn đó, không phải chạy lại cả vòng đời.

**Hai đợt** (rule #7 — 1 đợt = đúng 1 loại đề tài):
- **Ứng dụng 2026** (`UD26`) — đang **mở nhận** đề cương.
- **Cơ bản 2026** (`CB26`) — đã **đóng nhận**, đang xét duyệt / thực hiện.

| Mã | Đề tài | Trạng thái | PI | Demo được gì |
|---|---|---|---|---|
| `NCKH-2026-001` | Phát hiện đạo văn bằng học sâu | **Nháp** | PI 1 | Wizard nộp đề cương · **upload file → AI trích xuất** (cố ý chưa có file đính kèm) |
| `NCKH-2026-002` | Giám sát môi trường bằng IoT | **Đã nộp**, chưa vào vòng | PI 2 | Staff **tạo vòng chấm + gán đề tài** |
| `NCKH-2026-003` | Dự báo nguy cơ bỏ học | **Đang chấm** ⭐ | PI 1 | **Màn chính**: reviewer chấm điểm · Thư ký soạn biên bản · Chủ tịch chốt |
| `NCKH-2026-004` | Học liệu thích ứng | **Yêu cầu chỉnh sửa** | PI 1 | PI sửa & nộp lại, vòng mở lại (rule #1) |
| `NCKH-2026-005` | Đánh giá giảng dạy bằng NLP | **Đã duyệt**, chưa có HĐ | PI 2 | Staff **lập hợp đồng** + **xuất Word BM05** |
| `NCKH-2026-006` | Tối ưu lịch thi | HĐ **đang chạy**, báo cáo kỳ 1 **chờ duyệt** | PI 1 | Staff **duyệt báo cáo tiến độ** · PI **xin gia hạn** |
| `NCKH-2026-007` | Nhận dạng chữ viết tay | **Đang nghiệm thu** | PI 1 | Hội đồng **nghiệm thu** Đạt/Không đạt (BM11) · hồ sơ nghiệm thu |
| `NCKH-2026-008` | Khuyến nghị môn học | **Hoàn thành** | PI 2 | Vòng đời trọn vẹn: timeline, giải ngân đủ đợt, **quyết toán** |

Kèm theo:
- **3 hội đồng**, mỗi hội đồng **5 người** — số **LẺ** và nằm trong 3–5 (xét duyệt, Điều 8.2) /
  5–7 (nghiệm thu, Điều 12.2), đủ Chủ tịch/Thư ký/Phản biện, **tất cả đã xác nhận lời mời**, đã có lịch họp.
- **#3, #4, #5 chấm chung MỘT hội đồng, một buổi họp, ba slot con 45 phút** → mới có gì để xem ở
  màn "lịch chấm"; hội đồng nào cũng chỉ 1 đề tài thì tính năng đó vô hình.
- **Bộ tiêu chí đúng BM03**: 10+20+40+20+10 = **Cộng 100** — bộ không cộng đủ `maxTotalScore` thì
  không nộp phiếu chấm được.
- **File Word thuyết minh sinh sẵn** cho 6 đề tài (#2→#7), nội dung khớp từng đề tài → demo
  **AI đọc file → tóm tắt → đối chiếu biểu mẫu** chạy thật, không phải upload tay tại chỗ.

**Dữ liệu cũ vẫn giữ**: `DEMO-2026-001` (đã nộp) và `DEMO-2026-002` (hợp đồng `HD-2026-002` ACTIVE).

**Tắt data demo** khi bàn giao bản chạy thật: Admin → Cấu hình hệ thống → `DEMO_DATA_ENABLED` = `false`.
Tắt chỉ **ngăn seed thêm**, dữ liệu đã có vẫn nằm đó.

### Khi deploy lên bản đang chạy (không xoá DB)

**Không cần xoá dữ liệu cũ, không có migration mới.** Deploy code này lên DB đang chạy thì:

| Thứ | Chuyện gì xảy ra |
|---|---|
| **Mật khẩu** 9 tài khoản demo | **Đặt lại về `password`** mỗi lần khởi động, kể cả tài khoản đã có (E8). Chỉ chạm đúng 9 email demo, **không đụng** tài khoản người dùng thật. Tắt `DEMO_DATA_ENABLED` là seeder không sờ vào mật khẩu nữa |
| **Tên hiển thị** | Chỉ đổi nếu đang đúng bằng tên placeholder cũ (`PGS.TS Lê Phản Biện`…) |
| `reviewer4` / `reviewer5` | **Tạo mới** — hội đồng cần 5 người |
| Đề tài / hợp đồng / phiếu cũ | **Giữ nguyên**, seeder chỉ thêm chứ không xoá |
| **Bộ tiêu chí 125 điểm** cũ | **Giữ nguyên, không sửa** — nhưng seeder **tự tạo thêm** bộ BM03 100 điểm và vòng chấm demo pin vào bộ đó, nên màn chấm điểm vẫn chạy |
| Trùng số hợp đồng / mã đợt | Tự né sang hậu tố (`HĐ-2025-008-2`) |

**Seeder hỏng không làm sập API.** Mỗi kịch bản chạy trong **một giao dịch riêng**: lỗi thì rollback
sạch (không để lại nửa đề tài), ghi cảnh báo, các kịch bản còn lại vẫn dựng, ứng dụng vẫn khởi động.
Đây là điều kiện bắt buộc — trên deploy, seeder đổ là FE mất luôn backend.

⚠️ **Ổ đĩa của Render bị xoá mỗi lần redeploy.** 6 file Word thuyết minh sinh sẵn phải lưu qua
**Cloudinary**; nếu đang chạy `LocalDisk` thì sau redeploy bảng `documents` còn dòng mà file thì mất,
bấm tải sẽ lỗi. Kiểm cấu hình Cloudinary trước khi deploy.

---

## 4. Kịch bản demo — đóng vai nào, bấm gì

> **Tên mục điều hướng thật** (chụp từ app 09/08 — dùng đúng tên này khi diễn):
> **PI**: Bảng điều khiển · Đề cương của tôi · Nộp đề cương · Báo cáo tiến độ · Sản phẩm ·
> Lịch họp của tôi · Báo cáo tổng kết · Điều chỉnh hợp đồng · Tiến trình đề tài · Tìm kiếm AI.
> **Hội đồng**: Lời mời · Đề tài được phân công · Thành viên hội đồng · Chấm điểm · Lịch họp.
> **Staff/Admin**: Đợt nghiên cứu · Loại đề tài · Đặt hàng nghiên cứu · Người dùng · Đơn vị ·
> Tiêu chí chấm · Hội đồng & Chấm · Kho tài liệu · Thống kê · Cấu hình.
> **Hồ sơ cá nhân** nằm ở **menu avatar góc phải**, không phải thanh điều hướng trái.


> Mỗi mục dưới đây **độc lập**, mở thẳng vào là diễn được, không phải chạy mục trước.

**① PI nộp đề cương** (`pi.demo`) — dùng `NCKH-2026-001`
1. **"Đề cương của tôi"** → bản **nháp** → mở ra, đi hết wizard 5 bước → **Nộp**.
2. Hoặc tạo mới: **upload file Word** → AI trích xuất → prefill form → PI sửa → nộp (rule #10, #20).

**② Staff mở vòng chấm** (`staff.demo`) — dùng `NCKH-2026-002`
1. "Hội đồng & Chấm" → **Tạo vòng chấm** (Xét duyệt đề cương) → **gán đề tài** vào vòng.
2. Lập **hội đồng** (5 người, số lẻ) → **lịch họp** (địa điểm nếu trực tiếp / link nếu online)
   → đủ điều kiện nút **"Gửi thư mời"** mới sáng (rule #17).

**③ Hội đồng chấm & ra kết luận** ⭐ — dùng `NCKH-2026-003`
1. Đăng nhập **`reviewer1`** (Chủ tịch) → "Đề tài được phân công" → **chấm điểm** theo BM03.
   *(4/5 phiếu đã seed sẵn — phiếu của Chủ tịch cố ý để trống cho bước này.)*
2. Đăng nhập **`reviewer2`** (Thư ký) → tab **Biên bản** → xem bảng phiếu từng người
   (BM12 mục 10.1: phát ra / thu về / hợp lệ / điểm TB) → **soạn biên bản**.
3. Đăng nhập **`reviewer1`** → **Chốt biên bản** = khoá, trạng thái đề tài mới đổi (rule #12).

> Thử chốt khi chưa đủ phiếu sẽ bị chặn kèm câu dẫn chiếu **QĐ543 Điều 8.3.b** — đây là điểm đáng
> khoe, không phải lỗi.

**④ PI sửa theo yêu cầu hội đồng** (`pi.demo`) — dùng `NCKH-2026-004`

**⑤ Staff lập hợp đồng** (`staff.demo`) — dùng `NCKH-2026-005`
1. Tạo hợp đồng → **Xuất Word (BM05)**: căn cứ pháp lý, Bên A/Bên B, Điều 1–7, bảng sản phẩm,
   bảng đợt giải ngân, ô ký — đem ký ngoài rồi **upload bản ký làm minh chứng** (rule #21).
2. Ô "gia hạn tối đa" tự chặn theo **1/2 thời gian thực hiện** (Điều 10.4).

**⑥ Theo dõi thực hiện** — dùng `NCKH-2026-006`
1. `staff.demo` → **duyệt báo cáo tiến độ** kỳ 1 (đang chờ).
2. `pi.demo` → nộp sản phẩm / **xin gia hạn** / đề nghị điều chỉnh (BM07).
3. Tab **Tiến trình** của hợp đồng: timeline mốc + click mở minh chứng (rule #22).

**⑦ Nghiệm thu** — dùng `NCKH-2026-007`
1. `reviewer3` (Phản biện) và các thành viên → bỏ phiếu **Đạt / Không đạt** (BM11, không chấm điểm).
2. `reviewer2` soạn biên bản → `reviewer1` chốt. *(3/5 phiếu đã seed, cần thêm 1 phiếu mới đủ 2/3.)*

**⑧ Nhìn lại toàn bộ vòng đời** — mở `NCKH-2026-008`: timeline, giải ngân đủ đợt có minh chứng,
báo cáo tổng kết đã lưu trữ, **quyết toán** đã ký.

**Admin**: Thống kê (overview/track/funnel), người dùng, master data, Cấu hình hệ thống, **Công cụ test** (mục 6).

---

## 5. Mẹo nhập nhanh khi test

- **Nút "Điền dữ liệu mẫu"** ở form tạo đề xuất (góc trên): bấm 1 phát là điền sẵn tiêu đề, mục tiêu, phương pháp, 2 thành viên, 2 khoản kinh phí hợp lệ — chỉ việc bấm tiếp đến bước cuối và "Lưu nháp".
- **Bản nháp tự lưu trên trình duyệt**: gõ dở, F5 không mất chữ.
- Hạng mục kinh phí giờ là **dropdown** (không phải gõ tay) → chọn nhanh, lưu/đọc lại khớp.

---

## 6. Công cụ test thời gian (Admin → "Công cụ test")

Phần lớn hệ thống đi theo nút bấm nên **không cần chờ**. Riêng **nhắc hạn / quá hạn sản phẩm** phụ thuộc thời gian thật — dùng công cụ này:

- **Tua đồng hồ hệ thống** `+7/+14/+30/+90/+180 ngày` (hoặc nhập tuỳ ý) để mô phỏng "đến hạn"; **Đặt lại** về thời gian thật.
- **Chạy quét nhắc hạn**: kích hoạt ngay tác vụ gửi nhắc hạn (bình thường chạy mỗi 24h) → kiểm tra email/thông báo của PI.

Tương ứng API: `GET/POST /api/admin/system-clock`, `POST /api/admin/run-deadline-scan` (xem `API_CONTRACT.md` §10).

---

## 7. Cách "chuyên nghiệp" thường làm với dữ liệu mẫu

Tham khảo nếu sau này muốn nâng cấp khâu test/demo:

1. **Seed data (đang dùng)** — `DatabaseSeeder` tạo sẵn tài khoản + 1 đề tài + master data, idempotent. Phù hợp demo cố định.
2. **Demo-scenario seed (đang dùng)** — `DemoScenarioSeeder` dựng 8 đề tài ở 8 bước khác nhau kèm hội đồng/phiếu/hợp đồng/báo cáo (mục 3), bật/tắt bằng `DEMO_DATA_ENABLED`.
3. **Autofill ở FE (đang có nút "Điền dữ liệu mẫu")** — gắn dữ liệu giả ngay trong form, chỉ bật khi DEV. Nhanh nhất khi test thủ công.
4. **Faker/Factory** — sinh dữ liệu giả hàng loạt (vd Bogus cho .NET) cho test khối lượng lớn / phân trang.
5. **Bộ sưu tập request (Postman/Bruno/.http)** — lưu sẵn các request theo luồng, "Run collection" để dựng trạng thái bằng API thay vì click. Kết hợp với Swagger là đủ cho FE tự test.

> Khi bàn giao FE cho team khác: gửi kèm **`API_CONTRACT.md` + Swagger URL + file này**. Đó là bộ đủ để họ bắt tay làm mà không cần hỏi lại nhiều.
