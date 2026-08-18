# Bảng kiểm thử — mọi thứ đã làm 05→09/08

> **Cách dùng:** đi từ trên xuống, tick từng dòng. Mỗi dòng ghi **bấm gì** và **phải thấy gì**.
> Dòng nào sai thì ghi lại mã dòng (vd `S-04`) rồi báo — đỡ phải mô tả lại từ đầu.
>
> Cập nhật: **09/08/2026** · BE **125 test** xanh · FE typecheck + build xanh.

---

## 0. Chuẩn bị

```bash
docker compose up -d              # PostgreSQL 16 (cổng 5433)
dotnet run --project FURPMS.API  # http://localhost:5068
npm run dev                      # FE, http://localhost:5173
```

⚠️ Chỉ chạy **một** instance BE. Nếu lỗi lạ, kiểm cổng: `netstat -ano | findstr :5068`.

**Tài khoản**: mọi tài khoản demo dùng chung mật khẩu **`password`** — bảng đầy đủ ở `DEMO_GUIDE.md` §2.

**Dữ liệu**: 8 đề tài `NCKH-2026-001…008`, mỗi cái đứng ở một bước khác nhau (`DEMO_GUIDE.md` §3).

---

## ✅ Đã kiểm bằng API / test tự động — chỉ cần soát nhanh

Những mục dưới đây tôi đã chạy thật bằng API hoặc có test tự động phủ. Nếu ít thời gian thì
**bỏ qua phần này**, tập trung vào §1–§6.

| Mã | Đã kiểm gì | Bằng cách nào |
|---|---|---|
| A-01 | Dữ liệu demo dựng đủ: 10 đề tài · 3 hội đồng × 5 người · 22 phiếu · 3 biên bản · 6 file Word | Truy vấn DB sau khi xoá sạch và seed lại 2 lần |
| A-02 | Quorum 2/3: Thư ký lưu được biên bản #3 (4/5 phiếu, TB 87.25) | API |
| A-03 | Nghiệm thu 3/5 phiếu → **chặn**; bỏ phiếu thứ 4 → **qua** | API |
| A-04 | Số liệu biên bản nghiệm thu: 5 phát ra / 4 thu về / 4 hợp lệ / TB null | API |
| A-05 | Xuất Word hợp đồng BM05 đủ Điều 1–7 + số tài khoản + CCCD | Đọc lại file .docx |
| A-06 | Xuất Word **phụ lục** hợp đồng: chặn 409 khi chưa duyệt, ra file sau khi duyệt, bảng *trước → sau* đúng mốc thời gian | Đọc lại file .docx |
| A-07 | Hồ sơ nghiệm thu đủ thông tin đề tài + file mở được | API, đăng nhập bằng `reviewer3` |
| A-08 | Số tài khoản/CCCD: PI khai → đọc lại bị che → **Staff không thấy số thật** → Word có số đầy đủ → nhật ký ghi đúng | API |
| A-09 | Xoá sản phẩm đã nghiệm thu / đang là minh chứng giải ngân → chặn | API |
| A-10 | Xoá master data đang dùng → chặn; tạo mới rồi xoá → xoá được thật | API |
| A-11 | Cảnh báo quỹ giờ buổi họp: cả 2 nhánh (vượt giờ · còn đề tài chưa xếp) | API, ép dữ liệu rồi seed lại |
| A-12 | Khoá bộ tiêu chí đã chấm: sửa tên · sửa điểm · thêm/xoá tiêu chí đều chặn; **nhân bản rồi sửa bản sao thì được** | API |
| A-13 | Tải nhiều tệp tuần tự + chặn `.exe` | API |

