# Góp ý của thầy — buổi demo chiều 14/08

> Ghi lại **nguyên văn ý** anh nhớ được sau buổi demo (thầy bận, không online lâu). Mỗi mục ghi rõ
> *thầy nói gì* · *hiểu thành việc gì* · *trạng thái*. Đây là nguồn duy nhất cho đợt sửa sau demo —
> đừng sửa theo trí nhớ mà không đối chiếu file này.

---

## 1. Hồ sơ người dùng — chia phần, đừng cuộn một mạch ✅ XONG 14/08

**Thầy nói:** *"phần lý lịch khoa học hồ sơ người dùng chia phần ra sao á, chứ đừng có lướt lướt
xuống cuối thế."*

**Hiểu là:** trang Hồ sơ hiện là **một cột dài liên tục** — muốn xem mục cuối phải cuộn qua hết mọi
thứ. Cần cắt thành các phần rõ ràng, nhảy thẳng tới phần cần xem.

**Đã làm:** chia **4 tab** — Tài khoản · Lý lịch khoa học · Công trình & đề tài · Thông tin lập
hợp đồng. Mỗi tab tải riêng (lazy) nên mở trang cũng nhanh hơn.

---

## 2. Lý lịch khoa học — ĐỪNG ĐẾM SỐ, phải xem được NGUỒN ✅ XONG 14/08

**Thầy nói:** *"chỗ ghi đề tài dự án từng làm, đăng báo gì đó — đừng có đếm số mà phải xem được
nguồn, các thứ báo nào các thứ. Lên mạng search rồi tham khảo."*

**Hiểu là:** hiện lý lịch khoa học chỉ có **ô nhập SỐ LƯỢNG** (kiểu "số bài báo: 5"). Con số trần
không chứng minh được gì — hội đồng xét năng lực chủ nhiệm phải **đọc được từng công trình**: tên
bài, đăng ở đâu, năm nào, vai trò gì, link/DOI để tra.

**Căn cứ tìm được — mạnh hơn dự đoán ban đầu.** Không cần tham khảo mẫu ngoài: **chính BM02 đã
quy định sẵn**, và hệ thống đang làm thiếu đúng nửa sau:

| BM02 | Nội dung | Hệ thống cũ |
|---|---|---|
| 14.1–14.5 | Số bài ISI/SCOPUS · tạp chí QT · tạp chí trong nước · hội nghị QT · hội nghị trong nước | ✅ có (ô nhập tay) |
| **14.6** | *"Liệt kê đầy đủ các công bố nêu trên… (tên tác giả, năm xuất bản, tên công trình, **tên tạp chí, volume, trang số**)"* | ❌ **không có** |
| 13 | Sách, chuyên khảo, giáo trình | ❌ không có |
| 15 | Bằng sở hữu trí tuệ (tên · số/ký mã hiệu · nơi cấp · năm) | ❌ không có |
| 16.3 | Sản phẩm ứng dụng (tên · thời gian/hình thức/quy mô/địa chỉ · công dụng) | ❌ không có |
| **17.1 / 17.2** | Đề tài đã **chủ trì** / **tham gia** (tên+mã số · thời gian · cơ quan quản lý · **tình trạng nghiệm thu**) | ❌ không có |
| 18 | Giải thưởng KH&CN | ❌ không có |
| 19.4 | Chi tiết hướng dẫn SĐH (tên NCS · tên luận án · **vai trò chính/phụ** · thời gian) | ❌ không có |

**Đã làm:** bảng mới `academic_work`, mỗi loại ứng với **đúng một mục BM02**. Có đủ `volume` và
`pages` mà mục 14.6 đòi đích danh, và ô **tình trạng** với đúng 3 giá trị mục 17 liệt kê.

**8 ô đếm cũ nay là số suy ra** — máy chủ cộng lại từ danh sách sau mỗi thay đổi, không nhập tay
nữa. Lý do: khai tay hai chỗ (số ở 14.1–14.5, danh sách ở 14.6) thì sớm muộn cũng lệch nhau.

**Quyền:** ghi thì **chỉ chính chủ, kể cả Admin cũng 403** — lý lịch là lời khai có trách nhiệm.
Admin/Staff **xem** được để thẩm định năng lực chủ nhiệm theo Điều 7.

> 💡 Lưu ý cho lần đối chiếu sau: bản dự thảo đầu tôi tự đoán mục 17 chia theo **cấp quản lý**
> (nhà nước/bộ/tỉnh/cơ sở) theo mẫu Bộ KH&CN. Đọc BM02 mới thấy QĐ543 chia theo **vai trò**
> (chủ trì / tham gia). Đã sửa lại trước khi chạy migration.

---

## 3. Đầu trang Hồ sơ — tên dính nền xanh, chữ lệch ✅ XONG 14/08

**Thầy nói:** *"ngay bên trên đầu, cái tên bị dính nền xanh gì á, trông chữ hơi lệch."*

**Hiểu là:** lỗi hiển thị ở khối tiêu đề (avatar + tên) — chữ đè lên nền, hoặc căn lề lệch.

**Nguyên nhân (đã chụp màn xác minh):** khối tiêu đề có dải gradient cao 64px, rồi cả hàng
(avatar + tên + email + nhãn vai) bị kéo lên `-mt-8`. Ý định ban đầu chỉ là cho **avatar** đè lên
dải màu, nhưng lề âm đặt trên **cả hàng** nên khối chữ cũng bị kéo vào trong nền gradient — chữ
tối trên nền xanh-tím, lại lệch hẳn so với avatar.

