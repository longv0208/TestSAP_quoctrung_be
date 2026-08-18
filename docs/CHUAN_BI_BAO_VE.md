# Chuẩn bị bảo vệ — slide, demo, hỏi đáp

> Nguồn: `Cam-nang-tranh-loi-Capstone-SE.pdf` (19 trang) — mọi mục có ghi số trang để tra ngược.
> Phần **slide** cẩm nang KHÔNG có mục riêng; nội dung dưới đây suy ra từ quy tắc demo (§7) và
> hỏi đáp (§8). Phần còn lại trích thẳng, không diễn giải thêm.

---

## 0. Ba câu cần thuộc trước mọi thứ khác

Cẩm nang trang 18–19 chốt lại bằng ba lời khuyên, và đây là ba câu quyết định điểm nhiều hơn cả chất lượng code:

1. **Trung thực ăn điểm cao hơn che giấu.** Nhóm chủ động nói ra hạn chế được đánh giá tốt; nhóm lướt qua lỗi hoặc trả lời vòng vo **bị trừ nặng**.
2. **Buổi Review là cơ hội sửa sai, không phải thủ tục.** "Không tiếp thu góp ý của các lần Review" là **lý do trực tiếp dẫn tới không đạt**.
3. **Tài liệu là phần dễ ăn điểm nhất và mất điểm nhiều nhất.** Sản phẩm chạy tốt nhưng sơ đồ sai, thiếu trang, sai template ⇒ vẫn phải làm lại và nộp lại.

Và 6 nguyên nhân khiến nhóm **không đạt** (trang 2), xếp theo mức phổ biến — đứng đầu là:

> **Demo lỗi ở mainflow, hoặc không demo hết luồng chính trong thời gian quy định.** *(Rất cao)*

⇒ Mọi công sức chuẩn bị nên dồn vào **demo chạy trơn hết mainflow trong thời gian cho phép**, trước khi lo đến slide đẹp.

---

## 1. Slide

### 1.1. Nguyên tắc chi phối

Cẩm nang §7.1 (trang 12) liệt kê đây là **lỗi**:

> *"Trình bày xong toàn bộ lý thuyết rồi mới demo tất cả."*

⇒ **Không làm bộ slide lý thuyết rồi demo ở cuối.** Slide phải **xen kẽ**: nói tới luồng nào thì demo ngay luồng đó, xong quay lại slide cho luồng tiếp theo.

Hệ quả về cấu trúc: mỗi luồng cần **đúng 1 slide dẫn** (nói trong ~1 phút) rồi chuyển sang màn hình thật. Slide không lặp lại thứ demo sẽ chiếu.

### 1.2. Bộ slide đề xuất

| # | Slide | Ai nói | Sau slide này thì… |
|---|---|---|---|
| 1 | Bìa — tên đề tài, nhóm, GVHD | Người 1 | |
| 2 | **Bài toán**: quy trình NCKH cấp trường theo QĐ 543 đang chạy bằng giấy + email | Người 1 | |
| 3 | **Định vị sản phẩm**: hệ thống nội bộ một trường, 4 vai trò, vòng đời 6 bước | Người 1 | → demo đăng nhập + đổi vai |
| 4 | **Kiến trúc** (1 slide, sơ đồ khối trỏ được tới thư mục code) | Người 1 | |
| 5 | Luồng 1 — Mở đợt nghiên cứu | Người 1 | → **demo ngay** |
| 6 | Luồng 2 — Nộp đề cương (2 đường: nhập tay / AI trích xuất) | Người 2 | → **demo ngay** |
| 7 | Luồng 3 — Mở vòng chấm & lập hội đồng | Người 3 | → **demo ngay** |
| 8 | Luồng 4 — Chấm điểm & biên bản (nhấn: Chủ tịch chốt, máy không tự đếm phiếu) | Người 4 | → **demo ngay** |
| 9 | Luồng 5 — Hợp đồng · tiến độ · gia hạn | Người 5 | → **demo ngay** |
| 10 | Luồng 6 — Nghiệm thu 2 tầng (BM10 phản biện · BM11 bỏ phiếu) | Người 5 | → **demo ngay** |
| 11 | **AI trong hệ thống** — 4 câu bắt buộc (xem §3.5) | Người 2 hoặc 4 | |
| 12 | **Hạn chế & hướng phát triển** — nói thẳng cái chưa làm | Người 1 | |
| 13 | Cảm ơn | | |