> ⚠️ **Phần A-01…A-13 ở trên kiểm ở tầng API.**
>
> ✅ **Cập nhật 09–12/08 — đã chạy trên trình duyệt thật** (Chrome headless qua Playwright, script ở
> `furpms-web/.e2e/*.mjs`, gitignore): §1 (PI) · §2 (Staff vòng chấm/hội đồng/lịch họp) ·
> §3 (chấm điểm & biên bản) · §4 (hợp đồng, giải ngân, báo cáo tiến độ) · 9 ràng buộc nghiệp vụ ·
> trần kinh phí Điều 14 · tỷ lệ hạng mục Điều 15 · nhãn vai hội đồng.
> Mỗi lần chạy có bắt console error + mọi phản hồi API ≥ 400.
>
> ✅ **Chạy tiếp 12/08:** §5 (duyệt báo cáo tiến độ · hồ sơ nghiệm thu) · §6 (6 màn master data) ·
> §7 (đa vai M-01…M-03). Lỗi tìm được đã sửa trong cùng lượt — xem nhật ký commit ngày 12/08.
>
> ✅ **Rà toàn bộ giao diện 12/08 — `36/36 màn`** của cả 4 vai (`.e2e/ui-sweep.mjs`): soi chữ tiếng
> Anh lọt vào bản Việt, mã enum lòi ra, khoá i18n hiện thô, cộng bắt console error và mọi phản hồi
> API ≥ 400. Kết quả cuối: **0 lỗi nhãn · 0 lỗi API · 0 lỗi console**. Chạy lại bất cứ lúc nào bằng
> `node .e2e/ui-sweep.mjs` — đây là lưới an toàn cho mọi thay đổi giao diện về sau.

---

## ✅ Đã chạy trên trình duyệt 09/08

| Phần | Kết quả |
|---|---|
| **§3 Hội đồng** ⭐ | Chạy trọn vòng: chấm điểm → BM12 đủ 5/5 → Thư ký lưu nháp → Chủ tịch "Duyệt & khoá". **Tìm được 2 lỗi nặng đã sửa** (thiếu `projectId`, cấu hình bước nhảy điểm chưa từng chạy) |
| **9 ràng buộc** | Chặn đúng, thông báo tiếng Việt có dẫn chiếu QĐ543 — chi tiết ở `RANG_BUOC_TREN_MAN_HINH.md` |
| **§1 PI** | Menu · danh sách đề cương · trạng thái Việt hoá · card thông tin hợp đồng che `****NNNN` — **xanh, 0 lỗi** |
| **§4 Hợp đồng** | Danh sách 3 hợp đồng · chi tiết có 7 tab (Tiến trình · Giải ngân · Sản phẩm · Báo cáo tiến độ · Báo cáo tổng kết · Điều chỉnh · Quyết toán) · nút "Xuất hợp đồng (Word)" · **không còn "Phạm vi ký"** — **xanh, 0 lỗi** |

**Chưa chạy tay:** thao tác ghi thật ở §1/§4 (nộp đề cương từ wizard, upload nhiều tệp, xuất Word,
duyệt báo cáo) — mới kiểm màn hiện đúng chứ chưa bấm hết.

## 1. PI — nộp đề cương

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| P-01 | `pi.demo` → **"Đề cương của tôi"** | Thấy các đề tài của mình, trạng thái tiếng Việt (không còn `SUBMITTED`, `DRAFT`…) |
| P-02 | Mở `NCKH-2026-001` (bản nháp) → đi hết wizard 5 bước → **Nộp** | Nộp được, trạng thái đổi sang "Đã nộp" |
| P-03 | Tạo đề tài mới → **upload file Word** | AI trích xuất → prefill form → sửa được → nộp |
| P-04 | Ở tab tài liệu, **chọn 3–4 tệp một lượt** | Nút hiện `2/4`, xong báo "Đã tải lên N tệp"; danh sách có đủ |
| P-05 | Chọn kèm một tệp `.exe` | Báo **đích danh** tệp bị loại + lý do; các tệp còn lại **vẫn tải bình thường** |
| P-06 | Sửa/xoá thành viên nhóm khi đề cương **còn nháp** | Sửa/xoá được |
| P-07 | Thử xoá **chính mình (chủ nhiệm)** khỏi nhóm | Bị chặn: *"không xoá được chủ nhiệm đề tài"* |
| P-08 | Mở đề tài **đã nộp** → thử sửa thành viên | Bị chặn, chỉ đường sang BM07 |
| P-09 | `NCKH-2026-004` (yêu cầu chỉnh sửa) → sửa & nộp lại | Nộp lại được, vòng chấm mở lại |
| P-10 | **Menu avatar góc phải → "Hồ sơ"** (không nằm ở thanh trái) → card **"Thông tin để lập hợp đồng"** → khai số tài khoản + CCCD | Lưu xong hiện **`****7890`**, không hiện số đầy đủ |
| P-11 | Bấm "Cập nhật" lại card đó | Ô nhập **để trống**, không đổ sẵn `****7890` |
| P-12 | Nhập CCCD 5 chữ số | Báo lỗi ngay tại ô, chưa gửi đi |

