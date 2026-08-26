# 📇 Mục lục tài liệu — dùng cái nào, khi nào

> Xếp theo **tác dụng thực tế**, không theo bảng chữ cái. Đọc từ trên xuống: tầng 1 là thứ **phải
> tuân theo**, tầng 5 là thứ chỉ tra khi cần.
>
> Cập nhật 25/08/2026. Mỗi lần thêm/xoá file trong `docs/` phải sửa file này.
>
> 18/08: hai repo đã gom về `D:\capstone\newroot\` (`FURPMS_BEv2` + `furpms-web`). Mọi đường dẫn
> `core/FURPMS-Web` trong docs đã được sửa theo — gặp lại chỗ nào còn sót thì sửa luôn.

---

## 🥇 Tầng 1 — NGUỒN SỰ THẬT. Code sai so với đây là code SAI

| File | Là gì | Khi nào mở |
|---|---|---|
| **`QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx`** | **Văn bản pháp quy của trường.** Toàn bộ nghiệp vụ của đồ án phải khớp với nó: quy trình, thẩm quyền, thời hạn, 12 biểu mẫu (BM01–BM12) | **Trước khi code bất kỳ luồng nghiệp vụ nào**, và mỗi khi có tranh cãi "cái này đúng chưa" |
| `Mau-1_Thuyet-minh-khoa-hoc_V-14082020_v5.docx` | Mẫu thuyết minh đề cương (BM01) — đúng từng mục PI phải điền | Khi sửa wizard nộp đề cương, hoặc export thuyết minh |
| `NH_Son_Mau 3-Du toan kinh phi_Final-Tri_v5_final.xlsx` | Mẫu dự toán kinh phí (BM03) | Khi sửa phần dự toán / export Excel |
| **`RANG_BUOC_TREN_MAN_HINH.md`** | **Ràng buộc nhìn từ màn hình** — định làm gì → bị chặn thế nào → phải làm gì mới đi tiếp. Câu chữ **nguyên văn chụp từ màn hình thật**. Kèm kịch bản "khoe ràng buộc" 3 phút | Khi bị hỏi *"sao bấm không được?"*, và để chuẩn bị câu trả lời cho hội đồng |
| **`KE_HOACH_BAO_VE_LAN2.md`** | **★★★ DANH SÁCH VIỆC HIỆN HÀNH (25/08).** Nhóm bị cho **bảo vệ lần 2**. Biên bản 4 điểm + 3 góp ý miệng đã chốt → 7 nhóm việc, mỗi nhóm ghi rõ sửa file nào, migration gì, test gì. Kèm bảng **hiện trạng đã xác minh bằng cách đọc code** và phần quy ước bắt buộc cho AI mới | **Đầu mỗi phiên**, ngay sau `HANDOFF_HIEN_HANH.md`. Đây là thứ thay `PLAN_Week13` ở vai trò "đang làm gì" |
| **`HANDOFF_HIEN_HANH.md`** | **★★ ĐỌC ĐẦU TIÊN nếu bạn/AI mới tiếp nhận.** Trạng thái hiện hành: đã đổi sang **PostgreSQL**, đã deploy Railway, 3 bẫy deploy đã gặp thật, việc còn lại, và câu mẫu để bàn giao cho AI khác. Thay cho `HANDOFF_Week10.md` ở phần hạ tầng | **Đầu mỗi phiên mới**, và khi bàn giao |
| **`KICH_BAN_DEMO.md`** | **★ Kịch bản demo — mở file này trước mỗi buổi demo.** Bấm theo từng bước: chuẩn bị · tài khoản · luồng chính 12 phút · chỗ dễ vấp · câu hội đồng hay hỏi kèm câu trả lời sẵn | **Ngay trước khi demo**, và khi cần đi lại luồng chính để test |
| **`DEMO_SCRIPT_SUBMISSION.md`** | **Bản Demo Script để nộp trước buổi bảo vệ:** danh sách UC demo · thứ tự 8 workflow · phân công 4 người · tài khoản/data test · lời thoại chuyển phần · phương án dự phòng · checklist ký xác nhận | Khi chốt kịch bản với nhóm và khi nộp tài liệu demo cho giảng viên/hội đồng |
| **`SOFTWARE_PACKAGE_INSTALLATION_GUIDE.md`** | **Hướng dẫn cài đặt gói phần mềm:** đặt cạnh BE/FE khi nộp; có cấu trúc package, phiên bản, lệnh cài, migration/seed, cách xuất/restore sample SQL, tài khoản theo vai và checklist loại bí mật/file sinh | Khi đóng gói mục 3 “Software Package” và khi kiểm tra cài trên máy sạch |
| **`GOPY_Thay_Demo_1408.md`** | **Góp ý của thầy buổi demo 14/08** — 10 mục, mỗi mục ghi *thầy nói gì → hiểu thành việc gì → trạng thái* | Trước khi sửa bất cứ gì liên quan tới góp ý sau demo |
| **`TEST_CHECKLIST.md`** | **Bảng kiểm thử** — mọi thứ đã làm 05→09/08, chia theo vai (PI · Staff · Hội đồng · Admin), mỗi dòng ghi *bấm gì → phải thấy gì*. Kèm mục "chỗ còn mỏng" và "việc còn treo" | **Trước mỗi buổi demo** và khi cần test lại sau thay đổi lớn |
| **`BUSINESS_RULES.md`** | **Bảng tra quy tắc nghiệp vụ** — 91 luật chia 6 nhóm: luật → căn cứ (điều/biểu mẫu QĐ543 hoặc buổi chốt với thầy) → **nơi thực thi trong code** → mã lỗi. Kèm mục "đã rà và KHÔNG có luật" + độ phủ test | Khi bị hỏi *"quy tắc này ở đâu ra?"*, trước khi sửa một ràng buộc, và khi ôn bảo vệ |
| **`QD543_Compliance.md`** | **Bảng đối chiếu QĐ543 ↔ code**: từng biểu mẫu map vào entity/endpoint nào, đã làm chưa. §2 map từng mục BM04 ↔ field BE | Mở **cùng lúc** với file .docx ở trên — nó là bản dịch từ văn bản luật sang code |

> ⚠️ File `.docx`/`.xlsx` đọc bằng cách bóc XML (xem lệnh mẫu ở cuối file này), đừng đoán nội dung.

---

## 🥈 Tầng 2 — ĐANG LÀM GÌ. Mở đầu mỗi phiên

| File | Là gì | Khi nào mở |
|---|---|---|
| **`PLAN_Week13_Demo_0508.md`** | *(đã bị `KE_HOACH_BAO_VE_LAN2.md` thay ở vai trò "đang làm gì" — giữ để tra lịch sử)* 43 đầu việc gộp từ 2 note của nhóm sau demo 05/08, có trạng thái ✅/🔶/⬜/❓ từng mục + thứ tự làm + 8 câu phải hỏi thầy | **Đầu mỗi phiên.** Đây là danh sách việc hiện hành |
| `PLAN_Week12.md` | Kế hoạch sau demo 29/07 (P0–P8) + rà CRUD toàn hệ thống + các mục "chờ user quyết định" (Q1–Q5) | Khi cần biết lý do một quyết định cũ, hoặc tra Q1–Q5 |
| `PROGRESS.md` | **% hoàn thiện theo 8 nhóm chức năng** + "còn thiếu gì" | Khi cần trả lời "xong bao nhiêu %" |
| `README.md` | Backlog tổng + mục lục sống + thứ tự đọc cho người mới | Khi onboard người mới |
| **`Cam-nang-tranh-loi-Capstone-SE.pdf`** | **Cẩm nang tránh lỗi bảo vệ Capstone** (19 trang): 6 nguyên nhân không đạt, lỗi hardcode, lỗi AI, lỗi BR, checklist D-14/D-7/D-1 | **Trước mỗi mốc Review** và khi chuẩn bị bảo vệ. Xem §"Đối chiếu cẩm nang" bên dưới |

---

## 🥉 Tầng 3 — CÁCH HỆ THỐNG CHẠY. Tra khi code

| File | Là gì | Khi nào mở |
|---|---|---|
| `Process_Spec_v2.md` | **Luồng nghiệp vụ**: từng giai đoạn, ai làm gì, sản phẩm đầu ra, state machine | Trước khi code một flow |
| `API_CONTRACT.md` | **Giao kèo FE↔BE**: mọi endpoint, quyền, request/response, ghi chú thay đổi | Khi thêm/sửa endpoint, hoặc FE hỏi "gọi cái gì" |
| `ERD_v3_Project_Centric.dbml` | **Sơ đồ DB duy nhất** (~56 bảng, Project-centric) | Khi đụng schema |
| `Review2_Diagrams.md` · `RP4_Diagrams.md` | Use case / sequence / activity / state machine | Khi sửa luồng thì phải sửa theo |
| `SYSTEM_REVIEW.md` | Rà soát hệ thống + danh sách mã **CỐ Ý fix cứng** (không linh hoạt hoá) | ⚠️ Quan trọng khi bị hỏi về hardcode — xem §cẩm nang |

---

## 4️⃣ Tầng 4 — VẬN HÀNH & BÀN GIAO

| File | Là gì |
|---|---|
| `HANDOFF_Week10.md` | Bàn giao: chạy dự án thế nào, lỗi thường gặp khi clone (env FE, docker SQL) |
| `Review2_Tech_Stack.md` | Stack đang dùng và vì sao |
| `DEMO_GUIDE.md` | **Hướng dẫn chạy + kịch bản demo** (viết lại 08/08, thêm §8 ngày 26/08): tài khoản · bảng 8 đề tài demo và mỗi cái đứng ở bước nào · kịch bản ①→⑧ đóng vai nào bấm gì · §8 tính năng bảo vệ lần 2 (ngân sách, hạn/deadline, sổ quyết định, AI rà trùng lặp, chuyên môn hội đồng, cảnh báo lệch điểm). Đọc trước mỗi buổi demo |
| `EXPORT_TEST_GUIDE.md` | Cách test chức năng export |
| `FE_PROGRESS.md` | Tiến độ FE (ảnh chụp cũ) |

---

## 5️⃣ Tầng 5 — LỊCH SỬ. Chỉ tra khi cần biết "vì sao hồi đó làm vậy"

| File | Là gì |
|---|---|
| `DB_Redesign_v3_PostReview2.md` | Lý do refactor sang Project-centric sau Review 2 |
| `FURPMS_DB_Change_Spec_v1.4.md` | Spec đổi DB bản cũ |
| `DB_ANALYTIC_REPORT.md` | Phân tích DB (dài, tham khảo) |
| `Review2_Context_Handoff.md` | Ngữ cảnh bàn giao Review 2 |
| `Team_Contribution_Week1-8.md` | Đóng góp từng người tuần 1–8 |

---

## 🔍 Đối chiếu cẩm nang tránh lỗi ↔ hiện trạng dự án

Cẩm nang nêu **6 nguyên nhân không đạt**. Đối chiếu với FURPMS:

| # | Nguyên nhân (cẩm nang) | FURPMS đang thế nào |
|---|---|---|
| 1 | **Demo lỗi ở mainflow / không demo hết luồng chính** — *rất cao* | 🔶 Luồng lõi chạy thông, nhưng buổi 05/08 vẫn vấp. `PLAN_Week13` nhóm 1 chính là để xử. **Data demo (E7) + kịch bản (F5) đã xong 08/08** — 8 đề tài đứng ở 8 bước, kịch bản ①→⑧ ở `DEMO_GUIDE.md` §3–4. Chính việc dựng data đã lôi ra 2 lỗi chặn màn nghiệm thu (A12/A13) |
| 2 | **Hardcode tham số nghiệp vụ** — *rất cao* | 🔶 Đã có bảng `system_settings` + màn Admin (giới hạn upload, đại diện Bên A…). **Nhưng**: cẩm nang nói rõ *"sửa file appsettings rồi restart vẫn bị coi là hardcode"*, và câu hỏi kinh điển là *"đổi con số này rồi demo ngay"*. Phải rà lại mọi magic number: số kỳ báo cáo, tỷ lệ giải ngân 30/30/30, trần gia hạn ½, tổng điểm 100, số thành viên hội đồng |
| 3 | **Tài liệu sai/thiếu, không dùng template chính thức** — *rất cao* | ⬜ Chưa dùng template FLM. Cẩm nang nhấn: *"phần lớn nhóm rơi vào đạt-nhưng-phải-nộp-lại-tài-liệu"* |
| 4 | **Nghiệp vụ không sát thực tế, thiếu ràng buộc ngoại lệ** — *cao* | 🔴 Đúng chỗ đau: `PLAN_Week13` §F1 *"VALIDATE LẠI TOÀN BỘ"*, và 5 lỗi nghiệp vụ ở nhóm 1 |
| 5 | **Không tiếp thu góp ý các lần Review** — *cao* | ✅ Đang làm tốt: mỗi buổi đều có file plan riêng, đánh dấu trạng thái từng ý. **Cẩm nang khuyên lập bảng: góp ý → đã xử lý chưa → bằng chứng, mang đi bảo vệ** — nên làm |
| 6 | GVHD không duyệt / thành viên đóng góp quá ít | ❓ Ngoài phạm vi code. Cẩm nang cảnh báo: *"Git tổ chức lộn xộn ⇒ không có căn cứ chứng minh đóng góp cá nhân"*, và **cá nhân đóng góp ít thì trượt riêng dù nhóm đạt** |

### Ba mục cẩm nang đánh trúng chỗ FURPMS đang yếu

**① Business Rule "viết cho có"** — cẩm nang liệt kê đúng bệnh của mình:
> *"BR viết ra nhưng code không hề kiểm tra"* · *"BR mô tả sai so với thứ đã hiện thực — người chấm đọc BR rồi thử ngay trên app và phát hiện lệch"*

Ví dụ sống: **tổng điểm bộ tiêu chí = 100 nhưng tạo được hơn 100** (`PLAN_Week13` A11). Cách cẩm nang khuyên: *"rà ngược từ code ra BR — mọi câu lệnh `if` mang tính nghiệp vụ đều phải có một BR tương ứng, và ngược lại."*

**② Con số trên UI không giải thích được** — cẩm nang cảnh báo bị chỉ vào một con số và hỏi công thức. FURPMS có: điểm trung bình hội đồng · % hoàn thành báo cáo · tỷ lệ giải ngân từng đợt · trần gia hạn. Phải chuẩn bị **file công thức kèm ví dụ số cụ thể**.

**③ AI** — cẩm nang liệt kê nhiều lỗi mình đang dính hoặc suýt dính:
- *"Không có actor duyệt lại kết quả do AI sinh ra"* → ✅ có (PI review bản AI prefill; Chủ tịch chốt biên bản).
- *"Không quản lý giới hạn token / chi phí AI"* → ⬜ **chưa có**.
- *"Không có metric đánh giá chất lượng đầu ra"* → ⬜ **chưa có**.
- *"Model chỉ đúng với đúng một mẫu đã chuẩn bị sẵn"* → phải demo **nhiều mẫu file khác nhau**.
- *"Đưa AI vào chỗ mà một truy vấn đơn giản đã giải quyết tốt hơn"* → lý do khuyến nghị **bỏ semantic search cho việc TÌM KIẾM**, thay bằng tìm kiếm nâng cao (`PLAN_Week12` Q1).
  ⚠️ **Đảo ngược một phần, 26/08:** hạ tầng embedding nay ĐÃ LÀM, nhưng cho **rà trùng lặp đề cương** (gạch 3 của biên bản bảo vệ lần 2) — chỗ mà truy vấn từ khoá thực sự không giải được, vì đề cương trùng thường được viết lại hoàn toàn. Xem `AI_Duplicate_Detection.md`. Khuyến nghị bỏ semantic search cho ô tìm kiếm vẫn giữ nguyên.

---

## 🛠 Lệnh mở file gốc (đừng đoán nội dung)

```bash
# Đọc .docx (QĐ543, Mẫu 1)
python -c "
import zipfile,re,sys; sys.stdout.reconfigure(encoding='utf-8')
z=zipfile.ZipFile('docs/QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx')
x=z.read('word/document.xml').decode('utf-8')
print(re.sub(r'<[^>]+>','',re.sub(r'</w:p>','\n',x)))
"

# Đọc .pdf (cẩm nang) — cần: pip install pypdf
python -c "
import sys; sys.stdout.reconfigure(encoding='utf-8')
from pypdf import PdfReader
print('\n'.join((p.extract_text() or '') for p in PdfReader('docs/Cam-nang-tranh-loi-Capstone-SE.pdf').pages))
"
```

---

*Xếp lại 06/08/2026. Trước đó `docs/` có 24 file phẳng, không biết cái nào quan trọng hơn cái nào.*

- `THONG_BAO_VA_EMAIL.md` — 8 loại thông báo (kích hoạt khi nào, gửi cho ai, có kèm mail không) + phần chưa có.