**13 slide.** Cẩm nang không quy định số lượng; con số này bám theo nguyên tắc "1 slide dẫn cho 1 luồng".

### 1.3. Slide "Hạn chế" — đừng bỏ

Đây là chỗ áp dụng lời khuyên số 1. Nói trước những gì mình biết là thiếu, hội đồng sẽ không phải "bắt" được:

- **BM14/BM15** (hợp đồng thuê khoán chuyên môn) — ngoài phạm vi; QĐ543 Điều 9.3 ghi *"nếu cần"* nên là nhánh tuỳ chọn
- **BM07** (phiếu đề nghị thay đổi) — dữ liệu có đủ, chưa xuất được ra Word
- Hệ thống **không quản tiền**, chỉ theo dõi mốc giải ngân + minh chứng *(đây là quyết định có chủ đích, không phải thiếu sót — nói rõ vậy)*

---

## 2. Demo

### 2.1. Năm nguyên tắc (cẩm nang §7.2, trang 12)

1. **Dữ liệu mẫu đầy đủ, thật, đa dạng** — có lịch sử, nhiều user, nhiều trạng thái.
2. **Viết kịch bản, phân vai rõ, chạy thử ít nhất 2 lần CÓ BẤM GIỜ.**
3. **Không bao giờ lướt qua lỗi.** Dừng lại, nói rõ: *"Đây là lỗi X, nguyên nhân Y, nhóm đã ghi nhận."*
4. **Đừng sửa hệ thống cho dễ demo.** Cần demo tình huống phụ thuộc thời gian thì **chỉnh giờ server**, không gỡ ràng buộc.
5. **Chuẩn bị phương án dự phòng**: video ghi màn hình mainflow.

> Điểm 4 nhóm đã có sẵn: hệ thống có công cụ **tua thời gian** (Admin, nút góc dưới trái, +1/+7/+30/+90 ngày). Dùng nó thay vì sửa dữ liệu.

### 2.2. Bẫy demo cụ thể cẩm nang nêu (trang 12)

| Bẫy | Áp vào nhóm mình |
|---|---|
| **Dùng chung một tài khoản cho nhiều vai** → không chứng minh được phân quyền | Nhóm có 9 tài khoản riêng. **Mở sẵn từng phiên ở cửa sổ ẩn danh riêng**, đặt tên tab theo vai |
| Kịch bản lộn xộn, nhảy giữa nghiệp vụ không liên quan | Đi đúng thứ tự vòng đời, không nhảy cóc |
| Có chức năng đã làm nhưng **quên demo** | Phần mạnh nhất là **nghiệm thu 2 tầng** — đừng để nó rơi vào phút cuối rồi hết giờ |
| Phải ngồi nhập liệu tại chỗ | Dữ liệu đã seed sẵn 8 đề tài ở 8 bước — **không nhập gì trong lúc demo** trừ khi đang minh hoạ thao tác |

### 2.3. Phân vai — **4 người** (cập nhật 18/08)

Nhóm còn **4 người**: Chinh nghỉ vì lý do cá nhân (báo tối 18/08). **Thứ gánh luôn phần của Chinh.**

Bám theo **RP6 — Software User Guides**, vốn đã chia sẵn 8 workflow:

| Người | Workflow (RP6) | Điểm nhấn |
|---|---|---|
| **① Dũng** | Mở đầu + WF1 + WF8 | Đóng khung: mở đợt ở đầu, toàn cảnh portfolio ở cuối |
| **② Phát** | WF2 | AI trích xuất từ file Word |
| **③ Thứ** | WF3 + **WF4** | Cảnh báo trùng lịch · gate gửi thư mời → AI gợi ý điểm · Thư ký soạn → **Chủ tịch chốt** |
| **④ Trung** | WF5 → WF6 → WF7 | Một mạch liền: ký HĐ → tiến độ/gia hạn → nghiệm thu |

