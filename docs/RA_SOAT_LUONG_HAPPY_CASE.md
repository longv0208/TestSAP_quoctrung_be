# Rà soát luồng happy case — cái đã có, cái còn thiếu

> Nguồn: bạn tự đi lại toàn bộ luồng **trong đầu** từ trải nghiệm dùng hệ thống (14/08/2026), ghi ra
> mọi chỗ thấy "cấn". Không phải vừa test vừa viết.
>
> **File này là bản KIỂM CHỨNG lại từng nghi ngờ đó với code thật + QĐ 543/QĐ-ĐHFPT.** Chưa sửa gì.
> Mục đích: sau này lấy ra làm, và biết trước cái nào là lỗ hổng thật, cái nào lo thừa.
>
> Cách đọc: §1 là **tin tốt** (đã chạy rồi, khỏi lo). §2 là **lỗ hổng thật**. §3 là **câu hỏi nghiệp
> vụ chỉ bạn quyết được**. §4 là **thứ tự nên làm**.

---

## 1. Những chỗ bạn lo — nhưng thật ra ĐÃ CÓ

Tám chỗ. Ghi ra để không mất công làm lại.

| Bạn lo | Thực tế | Bằng chứng |
|---|---|---|
| Lịch họp có gửi cho PI không | **Có.** PI có endpoint riêng xem lịch hội đồng chấm đề tài mình | `CouncilMeetingsController.cs:50` — `GET /api/meetings/my` |
| Chủ tịch không liên lạc được với Thư ký, phải nhắn ngoài | **Có rồi.** Chủ tịch trả biên bản về kèm ghi chú cần sửa, Thư ký nhận thông báo đích danh, có tên Chủ tịch + nội dung cần sửa | `ReviewScoringService.cs:389-416`, loại thông báo `MINUTES_REVISION_REQUESTED` |
| Không lưu được biên bản nếu chưa chấm hết ⇒ thành viên vắng làm kẹt cả hội đồng | **Không kẹt.** Điều kiện là **quorum 2/3 làm tròn LÊN**, không phải 100%. Hội đồng 5 người cần 4 phiếu — vắng 1 vẫn lưu được | `ReviewScoringService.cs:678-702`, đúng QĐ543 **Điều 8.3.b** |
| Vòng nghiệm thu người chấm chỉ thấy đề cương cũ | **Thấy đủ hồ sơ.** Có panel riêng liệt kê sản phẩm, số sản phẩm đã đạt, các kỳ báo cáo tiến độ, báo cáo tổng kết | FE `AcceptanceDossierPanel.tsx` |
| Một hội đồng chỉ đặt được đúng 1 lịch | **Đặt được nhiều.** Và mỗi đề tài gán được **khung giờ con riêng** trong buổi họp | `POST /api/councils/{id}/meetings`; `CouncilProjectAssignment.MeetingId / SlotStartAt / SlotOrder` |
| Luồng giải ngân lộn xộn, không biết cái gì trước cái gì sau | **Code đã đúng QĐ543 Điều 16.** Xem §1.1 ngay dưới | `DisbursementService.cs:83-107, 122-125, 172-190` |
| Xong nghiệm thu là hết, Staff không còn việc | **Còn.** Thanh lý hợp đồng (BM13) đã dựng: tạo → ký → xác nhận kế toán → xác nhận tài sản | `ContractSettlementService.cs` |
| Mail hỏng thì không ai biết | **Có ghi log** đủ trạng thái + thông báo lỗi… nhưng **không ai đọc được** → xem §2.3 | `SmtpEmailService.cs:157-175`, bảng `email_logs` |

### 1.1. Thứ tự giải ngân — QĐ543 Điều 16 nói rất rõ

Chỗ bạn viết *"t không hiểu thật… cái gì trước cái gì sau"*. Quy định trả lời dứt khoát, và
**loại đề tài quyết định lịch giải ngân, không phải lựa chọn của ai**:

**Đề tài ỨNG DỤNG — 4 đợt, tỉ lệ 30% – 30% – 30% – 10%:**

