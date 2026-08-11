# Ràng buộc nghiệp vụ — nhìn từ màn hình

> **Dùng khi nào:** trước buổi demo, hoặc khi ai đó hỏi *"sao tôi bấm mà không được?"*.
>
> Mỗi mục trả lời đúng ba câu: **định làm gì** → **bị chặn thế nào** → **phải làm gì mới đi tiếp được**.
> Câu chữ trong cột "hệ thống nói gì" là **nguyên văn chụp từ màn hình thật**, không phải viết lại.
>
> Kiểm ngày **09/08/2026** bằng Chrome thật (Playwright) trên dữ liệu demo 8 đề tài.
> Tra quy tắc gốc + căn cứ QĐ543 ở `BUSINESS_RULES.md`; bảng đi test từng màn ở `TEST_CHECKLIST.md`.

---

## Cách hệ thống chặn — có 3 kiểu, đừng nhầm

| Kiểu | Nhìn thấy gì | Ví dụ |
|---|---|---|
| **Khoá nút** | Nút mờ đi, bấm không ăn | Thư ký chưa đủ phiếu → nút "Lưu nháp" mờ |
| **Ẩn hẳn** | Không có nút đó luôn | Chủ tịch không thấy nút soạn biên bản |
| **Báo lỗi** | Bấm được, hiện thông báo đỏ | Nhập điểm quá trần |

Kiểu **khoá/ẩn** dễ chịu hơn cho người dùng nhưng khó giải thích khi demo — nên phần dưới ghi rõ
từng chỗ dùng kiểu nào, để lúc đứng trước hội đồng còn biết đường nói.

---

## A. Chấm điểm

### A-1. Nhập điểm vượt trần của tiêu chí
| | |
|---|---|
| **Định làm** | Hội đồng chấm, nhập `99` cho tiêu chí "Mục đích, ý nghĩa…" (trần 10 điểm) |
| **Bị chặn** | Báo lỗi sau khi bấm **Nộp điểm** |
| **Hệ thống nói** | *"Điểm "Mục đích, ý nghĩa khoa học và thực tiễn của đề tài" phải trong khoảng 0–10."* |
| **Làm gì tiếp** | Nhập lại trong khoảng 0–10 |
| **Vì sao** | QĐ543 **BM03** — mỗi tiêu chí có trần riêng (10/20/40/20/10), tổng đúng 100 |

> **Sửa 09/08:** ô nhập nay **cắt ngay lúc gõ** — gõ 99 vào tiêu chí trần 10 thì ô tự về 10, gõ số
> âm thì về rỗng. Thông báo trên vẫn giữ làm lưới chắn cuối nếu ai gọi thẳng API.

### A-2. Nhập điểm thập phân khi hệ thống đặt số nguyên
| | |
|---|---|
| **Định làm** | Nhập `7.5` |
| **Bị chặn** | Báo lỗi khi nộp |
| **Hệ thống nói** | *"Điểm phải là SỐ NGUYÊN (đang nhập 7.5). Phòng QLKH đổi được ở Cấu hình hệ thống."* |
| **Làm gì tiếp** | Nhập số nguyên — **hoặc** Admin vào *Cấu hình hệ thống* đổi `SCORE_DECIMAL_PLACES` thành 1, hội đồng nhập được `7.5` ngay (đã chạy thật 09/08) |
| **Vì sao** | QĐ543 **không quy định**; BM03 để điểm tối đa toàn số nguyên. Thầy chốt: Admin đặt từ đầu, **không hồi tố** — phiếu đã chấm giữ nguyên |

### A-3. Sửa điểm sau khi Chủ tịch đã chốt biên bản
| | |
|---|---|
| **Định làm** | Thành viên mở lại phiếu, sửa điểm |
| **Bị chặn** | Báo lỗi |
| **Hệ thống nói** | *"Biên bản đã được Chủ tịch chốt — không sửa được điểm nữa."* |
| **Làm gì tiếp** | Không có đường sửa. Muốn chấm lại phải mở lại vòng |
| **Vì sao** | Rule #12 — chốt biên bản = **khoá**, vì đó là căn cứ đổi trạng thái đề tài |

---

## B. Biên bản hội đồng