**Ai nói thì người đó bấm demo.** Không tách một người "lái máy" cho bốn người còn lại nói: cẩm nang
§9 chấm **theo từng cá nhân**, và §8.1 cảnh báo tình huống hội đồng yêu cầu mở source ngay tại chỗ —
người vừa thuyết trình xong mà không dám chạm vào máy sẽ rất khó đỡ. Thầy hướng dẫn cũng dặn *"trình
bày flow xong thì demo cái đó luôn"*, tức nói và demo dính liền nhau.

Cách khử rủi ro đổi người:
- **Một máy duy nhất, một lần chia sẻ màn hình.** Đổi laptop giữa chừng mới là chỗ vỡ.
- **Ngồi sẵn theo đúng thứ tự trình bày**, cạnh nhau. Đổi người = đổi ghế, 5 giây.
- **Mở sẵn toàn bộ cửa sổ ẩn danh trước khi vào phòng**, mỗi vai một cửa sổ. Không ai đăng nhập
  trong lúc demo. Việc này cũng đồng thời chứng minh phân quyền — cẩm nang §7.1 nêu riêng lỗi
  *"dùng chung một tài khoản cho nhiều vai khi demo"*.
- **Người nói sau mở sẵn màn của mình** trong lúc người trước đang nói.

> ⚠️ **Khúc của Thứ (WF3→WF4) nặng nhất** — phải đổi qua lại 4 phiên: phản biện A, phản biện B, Thư
> ký, Chủ tịch. Gợi ý: mở **hai cửa sổ đặt cạnh nhau** cho hai phản biện thay vì chuyển tab, và
> chuẩn bị sẵn phiên Thư ký + Chủ tịch ở cửa sổ thứ ba. Nếu chạy thử thấy vẫn rối thì **cắt phần
> biên bản sang Trung** — Trung có 3 workflow nhưng nối liền một mạch nên nhẹ hơn tiếng.

> ⚠️ Chỉ **phản biện** (`memberRole = "Opponent"`) mới chấm được ở vòng nghiệm thu (BM10, Điều
> 12.3.b). Lập hội đồng nghiệm thu mà quên gán vai Phản biện thì đến lúc demo không ai chấm được.

---

## 3. Hỏi đáp

### 3.1. Công thức trả lời (cẩm nang §8.2, trang 13)

> **Có/Không → Lý do → Chỗ hiện thực → Hạn chế nếu có**

- Chưa làm thì **nói thẳng**: *"Nhóm chưa xử lý tình huống này."* Ngắn gọn và trung thực **tốt hơn vòng vo**.
- **Không tranh cãi.** Ghi nhận; có căn cứ thì trình bày ngắn.

### 3.2. Lỗi chết người: không hiểu code của chính mình (§8.1)

Cẩm nang mô tả tình huống *"rất xấu và khá phổ biến"*: được yêu cầu mở source một chức năng thì trả lời *"em dùng thư viện gì em không nhớ"*, hoặc thừa nhận dùng AI sinh code mà không hiểu nguyên lý.

**Bắt buộc:**
- Mọi thành viên đọc và hiểu **toàn bộ mainflow**, không chỉ phần mình làm.
- Tổ chức buổi **"hỏi chéo"**: mỗi người bị nhóm hỏi **10 câu về phần người khác làm**.
- Có dùng AI hỗ trợ code thì **phải đọc lại, hiểu, giải thích được từng đoạn**.

Ba câu cẩm nang nói riêng là hay bị hỏi mà không ai trả lời được:
- Cơ chế **authentication/authorization** nằm ở đâu trong code?
- **Service call fail thì handle ở đâu?**
- Chỉ ra chỗ code xử lý **một nghiệp vụ cụ thể**.

