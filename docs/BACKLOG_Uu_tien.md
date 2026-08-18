# Backlog theo mức ưu tiên — từ buổi tự test 12/08

> Gom toàn bộ phản hồi khi anh tự bấm tay qua 4 vai, **đối chiếu lại với QĐ543** ở những chỗ anh
> hỏi "so lại với doc xem". Xếp theo *rủi ro khi demo* chứ không theo thứ tự phát hiện.
>
> Cột **Doc?** = đã tra QĐ543 chưa: ✅ có căn cứ · ⚠️ quy định không nói · ❓ cần hỏi thầy.

---

## Đã sửa xong trong lượt này (12/08)

| # | Việc | Vì sao gấp |
|---|---|---|
| ✅ | **Đổi vai xong bị chặn oan "không có quyền"** | Hồi quy do chính thay đổi `RoleGuard` sáng nay. Vai đang xem lưu chung một khoá localStorage nên người sau đăng nhập thừa hưởng vai của người trước. **Chặn cả luồng chính.** |
| ✅ | Trang "không có quyền" nay nói rõ đang ở vai nào + nút đổi vai | Lối thoát cũ nằm trong menu avatar mà màn đó không hiện menu |
| ✅ | **Chuông tự cập nhật** (60 giây/lần + khi quay lại tab) | Trước phải tải lại trang mới thấy |
| ✅ | **Đơn vị + Học vị của người dùng không hề được lưu** | `UpdateUserAsync` nhận rồi bỏ đi — sửa xong mở lại là trắng |
| ✅ | Danh sách người dùng **lọc theo vai** + đếm + sắp theo vai | Trước sắp theo tên nên các vai xen kẽ |
| ✅ | Tên tài khoản demo kèm mã vai — `… pi1`, `… rv3`, `… staff` | Nhìn danh sách biết ngay ai đóng ai. Seeder đặt lại đúng bộ tên mỗi lần khởi động ⇒ xoá sạch DB vẫn ra đúng |
| ✅ | **Bỏ chốt chặn "chưa tới kỳ báo cáo thì chưa nộp được"** | Tôi tự thêm, **không có căn cứ QĐ543**. Điều 10.1 chỉ định *khi nào Trường tổ chức đánh giá*, không cấm chủ nhiệm nộp sớm. Giữ lại đúng ràng buộc thật: **kỳ trước phải có kết quả rồi mới tới kỳ sau** |

---

## 🔴 P0 — chạm luồng chính, phải xong trước khi demo