### B-1. Chủ tịch muốn tự soạn biên bản
| | |
|---|---|
| **Định làm** | Chủ tịch mở tab **Biên bản**, tìm nút soạn |
| **Bị chặn** | **Ẩn hẳn** — không có nút nào |
| **Làm gì tiếp** | Thư ký soạn trước; Chủ tịch chỉ **duyệt/chốt** |
| **Vì sao** | Rule #12 (ghi âm thầy tuần 7): Thư ký soạn → Chủ tịch duyệt = khoá. Hai vai tách nhau để có đối trọng |

### B-2. Thư ký lưu biên bản khi chưa đủ 2/3 phiếu
| | |
|---|---|
| **Định làm** | Đề tài nghiệm thu mới có **3/5** phiếu, Thư ký gõ nội dung rồi bấm lưu |
| **Bị chặn** | **Khoá nút** — "Lưu nháp" mờ đi |
| **Nếu gọi thẳng API** | *"Chưa đủ số thành viên chấm để lưu biên bản: mới có 3/5 phiếu, cần ít nhất 4 (QĐ543 Điều 8.3.b — tham dự ít nhất 2/3 số thành viên)."* |
| **Làm gì tiếp** | Để thêm ít nhất 1 thành viên nữa bỏ phiếu → nút sáng lên |
| **Vì sao** | QĐ543 **Điều 8.3.b** — họp hợp lệ cần ≥2/3 thành viên dự, và người dự **phải** đánh giá. 2/3 làm tròn LÊN: 5 người cần 4, không phải 3 |

### B-3. Nghiệm thu mà phản biện chưa cho ý kiến
| | |
|---|---|
| **Định làm** | Đủ 4/5 phiếu nhưng **không có phiếu của Phản biện**, Thư ký lưu biên bản |
| **Bị chặn** | Báo lỗi |
| **Hệ thống nói** | *"Hội đồng nghiệm thu phải có thành viên phản biện dự họp và cho ý kiến (QĐ543 Điều 12.3.b) — chưa có phiếu nào của phản biện."* |
| **Làm gì tiếp** | `reviewer3` (Phản biện) bỏ phiếu Đạt/Không đạt |
| **Vì sao** | QĐ543 **Điều 12.3.b** — riêng hội đồng nghiệm thu bắt buộc có phản biện dự |

---

## C. Hội đồng & lịch họp

### C-1. Gửi thư mời khi hội đồng chưa đủ điều kiện
| | |
|---|---|
| **Định làm** | Staff bấm **Gửi thư mời** |
| **Bị chặn** | Báo lỗi, nêu **đúng thứ đang thiếu** |
| **Hệ thống nói** | *"Chưa có Chủ tịch hội đồng…"* / *"Chưa có Thư ký…"* / *"Chưa có lịch họp — đặt ngày/giờ + địa điểm trước khi gửi thư mời."* / *"Hội đồng đang có 4 thành viên — phải là SỐ LẺ (3, 5)…"* |
| **Làm gì tiếp** | Bổ sung đúng phần thiếu rồi bấm lại |
| **Vì sao** | Rule #17 (thầy tuần 10) — gán đủ rồi mới mời **một lượt**, tránh spam khi còn sửa. Số lẻ để bỏ phiếu luôn có chênh lệch làm căn cứ kết luận |

### C-2. Đặt khung giờ chấm nằm ngoài buổi họp
| | |
|---|---|
| **Định làm** | Buổi họp 9:00–12:00, đặt slot đề tài lúc 13:00 |
| **Bị chặn** | Báo lỗi kèm **giờ cụ thể** |
| **Hệ thống nói** | *"Khung giờ chấm (11/08 13:00–13:45) nằm ngoài buổi họp (11/08 09:00–12:00). Sửa lại khung giờ hoặc kéo dài buổi họp."* |
| **Làm gì tiếp** | Sửa giờ, hoặc kéo dài buổi họp |

### C-3. Hai đề tài trùng khung giờ
| | |
|---|---|
| **Bị chặn** | *"Khung giờ chấm (09:00–09:45) chồng lên khung của đề tài khác (09:30–10:15) — hội đồng chỉ chấm được một đề tài tại một thời điểm."* |

### C-4. Buổi họp không đủ chỗ cho số đề tài
| | |
|---|---|
| **Định làm** | Gán thêm đề tài vào hội đồng đã có lịch họp |
| **Bị chặn** | **KHÔNG chặn** — chỉ hiện **banner vàng** ngay đầu màn chi tiết hội đồng |
| **Hệ thống nói** | *"Tổng khung giờ đã chia (135 phút) vượt quá thời lượng buổi họp (60 phút)…"* hoặc *"Còn 1 đề tài chưa có khung giờ mà buổi họp đã kín (90/90 phút)…"* |
| **Vì sao chỉ cảnh báo** | Rule #17 cho đổi lịch **bất kỳ lúc nào** — khoá cứng sẽ cản đúng thao tác hợp lệ |