### 3.3. 20 câu hay gặp nhất (§13, trang 17–18) — và câu trả lời của nhóm

**Về cấu hình**

| Câu hỏi | Trả lời |
|---|---|
| 1. Con số này (%, ngưỡng) có đổi được không? **Demo ngay.** | **Có** — màn *Cài đặt*: dung lượng file tối đa, định dạng cho phép, số ngày xác nhận thư mời, số chữ số thập phân khi chấm, mốc nhắc hạn, bật/tắt email, cho phép xác nhận hộ. **Mở ra bấm tại chỗ.** |
| 2. Ai cấu hình — admin hay chủ tài nguyên? | **Admin**, ở màn Cài đặt. Đây là **hệ thống nội bộ một trường**, không phải nền tảng đa tổ chức. |

**Về kiến trúc & code**

| Câu hỏi | Trả lời |
|---|---|
| 3. Chỉ vào sơ đồ kiến trúc và mở code tương ứng | 4 tầng: `FURPMS.API` (controller) → `FURPMS.Application` (interface/DTO) → `FURPMS.Infrastructure` (service + EF) → `FURPMS.Domain` (entity). **Mở sẵn Solution Explorer.** |
| 4. Vì sao chọn công nghệ này? | .NET 8 + PostgreSQL + React. PostgreSQL vì **Railway không có SQL Server** — đã migrate 14/08. |
| 5. Service đồng bộ hay bất đồng bộ? Fail thì handle ở đâu? | Chủ yếu đồng bộ. **Bất đồng bộ**: sinh tóm tắt AI (`AiSummaryPregenerationService` chạy nền, hàng đợi trong bộ nhớ) và quét nhắc hạn. Lỗi tập trung ở `GlobalExceptionMiddleware` map exception → HTTP status; riêng lỗi Gemini có **thử lại 3 lần** rồi mới báo. |

**Về nghiệp vụ**

| Câu hỏi | Trả lời |
|---|---|
| 6. Người dùng huỷ sau khi đã duyệt thì sao? | *(cần chuẩn bị — xem §4)* |
| 7. Thanh toán thất bại giữa chừng? | **Không áp dụng** — hệ thống không xử lý tiền, chỉ theo dõi mốc giải ngân + minh chứng. Nói rõ đây là quyết định có chủ đích. |
| 8. Người tạo và người duyệt có thể là cùng một người? | **Không.** COI: thành viên trong nhóm đề tài không được làm thành viên hội đồng chấm chính đề tài đó — kiểm khi thêm thành viên, vi phạm trả 400. |
| 9. Nghiệp vụ ngoài thực tế có đúng vậy không? Đã khảo sát chưa? | Bám **QĐ 543/QĐ-ĐHFPT** và đã qua **3 buổi chốt với GVHD** (tuần 7, tuần 10, demo 14/08). Mọi rule đều ghi số điều khoản trong `CLAUDE.md`. |

**Về con số**

| Câu hỏi | Trả lời |
|---|---|
| 10. Con số trên màn hình tính thế nào? | Tổng điểm = cộng điểm từng tiêu chí; trần theo `maxTotalScore` của bộ tiêu chí (BM03 = 100, BM10 = 20). |
| 11. Cho xem dòng tiền vào/ra | **Không có dòng tiền.** Hệ thống chỉ đánh dấu mốc giải ngân + lưu minh chứng. |

**Về AI** — xem §3.5, đây là nhóm câu dễ mất điểm nhất.

**Về Business Rule**

| Câu hỏi | Trả lời |
|---|---|
| 14. Ràng buộc này chỉ chỗ code kiểm | Ví dụ sẵn: gia hạn ≤ 1/2 thời gian → `ContractService.ValidateMaxExtension`, có ghi *"QĐ543 Điều 10.4"* ngay trong câu báo lỗi. |
| 15. Hệ thống lớn thế mà chỉ ngần này BR? | **Chuẩn bị đếm lại số BR trước khi ra.** Nếu ít, bổ sung từ các rule đã code mà chưa ghi vào tài liệu. |