| Đợt | Mở khi |
|---|---|
| 1 | Sau khi **ký hợp đồng** NCKH với chủ nhiệm |
| 2 | Sau khi đánh giá tiến độ **giai đoạn 1** = "Đạt" |
| 3 | Sau khi đánh giá tiến độ **giai đoạn 2** = "Đạt" |
| 4 | Sau khi **Hội đồng nghiệm thu** đánh giá "Đạt" |

**Đề tài CƠ BẢN — 1 lần duy nhất,** toàn bộ kinh phí sau khi Hội đồng nghiệm thu đánh giá "Đạt".

Hệ thống đang làm đúng cả hai: sinh đợt theo loại đề tài, đợt nào gắn sản phẩm minh chứng thì
sản phẩm phải nghiệm thu Đạt mới đánh dấu được, và **đợt cuối bị khoá** cho tới khi đề tài
`Completed`. Hợp đồng chỉ có 1 đợt thì không khoá — nếu khoá thì đề tài không có gì để khởi động.

> **Nên nhớ:** hệ thống **không quản tiền** (rule #15) — nó chỉ theo dõi **mốc** và giữ **minh chứng**.
> Bốn dòng trên là *mốc*, không phải lệnh chi.

**Vậy vấn đề thật ở đây không phải thiếu tính năng, mà là màn hình không nói ra thứ tự này.**
Bạn — người làm ra hệ thống — đọc màn hình còn không suy ra được, thì người dùng chịu. Việc cần
làm là **hiển thị điều kiện mở của từng đợt ngay trên màn giải ngân** (xem §2.6), không phải viết
lại logic.

---

## 2. Lỗ hổng THẬT — kiểm chứng được trong code

Xếp theo mức độ đau, nặng nhất trước.

### 2.1. 🔴 Thư mời không nói mời chấm đề tài nào — và mời được cả khi hội đồng chưa có đề tài

Cái "cấn" đầu tiên bạn ghi. **Đúng hoàn toàn.**

Nội dung thư đang là:

> *"Bạn được mời tham gia một hội đồng đánh giá đề tài. Vui lòng xác nhận hoặc từ chối trước dd/MM/yyyy."*

Không tên đề tài, không lĩnh vực, không số lượng. Người được mời không có căn cứ nào để quyết
định nhận hay từ chối — mà từ chối/nhận lại có deadline.

Nặng hơn: cổng chặn trước khi gửi thư mời kiểm **Chủ tịch · Thư ký · có lịch họp · đủ số người ·
số lẻ** — nhưng **không kiểm hội đồng đã được gán đề tài nào chưa**. Nên mời xong hội đồng vẫn có
thể trống trơn.

*Bằng chứng:* `CouncilService.cs:353-382` (cổng chặn), `:414-416` (nội dung thư).

**Vướng ở đây là câu hỏi thứ tự** — xem §3.1, phải quyết trước khi sửa.

### 2.2. 🔴 Vòng nghiệm thu: người chấm thấy sản phẩm, nhưng AI thì không

Bạn viết *"địt mẹ chết mẹ rồi… hình như chưa sửa cái này"*. **Đúng.**

Gợi ý chấm của AI đọc đúng hai thứ, ở cả hai vòng như nhau:
- bảng `Proposals` — **đề cương gốc**
- file đề cương mới nhất

Câu lệnh gửi cho AI ghi nguyên văn *"FILE **đề cương gốc** — đọc file làm căn cứ CHÍNH"* và đưa
vào trường *"**Sản phẩm dự kiến**"*.

Nghĩa là ở vòng nghiệm thu, **AI đang chấm bản kế hoạch chứ không chấm kết quả**. Nó không hề đọc
`ProjectDeliverable`, `FinalReport` hay `ProgressReport` — dù dữ liệu đó có sẵn và giao diện người
chấm đã hiển thị.

*Bằng chứng:* `AiAdvisorService.cs:161-204`.

**Hệ quả thực tế:** thành viên hội đồng bấm "AI gợi ý" ở vòng nghiệm thu sẽ nhận điểm và lý do
nói về đề cương — nghe vẫn trôi chảy nên **rất dễ tin nhầm**, đó mới là chỗ nguy.

**Việc cần làm:** tách nguồn dữ liệu theo loại vòng. Nghiệm thu thì đọc sản phẩm + báo cáo tổng
kết + các kỳ tiến độ, và đối chiếu **sản phẩm thực giao so với sản phẩm đã cam kết trong đề cương**
— đó mới đúng việc của hội đồng nghiệm thu.

### 2.3. 🟠 Mail hỏng có ghi log, nhưng không màn nào đọc được

Bạn hỏi *"chuyên nghiệp người ta làm sao biết mail đã gửi đi?"*.

Bảng `email_logs` đã ghi đủ: người nhận, loại thư, tiêu đề, thời điểm, **trạng thái**, **thông báo lỗi**.
Nhưng **không có endpoint nào và không có màn nào đọc nó**. Dữ liệu nằm đó, không ai thấy.

*Bằng chứng:* ghi ở `SmtpEmailService.cs:157-175`; tìm `EmailLog` trong `FURPMS.API/Controllers/` → **0 kết quả**.

**Việc cần làm (rẻ):** một màn Admin "Nhật ký gửi thư" — lọc theo trạng thái, xem lỗi, nút gửi lại.
Chi phí thấp, mà đúng lúc bảo vệ hỏi *"mail không tới thì sao"* thì có cái để chỉ.

### 2.4. 🟠 Đơn điều chỉnh: chỉ PI gửi được — QĐ543 nói **cả hai bên**

Hệ thống chặn cứng: *"Chỉ chủ nhiệm đề tài mới gửi được đề nghị điều chỉnh."*

QĐ543 **Điều 6.1 của hợp đồng** ghi:

> *"Trong quá trình thực hiện Hợp đồng, nếu **một trong hai bên** có yêu cầu sửa đổi, bổ sung nội
> dung hoặc có căn cứ để chấm dứt thực hiện Hợp đồng phải thông báo cho bên kia ít nhất là **15
> ngày làm việc**. Các sửa đổi, bổ sung phải lập thành văn bản **phụ lục**…"*

Ba chỗ lệch:
1. **Bên A (Trường/Phòng QLKH) không có đường gửi đề nghị điều chỉnh** — chỉ duyệt được đơn của PI.
2. **Không có ràng buộc báo trước 15 ngày làm việc.**
3. **Không sinh ra phụ lục hợp đồng** — đây chính là câu bạn hỏi *"cái kiểu gửi gia hạn hợp đồng này
   có phải phụ lục không?"*. **Theo QĐ543 thì đúng là phụ lục**, và phải lập thành văn bản. Hiện
   duyệt xong không xuất ra văn bản nào.

*Bằng chứng:* `ChangeRequestService.cs:45-46`; `ChangeRequestsController.cs:24, 46`.

### 2.5. 🟠 Không chặn gia hạn quá 1/2 thời gian

QĐ543 **Điều 10.4**: *"Gia hạn tối đa **1/2 tổng thời gian thực hiện** của đề tài được phê duyệt."*

Bạn nhớ đúng ("tối đa 50%"). Nhưng **code không kiểm**. Staff duyệt một đơn xin gia hạn 24 tháng
cho đề tài 12 tháng thì hệ thống nhận bình thường.

Đây là loại ràng buộc rẻ nhất để làm mà nhìn rất "có quy định": một phép so sánh, một thông báo lỗi
dẫn đúng điều khoản.

### 2.6. 🟠 Màn giải ngân không nói điều kiện mở của từng đợt

Không phải lỗi logic (§1.1 đã xác nhận code đúng), mà là **lỗ hổng giải thích**. Người dùng nhìn 4
dòng đợt giải ngân không biết vì sao đợt 3 chưa bấm được.

**Việc cần làm:** mỗi đợt hiện một dòng điều kiện + trạng thái, kiểu:

```
Đợt 3 · 30%   [Chưa mở]   Cần: đánh giá tiến độ giai đoạn 2 = "Đạt"  (QĐ543 Điều 16.1.b)
                          → hiện tại: kỳ 2 đang chờ Staff đánh giá
```

Sửa cái này thì **chính bạn cũng hết lú**, và hội đồng hỏi là chỉ thẳng vào màn hình được.

### 2.7. 🟡 Báo cáo tiến độ "Không đạt" — ghi nhận xong rồi thôi

Hệ thống nhận đủ 3 kết quả (Đạt / Không đạt / Đạt có điều kiện) rồi đặt trạng thái `Evaluated`. Hết.

QĐ543 **Điều 10.3** nói kết quả đánh giá tiến độ là **căn cứ để Hiệu trưởng quyết định**:
tiếp tục cấp kinh phí · phê duyệt điều chỉnh · cho gia hạn · **hoặc đình chỉ thực hiện đề tài**.

Tức là "Không đạt" phải dẫn tới một **nhánh xử lý**, hiện chưa có nhánh nào. Không có đường "đình chỉ".

Đây đúng là chỗ bạn viết *"k đạt thì sao XD?"* — quy định có trả lời, hệ thống thì chưa.

### 2.8. 🟡 Không có hạn nộp hồ sơ nghiệm thu

QĐ543: *"Chủ nhiệm đề tài phải nộp báo cáo nghiệm thu… ít nhất **30 ngày trước khi kết thúc đề tài**."*

Không thấy ràng buộc hay cảnh báo nào theo mốc này.

### 2.9. 🟡 Không giới hạn số đề tài trên một hội đồng / một buổi

Bạn tự trả lời trong lúc viết: *"chắc thêm cái giới hạn số đề tài trong 1 hội đồng ngày đó lịch đó
thôi, kiểu 5 nhóm"*. Hiện **không có ràng buộc nào** — gán 30 đề tài vào một buổi 2 tiếng vẫn lọt.

Model đã đỡ được việc này (mỗi đề tài có `SlotStartAt` + `SlotDurationMinutes`), nên có thể cảnh báo
theo cách tự nhiên hơn con số cứng: **tổng thời lượng các slot vượt quá thời lượng buổi họp**.
Cảnh báo mềm, không chặn.

### 2.10. 🟡 Hợp đồng: PI không có bước đồng ý

Bạn viết *"hệ thống t thì nó cứ bấm ký là ký thôi"*. Đúng — không tìm thấy dấu vết PI xác nhận.
Staff tạo và đánh dấu ký một mình.

Cân nhắc: QĐ543 cho ký qua **Econtract** (phần mềm ký điện tử ngoài), nên đường đúng có lẽ **không
phải** làm ký số trong hệ thống, mà là một bước **PI xác nhận nội dung trước khi xuất bản Word đem
ký**. Rẻ, và đúng tinh thần "hệ thống giữ minh chứng".

### 2.11. 🟢 Chưa có xuất lịch ra file

Bạn hỏi *"liệu có xuất file lịch được không XD?"*. Chưa có. Không có `.ics`, không có Excel.

Với hội đồng nhiều buổi thì đây là thứ giảng viên thật sự cần. Ưu tiên thấp nhưng dễ ăn điểm.

### 2.12. 🟢 Chưa có ảnh chụp DB để test lặp

Bạn hỏi *"chuyên nghiệp họ làm sao nhỉ?"* — câu trả lời ngắn: **họ không dựa vào ảnh chụp thủ công.**
Hai cách phổ biến:

1. **Seed theo kịch bản** — mỗi kịch bản một tập dữ liệu sinh bằng code, chạy phát là có. Ưu điểm:
   nằm trong Git, ai cũng dựng lại được, không phụ thuộc file `.bak` trên máy bạn.
2. **Ảnh chụp Postgres** — giờ đã đổi sang Postgres nên việc này thành dễ:
   `pg_dump` ra file, `pg_restore` để quay lại. Không cần thêm code.

Gợi ý: dùng (2) cho việc bạn đang cần ngay (giữ trạng thái đang test), và (1) nếu còn thời gian.

---

## 3. Câu hỏi nghiệp vụ — chỉ bạn quyết được

Không code được cho tới khi có câu trả lời.

### 3.1. Tạo hội đồng trước hay gán đề tài trước?

Chỗ bạn xoắn nhất, và **xoắn đúng**. Hai phương án bạn tự nghĩ ra:

**(A) Hội đồng có sẵn, gán đề tài sau** — mời người vào hội đồng như một tổ chức, xong mới rót đề
tài vào. Giống *"lập hội đồng khoa"*.

**(B) Có đề tài rồi mới lập hội đồng cho đúng chuyên môn** — giống *"lập hội đồng chấm cho đề tài X"*.

Điều đáng nói: **model hiện tại đỡ được cả hai** (`CouncilProjectAssignment` là bảng nối nhiều-nhiều
tách rời). Nên đây thuần là câu hỏi **thứ tự thao tác trên màn hình**, không phải câu hỏi cấu trúc dữ liệu.

Câu thầy bạn nói mà bạn trích — *"hệ thống là mô phỏng thế giới thật"* — chính là cách chọn: hỏi
Phòng QLKH **thực tế họ làm cái nào trước**. Nếu chưa hỏi được thì cứ theo QĐ543 Điều 8: hội đồng
được lập **để xét duyệt các đề tài cụ thể**, nghiêng về **(B)**.

Dù chọn hướng nào, **thư mời vẫn phải nói rõ chấm đề tài nào** (§2.1).

### 3.2. "Ca chấm" có cần thành khái niệm riêng không?

Bạn hình dung: một hội đồng, nhiều ca; mỗi ca tick chọn vài đề tài.

**Tin tốt: dữ liệu đã đủ để làm mà không cần thêm bảng nào.** `CouncilMeeting` = ca chấm (một hội
đồng nhiều buổi), `CouncilProjectAssignment.MeetingId + SlotStartAt + SlotOrder` = đề tài nào nằm ca
nào, giờ nào.

Còn thiếu **duy nhất phần giao diện**: màn cho Staff tạo nhiều buổi cho một hội đồng, rồi kéo/tick
đề tài vào từng buổi.

Câu chuyện review đồ án của bạn (4–5 nhóm cùng phòng cùng giờ, thầy gọi nhóm nào đủ thì lên) —
model hiện tại **mô tả được đúng như vậy**: một buổi, nhiều đề tài, mỗi đề tài một slot.

### 3.3. PI có thấy danh mục đề tài đặt hàng không?

Bạn ghi *"t có nghĩ về vụ PI thấy đề tài… mô tả… chưa có cái đó"*. Đây là **luồng Ứng dụng**
(rule #8): Staff đăng danh mục đặt hàng → nhiều PI đăng ký cùng một đề tài → chọn ra người được duyệt.

Luồng đang chạy là **Cơ bản** (PI tự đề xuất, rule #9). Luồng Ứng dụng nhiều-người-đăng-ký vẫn là
epic tương lai. **Cần bạn quyết có đưa vào phạm vi bảo vệ hay không** — nó không nhỏ.

### 3.4. Bớt loại đơn điều chỉnh xuống còn mỗi "gia hạn"?

Bạn định xoá bớt cho đỡ đau đầu. Cân nhắc ngược lại: QĐ543 Điều 6.1 nói tới *"sửa đổi, bổ sung nội
dung"* nói chung, không chỉ thời gian. Xoá hết chỉ giữ gia hạn thì **hẹp hơn quy định**.

Đề xuất: **giữ nguyên các loại, chỉ làm kỹ (test + xuất phụ lục) cho loại gia hạn.** Loại khác vẫn
gửi/duyệt được, chỉ chưa có văn bản đi kèm. Như vậy không mất tính đúng, mà cũng không phải làm hết.

---

## 4. Nên làm theo thứ tự nào

Xếp theo **đau ÷ công**, không phải theo thứ tự trong luồng.

| # | Việc | Vì sao ưu tiên vậy | Công |
|---|---|---|---|
| 1 | **§2.2** AI nghiệm thu đọc đúng hồ sơ | Sai mà nghe vẫn hợp lý ⇒ nguy nhất. Hội đồng hỏi trúng là lộ | Vừa |
| 2 | **§2.1** Thư mời ghi rõ đề tài + chặn mời khi chưa gán | Nhìn thấy ngay khi demo. Cần chốt §3.1 trước | Nhỏ–vừa |
| 3 | **§2.6** Màn giải ngân hiện điều kiện từng đợt | Rẻ, và gỡ đúng chỗ chính bạn còn lú | Nhỏ |
| 4 | **§2.5** Chặn gia hạn > 1/2 thời gian | Một phép so sánh, mà rất "có quy định" | Rất nhỏ |
| 5 | **§2.3** Màn nhật ký gửi thư | Rẻ, trả lời thẳng câu "mail hỏng thì sao" | Nhỏ |
| 6 | **§3.2** Giao diện nhiều ca chấm + gán đề tài vào ca | Không cần đổi DB. Nhưng cần nhiều đề tài mẫu để demo | Vừa–lớn |
| 7 | **§2.4** Bên A gửi điều chỉnh + xuất phụ lục | Lệch quy định thật, nhưng ít ai bấm trúng lúc demo | Vừa |
| 8 | **§2.7** Nhánh "đình chỉ đề tài" | Cùng lý do trên | Vừa |
| 9 | **§2.10** PI xác nhận trước khi xuất hợp đồng | | Nhỏ |
| 10 | **§2.9** Cảnh báo quá tải buổi họp · **§2.8** hạn 30 ngày | Cảnh báo mềm, không chặn | Nhỏ |
| 11 | **§2.11** Xuất lịch · **§2.12** ảnh chụp DB | Tiện ích, không ảnh hưởng nghiệp vụ | Nhỏ |

**Nếu chỉ kịp làm 4 việc: 1, 2, 3, 4.** Ba trong bốn cái đó nhỏ, và cái số 1 là chỗ duy nhất hệ
thống đang **nói sai** chứ không phải nói thiếu.

---

## 5. Ghi chú kiểm chứng

- Toàn bộ §1 và §2 đối chiếu **code thật** ở `D:\capstone\FURPMS_BEv2` (nhánh `dev`, `5536b5e`) và
  FE `core/FURPMS-Web` (nhánh `dev`, `600f479`). Số dòng đúng tại thời điểm 14/08/2026.
- Trích dẫn QĐ543 lấy trực tiếp từ `docs/QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx`,
  không lấy từ trí nhớ.
- **Chưa sửa dòng code nào. Chưa commit. Chưa push.**

### Còn chưa kiểm được

Nói rõ để không hiểu nhầm là đã soát hết:

- **Biên bản có 3 lựa chọn kết quả** (Đạt / Không đạt / Cần chỉnh sửa) — bạn bảo thường chỉ thử 1.
  Tôi mới xác nhận cả 3 đều **được nhận** ở tầng lưu (`ReviewScoringService.cs:260`), **chưa** đi
  hết hậu quả của "Cần chỉnh sửa" và "Không đạt" trên giao diện.
- **Luồng nghiệm thu chạy thật đầu-cuối** — mới đọc code, chưa bấm tay qua toàn bộ.
- **Xuất Word của phụ lục / phiếu điều chỉnh** — chưa có nên chưa có gì để xem.

---

*Liên quan: `BACKLOG_Uu_tien.md` (P2/P3 cũ) · `HANDOFF_HIEN_HANH.md` (hiện trạng + deploy) ·
`Process_Spec_v2.md` (luồng nghiệp vụ) · `CLAUDE.md` §Business Rules (rule #15 không quản tiền,
#8/#9 hai luồng nộp).*