### C-5. Thêm người trong nhóm nghiên cứu vào hội đồng chấm chính đề tài đó
| | |
|---|---|
| **Bị chặn** | Báo lỗi xung đột lợi ích |
| **Vì sao** | Rule #5 — COI |

---

## D. Đề cương & nhóm nghiên cứu

### D-1. Sửa thành viên sau khi đã nộp đề cương
| | |
|---|---|
| **Bị chặn** | *"Đề cương đang ở trạng thái SUBMITTED — chỉ sửa được thành viên khi đề cương còn là bản nháp hoặc đang chờ chỉnh sửa. Đã nộp thì gửi đề nghị thay đổi nhân sự (BM07)."* |
| **Làm gì tiếp** | Gửi đề nghị điều chỉnh nhân sự (BM07) |
| **Vì sao** | Nộp rồi mà vẫn thêm bớt người thì **hội đồng chấm một danh sách, hồ sơ lưu một danh sách khác** |

### D-2. Xoá chủ nhiệm khỏi nhóm
| | |
|---|---|
| **Bị chặn** | *"Không xoá được chủ nhiệm đề tài khỏi danh sách thành viên — mọi đề tài đều phải có chủ nhiệm."* |

### D-3. Xoá thành viên đang có dòng thuê khoán trong dự toán
| | |
|---|---|
| **Bị chặn** | *"Thành viên "…" đang có dòng thuê khoán chuyên môn trong dự toán — xoá dòng dự toán đó trước rồi mới xoá được thành viên."* |

### D-4. Nộp đề cương khi CV quá cũ
| | |
|---|---|
| **Bị chặn** | *"Lý lịch khoa học (CV) chưa được cập nhật gần đây. Vui lòng cập nhật CV hoặc xác nhận CV vẫn đúng trước khi nộp."* |
| **Làm gì tiếp** | Cập nhật CV, hoặc tick xác nhận trong hộp thoại |

---

## E. Hợp đồng & giải ngân

### E-1. Gia hạn quá nửa thời gian thực hiện
| | |
|---|---|
| **Định làm** | Đề tài 12 tháng, nhập gia hạn tối đa **9 tháng** |
| **Bị chặn** | *"Gia hạn tối đa không được quá 1/2 thời gian thực hiện (QĐ543 Điều 10.4). Đề tài 12 tháng ⇒ tối đa 6 tháng, đang nhập 9."* |
| **Làm gì tiếp** | Nhập ≤ 6 |

### E-2. Chi đợt giải ngân cuối khi chưa nghiệm thu
| | |
|---|---|
| **Bị chặn** | *"Đợt 3 là đợt giải ngân CUỐI — theo QĐ543 (BM05 Điều 4.2) chỉ được chi kinh phí còn lại sau khi đề tài được hội đồng nghiệm thu công nhận kết quả Đạt. Đề tài đang ở trạng thái "IN_PROGRESS"…"* |
| **Làm gì tiếp** | Hoàn tất nghiệm thu Đạt trước |
| **Lưu ý** | Chỉ áp khi hợp đồng có **≥2 đợt**. Hợp đồng 1 đợt thì đợt đó vừa đầu vừa cuối — chặn là cấm luôn tiền tạm ứng |

### E-3. Chi đợt gắn sản phẩm chưa nghiệm thu
| | |
|---|---|
| **Bị chặn** | *"Sản phẩm minh chứng "…" chưa nghiệm thu Đạt — chưa thể đánh dấu đã giải ngân đợt này."* |

### E-4. Lập quyết toán khi còn đợt chưa chi
| | |
|---|---|
| **Bị chặn** | *"Còn 2 đợt giải ngân chưa đánh dấu đã chi (đợt 2, 3) — chưa thể lập quyết toán. Đợt cuối chỉ mở sau khi hội đồng nghiệm thu kết luận Đạt."* |

### E-5. Xuất phụ lục hợp đồng khi đơn chưa duyệt
| | |
|---|---|
| **Bị chặn** | *"Đề nghị điều chỉnh đang ở trạng thái "PENDING" — chỉ xuất phụ lục sau khi đã được duyệt."* |
| **Vì sao** | In bản chờ duyệt ra là tạo **giấy tờ khống** |