**Về tài liệu**

| Câu hỏi | Trả lời |
|---|---|
| 17. Chức năng ghi trong tài liệu có trong phần mềm không? | ⚠️ **Rủi ro cao** — xem §5. |
| 18. Sơ đồ vẽ tay hay chụp từ hệ thống thật? | ERD chụp từ DBeaver trên DB thật; sơ đồ luồng vẽ tay. **Nói đúng như vậy.** |
| 19. Hình/bảng này nhắc ở đoạn nào? | Mọi hình/bảng phải có **cross-reference** trong nội dung — kiểm trước khi nộp. |

### 3.4. Chuẩn bị "Nếu… thì sao?" — tối thiểu 5 tình huống mỗi mainflow (§12, D-3)

Gợi ý cho FURPMS:
- Nếu PI nộp sau hạn? → hệ thống khoá form, có **log gia hạn** cấp đợt (không ghi đè hạn gốc)
- Nếu reviewer từ chối lời mời? → thông báo Staff, Staff gán người thay
- Nếu hội đồng không đủ người dự họp? → QĐ543 yêu cầu ≥ 2/3 (Điều 8.3.b, 12.3.b)
- Nếu Chủ tịch chốt biên bản rồi mới phát hiện sai? → **hiện chưa mở khoá được** *(nói thẳng là hạn chế)*
- Nếu đề tài xin gia hạn quá 1/2 thời gian? → chặn, kèm câu dẫn Điều 10.4
- Nếu sản phẩm chấm nhầm "Đạt"? → Staff sửa lại được (nút *Sửa đánh giá*)

### 3.5. AI — bốn câu bắt buộc trả lời được (§5, trang 11)

Cẩm nang cảnh báo riêng: *"Lạm dụng từ smart/thông minh mà không có gì tương xứng"*, *"đưa AI vào chỗ mà một truy vấn đơn giản đã giải quyết tốt hơn"*, và **"nếu là rule-based thì nói thẳng là rule-based, đừng gọi là AI"**.

| Câu | Trả lời của nhóm |
|---|---|
| **AI giải quyết vấn đề gì?** | (a) Trích xuất trường có cấu trúc từ file đề cương Word → điền sẵn form, PI vẫn sửa được. (b) Tóm tắt + nêu ưu/nhược + gợi ý điểm cho người chấm, **có đối chiếu file đính kèm với biểu mẫu** và chỉ ra điểm vênh. |
| **Dữ liệu ở đâu?** | Không tự huấn luyện. Gọi **Google Gemini API**; đầu vào là file đề cương + các trường PI đã nhập. |
| **Đo bằng metric nào?** | ⚠️ **Chưa có metric định lượng.** Nói thẳng. Có thể nêu bằng chứng định tính: AI phát hiện được file đính kèm không phải đề cương của đề tài đó. |
| **Ai kiểm chứng đầu ra?** | **Con người quyết định cuối cùng.** Gợi ý điểm chỉ là gợi ý — người chấm tự nhập, và kết quả đề tài do **Chủ tịch hội đồng chốt**, hệ thống không tự quyết. |

> Câu hỏi 13 của cẩm nang — *"Ai kiểm chứng kết quả AI trả về?"* — nhóm trả lời rất mạnh, vì đây đúng là thiết kế của hệ thống. **Chuẩn bị nhiều mẫu file khác nhau để demo, kể cả mẫu khó** — người chấm thường tự lấy mẫu mới để thử.

---

## 4. Checklist theo mốc (§12, trang 16–17)

### D-7 — một tuần trước