| # | Việc | Doc? | Ghi chú |
|---|---|---|---|
| ✅ P0-1 | **Tóm tắt AI phải sinh SẴN lúc PI nộp, không bắt người chấm ngồi chờ** | ❓ | Thầy đã góp ý. Hiện phản biện mở ra mới bấm "Tạo" rồi chờ — đúng lúc đang có hội đồng ngồi nhìn. Cần: nộp xong chạy nền, lưu lại, người chấm chỉ việc đọc.  **XONG 14/08** — hàng đợi + `AiSummaryPregenerationService`; nộp xong chỉ xếp hàng, sinh nền; quét bù lúc khởi động. Đo thật: bắt được 11 đề cương thiếu tóm tắt. |
| ~~P0-2~~ | ~~Tóm tắt AI không đọc file đính kèm~~ | — | ❌ **BÁO ĐỘNG NHẦM — tôi ghi sai.** `AiSummaryService` **CÓ** đọc file (`GetLatestProposalFileAsync`; PDF gửi thẳng bytes, .docx bóc text). Chính bản tóm tắt anh chụp cũng ghi *"AI đã đọc file đề cương: Report3_SRS_FURPMS_v0.9.docx"* và chỉ ra file không khớp đề tài — đó là **AI làm đúng việc**, không phải nó không đọc. |
| ✅ P0-3 | Bấm "Duyệt & khoá" biên bản không có xác nhận | — | **XONG 12/08** — hộp thoại nêu rõ: khoá xong không sửa điểm, không sửa biên bản, trạng thái đề tài đổi theo |
| ✅ P0-4 | Tạo vòng NGHIỆM THU khi chưa có đề tài nào được duyệt | — | **XONG 12/08** — 409 kèm căn cứ Điều 11.2.c và chỉ đường mở vòng xét duyệt trước. Đo thật: ACCEPTANCE → 409, REVIEW → 200 |
| ✅ P0-5 | Lịch họp bấm "Bắt đầu" là kẹt vĩnh viễn | — | **XONG 12/08** — thêm `POST /meetings/{id}/undo-start` (chỉ khi chưa ai điểm danh) + nút "Hoàn tác bắt đầu". Danh sách nay chỉ hiện nút HỢP LỆ với trạng thái, không còn bấm sai ăn 409 |
| ✅ P0-6 | "Sửa đề cương" không thấy file đã đính kèm | — | **XONG 12/08** — bước 2 chỉ biết tệp vừa chọn trong phiên, chưa bao giờ hỏi máy chủ. Nay liệt kê đủ tệp đã đính kèm |
| ✅ P0-7 | Nút AI ở wizard không chạy | — | **XONG 12/08** — "Kiểm tra trùng lặp" gọi `/ai/similarity-check` mà BE KHÔNG có ⇒ gỡ. Đồng thời phát hiện AI-điền-hộ bị khoá sau nhánh `isApplied` nên đề tài ứng dụng không dùng được ⇒ mở cho cả hai loại |
| ✅ P0-8 | **Vòng nghiệm thu đang nhận nhầm đề tài chưa đủ hồ sơ** | ✅ | **XONG lõi 19/08:** cả tạo/gom hàng loạt, thêm lẻ và mở vòng đều chặn 409 nếu REVIEW của lĩnh vực chưa chốt hết. Chỉ đề tài `PASSED` REVIEW + project `ACCEPTANCE` + final report `ACCEPTED/ARCHIVED` được vào vòng; FE chỉ bày project `ACCEPTANCE`. Còn một quyết định nghiệp vụ độc lập cần thầy chốt nếu muốn siết thêm: có bắt buộc mọi sản phẩm cam kết đã nộp/Staff đánh giá trước khi lập hội đồng hay để chính hội đồng nghiệm thu sản phẩm đó. |

---

## 🟠 P1 — hội đồng dễ soi trúng