### E-6. Xin gia hạn cho đề tài đã nghiệm thu xong
| | |
|---|---|
| **Bị chặn** | Báo lỗi — đề tài đã hoàn thành/huỷ/chấm dứt thì không còn gì để điều chỉnh |
| **Vì sao** | Thầy bắt được lúc demo 05/08 |

### E-7. Xoá sản phẩm đang là minh chứng của đợt giải ngân
| | |
|---|---|
| **Bị chặn** | *"Sản phẩm "Hệ thống tích hợp LMS" đang là minh chứng của đợt giải ngân 3 — gỡ khỏi đợt đó trước rồi mới xoá được."* |
| **Vì sao** | Xoá đi là đợt giải ngân **mất căn cứ mở**, sau này không ai truy lại được vì sao tiền đã chi |

---

## F. Báo cáo tiến độ & sản phẩm

### F-1. Duyệt báo cáo không có file lẫn link
| | |
|---|---|
| **Bị chặn** | *"Báo cáo chưa có file đính kèm lẫn link — chưa có gì để đọc thì chưa đánh giá được. Đề nghị chủ nhiệm bổ sung bản báo cáo trước."* |
| **Vì sao** | Thầy 29/07: *"Staff phải xem được bản báo cáo mới đánh giá"* |

### F-2. Nộp kỳ 2 khi kỳ 1 chưa được duyệt
| | |
|---|---|
| **Bị chặn** | *"Kỳ 1 đã nộp nhưng phòng QLKH chưa đánh giá — chờ có kết quả kỳ trước rồi mới nộp kỳ này."* |
| **Vì sao** | QĐ543 **Điều 10.1** — kỳ báo cáo phải tuần tự |

### F-3. Nộp lại sản phẩm đã nghiệm thu Đạt
| | |
|---|---|
| **Bị chặn** | *"Sản phẩm "…" đã nghiệm thu ĐẠT — không nộp lại được. Nếu cần thay bản khác, liên hệ phòng QLKH để mở lại."* |

### F-4. Xoá kỳ báo cáo đã nộp
| | |
|---|---|
| **Bị chặn** | *"Báo cáo đang ở trạng thái SUBMITTED — chỉ xoá được bản nháp. Báo cáo đã nộp là căn cứ trong hồ sơ nghiệm thu, không xoá khỏi lịch sử."* |

---

## G. Quản trị

### G-1. Xoá đợt đang có đề tài
| | |
|---|---|
| **Bị chặn** | *"Đợt "CB26 (2026)" đã có đề tài, vòng chấm — không xoá được. Đợt đã dùng thật thì ĐÓNG lại, không xoá khỏi lịch sử."* |
| **Làm gì tiếp** | Dùng **Đóng đợt** thay vì xoá |

### G-2. Xoá đơn vị đang được dùng
| | |
|---|---|
| **Bị chặn** | *"Đơn vị "Khoa Công nghệ Thông tin (Demo)" đang được người dùng, đề tài, danh mục đặt hàng sử dụng — chỉ có thể vô hiệu hoá, không xoá vĩnh viễn được."* |
| **Làm gì tiếp** | Vô hiệu hoá (`isActive = false`) |

### G-3. Sửa bộ tiêu chí đã dùng để chấm
| | |
|---|---|
| **Bị chặn** | *"Bộ tiêu chí này đã được dùng để chấm (14 phiếu) nên không sửa được nữa — sửa là đổi nghĩa những phiếu đã chấm. Hãy bấm "Nhân bản" để tạo bộ mới rồi gắn cho vòng chấm sau."* |
| **Làm gì tiếp** | **Nhân bản** → sửa bản sao → gắn cho vòng mới. Vòng đang dùng bộ cũ giữ nguyên |
| **Vì sao** | Rule #13. Sửa tên tiêu chí là **đổi nghĩa phiếu đã ký**; hạ điểm tối đa còn tệ hơn — phiếu cũ chấm 20 trên tiêu chí nay trần 10 |

### G-4. Chấm bằng bộ tiêu chí không cộng đủ 100
| | |
|---|---|
| **Bị chặn** | *"Bộ … đang cộng được 125/100 điểm nên chưa dùng để chấm được (QĐ543 BM03: phiếu chấm phải cộng đúng tổng điểm). Nhờ phòng QLKH chỉnh lại bộ tiêu chí trước."* |