## 2. Staff — vòng chấm & hội đồng

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| S-01 | `staff.demo` → "Hội đồng & Chấm" → **Tạo vòng chấm** | Loại vòng chỉ có **Xét duyệt đề cương** / **Nghiệm thu**; **không còn** ô "Phương diện = Tài chính" |
| S-02 | Gán `NCKH-2026-002` vào vòng vừa tạo | Gán được |
| S-03 | Lập hội đồng → gán **4 người** | Cảnh báo **số chẵn** ngay lúc gán, không đợi tới lúc bấm gửi mời |
| S-04 | Gán đủ **5 người** nhưng **chưa có lịch họp** → bấm "Gửi thư mời" | Bị chặn, nói rõ thiếu lịch họp |
| S-05 | Thêm lịch họp (offline → **bắt buộc địa điểm**; online → link) → "Gửi thư mời" | Gửi được |
| S-06 | Mở hội đồng của `NCKH-2026-003` (3 đề tài) | Có tab **"Lịch chấm"**; banner vàng cảnh báo quỹ giờ nếu không đủ |
| S-07 | Mở hội đồng chỉ có **1 đề tài** | **Không có** tab "Lịch chấm" |
| S-08 | Ở "Lịch chấm", đặt khung giờ **tràn ra ngoài** buổi họp | Bị chặn kèm giờ cụ thể |
| S-09 | Đặt 2 khung giờ **chồng nhau** | Bị chặn, nêu đề tài bị đụng |
| S-10 | Thử thêm thành viên là **người trong nhóm nghiên cứu** của chính đề tài đó | Bị chặn (COI) |

## 3. Hội đồng — chấm điểm & biên bản ⭐

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| R-01 | `reviewer1` (Chủ tịch) → "Đề tài được phân công" → `NCKH-2026-003` → **chấm điểm** | 5 tiêu chí BM03 (10+20+40+20+10), ô nhập **chỉ cho số nguyên** |
| R-02 | Nhập `7.5` vào một tiêu chí | Bị chặn kèm câu chỉ đường "Phòng QLKH đổi được ở Cấu hình hệ thống" |
| R-03 | Nhập điểm **vượt trần** một tiêu chí | Bị chặn |
| R-04 | Xem tab **"Thông tin đề tài"** | Thấy đủ mục tiêu/phương pháp/sản phẩm PI nhập, không chỉ mỗi file |
| R-05 | `reviewer2` (Thư ký) → tab **Biên bản** | Bảng phiếu **từng người có tên**: vai · điểm · đã nộp chưa (BM12 §10.1) |
| R-06 | Thư ký **soạn biên bản** | Lưu được (đã đủ 4/5 phiếu) |
| R-07 | `reviewer1` → **Chốt biên bản** | Khoá lại, trạng thái đề tài đổi |
| R-08 | Sau khi chốt, `reviewer2` thử sửa điểm | Bị chặn: *"Biên bản đã được Chủ tịch chốt"* |
| R-09 | `reviewer4` thử **soạn biên bản** (không phải Thư ký) | Bị chặn |
| R-10 | `NCKH-2026-007` → `reviewer3` (Phản biện) bỏ phiếu **Đạt/Không đạt** | Chỉ có 2 lựa chọn, **không có ô điểm** (BM11) |
| R-11 | Thư ký thử soạn biên bản nghiệm thu khi mới **3/5 phiếu** | Bị chặn kèm dẫn chiếu **QĐ543 Điều 8.3.b** |
| R-12 | Bỏ thêm 1 phiếu (đủ 4/5) → soạn lại | Lưu được; số liệu hiện **5 phát ra / 4 thu về**, **không có** điểm trung bình |

