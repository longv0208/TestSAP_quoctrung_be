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

## 4. Hợp đồng — format lại cho đẹp ✅ XONG 14/08

**Thầy nói:** *"format lại hợp đồng cho đẹp, lên mạng tham khảo các thứ."*

**Hiểu là:** file Word hợp đồng xuất ra (BM05) bố cục còn thô so với hợp đồng thật.

**Căn cứ tìm được:** đây **không phải chuyện thẩm mỹ tuỳ ý**. Thể thức văn bản hành chính Việt
Nam có quy định pháp lý — **Nghị định 30/2020/NĐ-CP, Phụ lục I**.

Bản cũ **không đặt gì cả**:

| Hạng mục | Trước | Nay (theo NĐ30) |
|---|---|---|
| Phông chữ | không đặt ⇒ Word dùng Calibri/Aptos của **máy người mở** | **Times New Roman 13** (quy định 13–14) |
| Khổ giấy | không đặt | **A4** |
| Lề | không đặt | trên/dưới 20mm · **trái 30mm** (chừa đóng gáy) · phải 15mm |
| **Quốc hiệu** | **căn TRÁI** ← đúng chỗ thầy chỉ | in hoa đậm **CĂN GIỮA** |
| Tiêu ngữ | gạch ngang `–` (sai thể thức) và `***` thay cho đường kẻ | gạch nối `-`, đậm 14pt, có **đường kẻ ngang** dài đúng bằng dòng chữ |
| Khối "Căn cứ…" | thường, căn trái | **nghiêng**, căn đều hai bên, lùi đầu dòng 1cm |
| Khối ký | bảng **kẻ ô** ⇒ ra cái lưới quanh chỗ ký | bảng **không viền**, 2 cột căn giữa, chừa 4 dòng cho chữ ký + dấu |

Áp cho cả **3 văn bản**: hợp đồng (BM05) · biên bản thanh lý (BM13) · phụ lục điều chỉnh.
Sửa luôn "CỘNG HOÀ" → "CỘNG HÒA".

**Cách xác minh:** xuất hợp đồng thật rồi **bóc XML kiểm từng thuộc tính** — `rFonts` = Times New
Roman, `sz` = 26 (13pt), `pgSz` = 11906×16838 (A4), `pgMar left` = 1701 (30mm), quốc hiệu
`jc=center`, tiêu ngữ có `pBdr`, bảng ký 6 cạnh `w:val="none"`.

(Ứng với **HD-8** và nửa phần Word của **P3-2** trong `BACKLOG_Uu_tien.md`; nửa còn lại — giao
diện màn hợp đồng — vẫn treo.)

---

## 5. Email gửi tới `pokewar.2233@gmail.com` có gì đó sai ✅ XONG 13/08

**Thầy nói / anh quan sát:** *"nó bị gửi mail bị sao ấy với tk pokewar.2233@gmail.com, không biết phải không."*

**Nguyên nhân đã tìm ra (13/08):** `EmailSettings:RedirectAllTo` chuyển hướng **TẤT CẢ** mail về
hộp thư đó, tiêu đề bị chèn tiền tố `[→ người-nhận-thật]` ⇒ nhìn rất lạ khi trình diễn.

**Đã sửa:** thêm `RedirectDomains` — chỉ hứng mail của miền giả (`furpms.edu.vn`), địa chỉ thật đi
thẳng. **Còn tồn:** thư vẫn dễ vào Spam vì `FromEmail` là `@gmail.com` gửi qua relay Brevo
(SPF/DKIM không khớp) — không sửa được bằng code.

---

## 6. Người chấm phải chờ AI **hai lần** ✅ XONG 14/08

**Anh quan sát:** *"tóm tắt xong bấm gợi ý cho chấm thì lại phải chờ tiếp. Hay là cho 1 nút bấm
thôi nhỉ, nó sẽ tóm tắt và gợi ý luôn."*

**Hiểu là:** phản biện mở đề tài → bấm "Tóm tắt" chờ 30–60s → bấm tiếp "Gợi ý chấm" chờ thêm lượt
nữa. Hai lần chờ ngay lúc hội đồng đang ngồi nhìn. Chưa kể gói Gemini miễn phí **giới hạn số
request mỗi phút** ⇒ bấm hai lần liên tiếp dễ bị chặn.