| # | Việc | Doc? | Căn cứ / ghi chú |
|---|---|---|---|
| ✅ P1-1 | **Chủ tịch có được sửa biên bản không?** | ✅ | **QĐ543 Điều 8.3.c / 12.3.c:** *"Thư ký ghi biên bản cùng dự thảo kết luận của Hội đồng và **các thành viên của Hội đồng thông qua** biên bản họp."* ⇒ Thư ký **soạn**, hội đồng **thông qua**, Chủ tịch **chốt** — quy định **không** cho Chủ tịch tự sửa. Hiện code đúng. **Nhưng thiếu đường "Chủ tịch yêu cầu Thư ký sửa"** — hiện hai người phải tự liên lạc ngoài hệ thống. Nên thêm: Chủ tịch trả lại kèm ghi chú → Thư ký nhận thông báo.  **XONG 14/08** — `POST .../minutes/request-revision`, ghi chú lưu trên biên bản (Thư ký mở màn là thấy) + thông báo `MINUTES_REVISION_REQUESTED`. Không lách được để sửa bản đã chốt. |
| ⚪ P1-2 | **Hợp đồng: form tạo quá ít trường so với BM05** | ⚠️ | Bản `..._clean.docx` **chỉ có phần điều khoản, không kèm biểu mẫu** ⇒ chưa đối chiếu được đủ trường. Nhưng file Word xuất ra đã có Bên A/Bên B, số tài khoản, kinh phí… ⇒ **hệ thống đang tự bốc từ hồ sơ PI**, Staff không phải nhập lại. Việc cần làm: mở đúng file biểu mẫu BM05 gốc, đối chiếu từng trường, liệt kê cái nào còn thiếu.  **KHÔNG CẦN SỬA** — đã tra: hệ thống tự bốc Bên B từ hồ sơ PI, Staff chỉ nhập 5 trường hệ thống không tự biết |
| ✅ P1-3 | **Ký hợp đồng: bấm một nút là xong, không cần bản ký** | ⚠️ | Rule #21 định nghĩa: xuất Word → ký ngoài → **upload bản ký làm minh chứng**. Hiện `POST /contracts/{id}/sign` không đòi gì. Đề xuất: chặn ký khi chưa có bản ký đính kèm, **hoặc** tách hai trạng thái "đã ký" (có minh chứng) vs "ghi nhận ký". Cần anh chốt.  **XONG 12/08** — chặn ký khi chưa có bản ký đính kèm, thêm ô ngày ký thực tế |
| ✅ P1-4 | **CRUD hợp đồng** | — | Có sửa + xoá (chỉ khi chưa ký). Cần rà lại: sửa được những trường nào sau khi ký? Rule #21 nói sau khi ký phải đi đường **phụ lục**, không sửa đè.  **XONG 12/08** — `PUT` chặn sau khi ký, chỉ đường sang phụ lục (BM05 Điều 6.1) |
| ✅ P1-5 | **Quyết toán: bấm một phát xong hết, không xác nhận, không sửa lại** | ✅ | QĐ543 **Điều 13.1.e** đòi *"Xác nhận của Ban kế toán về việc đề tài đã quyết toán kinh phí và đã xử lý tài sản"* và **Điều 13.2** đòi ký **Biên bản thanh lý hợp đồng (BM13)**. **Bổ sung 19/08:** chỉ lập sau nghiệm thu Đạt + chi xong; kế toán/tài sản phải xác nhận trước; ký BM13 mới đặt hợp đồng `SETTLED`. Nghiệm thu Đạt chỉ đặt đề tài `COMPLETED`, không đánh đồng hai mốc. |
| ✅ P1-6 | **Thông báo còn thiếu** | — | Xác nhận giải ngân · duyệt báo cáo tiến độ. Chi tiết ở `THONG_BAO_VA_EMAIL.md`.  **XONG 14/08** — `DISBURSEMENT_CONFIRMED` + `PROGRESS_REPORT_EVALUATED` (ưu tiên HIGH khi Không đạt). |
| ✅ P1-7 | **Quên mật khẩu qua email** | — | **XONG 12/08** — `POST /auth/forgot-password` + `/auth/reset-password`, liên kết "Quên mật khẩu?" ở màn đăng nhập, 2 màn mới. Mã băm SHA-256 trong DB, sống 30 phút, dùng một lần; email không tồn tại vẫn trả 200 (chống dò tài khoản). |
| P1-8 | **Hết hạn vòng chấm xử lý thế nào?** | ⚠️ | Hiện chỉ có `ResearchCycle.ReviewDeadline`, vòng chấm không có deadline riêng. **Không nên tự cho đề tài rớt** vì reviewer chậm là lỗi vận hành, không phải lỗi PI. Đề xuất: khoá nhận phiếu + cờ Quá hạn + thông báo Staff, rồi Staff gia hạn / đổi người / kết luận hành chính có lý do và audit log. Cần thầy chốt trước khi thêm state/migration. |

---

## 🟡 P2 — làm giao diện đúng nghiệp vụ hơn