## 4. Staff — hợp đồng & giải ngân

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| C-01 | `NCKH-2026-005` (đã duyệt, chưa có HĐ) → **Lập hợp đồng** | Form **không còn** ô "Phạm vi ký" |
| C-02 | Nhập gia hạn tối đa **9 tháng** cho đề tài 12 tháng | Bị chặn: tối đa **6** (QĐ543 Điều 10.4) |
| C-03 | **Xuất Word** hợp đồng | Đủ căn cứ pháp lý + Bên A/B + **Điều 1–7** + bảng sản phẩm + bảng giải ngân + ô ký |
| C-04 | Xem phần Bên B trong file | Có **số tài khoản + CCCD** của PI (nếu PI đã khai ở P-10) |
| C-05 | Upload **nhiều tệp** bản hợp đồng đã ký một lượt | Nút hiện tiến độ, tải xong đủ |
| C-06 | `NCKH-2026-006` → PI gửi **đề nghị gia hạn** → Staff **duyệt** | Duyệt được |
| C-07 | Sau khi duyệt → **Xuất phụ lục hợp đồng** | Ra .docx có bảng *trước → sau* ghi **mốc thời gian thật**, không phải `0 → 3` |
| C-08 | Thử xuất phụ lục cho đơn **chưa duyệt** | Bị chặn |
| C-09 | Thử xác nhận **đợt giải ngân cuối** khi đề tài chưa nghiệm thu | Bị chặn kèm **BM05 Điều 4.2** |
| C-10 | Thử lập **quyết toán** khi còn đợt chưa chi | Bị chặn, **liệt kê số đợt** còn treo |
| C-11 | Thử xoá sản phẩm **đang là minh chứng** của một đợt giải ngân | Bị chặn, nêu **số đợt** |
| C-12 | Tab **"Tiến trình"** của hợp đồng | Timeline mốc, click mở được minh chứng |

## 5. Staff — báo cáo tiến độ & nghiệm thu

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| B-01 | `NCKH-2026-006` → **duyệt báo cáo tiến độ** kỳ 1 | Duyệt được (báo cáo này có link) |
| B-02 | Tạo một kỳ báo cáo **không có file lẫn link** → thử duyệt | Bị chặn: *"chưa có gì để đọc thì chưa đánh giá được"* |
| B-03 | PI thử nộp **kỳ 2** khi kỳ 1 chưa được duyệt | Bị chặn (Điều 10.1 — kỳ phải tuần tự) |
| B-04 | Xoá một kỳ báo cáo **còn nháp** | Xoá được |
| B-05 | Thử xoá kỳ báo cáo **đã nộp** | Bị chặn |
| B-06 | `NCKH-2026-007` → mở **hồ sơ nghiệm thu** (vai hội đồng) | Có đủ thông tin đề tài · thành viên · hợp đồng · **từng kỳ báo cáo ghi ai duyệt / vai gì** |
| B-07 | Bấm vào tên file trong hồ sơ nghiệm thu | **Tải được thật**, không phải chỉ hiện tên |
| B-08 | Thử nộp lại sản phẩm đã **nghiệm thu Đạt** | Bị chặn |

## 6. Admin — cấu hình & master data

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| D-01 | Cấu hình hệ thống → đổi `SCORE_DECIMAL_PLACES` = 1 | Ô nhập điểm ở màn chấm cho nhập `7.5`; phiếu **đã chấm không đổi** |
| D-02 | Bộ tiêu chí → xem danh sách | Bộ đã dùng chấm hiện **đang khoá** kèm số phiếu |
| D-03 | Thử sửa tên bộ **đã khoá** | Bị chặn, chỉ đường sang **"Nhân bản"** |
| D-04 | **Nhân bản** rồi sửa bản sao | Sửa được |
| D-05 | Tạo bộ tiêu chí tổng **125 điểm** rồi thử chấm bằng nó | Bị chặn (BM03 phải cộng đúng 100) |
| D-06 | Xoá đơn vị / loại sản phẩm / hạng mục chi **đang được dùng** | Bị chặn, **liệt kê chỗ đang dùng** |
| D-07 | Tạo mới một loại sản phẩm rồi xoá | Xoá được |
| D-08 | Tạo đợt mới | Ô năm ghi **"Năm"** (không phải "Năm học"), gợi ý **`2026`**; gõ `2025-2026` bị chặn tại chỗ |
| D-09 | Xoá loại đề tài đang được đợt dùng | Bị chặn, bảo vô hiệu hoá |

## 7. Đa vai — cần tài khoản có ≥2 vai

> ✅ **Từ 12/08 data demo đã có sẵn:** `staff.demo@furpms.edu.vn` mang **2 vai** — Cán bộ (Phòng
> QLKH) + Giảng viên (`DatabaseSeeder.SeedMultiRoleAccountAsync`). Đăng nhập là thấy ngay dropdown
> đổi vai trong menu avatar. **Đã chạy trên trình duyệt 12/08: M-01…M-03 đạt.**