- [ ] Kiểm 8 loại sơ đồ (Use Case, Sequence, Class, State Machine, ERD, Architecture, Context, Activity)
- [ ] **Sơ đồ đồng bộ với nhau**: cùng tên chức năng, cùng actor, cùng luồng
- [ ] **Đối chiếu sơ đồ kiến trúc với repository thật** — từng khối trỏ được tới code
- [ ] Rà FR/NFR/BR: **có làm mới ghi**; mọi `if` nghiệp vụ trong code đều có BR tương ứng
- [ ] Test Report: không còn case Fail chưa giải thích, số liệu nhất quán, đúng template FLM
- [ ] Danh mục tài liệu: Acknowledgement (**có tên GVHD**), User Guide, Class Diagram, danh mục bảng/hình, bảng viết tắt, số trang, mục lục
- [ ] Xoá placeholder còn sót; **phóng to mọi sơ đồ tới mức đọc được**
- [ ] Caption: **hình ở dưới, bảng ở trên**; mọi hình/bảng có cross-reference
- [ ] Cập nhật trường tự động (Ctrl+A → F9)
- [ ] **Bảng công thức tính toán** (điểm, trọng số) kèm ví dụ số cụ thể
- [ ] **Dữ liệu demo phong phú, nhiều trạng thái, có lịch sử** ✅ *(đã seed 8 đề tài ở 8 bước)*

### D-3

- [ ] **Viết kịch bản demo, phân vai, chạy thử CÓ BẤM GIỜ ít nhất 2 lần**
- [ ] **Buổi "hỏi chéo"**: mỗi người trả lời 10 câu về phần người khác làm
- [ ] Chuẩn bị "Nếu… thì sao?" — **tối thiểu 5 tình huống mỗi mainflow**
- [ ] **Elevator pitch 60 giây** về định vị sản phẩm
- [ ] Quay **video dự phòng** mainflow
- [ ] Xác nhận GVHD đã duyệt ra bảo vệ *(cẩm nang: xác nhận trước hạn ít nhất 2 tuần)*

### Ngày bảo vệ

- [ ] **Có mặt sớm ít nhất 30 phút**
- [ ] Demo đúng kịch bản, **không lướt qua lỗi**
- [ ] Trả lời theo công thức: Có/Không → Lý do → Chỗ hiện thực → Hạn chế
- [ ] **Ghi chép toàn bộ góp ý** để chỉnh sửa và nộp lại đúng hạn

---

## 5. Bảng tự đánh giá rủi ro (§14, trang 18)

0 = chưa làm · 1 = làm một phần · 2 = làm tốt

| # | Tiêu chí | Ước lượng | Ghi chú |
|---|---|---|---|
| 1 | Tài liệu đúng template, đủ trang bắt buộc | ? | Đã rà đợt nộp RP1–7 |
| 2 | 8 loại sơ đồ đúng chuẩn, **nhất quán với nhau và với code** | **1** | ⚠️ 4 main flow (2,4,5,6) đã lệch code — xem §6 |
| 3 | Không còn tham số nghiệp vụ hardcode | **2** | 7 cấu hình đưa vào màn Cài đặt |
| 4 | Mọi mainflow xử lý ≥ 5 tình huống ngoại lệ | **1** | Cần liệt kê ra giấy — xem §3.4 |
| 5 | Giải thích được mọi con số trên UI | 2 | |
| 6 | Nghiệp vụ đã khảo sát/kiểm chứng | **2** | Bám QĐ543 + 3 buổi chốt GVHD |
| 7 | Test Report đầy đủ, không Fail chưa giải thích | ? | |
| 8 | **Mọi thành viên hiểu toàn bộ mainflow + code** | **?** | ⚠️ Phải làm buổi hỏi chéo |
| 9 | Đã xử lý hết góp ý Review + GVHD, **có bằng chứng** | **1** | ⚠️ Cần lập bảng theo dõi — xem dưới |
| 10 | Kịch bản demo đã chạy thử **có bấm giờ** | **0** | ⚠️ Chưa làm |
| 11 | **AI có metric đánh giá + cơ chế người kiểm chứng** | **1** | Có người kiểm chứng, **chưa có metric** |
| 12 | Đã rà dữ liệu cá nhân, bản quyền, **secret key** | **1** | ⚠️ Quét lại repo tìm API key trước khi nộp |
| 13 | Business Rule **được code kiểm thật** | 2 | Mỗi rule dẫn số điều khoản |
| 14 | Phương án dự phòng dữ liệu demo | **1** | Có lệnh reset DB; **chưa có video dự phòng** |
| 15 | Caption đúng quy ước + cross-reference | ? | |