| # | Việc | Doc? | Ghi chú |
|---|---|---|---|
| ✅ P2-1 | **Màn "Sửa đề cương" có phần "Sản phẩm dự kiến" và "Tài liệu đính kèm" phân loại (thuyết minh / lý lịch khoa học)** — trong khi lúc **tạo** thì không có | ✅ | QĐ543 **Điều 6.4** yêu cầu hồ sơ có **lý lịch khoa học (BM02)**; **Điều 11.1** liệt sản phẩm cam kết. ⇒ Hai phần này **đúng nghiệp vụ**, cái sai là **lúc tạo lại không có** ⇒ PI nộp lần đầu thiếu. Cần đưa lên bước tạo.  **XONG 12/08** — wizard nay có Sản phẩm dự kiến + hiện tệp đã đính kèm khi sửa |
| ✅ P2-2 | **Staff: đổi "Xét duyệt" thành "Xem chi tiết đề tài"** + thêm tab tiến độ như bên PI | — | **XONG 18/08** — màn đổi thành **Đề cương**; nút **Xem chi tiết** mở trang có 2 tab: Nội dung đề cương (thông tin · sản phẩm · file đọc tại chỗ) và Tiến trình đề tài (vòng xét duyệt + timeline hợp đồng/báo cáo/giải ngân dùng chung với PI). Thao tác vòng vẫn chỉ nằm ở **Hội đồng & Chấm**. |
| P2-3 | **Tiêu đề lịch họp: không gợi ý, không chặn trùng tên** | ⚠️ | Đề xuất: gợi ý sẵn `"Họp HĐ <loại vòng> — <tên đề tài>"`. Trùng tên thì **cảnh báo chứ không chặn** (hai đợt khác nhau trùng tên là bình thường). |
| ⚪ P2-4 | **Gán hội đồng khi chưa gửi thư mời** | ✅ | **Đúng như hiện tại.** Rule #13: gán hết rồi mới gửi một lượt. Không cần sửa.  **KHÔNG CẦN SỬA** — đúng rule #13: gán hết rồi mới gửi một lượt |
| ⚪ P2-5 | **Nhập số tháng gia hạn quá lớn** | — | Hiện chặn báo lỗi. Đề xuất tự kẹp về tối đa — **anh nghiêng về "thôi"**, tôi đồng ý: báo lỗi rõ ràng hơn là âm thầm đổi số người ta gõ.  **KHÔNG LÀM** — anh chốt: báo lỗi rõ ràng hơn là âm thầm đổi số người ta gõ |
| P2-6 | **Ô "nhận xét chung / kiến nghị" khác màu các ô chấm** | — | Vì không thuộc tiêu chí chấm. Chấp nhận được, nhưng nên có tiêu đề nhóm cho rõ. |
| ✅ P2-7 | **Trạng thái còn tiếng Anh ở màn Staff xét duyệt** | — | Sót sau đợt rà 36 màn (màn này vào bằng đường khác).  **XONG 14/08** — thiếu key `INACTIVE` nên StatusBadge hiện nguyên chữ Anh. Quét 11 màn + 3 chi tiết: sạch. |
| ✅ P2-8 | **Tab "Kho tài liệu" trống** | — | Cần xác định: chưa làm, hay có mà không có dữ liệu.  **XONG 14/08** — ẩn khỏi menu Staff/Admin theo yêu cầu (route/page giữ nguyên) |
| P2-9 | **Admin có nên sửa thông tin cá nhân của người khác không?** | ⚠️ | Quy định không nói. Đề xuất: Admin sửa **vai + khoá/mở tài khoản**; thông tin cá nhân (điện thoại, học vị) để chính chủ sửa ở Hồ sơ. |
| ✅ P2-10 | **Tạo tài khoản mới không gửi mail** | — | **XONG 12/08** — `ACCOUNT_CREATED`: chuông + mail kèm mật khẩu tạm, dẫn thẳng tới màn đổi mật khẩu. Đo thật: `email_logs` ghi `SENT`. |
| ✅ P2-11 | **Reviewer khó tìm đề tài được phân công; Staff có tab Phân công trùng chức năng** | — | **XONG 19/08** — thêm lọc theo đợt · lĩnh vực · trạng thái · vòng; DTO trả cycle/track id+code. Ẩn mục Phân công khỏi sidebar và đổi quick action sang Hội đồng & Chấm, nhưng giữ route cũ để bookmark không 404. |

---

## 🔵 P3 — để sau, không ảnh hưởng demo

| # | Việc | Ghi chú |
|---|---|---|
| P3-1 | Tích hợp Google Meet | Đã ghi trong RP nên **sẽ phải quay lại**. Hiện chỉ dán link tay. |
| P3-2 | Làm đẹp UI hợp đồng + bố cục file Word xuất ra | Anh thấy khác hợp đồng đời thật |
| P3-3 | Quản lý token/chi phí AI + đo chất lượng đầu ra AI | Cẩm nang capstone có nêu |
| P3-4 | `/ai/search` semantic | Đề xuất bỏ, thay bằng tìm kiếm nâng cao |

