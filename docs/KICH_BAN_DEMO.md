# Kịch bản demo FURPMS — bấm theo từng bước

> **Đây là file cần mở trước mỗi buổi demo.** Anh nói hôm 14/08: *"kịch bản vốn tôi còn chả biết ở
> đâu mà đọc, làm khá cấn, lỗi lung tung"*. Mở file này, bấm theo thứ tự, không phải nhớ gì.

---

## 0. Chuẩn bị (5 phút trước giờ demo)

```bash
# 1. Bật PostgreSQL (14/08: đổi khỏi SQL Server để deploy được lên Railway)
cd D:\capstone\newroot\FURPMS_BEv2 && docker compose up -d

# 2. Bật máy chủ (tự chạy migration + seed khi khởi động)
dotnet run --project FURPMS.API --urls "http://localhost:5068"

# 3. Bật giao diện (cửa sổ khác)
cd "d:\Downloads\doc\9 đồ án\core\FURPMS-Web" && npm run dev
```

Sẵn sàng khi: http://localhost:5068/swagger mở được **và** http://localhost:5173 hiện trang chủ.

> ⚠️ **Kiểm `.env` của giao diện trỏ đâu.** Phải là `VITE_API_BASE_URL=http://localhost:5068/api`.
> Trỏ vào Render thì bản free **ngủ sau ~15 phút** không dùng — request đầu mất rất lâu rồi
> timeout, nhìn y như "lỗi máy chủ" dù máy chủ ở máy vẫn chạy tốt.

### Muốn làm lại từ dữ liệu sạch

```bash
docker exec -i furpms-db-1 psql -U postgres -c "DROP DATABASE IF EXISTS furpms"
docker exec -i furpms-db-1 psql -U postgres -c "CREATE DATABASE furpms"
# rồi chạy lại `dotnet run` — seeder dựng lại toàn bộ
```

---

## Tài khoản demo

Mật khẩu **tất cả** tài khoản: `password`

| Vai | Email | Dùng để trình diễn |
|---|---|---|
| Quản trị | `admin@furpms.edu.vn` | Người dùng, phân vai, cấu hình |
| Chuyên viên | `staff.demo@furpms.edu.vn` | Đợt, hội đồng, lịch họp, hợp đồng |
| Chủ nhiệm | `pi.demo@furpms.edu.vn` | Nộp đề cương, báo cáo tiến độ |
| Chủ nhiệm 2 | `pi2.demo@furpms.edu.vn` | Đề tài thứ hai |
| Phản biện 1–5 | `reviewer{1..5}.demo@furpms.edu.vn` | Chấm điểm, biên bản |