**Đã làm:** endpoint `POST /ai/councils/{councilId}/proposals/{proposalId}/review-kit` trả cả hai.
Hai phần chạy **song song** nên tổng chờ xấp xỉ một lần gọi — đo thật **10.9 giây**. Giao diện còn
**một nút** *"Tóm tắt & gợi ý điểm"*, form chấm điểm chỉ đọc kết quả.

Một phần hỏng **không kéo đổ phần kia**: đo trên hội đồng chưa gắn bộ tiêu chí thì tóm tắt vẫn về
bình thường, phần gợi ý báo rõ *"chưa có bộ tiêu chí nào áp dụng cho hội đồng này"*.

**Liên quan P0-1** (`BACKLOG_Uu_tien.md`): thầy đã góp ý từ trước là **sinh sẵn lúc PI nộp**, người
chấm chỉ việc đọc. Gộp một nút là bước đệm; sinh sẵn mới là đích.

---

## 7. AI bên PI chạy lỗi ✅ XONG 14/08 — tìm ra nguyên nhân thật

**Anh quan sát:** *"AI lúc ở PI chạy lỗi thì phải."*

**Nguyên nhân:** `axiosClient` đặt hạn chờ **15 giây**, trong khi đo thẳng vào máy chủ:

| Lời gọi | Thời gian thật |
|---|---|
| `POST /proposals/{id}/generate-summary` | **36.5 giây** |
| `POST /ai/proposals/{id}/feedback` | **54.2 giây** |
| `POST /ai/councils/.../review-kit` | 10.9 giây |

⇒ **Mọi** lời gọi AI đều bị trình duyệt huỷ giữa chừng rồi báo lỗi, trong khi máy chủ vẫn chạy
xong bình thường và lưu kết quả vào `llm_outputs`. Nhìn từ ngoài y như "AI hỏng".

Cũng giải thích hiện tượng *bấm lại thì thấy kết quả*: lần trước đã sinh xong và nằm sẵn trong
cache, chỉ là lần đó bị báo lỗi.

**Đã sửa:** thêm `AI_TIMEOUT_MS = 180s`, áp cho **9 đường** thật sự gọi mô hình. Không nâng hạn
mặc định cho mọi lời gọi — màn hình bình thường mà treo một phút thì tệ hơn là báo lỗi sớm.

---

## 8. Không biết đọc kịch bản demo ở đâu ✅ XONG 14/08

**Anh nói:** *"kịch bản vốn tôi còn chả biết ở đâu mà đọc, làm khá cấn, lỗi lung tung."*

**Đã làm:** `docs/KICH_BAN_DEMO.md` — chuẩn bị · tài khoản · luồng chính 12 phút có ghi sẵn *"nói
gì"* ở mỗi bước · phần trình diễn thêm · **chỗ dễ vấp** · **câu hội đồng hay hỏi kèm câu trả lời
sẵn**. Đã đưa lên đầu `00_INDEX.md` tầng 1.

---

## 9. FE bật lỗi server dù BE đang chạy — 🔶 CHƯA DỰNG LẠI ĐƯỢC, nhưng vá 4 lỗ khác

**Anh quan sát:** khi **tạo đợt từ đầu**, BE vẫn chạy mà FE báo lỗi server.

**Chưa dựng lại được.** Gọi thẳng API tạo đợt: sạch. Lái trình duyệt qua cả 7 màn Staff
(`/cycles`, `/review-board`, `/councils`, `/meetings`, `/contracts`, `/proposal-reviews`,
`/assignments`): không console error, không HTTP ≥ 400, không lỗi trên màn.

**Nghi ngờ:** lúc đó giao diện trỏ vào **Render** — bản free ngủ sau ~15 phút không dùng, request
đầu rất lâu rồi timeout, nhìn y như "lỗi máy chủ" dù máy chủ ở máy vẫn chạy tốt. Trùng khớp với
mục 10 (anh định mua Railway vì "chạy cho mượt"). Đã ghi cách kiểm `.env` vào `KICH_BAN_DEMO.md`.

**Nhưng dò ra 4 lỗ THẬT trong đúng luồng đó** — máy chủ không kiểm gì cả, chỉ form giao diện chặn:

| Trường hợp | Trước | Nay |
|---|---|---|
| Hạn nộp **trước** ngày mở | 200 — đợt không ai nộp được | chặn |
| Trùng năm + trùng loại đề tài | 200 — hai đợt song song, PI không biết nộp đâu (trái rule #7) | chặn |
| Năm 1800 | 200 | chặn |
| Tên rỗng | 200 | chặn |

Kiểm ở **tầng dịch vụ** vì mọi đường ghi đều đi qua đó; đường **sửa** cũng kiểm, và kiểm sau khi
gán để bắt trạng thái cuối cùng.

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
| — | **Tất cả đã xong trừ #9 (không dựng lại được) và #10 (Railway — việc của anh)** | |

---

## 11. Thông báo lỗi nhìn từ phía NGƯỜI DÙNG (anh nêu 14/08) ✅ XONG 14/08

**Anh nói:** *"có mấy cái thông báo lỗi mà nó nói theo luật #13 gì á… làm tôi cảm thấy đấy là cái
luật gì? để vô đó chi vậy?"*

Đúng. `rule #13` là **số hiệu nội bộ trong `CLAUDE.md`** — người dùng không có cách nào biết nó là
gì, đọc xong chỉ thấy hệ thống đang nói chuyện với chính nó.

**Rà toàn bộ thì ra ba lớp, mỗi lớp một đợt quét trước KHÔNG thể thấy:**

| Lớp | Số chỗ | Vì sao lọt |
|---|---|---|
| Số hiệu quy tắc nội bộ (`rule #7/#13/#16`) | 6 | Chỉ hiện khi vi phạm đúng ràng buộc đó |
| Thông báo lỗi máy chủ còn **tiếng Anh**, kèm **GUID** — `Contract 3f2a-8b91… not found.` | **175** (71 chuỗi) | Chỉ hiện khi thao tác thất bại |
| Lỗi validate giao diện còn tiếng Anh | 52 | Chỉ hiện khi nhập sai |

**Đã sửa hết.** Nguyên tắc áp dụng:

- Nói **lý do**, không nói số hiệu: *"Mỗi năm chỉ mở MỘT đợt cho mỗi loại — muốn mở cả hai loại thì
  tạo hai đợt riêng"* thay cho *"(rule #7)"*.
- **Bỏ GUID** khỏi thông báo. Người dùng không tra được mã đó, mà nhìn vào thì tưởng hệ thống sập.
  Log vẫn giữ đủ stack trace để lập trình viên tra.
- Nêu **việc phải làm tiếp**, không chỉ nêu cái sai: *"Phiên đăng nhập không hợp lệ. Hãy đăng nhập
  lại."*

---

## 12. Định dạng ngày/số trong file xuất ra — rủi ro khi lên máy chủ ✅ XONG 14/08

**Anh hỏi:** *"nếu nó hiện trong hợp đồng lúc xuất ra ngày tháng kiểu Mỹ luôn thì có vẻ không ổn?"*

**Kiểm thật: hiện tại ĐÚNG kiểu Việt** — `01/09/2026`, tiền `145.000.000`.

**Nhưng đang may chứ không phải chắc.** Ứng dụng **không đặt culture** ở đâu cả nên lấy theo máy
đang chạy, mà trong .NET dấu `/` trong `"dd/MM/yyyy"` **không phải ký tự cố định** — nó là chỗ dành
cho dấu phân cách ngày của culture hiện hành. Máy anh cho ra đúng, còn máy chủ (Render/Railway)
thường chạy `en-US` hoặc invariant ⇒ **cùng một hợp đồng xuất ở hai nơi ra hai kiểu ngày và hai kiểu
số tiền**. Với văn bản đem đi ký thì không chấp nhận được.

**Đã ghim cứng culture `vi-VN`** cho mọi chỗ định dạng ngày/số trong `DocumentExportService`
(13 chỗ). Kèm 3 test chứng minh bẫy là thật: cùng chuỗi `"dd/MM/yyyy"`, culture `da-DK` cho ra
`01.09.2026` còn `vi-VN` cho ra `01/09/2026`.

⚠️ **Việc còn treo khi deploy:** phần còn lại của ứng dụng (ngoài xuất văn bản) vẫn chưa ghim
culture. Chưa thấy triệu chứng, nhưng nên đặt `CultureInfo.DefaultThreadCurrentCulture` trong
`Program.cs` trước khi lên Railway.