**Đã sửa:** chỉ avatar mang lề âm; khối chữ nằm hẳn dưới nền thẻ nên luôn đọc được, bất kể tên
dài hay ngắn. Đã chụp lại đối chiếu trước/sau.

---

## 4. Hợp đồng — format lại cho đẹp

**Thầy nói:** *"format lại hợp đồng cho đẹp, lên mạng tham khảo các thứ."*

**Hiểu là:** file Word hợp đồng xuất ra (BM05) bố cục còn thô so với hợp đồng thật.

**Việc:** làm lại bố cục file Word — canh lề, font, khoảng cách, bảng kinh phí, khối chữ ký.
(Trùng với **P3-2** trong `BACKLOG_Uu_tien.md` — nay thầy nêu nên **nâng ưu tiên**.)

---

## 5. Email gửi tới `pokewar.2233@gmail.com` có gì đó sai

**Thầy nói / anh quan sát:** *"nó bị gửi mail bị sao ấy với tk pokewar.2233@gmail.com, không biết phải không."*

**Nguyên nhân đã tìm ra (13/08):** `EmailSettings:RedirectAllTo` chuyển hướng **TẤT CẢ** mail về
hộp thư đó, tiêu đề bị chèn tiền tố `[→ người-nhận-thật]` ⇒ nhìn rất lạ khi trình diễn.

**Đã sửa:** thêm `RedirectDomains` — chỉ hứng mail của miền giả (`furpms.edu.vn`), địa chỉ thật đi
thẳng. **Còn tồn:** thư vẫn dễ vào Spam vì `FromEmail` là `@gmail.com` gửi qua relay Brevo
(SPF/DKIM không khớp) — không sửa được bằng code.

---

## 6. Người chấm phải chờ AI **hai lần**

**Anh quan sát:** *"tóm tắt xong bấm gợi ý cho chấm thì lại phải chờ tiếp. Hay là cho 1 nút bấm
thôi nhỉ, nó sẽ tóm tắt và gợi ý luôn."*

**Hiểu là:** phản biện mở đề tài → bấm "Tóm tắt" chờ 30–60s → bấm tiếp "Gợi ý chấm" chờ thêm lượt
nữa. Hai lần chờ ngay lúc hội đồng đang ngồi nhìn. Chưa kể gói Gemini miễn phí **giới hạn số
request mỗi phút** ⇒ bấm hai lần liên tiếp dễ bị chặn.

**Việc:** gộp thành **một lần gọi** trả về cả tóm tắt lẫn gợi ý điểm. Vừa bớt một lượt chờ, vừa
bớt một request.

**Liên quan P0-1** (`BACKLOG_Uu_tien.md`): thầy đã góp ý từ trước là **sinh sẵn lúc PI nộp**, người
chấm chỉ việc đọc. Gộp một nút là bước đệm; sinh sẵn mới là đích.

---

## 7. AI bên PI chạy lỗi

**Anh quan sát:** *"AI lúc ở PI chạy lỗi thì phải."*

**Việc:** dựng lại lỗi, đọc log, sửa. (Chưa rõ lỗi gì — phải tự dò.)

---

## 8. Không biết đọc kịch bản demo ở đâu

**Anh nói:** *"kịch bản vốn tôi còn chả biết ở đâu mà đọc, làm khá cấn, lỗi lung tung."*

**Việc:** một file kịch bản demo **duy nhất, dễ tìm** — mở ra là bấm theo được từng bước, kèm tài
khoản + mật khẩu + thứ tự màn. Link từ `docs/00_INDEX.md` và README.

---

## 9. FE bật lỗi server dù BE đang chạy

**Anh quan sát:** khi **tạo đợt từ đầu**, BE vẫn chạy mà FE báo lỗi server.

**Việc:** dựng lại luồng tạo đợt, bắt lỗi thật (log BE + response), sửa.

---

## 10. Hạ tầng — dự tính mua Railway ~20$

**Anh nói:** *"gần như chắc chắn sẽ làm — bỏ ra 20 đô mua Railway để deploy BE cho mượt. Chắc mai sẽ làm."*

**Không phải việc code.** Ghi lại để không quên. Hiện BE ở Render (bản free **ngủ sau ~15 phút**
không dùng ⇒ lần bấm đầu rất lâu — đây có thể chính là nguyên nhân mục 9 nếu lúc đó trỏ vào Render).

---

## Thứ tự làm (theo mức thầy dễ soi lại)

| Ưu tiên | Mục | Vì sao |
|---|---|---|
| 1 | **2 — lý lịch khoa học có nguồn** | Thầy nói kỹ nhất, có căn cứ QĐ543 |
| 2 | **1 — chia phần hồ sơ** | Cùng màn với mục 2, làm một thể |
| 3 | **3 — sửa lỗi hiển thị đầu trang** | Nhỏ, nhưng thầy nhìn thấy ngay |
| 4 | **6 — gộp một nút AI** | Bớt chờ trước mặt hội đồng |
| 5 | **7 + 9 — lỗi AI bên PI, lỗi tạo đợt** | Lỗi thật, chặn luồng |
| 6 | **4 — format hợp đồng** | Thầy nêu đích danh |
| 7 | **8 — file kịch bản demo** | Giúp chính anh lần sau đỡ cấn |