> Tên tài khoản có **mã vai ở cuối** (`… pi1`, `… rv3`, `… staff`) để nhìn danh sách là biết ai
> đóng vai gì. Seeder đặt lại đúng bộ tên mỗi lần khởi động ⇒ xoá sạch DB vẫn ra đúng.
>
> `staff.demo` có **hai vai** (Chuyên viên + Giảng viên) để trình diễn đổi vai ở dropdown header
> (rule #23).

---

## Luồng chính — 12 phút

### 1. Chủ nhiệm nộp đề cương *(pi.demo)*

1. **Hồ sơ** → tab **Công trình & đề tài** → *"Thêm công trình"*
   → khai một bài báo: tên · tạp chí · **volume · trang số** · DOI
   → sang tab **Lý lịch khoa học**, chỉ ô thống kê tương ứng tăng lên 1.
   > 💬 *Nói gì:* QĐ543 Biểu mẫu 02 mục 14.6 đòi liệt kê đầy đủ kèm tạp chí, volume, trang số —
   > không phải chỉ ghi số lượng. Số ở mục 14.1–14.5 tự cộng từ danh sách nên không bao giờ lệch.
2. **Đề tài của tôi** → *"Nộp đề cương mới"* → chọn đợt + lĩnh vực
3. Bước 2: tải tệp thuyết minh → *"Trích xuất bằng AI"* → AI điền hộ → sửa lại
4. Bước 3: điền **Sản phẩm dự kiến** (QĐ543 Điều 11.1 — hồ sơ nghiệm thu đối chiếu đúng mục này)
5. Bước 4: dự toán theo **6 hạng mục Điều 15**; vượt trần Điều 14 là chặn ngay tại chỗ
   > 💬 Thử gõ 200 triệu cho đề tài cơ bản → chặn, nói rõ trần 100 triệu.
6. **Lưu nháp** → **Nộp duyệt** (có hộp thoại nhắc cập nhật lý lịch)

### 2. Chuyên viên mở vòng chấm & lập hội đồng *(staff.demo)*

1. **Hội đồng & Chấm** → chọn đợt → *"Tạo vòng chấm"* → **Xét duyệt đề cương**
   > 💬 Thử chọn **Nghiệm thu** khi chưa đề tài nào được duyệt → chặn, dẫn Điều 11.2.c.
2. Tạo hội đồng → gán 5 thành viên (Chủ tịch · Thư ký · 2 Phản biện · Uỷ viên)
   > 💬 Thử gán chính chủ nhiệm vào hội đồng đề tài của họ → chặn (xung đột lợi ích, rule #5).
3. **Lịch họp** → tạo buổi họp: ngày giờ + **địa điểm** (offline) hoặc link (online)
   > 💬 Chưa đủ thành viên/lịch thì nút *"Gửi thư mời"* chưa hiện — rule #17.
4. **Gửi thư mời** một lượt cho cả hội đồng

### 3. Hội đồng chấm & lập biên bản *(reviewer1 = Chủ tịch, reviewer2 = Thư ký)*

1. `reviewer2` → **Đề tài được phân công** → mở đề tài
2. Thẻ AI trên cùng: **một nút** *"Tóm tắt & gợi ý điểm"* — chạy ~10 giây ra **cả hai**
   > 💬 Trước đây phải bấm hai nút, chờ hai lượt 30–60 giây. Nay máy chủ chạy song song. Một phần
   > lỗi cũng không kéo đổ phần kia.
   > ⚠️ **Trước buổi demo nhớ gắn bộ tiêu chí cho hội đồng**, không thì phần gợi ý điểm báo
   > *"chưa có bộ tiêu chí nào áp dụng"* (tóm tắt vẫn ra bình thường).
3. Chấm điểm từng tiêu chí — AI gợi ý hiện dưới mỗi tiêu chí, bấm *"Áp dụng"* nếu đồng ý
   > 💬 AI **chỉ gợi ý**, điểm cuối là của người chấm (rule #12).
4. Thư ký soạn **biên bản**: điểm danh · Hỏi–Đáp · ý kiến từng thành viên (chuyên môn / kinh phí)
5. Đổi sang `reviewer1` (Chủ tịch) → *"Duyệt & khoá"* → có hộp thoại xác nhận nêu rõ hậu quả
   > 💬 Thư ký **soạn**, Chủ tịch **chốt** — QĐ543 Điều 8.3.c. Khoá xong không sửa được nữa.

### 4. Hợp đồng *(staff.demo)*

1. **Hợp đồng** → *"Lập hợp đồng"* cho đề tài vừa duyệt
   > 💬 Chỉ nhập 5 trường — thông tin Bên B (họ tên, đơn vị, số tài khoản, CCCD) hệ thống **tự bốc
   > từ hồ sơ chủ nhiệm**.
2. *"Xuất hợp đồng (Word)"* → mở file, chỉ các điều khoản đúng BM05
3. Thử bấm **"Ký hợp đồng"** ngay → **chặn**, chỉ đúng 3 bước: xuất Word → ký ngoài → tải bản ký lên
   > 💬 BM05 Điều 7.2: hợp đồng ký điện tử trên phần mềm ngoài (Econtract). Hệ thống là **sổ cái
   > giữ bằng chứng**, không phải công cụ ký. Nút không được nói dối.
4. Tải một file bất kỳ làm "bản đã ký" → nút mở khoá → nhập **ngày ký thực tế** → ký
5. Thử **sửa** hợp đồng sau khi ký → chặn, dẫn sang đường **phụ lục** (BM05 Điều 6.1)
6. Tab **Tiến trình**: mốc ký · giải ngân từng đợt · bấm mở minh chứng

### 5. Tiến độ & nghiệm thu

1. `pi.demo` → **Báo cáo tiến độ** → nộp BM06
2. `staff.demo` → duyệt trực tiếp (**không lập hội đồng** — rule #16, Điều 18.1 chỉ trả thù lao 2 hội đồng)
3. Mở vòng **Nghiệm thu** → hội đồng chấm → Chủ tịch chốt
4. **Hợp đồng** → *"Xuất biên bản thanh lý (BM13)"*

---

## Phần trình diễn thêm (nếu còn thời gian)

| Tính năng | Đường đi | Nói gì |
|---|---|---|
| **Tạo tài khoản gửi mail** | Admin → Người dùng → Thêm | Mail kèm mật khẩu tạm, dẫn thẳng màn đổi mật khẩu |
| **Quên mật khẩu** | Đăng xuất → *"Quên mật khẩu?"* | Mã băm SHA-256, sống 30 phút, dùng một lần; email không tồn tại vẫn trả 200 (chống dò tài khoản) |
| **Đổi vai** | `staff.demo` → dropdown avatar | Rule #23 — chỉ hiện vai user thực có |
| **Thông báo** | Chuông trên header | Tự cập nhật 60 giây/lần; 16 loại thông báo |
| **Gia hạn đợt** | Staff → Đợt → Gia hạn | **Ghi log, không ghi đè** — deadline gốc vẫn còn (rule #19) |
| **Cảnh báo trùng lịch** | Staff → Hội đồng → Xem xung đột | Giảng viên ở 2 hội đồng giao giờ |

---

## Chỗ dễ vấp — biết trước để đỡ luống cuống

| Hiện tượng | Vì sao | Xử lý |
|---|---|---|
| Bấm gì cũng lâu rồi lỗi | `.env` trỏ Render (bản free ngủ sau ~15 phút) | Đổi về `http://localhost:5068/api`, chạy lại `npm run dev` |
| Không nhận được mail | Mail của địa chỉ `@furpms.edu.vn` bị **hứng** về hộp thư thật (địa chỉ giả) | Xem hộp thư trong `CatchFakeMailInbox`; địa chỉ **thật** thì đi thẳng |
| Mail vào Spam | `FromEmail` là `@gmail.com` gửi qua relay Brevo ⇒ SPF/DKIM không khớp | Không sửa được bằng code — mở sẵn tab Spam trước khi demo |
| AI gợi ý điểm báo "chưa có bộ tiêu chí" | Hội đồng chưa gắn bộ tiêu chí | Staff → Hội đồng → gắn bộ tiêu chí **trước** buổi demo |
| Bấm "Bắt đầu" họp nhầm | — | Có nút **"Hoàn tác bắt đầu"** (khi chưa ai điểm danh) |
| Đổi vai xong bị chặn | Vai đang xem khác vai của màn | Trang báo lỗi có sẵn nút đổi vai |

---

## Câu hỏi hội đồng hay hỏi — trả lời sẵn

| Hỏi | Trả lời |
|---|---|
| *Bằng chứng ký hợp đồng đâu?* | File bản ký đính kèm, kèm **ngày ký thực tế** ghi trên giấy (tách khỏi ngày bấm nút). BM05 Điều 7.2 quy định ký trên Econtract — hệ thống giữ bằng chứng. |
| *Hệ thống có quản tiền không?* | Không. Chỉ theo dõi **mốc giải ngân** + giữ **minh chứng**. Kế toán chi tiền ngoài hệ thống (chốt tuần 10). |
| *AI tự chấm điểm à?* | Không. AI **chỉ gợi ý**, hiện dưới từng tiêu chí, người chấm bấm "Áp dụng" nếu đồng ý. Kết quả cuối là **quyết định của Chủ tịch** sau khi hội đồng họp kín (rule #12). |
| *Sao chỉ 2 hội đồng?* | QĐ543 **Điều 18.1** chỉ cấp thù lao cho hội đồng xét duyệt và hội đồng nghiệm thu. Đánh giá tiến độ giữa kỳ do Chuyên viên duyệt (BM06 đề *"Kính gửi: Phòng Quản lý khoa học"*). |
| *Trần kinh phí lấy đâu ra?* | QĐ543 **Điều 14**: cơ bản ≤ 100 triệu, ứng dụng ≤ 150 triệu. Trần để ở master data vì **Điều 14.3** cho phép Hiệu trưởng duyệt vượt. |
| *Lý lịch khoa học kiểm chứng kiểu gì?* | Từng công trình có **tạp chí, volume, trang số, DOI và link mở nguồn** — đúng mục 14.6 của BM02. Số lượng tự cộng từ danh sách. |

---

## Liên quan

- Góp ý mới nhất của thầy: `GOPY_Thay_Demo_1408.md`
- Việc còn treo theo mức ưu tiên: `BACKLOG_Uu_tien.md`
- Quy tắc nghiệp vụ (đánh số): `../CLAUDE.md`
- Giao kèo API: `API_CONTRACT.md`