| Mã | Bấm gì | Phải thấy gì |
|---|---|---|
| M-01 | Dropdown vai ở header | Chỉ hiện vai người đó **thực có** |
| M-02 | Chuyển sang vai **Giảng viên** | Menu đổi sang PI |
| M-03 | Vẫn ở vai Giảng viên → **gõ thẳng URL** `/contracts` | Trang **"Bạn không có quyền truy cập"**. *(Sửa 12/08: guard trước đây soi toàn bộ vai người đó có nên vẫn vào được, thấy hợp đồng của mọi chủ nhiệm và còn nguyên nút "Tạo hợp đồng")* |
| M-04 | Chuyển lại vai Staff | Nút quay lại |

## 8. Toàn cục

| Mã | Kiểm gì | Phải thấy gì |
|---|---|---|
| G-01 | Rà mọi màn đã đi qua | **Không có chữ tiếng Anh** nào trong thông báo lỗi |
| G-02 | Đổi ngôn ngữ vi ↔ en | Không có key thô kiểu `reports.chooseFiles` lòi ra |
| G-03 | Chế độ sáng/tối | Mặc định sáng, đổi được |
| G-04 | Mở Console trình duyệt suốt buổi test | Không có lỗi đỏ |
| G-05 | Tab Network | Không có API nào trả **500** |

---

## Chỗ tôi biết là còn mỏng

Ghi ra để cậu test kỹ hơn ở đây, hoặc chấp nhận rủi ro có ý thức:

| Chỗ | Vì sao mỏng |
|---|---|
| ~~Toàn bộ giao diện~~ | **Đã bấm thử 09–12/08** — xem ghi chú ở §A. Còn lại: §5 nghiệm thu · §6 Admin · §7 đa vai |
| **Đề cương lưu trước 12/08** | Dự toán cũ có thể còn tiền ở hạng mục ngoài QĐ543 Điều 15. Form **có hiện** dòng đó kèm nhãn "hạng mục cũ" (sửa 12/08 sau khi test thấy nó vô hình mà vẫn cộng vào tổng), nhưng đề cương như vậy **không nộp được** cho tới khi chuyển tiền sang 06 hạng mục hiện hành |
| Luồng AI (B1–B5) | Phụ thuộc mạng + Gemini; chưa test bằng dữ liệu thật quy mô |
| COI · trùng lịch giảng viên | Có code, **chưa có test tự động** |
| Sinh Word hợp đồng/phụ lục | Đã kiểm bằng file thật nhưng **chưa có test tự động** cho hợp đồng gốc |
| Đa vai (§7) | Data demo chưa có tài khoản đa vai sẵn |
| Thông báo / email | `EMAIL_ENABLED` đang bật — cẩn thận gửi mail thật khi test |

## Việc còn treo (không phải lỗi, là chưa làm)

| Việc | Vì sao chưa |
|---|---|
| **Chốt 4 loại điều chỉnh** + quan hệ với BM07 | Cần quyết định nghiệp vụ — hiện có **2 cơ chế song song** (`AmendmentRequest` + `ProposalChangeRequest`), cả hai đều có UI |
| **B6** — AI chạy local thay Gemini | Chờ ý thầy: máy demo phải đủ khoẻ, chất lượng kém hơn rõ |
| **E5** — "UI/UX sửa lại nhiều" | Ghi chú quá chung, cần hỏi thầy chỉ màn nào |
| **F6** — "viết doc" | Cần biết thầy muốn SRS / báo cáo đồ án / doc kỹ thuật |
| 4 việc UI lớn | Trang chi tiết đề tài riêng · màn quản lý hội đồng · gom nhóm màn Xét duyệt · lọc hợp đồng |
| Chuẩn hoá zod FE · thùng rác · i18n màn cũ | Nhỏ, ít nhìn thấy |
| **E8** — đổi hết mật khẩu thành `password` | Chưa chốt; đổi thì phải sửa mọi doc đang chép mật khẩu |

---

## Liên quan

- Tài khoản + kịch bản diễn ①→⑧: `DEMO_GUIDE.md`
- Quy tắc nghiệp vụ (luật → căn cứ QĐ543 → dòng code): `BUSINESS_RULES.md`
- Danh sách 43 việc từ note demo 05/08: `PLAN_Week13_Demo_0508.md`