### G-5. Nhập năm đợt kiểu "2025-2026"
| | |
|---|---|
| **Bị chặn** | Báo ngay tại ô: *"Nhập một năm dương lịch 4 chữ số, ví dụ 2026"* |
| **Vì sao** | QĐ543 **không có khái niệm năm học** — văn bản dùng năm dương lịch (*"Quý I hằng năm…"*, biểu mẫu ghi *"NĂM 20…"*) |

---

## Kịch bản demo "khoe ràng buộc" — 3 phút

Nếu hội đồng hỏi *"hệ thống có kiểm soát gì không?"*, diễn đúng 3 bước này:

1. **Chấm điểm** (`reviewer1` → `NCKH-2026-003`): nhập `7.5` → hệ thống chặn kèm **chỉ đường**
   *"Phòng QLKH đổi được ở Cấu hình hệ thống"* — cho thấy đây là **tham số cấu hình được**, không hardcode.
2. **Biên bản nghiệm thu** (`reviewer2` → `NCKH-2026-007`): nút lưu **mờ** vì mới 3/5 phiếu → cho
   một người nữa bỏ phiếu → **nút sáng lên**. Kèm câu dẫn chiếu **QĐ543 Điều 8.3.b**.
3. **Xoá đợt** (`admin` → CB26): hệ thống từ chối và **gợi ý dùng Đóng đợt** — cho thấy hệ thống
   phân biệt *xoá nhầm* với *kết thúc vòng đời*.

Ba cái này đều **dẫn chiếu văn bản** trong chính thông báo lỗi — đó là thứ khó cãi nhất.

---

## Đã dọn 09/08 (đợt sau khi test trình duyệt)

| Chỗ | Trước | Nay |
|---|---|---|
| Ô nhập điểm | `max=10` nhưng gõ được `99`, bấm nộp mới báo | **Cắt ngay lúc gõ**: 99 → 10 · 7.5 → 8 (khi cấu hình số nguyên) · −3 → rỗng |
| Chính tả | Lẫn *"Xóa"* và *"Xoá"*, *"khóa"* và *"khoá"* | Thống nhất **"xoá / khoá / hoà"** — sửa 36 chỗ FE + 48 chỗ BE |
| Breadcrumb màn chấm | Hiện GUID thô `E01a2af6 D7e5 4467…` | **Bỏ mảnh ID** khỏi breadcrumb; tên đề tài đã có ở tiêu đề trang |
| Bước nhảy điểm của Admin | **Không có tác dụng** — xem bên dưới | Chạy thật: Admin đổi → hội đồng nhập được thập phân |

### Lỗi nghiêm trọng phát hiện lúc dọn: cấu hình bước nhảy điểm chưa từng chạy

Rà cái **403** cứ lặp trong log trình duyệt thì lộ ra chuỗi đứt ở **hai chỗ**:

1. Màn chấm điểm đọc bước nhảy qua `GET /system-settings` — endpoint **chỉ cho Admin**. Hội đồng
   luôn ăn **403**, lỗi bị nuốt, rơi về mặc định số nguyên.
2. `SCORE_DECIMAL_PLACES` **chưa từng được seed thành dòng** trong `system_settings`, nên Admin
   sửa cũng **404** — không có gì để sửa.

Nghĩa là tính năng A10 (*"Admin đặt bước nhảy điểm từ đầu"* — thầy chốt 08/08) **chưa bao giờ hoạt
động**, dù code chặn phía máy chủ vẫn đúng. Nay: thêm `GET /system-settings/scoring-policy` cho mọi
người dùng đã đăng nhập, và seed dòng cấu hình. Kiểm bằng API: Admin đổi sang `1` → hội đồng đọc
được `1`; hội đồng vẫn **403** với danh sách cấu hình đầy đủ (đúng, đó là việc của Admin).

## Chỗ còn chưa gọn

| Chỗ | Vấn đề | Vì sao chưa làm |
|---|---|---|
| 4/5 màn master data | Xoá đã có API nhưng **chưa có màn**: loại sản phẩm · vai trò nhân sự chưa có trang; hạng mục chi · cấu hình tài chính có trang nhưng **ẩn khỏi menu** theo rule #15 | Thêm nút xoá vào màn không ai vào được là thêm code chết. Endpoint để đó không hại gì, dựng 2 màn mới sát ngày bảo vệ thì rủi ro hơn giá trị |