---

## 📄 Kế hoạch riêng cho HỢP ĐỒNG (anh chốt là ưu tiên nhất)

### Hiện trạng đã tra được trong code

| Câu hỏi của anh | Sự thật đo được |
|---|---|
| "Form tạo hợp đồng ít trường quá, Bên B thì sao?" | **Đúng như anh đoán: hệ thống tự bốc.** File Word xuất ra điền sẵn Bên B từ hồ sơ chủ nhiệm — họ tên, đơn vị công tác, điện thoại, email, **số tài khoản + ngân hàng**. Staff chỉ nhập 5 trường vì 5 trường đó là thứ *duy nhất* hệ thống không tự biết. |
| "Có cho CRUD hợp đồng chưa?" | Có `GET · POST · PUT · DELETE · sign`. **Nhưng `PUT` KHÔNG chặn khi hợp đồng đã ký** ⇒ sửa đè được bản đã ký, trái rule #21 (phải đi đường phụ lục). |
| "Ký chỉ cần bấm một phát?" | Đúng. `POST /contracts/{id}/sign` **không đòi bằng chứng gì**: đổi trạng thái sang Đang hiệu lực rồi thôi. Trong khi `ContractDocumentsController` để upload bản ký thì đã có sẵn, chỉ là không ai bắt buộc dùng. |
| "Quyết toán bấm phát là xong?" | Đúng. Hai nút đánh dấu, không xác nhận, không bỏ đánh dấu được, **chưa có Biên bản thanh lý BM13** (Điều 13.2). |

### Ký hợp đồng — ba cách người ta hay làm

> ✅ **QĐ543 đã chốt sẵn cách ký, không phải chọn:** BM05 **Điều 7.2–7.3** ghi rõ hợp đồng
> *"được thực hiện qua phương thức **ký điện tử trên phần mềm Econtract**"*, cấp *"chứng thư số 1
> lần (OTP)"*, và *"các bên **tự bảo quản và lưu trữ**"*. Tức là **ký ở phần mềm ngoài** — hệ thống
> này chỉ sinh biểu mẫu và giữ bản đã ký. Đúng cách 2 dưới đây, và có căn cứ để trả lời hội đồng.

| Cách | Mô tả | Hợp với mình không |
|---|---|---|
| **1. Tích hợp nhà cung cấp chữ ký số** (FPT.eContract, VNPT-CA, Viettel-CA, DocuSign) | Hệ thống đẩy file lên, hai bên ký bằng USB token/OTP, nhận về file đã ký + dấu thời gian có giá trị pháp lý | ❌ Cần hợp đồng thương mại + tài khoản CA thật. Không khả thi cho capstone, và **thầy cũng không đòi** |
| **2. Ký ngoài → tải bản ký lên làm minh chứng** | Hệ thống sinh Word/PDF → hai bên ký ngoài (tay hoặc chữ ký số cá nhân) → upload lại. Hệ thống đóng vai **sổ cái giữ bằng chứng**, không phải công cụ ký | ✅ **Đúng cái nhóm đã chốt ở rule #21**, và hạ tầng đã có đủ (xuất Word + `ContractDocumentsController`) |
| **3. "Ký trong hệ thống" bằng xác nhận có ràng buộc** | Không phải chữ ký số pháp lý, mà ghi nhận: ai bấm, lúc nào, trên **phiên bản file nào** (lưu hash), có nhập lại mật khẩu để xác nhận | 🔸 Nhiều hệ thống nội bộ dùng. Có thể **bổ sung** cho cách 2, không thay thế |

### Đề xuất: giữ cách 2, nhưng bắt nút "Ký" phải nói thật

Vấn đề hiện tại **không phải thiếu tính năng, mà là nút đang nói dối**: bấm "Ký hợp đồng" là hợp
đồng thành "Đang hiệu lực" dù chưa ai ký gì cả. Hội đồng hỏi *"bằng chứng ký đâu"* là không có gì
để đưa ra.

Sửa nhỏ, ánh xạ đúng đời thực:

1. **Đổi nhãn nút** "Ký hợp đồng" → **"Ghi nhận đã ký"**. Hệ thống không ký thay ai — nó ghi nhận
   việc đã xảy ra ngoài đời. Nhãn đúng thì không phải giải thích.
2. **Chặn ghi nhận khi chưa có bản ký đính kèm.** Chưa có thì nút mờ, kèm câu chỉ đường:
   *"Xuất hợp đồng (Word) → ký ngoài → Tải bản đã ký lên đây."* Đúng ba bước rule #21 đã định.
3. **Thêm ô "Ngày ký thực tế"** — ngày ghi trên giấy, tách khỏi ngày bấm nút. Hai ngày này luôn
   khác nhau, và mọi mốc hợp đồng phải tính theo ngày trên giấy.
4. **Ký xong là khoá**: `PUT /contracts/{id}` chặn lại, muốn đổi phải qua **phụ lục** (rule #21).
   Đây là lỗ hổng thật hiện nay, không chỉ là chuyện nhãn.
5. *(tuỳ chọn, làm sau)* Lưu **hash của file bản ký** để về sau đối chiếu được là file không bị
   tráo — chính là ý tưởng của cách 3, gắn thêm vào cách 2.

**Việc khoe được trước hội đồng:** *"Hệ thống không giả vờ ký thay người. Nó sinh đúng biểu mẫu,
giữ bản đã ký làm bằng chứng, và từ lúc ký thì khoá lại — muốn đổi phải có phụ lục."*

### Danh sách việc cho nhóm hợp đồng

| Mã | Việc | Ưu tiên | Cần anh chốt? |
|---|---|---|---|
| ✅ HD-1 | Nút "Ghi nhận đã ký" + **bắt buộc có bản ký** mới cho ghi nhận | — | **XONG 12/08** (anh chọn phương án chặt) |
| ✅ HD-2 | **Khoá sửa sau khi ký** (`PUT` trả 409, chỉ đường sang phụ lục) | — | **XONG 12/08** |
| ✅ HD-3 | Ô **"Ngày ký thực tế"** tách khỏi ngày bấm nút | — | **XONG 12/08** — chặn cả ngày tương lai |
| ✅ HD-4 | **Quyết toán**: bỏ đánh dấu được · khoá sau khi ký biên bản thanh lý | — | **XONG 12/08** |
| ✅ HD-5 | **Biên bản thanh lý hợp đồng BM13** (Điều 13.2) | — | **XONG 12/08** — `GET /contracts/{id}/export-settlement-word` |
| ✅ HD-6 | Đối chiếu **BM05 gốc** từng trường | — | **XONG 12/08.** Cấu trúc 7 Điều vốn đã khớp; bù **7 chỗ thiếu**: 3 căn cứ pháp lý · Địa chỉ Bên B · mục "Đại diện cho các thành viên" · **số tiền bằng chữ** · Điều 5 đầy đủ 15 mục a–h (trước gộp thành 2 câu) · Điều 6.1 vế phụ lục · Điều 6.3 vế Trọng tài/Toà án. Bản Word từ 74 → **95 dòng** |
| HD-7 | Lưu **hash file bản ký** để chống tráo file | P2 | không |
| ✅ HD-8 | Bố cục file Word xuất ra cho giống hợp đồng thật | P3 | không  **XONG 14/08** — thể thức theo Nghị định 30/2020: Times New Roman 13, A4, lề 30mm, quốc hiệu căn giữa, khối ký không viền |
| ✅ HD-9 | Danh sách hợp đồng hiện + lọc theo **loại đề tài / đợt / lĩnh vực** để phân biệt lịch giải ngân | — | **XONG 19/08** |

### AI chống gãy khi demo — 19/08

- Giữ `llm_outputs` làm **single source of truth**, không tạo thêm bảng `ProposalAiAnalyses` trùng dữ liệu.
- Gemini thử tối đa 3 lượt: 2 lượt model chính `gemini-3.5-flash-lite`, lượt cuối fallback ổn định `gemini-3.1-flash-lite`; delay exponential + jitter cho 429/5xx. Đã bỏ 2.5 vì Gemini từ chối model này với tài khoản mới dù endpoint liệt kê model vẫn có thể còn trả tên.
- AI upload đề cương nay trích xuất cả **06 hạng mục kinh phí** và **thành viên nhóm** để điền bước 3/4; chỉ điền khi dữ liệu người dùng còn trống. Nếu tài liệu chỉ ghi tổng mà không có phân bổ, UI chỉ báo tổng để PI tự phân bổ, tuyệt đối không gán tiền vào một hạng mục tuỳ tiện.
- DOCX/TXT được làm sạch ký tự/khoảng trắng rác và cắt trần context trước khi gửi.
- Gợi ý điểm lưu cache theo `(proposal, council)`; mở lại trang reviewer tải cache ngay. Khi chạy lại lỗi, giữ bản thành công gần nhất.
- FE khóa nút AI và đếm ngược 10 giây sau mỗi lượt; lỗi quá tải hiện câu thân thiện, không lộ raw 429/503.
- Seed ca `[TEST-VÒNG 1 THIẾU PHIẾU]` có sẵn tóm tắt + gợi ý điểm để demo offline; nút **Chạy lại** vẫn gọi Gemini thật.

---

## Ba câu anh hỏi — trả lời bằng QĐ543

### 1. Báo cáo tiến độ · Báo cáo tổng kết · Sản phẩm — khác nhau thế nào?

| | Là gì | Khi nào | Biểu mẫu | Ai duyệt |
|---|---|---|---|---|
| **Báo cáo tiến độ** | Báo *đang làm tới đâu*, giữa chừng | Ứng dụng **2 lần** (cuối GĐ1, GĐ2) · Cơ bản **1 lần** giữa kỳ | **BM06** | Phòng QLKH (rule #16) |
| **Sản phẩm** | Thứ **cam kết giao** trong đề cương/hợp đồng | Nộp dần theo mốc | — | Nghiệm thu từng cái |
| **Báo cáo tổng kết** | Báo *kết quả cuối cùng*, để nghiệm thu | Nộp **≥ 30 ngày trước khi kết thúc** đề tài | **BM09** | Hội đồng nghiệm thu |

Nguồn: **Điều 10.1** (tiến độ), **Điều 11.1.a + 11.2.a** (tổng kết + hạn 30 ngày), **Điều 11.1** (sản phẩm cam kết).

⇒ Ba thứ **khác nhau thật**, không phải trùng lặp. Nhưng hiện hệ thống **chưa chặn hạn 30 ngày** của Điều 11.2.a.

### 2. Chủ tịch có được sửa biên bản không?
**Không.** Điều 8.3.c và 12.3.c đều ghi Thư ký soạn, *"các thành viên của Hội đồng thông qua"*. Code đang đúng. Cái thiếu là **đường yêu cầu sửa trong hệ thống** (P1-1).

### 3. Quyết toán để làm gì?
Điều 17 + Điều 13.1.e + 13.2: là bước **đóng hồ sơ tài chính** — kế toán xác nhận đã quyết toán kinh phí và xử lý tài sản, rồi ký **Biên bản thanh lý hợp đồng (BM13)**. Hiện hệ thống mới có phần đánh dấu, **thiếu BM13**.

---

## Thứ tự đề xuất làm

1. **P0-6, P0-7** — luồng PI đang hỏng, PI là actor duy nhất
2. **P0-1, P0-2** — AI là điểm thầy đã góp ý, và đang chạy sai chỗ dễ thấy nhất
3. **P0-3, P0-4, P0-5** — ba ràng buộc rẻ, chặn được tai nạn khi demo trực tiếp
4. **P1-1, P1-5** — hai chỗ lệch quy định rõ nhất
5. **P1-7 (quên mật khẩu)** + **P1-3 (ký hợp đồng)** — cần anh chốt hướng trước
6. P2 trở đi

> **Cần anh quyết trước khi làm:** P1-3 (ký hợp đồng có bắt buộc bản ký không) · P2-9 (Admin sửa
> được gì của người khác) · P0-1/P0-2 (AI chạy lúc nào, đọc gì) — ba cái này chọn sai hướng thì
> làm lại từ đầu.