**Cách đọc:** 26–30 sẵn sàng · 18–25 nhiều khả năng "đạt nhưng phải sửa và nộp lại" · **dưới 18 rủi ro bảo vệ lần 2**.

### Việc cần làm ngay, theo thứ tự

1. **Kịch bản demo + chạy thử có bấm giờ** (dòng 10 đang 0, và đây là nguyên nhân không đạt phổ biến **nhất**)
2. **Bảng theo dõi góp ý Review/GVHD** → trạng thái xử lý → bằng chứng đã sửa. **Mang bảng này đi bảo vệ** (cẩm nang §9)
3. **Buổi hỏi chéo** (dòng 8)
4. **Vẽ lại 4 main flow đã lệch** (dòng 2)
5. **Quét repo tìm secret/API key** (dòng 12) — nhóm có key Gemini, kiểm kỹ
6. Quay **video dự phòng** (dòng 14)

---

## 6. Rủi ro lớn nhất hiện tại: tài liệu không khớp phần mềm

Cẩm nang §1.5 (trang 4) liệt kê đúng tình huống nhóm đang có:

> *"Có mục mô tả chức năng không tồn tại trong phần mềm"* · *"Sơ đồ kiến trúc vẽ ra không khớp với code"*

Và §13 câu 17: *"Chức năng ghi trong tài liệu này có trong phần mềm không?"*

**Những chỗ đã biết là lệch** *(chưa sửa — cần quyết định sửa tài liệu hay bỏ tính năng)*:

| Chỗ | Vấn đề |
|---|---|
| Main flow 2 | Có *"View recommended list"* — hệ thống **không có** máy đề xuất reviewer; thiếu hẳn khái niệm hội đồng |
| Main flow 4 | Vẽ *"discuss → hệ thống cập nhật trạng thái"* — **trái với thiết kế thật**: Chủ tịch chốt biên bản mới đổi trạng thái |
| Main flow 5 | Thiếu bước ký hợp đồng; gia hạn phải ra **phụ lục có chữ ký** chứ không chỉ "update deadline" |
| Main flow 6 | Thiếu **nghiệm thu 2 tầng** (BM10 phản biện · BM11 bỏ phiếu) và BM13 thanh lý |
| RP1/2/3/4/7 | Còn nhắc **Google Meet · Semantic Search · Google Calendar · Screening round · AutoMapper** — đều **không có trong phần mềm** |
| RP6 | Ảnh chụp trước các thay đổi 17/08 (menu, màn chấm nghiệm thu, nút họp) |

> Nguyên tắc vàng của cẩm nang: **"có làm mới ghi, không làm đừng ghi"**. Với mỗi FR/NFR/BR viết ra, **phải chỉ được chỗ đã hiện thực trong code hoặc chỗ đã kiểm thử**.

---

## 7. Nhóm lỗi UI/UX cần soát trước khi demo (§11, trang 15)

Những mục có khả năng dính:

- [ ] **Không thống nhất ngôn ngữ trên giao diện** ⚠️ — nhóm sẽ demo bằng **tiếng Anh**, mà **câu lỗi từ server luôn là tiếng Việt** (mọi lỗi 400/409 hiện nguyên văn `message` của BE). Bấm sai một nút là lộ ngay
- [ ] Nút **"Duyệt"** và **"Không duyệt"** đặt quá gần nhau
- [ ] **Giao diện dư thừa** (cùng một điều khiển ở hai nơi) — đã dọn một đợt 17/08
- [ ] **Danh sách dài không có phân trang / tìm kiếm** → không dùng được với dữ liệu thật
- [ ] **Nhãn sai nghĩa** so với chức năng thật của nút
- [ ] Định dạng **tiền và ngày tháng** theo quy ước chung
- [ ] **Thuật ngữ kỹ thuật** khó hiểu với người dùng cuối
