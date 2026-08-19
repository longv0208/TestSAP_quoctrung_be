# FURPMS — Demo Script nộp trước buổi bảo vệ

> Tài liệu nộp cho giảng viên/hội đồng trước buổi demo. Nội dung gồm: danh sách Use Case sẽ demo,
> thứ tự trình diễn, phân công người trình bày, dữ liệu và tài khoản kiểm thử, kịch bản bấm trên hệ
> thống, phương án dự phòng và checklist xác nhận trước khi nộp.

## 1. Thông tin bản demo

| Mục | Nội dung |
|---|---|
| Tên hệ thống | FURPMS — FPT University Research Project Management System |
| Phạm vi demo | Vòng đời đề tài nghiên cứu từ mở đợt đến theo dõi danh mục hoàn thành |
| Số workflow | 8 workflow, bám theo RP6 — Software User Guide |
| Số thành viên trình bày | 4 người: Dũng, Phát, Thứ, Trung |
| Thời lượng mục tiêu | 18 phút; tối đa 20 phút |
| Môi trường chính | Local: FE `http://localhost:5173`, API `http://localhost:5068` |
| Dữ liệu | PostgreSQL đã migration và seed bộ dữ liệu demo |
| Mật khẩu tài khoản mẫu | `password` |
| Ngày chạy thử cuối | Điền trước khi nộp: `____/____/2026` |
| Người xác nhận bản nộp | Điền trước khi nộp: `________________` |

## 2. Danh sách workflow và Use Case sẽ demo

| Thứ tự | Workflow | Use Case liên quan | Vai hệ thống | Người trình bày | Thời lượng |
|---:|---|---|---|---|---:|
| 1 | Open a research cycle | UC-05, **UC-06**, UC-08 | Administrator | **Dũng** | 1:30 |
| 2 | Submit a research proposal | **UC-09**, UC-10, UC-11, UC-12, UC-23 | PI | **Phát** | 2:30 |
| 3 | Open a review round and form a council | UC-14, UC-15, **UC-16**, UC-17 | Staff | **Thứ** | 2:30 |
| 4 | Respond to invitation and score a proposal | **UC-20**, UC-21, UC-22, UC-34 | Review Committee | **Thứ** | 3:30 |
| 5 | Create and track the research contract | **UC-25**, UC-26, UC-27 | Staff | **Trung** | 2:00 |
| 6 | Report progress and request an amendment | **UC-28**, UC-31, UC-32 | PI + Staff | **Trung** | 2:00 |
| 7 | Submit the final report and complete acceptance | **UC-29**, UC-22, UC-30 | PI + Staff + Committee | **Trung** | 2:30 |
| 8 | Monitor the portfolio | **UC-33**, UC-34, UC-35 | Admin + Staff | **Dũng** | 1:30 |
|  | **Tổng** |  |  |  | **18:00** |

### Phạm vi phải nói rõ

- Demo tập trung vào các UC hoàn thành và các phần lõi đã chạy được của UC `Partial`.
- Không demo UC-18 đồng bộ Google/Outlook Calendar và UC-24 tìm kiếm ngữ nghĩa vì chưa hoàn thành.
- Không trình bày UC-07 như chức năng kế toán: hệ thống chỉ quản lý mốc giải ngân và minh chứng,
  không trực tiếp chuyển tiền.
- Không trình bày UC-19 như hai vòng Khoa học → Tài chính. Phạm vi hiện hành có vòng xét duyệt đề
  cương và vòng nghiệm thu; báo cáo tiến độ do Staff đánh giá.
- AI chỉ hỗ trợ trích xuất và gợi ý. Người dùng kiểm tra, chỉnh sửa và chịu trách nhiệm về kết quả cuối.

## 3. Phân công trình bày chính thức

| Người | Phần phụ trách | Điểm phải nhấn mạnh | Câu bàn giao |
|---|---|---|---|
| **Dũng** | Mở đầu, WF1 và WF8 | Đóng khung vòng đời; cấu hình đợt ở đầu; dashboard và timeline ở cuối | “Sau khi đợt được mở, Phát sẽ trình bày cách chủ nhiệm nộp một đề cương vào đúng đợt này.” |
| **Phát** | WF2 | Upload Word, AI trích xuất, người dùng kiểm tra, thành viên, sản phẩm và kinh phí | “Đề cương sau khi nộp được chuyển sang Phòng QLKH; Thứ sẽ mở vòng và tổ chức hội đồng xét duyệt.” |
| **Thứ** | WF3 và WF4 | Lập hội đồng, xung đột lợi ích/lịch, thư mời, AI gợi ý điểm, Thư ký soạn và Chủ tịch chốt | “Khi hội đồng chốt đề tài đạt, Trung sẽ tiếp nối bằng hợp đồng và toàn bộ giai đoạn thực hiện.” |
| **Trung** | WF5, WF6 và WF7 | Hợp đồng → tiến độ/điều chỉnh → báo cáo tổng kết/nghiệm thu/thanh lý | “Đề tài đã khép vòng nghiệp vụ; Dũng sẽ quay lại dashboard để tổng hợp toàn bộ danh mục.” |

**Nguyên tắc:** ai nói thì người đó bấm. Bốn phần nối tiếp thành một câu chuyện; không giới thiệu lại
hệ thống khi đổi người và không đăng nhập lại trước hội đồng.

## 4. Tài khoản kiểm thử

Mọi tài khoản dưới đây dùng mật khẩu `password` trong môi trường Development.

| Phiên mở sẵn | Vai | Email | Dùng ở workflow |
|---|---|---|---|
| Cửa sổ A | Administrator | `admin@furpms.edu.vn` | WF1, WF8 |
| Cửa sổ B | Staff — Phòng QLKH | `staff.demo@furpms.edu.vn` | WF3, WF5, WF6, WF7, WF8 |
| Cửa sổ C | PI 1 | `pi.demo@furpms.edu.vn` | WF2, WF6, WF7 |
| Cửa sổ D | Chủ tịch hội đồng | `reviewer1.demo@furpms.edu.vn` | WF4, WF7 — chốt biên bản |
| Cửa sổ E | Thư ký hội đồng | `reviewer2.demo@furpms.edu.vn` | WF4, WF7 — soạn biên bản |
| Cửa sổ F | Phản biện | `reviewer3.demo@furpms.edu.vn` | WF7 — BM10 và phiếu nghiệm thu |
| Dự phòng | Thành viên hội đồng | `reviewer4.demo@furpms.edu.vn` | Bổ sung phiếu khi cần |
| Dự phòng | Thành viên hội đồng | `reviewer5.demo@furpms.edu.vn` | Bổ sung phiếu khi cần |

> Vai Chủ tịch/Thư ký/Phản biện là chức danh của thành viên trong từng hội đồng, không phải một role
> đăng nhập độc lập. Seeder cố định `reviewer1`, `reviewer2`, `reviewer3` theo thứ tự trên để demo dễ nhớ.

## 5. Dữ liệu demo cố định

| Workflow | Dữ liệu mở sẵn | Trạng thái trước khi demo | Thao tác chính |
|---:|---|---|---|
| WF1 | Đợt `UD26` và `CB26` | Một đợt mở, một đợt đã đóng nhận | Xem cấu hình; minh họa tạo/mở đợt bằng dữ liệu nháp nếu cần |
| WF2 | `NCKH-2026-001` | Bản nháp | Upload file Word hoặc mở bản nháp; AI điền form; kiểm tra và nộp |
| WF3 | `NCKH-2026-002` | Đã nộp, chưa vào vòng | Gán vào vòng xét duyệt; lập hội đồng; tạo lịch; gửi lời mời |
| WF4 | `NCKH-2026-003` | Đang xét duyệt, đã có 4/5 phiếu | Điền phiếu cuối; xem AI gợi ý; Thư ký soạn; Chủ tịch chốt |
| WF5 | `NCKH-2026-005` | Đã duyệt, chưa có hợp đồng | Tạo hợp đồng; xuất BM05; xem lịch giải ngân theo loại đề tài |
| WF6 | `NCKH-2026-006` | Hợp đồng đang hiệu lực; kỳ 1 chờ duyệt | Staff đánh giá tiến độ; PI xin điều chỉnh; Staff duyệt/từ chối |
| WF7 | `NCKH-2026-007` | Đang nghiệm thu | Mở hồ sơ; Phản biện BM10; bỏ phiếu BM11; chốt kết quả |
| WF8 | `NCKH-2026-008` | Hoàn thành | Xem timeline, quyết toán, thanh lý và dashboard tổng hợp |

Không dùng một đề tài duy nhất để diễn toàn bộ 8 workflow tại chỗ. Các đề tài được seed ở ngay trước
từng bước nhằm giảm thời gian nhập và tránh một lỗi giữa buổi làm mất toàn bộ phần sau.

## 6. Kịch bản thao tác và lời thoại

### WF1 — Dũng: Mở đợt nghiên cứu

1. Mở cửa sổ Admin → **Đợt nghiên cứu**.
2. Chỉ ra mỗi đợt có loại đề tài, năm, thời gian nhận hồ sơ và các lĩnh vực được mở.
3. Mở `UD26`; chỉ ra trạng thái đang mở và thời hạn nộp.
4. Nếu trình diễn tạo mới, chỉ lưu bản nháp hoặc dùng tên có tiền tố `[DEMO]`; không thay đổi đợt seed
   đang dùng cho các workflow sau.

**Lời nói gợi ý:** “Đợt nghiên cứu là cổng vào của quy trình. Hệ thống chỉ cho PI nộp trong khoảng
thời gian hợp lệ và lấy loại đề tài của đợt để áp trần kinh phí cùng lịch giải ngân tương ứng.”

### WF2 — Phát: Nộp đề cương có AI hỗ trợ

1. Chuyển cửa sổ PI → **Nộp đề cương** → chọn đúng đợt/lĩnh vực.
2. Upload file `.docx` đã chuẩn bị → **Phân tích bằng AI**.
3. Chỉ ra AI điền trước tiêu đề, mục tiêu, phương pháp và dữ liệu tìm được; PI vẫn được sửa.
4. Đi qua thành viên nhóm, sản phẩm dự kiến và dự toán kinh phí.
5. Chỉ ra validate trần kinh phí; không cố tình phá dữ liệu chính.
6. Lưu nháp rồi nộp, hoặc dùng `NCKH-2026-001` để hoàn tất bước cuối.

**Lời nói gợi ý:** “AI giảm nhập liệu nhưng không tự nộp. PI là người xác nhận dữ liệu, sửa kết quả
trích xuất và chịu trách nhiệm về hồ sơ cuối cùng.”

### WF3 — Thứ: Mở vòng, lập hội đồng và gửi lời mời

1. Chuyển cửa sổ Staff → **Hội đồng & Chấm** → chọn đợt/lĩnh vực của `NCKH-2026-002`.
2. Tạo hoặc mở vòng **Xét duyệt đề cương**; gán đề tài vào vòng.
3. Mở hội đồng đã chuẩn bị; chỉ ra số thành viên lẻ, đủ Chủ tịch, Thư ký và Phản biện.
4. Chỉ ra hệ thống chặn xung đột lợi ích và lịch họp bị trùng.
5. Tạo lịch hợp lệ rồi gửi lời mời. Giải thích một lời mời hội đồng có thể liệt kê nhiều đề tài,
   nhưng sau khi nhận, mỗi đề tài là một công việc chấm riêng.

**Lời nói gợi ý:** “Nút gửi chỉ khả dụng khi hội đồng đủ điều kiện và đã có lịch. Ràng buộc nằm ở
backend nên không thể bỏ qua bằng cách gọi API trực tiếp.”

### WF4 — Thứ: Hội đồng nhận lời, chấm và chốt

1. Mở cửa sổ reviewer; nếu cần chỉ nhanh tab **Lời mời** và danh sách đề tài được mời chấm.
2. Mở `NCKH-2026-003` trong **Đề tài được phân công**.
3. Chỉ ra file thuyết minh, ưu/nhược điểm và gợi ý điểm từ AI.
4. Dùng **Điền nhanh** hoặc nhập phiếu cuối; nhấn mạnh AI không tự nộp phiếu.
5. Chuyển cửa sổ Thư ký → tab **Biên bản** → xem tổng hợp phiếu và soạn biên bản.
6. Chuyển cửa sổ Chủ tịch → duyệt và khóa biên bản.
7. Chỉ ra sau khi khóa mới phát sinh kết quả chính thức; biên bản không còn chỉnh sửa tùy ý.

**Lời nói gợi ý:** “Điểm của từng thành viên là dữ liệu đầu vào. Thư ký tổng hợp, nhưng chỉ Chủ tịch
có thẩm quyền khóa kết luận; hệ thống không tự lấy trung bình rồi tự quyết định thay hội đồng.”

### WF5 — Trung: Lập và theo dõi hợp đồng

1. Chuyển cửa sổ Staff → **Hợp đồng** → tạo hợp đồng cho `NCKH-2026-005`.
2. Chỉ ra thông tin PI được lấy từ hồ sơ; Staff nhập phần hành chính còn thiếu.
3. Xuất hợp đồng Word BM05; giải thích hệ thống lưu hồ sơ, không giả làm nền tảng ký điện tử.
4. Mở tab giải ngân và chỉ ra lịch sinh theo loại đề tài:
   - Nghiên cứu ứng dụng: bốn đợt theo mẫu cấu hình.
   - Nghiên cứu cơ bản: một đợt sau nghiệm thu.
5. Chỉ ra phải có điều kiện và minh chứng trước khi ghi nhận một mốc giải ngân.

### WF6 — Trung: Báo cáo tiến độ và yêu cầu điều chỉnh

1. Mở `NCKH-2026-006` → kỳ báo cáo tiến độ đang chờ.
2. Ở Staff, mở file/nội dung PI đã nộp rồi đánh giá Đạt/Không đạt/Có điều kiện.
3. Ở PI, mở **Điều chỉnh hợp đồng** → chọn gia hạn thời gian → nhập thời gian đề nghị và lý do.
4. Quay lại Staff → xem giá trị hiện tại/đề nghị và duyệt hoặc từ chối.
5. Chỉ ra lịch sử yêu cầu được giữ lại; không ghi đè âm thầm lên dữ liệu gốc.

### WF7 — Trung: Báo cáo tổng kết, nghiệm thu và kết thúc hợp đồng

1. Ở PI, chỉ ra báo cáo tổng kết có thể nộp file hoặc liên kết hợp lệ và chọn ngôn ngữ.
2. Ở Staff, mở hồ sơ nghiệm thu `NCKH-2026-007`: báo cáo tổng kết, sản phẩm và tiến độ.
3. Ở Phản biện, hoàn tất nhận xét BM10; nhấn mạnh chỉ Phản biện viết phiếu này.
4. Các thành viên bỏ phiếu BM11 Đạt/Không đạt; Thư ký soạn và Chủ tịch chốt biên bản.
5. Chỉ ra nghiệm thu Đạt là điều kiện để hoàn tất quyết toán/thanh lý, không tự động xóa lịch sử
   hay biến việc thanh lý thành một giao dịch tiền trong hệ thống.

### WF8 — Dũng: Theo dõi toàn bộ danh mục

1. Mở `NCKH-2026-008` để xem timeline đầy đủ từ đề cương đến hoàn thành.
2. Mở dashboard/Thống kê → lọc theo đợt, lĩnh vực và trạng thái.
3. Chỉ ra notification và các mốc còn chờ xử lý.
4. Kết luận bằng giá trị của hệ thống: một nguồn dữ liệu xuyên suốt cho Admin, Staff, PI và hội đồng.

**Câu kết:** “FURPMS không chỉ lưu một bộ hồ sơ; hệ thống kiểm soát thứ tự nghiệp vụ và lưu dấu vết
ra quyết định xuyên suốt vòng đời đề tài.”

## 7. Chuẩn bị cửa sổ trước khi bước vào phòng

| Cửa sổ | Tài khoản | Trang mở sẵn |
|---|---|---|
| A | Admin | `/research-cycles` |
| B | Staff | `/review-board` với đúng đợt/lĩnh vực |
| C | PI | `/proposals/submit` hoặc bản nháp `NCKH-2026-001` |
| D | Reviewer 1 | Chi tiết chấm `NCKH-2026-003` |
| E | Reviewer 2 | Biên bản `NCKH-2026-003` |
| F | Reviewer 3 | Hồ sơ nghiệm thu `NCKH-2026-007` |

- Dùng một máy và chia sẻ màn hình một lần.
- Mỗi vai dùng một profile/cửa sổ riêng để giữ phiên đăng nhập.
- Đặt tên cửa sổ theo vai; không dùng chung một tài khoản cho nhiều vai.
- Thu gọn bookmark bar, đóng tab cá nhân và tắt popup không liên quan.
- Mỗi người ngồi theo thứ tự Dũng → Phát → Thứ → Trung; cuối phần Trung chuyển lại Dũng.

## 8. Phương án khi AI hoặc dịch vụ ngoài không ổn định

1. Trước buổi, chạy AI thật ít nhất một lần với file Word sẽ demo và xác nhận trạng thái Completed.
2. Giữ `NCKH-2026-003` có kết quả AI seed/cache để WF4 vẫn hiển thị tức thì.
3. Nếu Gemini 429/503: nói rõ cơ chế retry/cooldown, dùng kết quả lưu gần nhất rồi tiếp tục; không bấm
   liên tục và không để phần AI chặn toàn bộ mainflow.
4. Nếu email thật chậm: mở notification trong hệ thống và email log; không chờ hộp thư tại chỗ.
5. Nếu mạng ngoài mất: demo local; file Word, hợp đồng và dữ liệu seed phải có sẵn trên máy.
6. Chuẩn bị video dự phòng ngắn cho WF2 và WF4, là hai phần phụ thuộc AI nhiều nhất.

## 9. Checklist nộp trước buổi

### Tệp nộp

- [ ] File Demo Script này đã điền ngày, người xác nhận và thời lượng chạy thử.
- [ ] Bảng Use Case Status & Traceability đã nộp kèm.
- [ ] Slide dùng đúng 8 workflow và đúng bốn tên Dũng, Phát, Thứ, Trung.
- [ ] Link bản deploy hoặc hướng dẫn chạy local đã được kiểm tra từ một máy khác.
- [ ] Danh sách tài khoản test và mật khẩu demo đã gửi cho giảng viên nếu được yêu cầu.
- [ ] File Word proposal dùng cho AI và một file dự phòng đã đặt trong thư mục nộp.
- [ ] Video dự phòng WF2/WF4 đã mở thử, có âm thanh/hình rõ và không lộ API key.

### Kiểm tra kỹ thuật

- [ ] PostgreSQL healthy; migration và seed chạy không lỗi.
- [ ] API `/swagger` mở được; FE đăng nhập được bằng cả bốn vai.
- [ ] Không có hai instance backend tranh cổng `5068`.
- [ ] FE trỏ đúng API sẽ demo.
- [ ] Gemini model/API key hoạt động; đã có cache dự phòng.
- [ ] Hội đồng của `NCKH-2026-003` gắn đúng rubric BM03 100 điểm.
- [ ] Hội đồng nghiệm thu có đúng người giữ vai Phản biện.
- [ ] File proposal, hợp đồng, minh chứng và báo cáo mở được, không có link giả/404.
- [ ] Chuông thông báo xuống dòng đầy đủ và bấm mở chi tiết được.
- [ ] Đồng hồ hệ thống ở đúng mốc; không còn offset từ lần test trước.

### Chạy thử nhóm

- [ ] Chạy liền mạch lần 1, có bấm giờ: `____ phút ____ giây`.
- [ ] Sửa các điểm vấp và chạy lần 2: `____ phút ____ giây`.
- [ ] Mỗi người tự bấm đúng phần mình và nói được mục tiêu nghiệp vụ.
- [ ] Thứ đã tập riêng đoạn chuyển Staff → Reviewer → Thư ký → Chủ tịch.
- [ ] Trung đã tập WF5 → WF6 → WF7 thành một mạch, không quay lại màn hình thừa.
- [ ] Dũng giữ được tổng thời gian và biết cắt phần tùy chọn nếu gần hết giờ.

## 10. Bản rút gọn khi chỉ có 12 phút

| Người | Giữ lại | Cắt bớt |
|---|---|---|
| Dũng | Mở `UD26`, nói rule thời gian; kết bằng dashboard | Không tạo đợt mới |
| Phát | Upload Word → AI prefill → xem nhanh bước Review | Không nhập lại toàn bộ form |
| Thứ | Mở hội đồng đã lập; phiếu cuối → biên bản → Chủ tịch chốt | Không tạo từng thành viên/lịch tại chỗ |
| Trung | Mở hợp đồng; tiến độ; hồ sơ nghiệm thu; timeline hoàn thành | Không xuất/tải từng biểu mẫu |

Không được cắt: AI có human-in-the-loop, chốt biên bản theo thẩm quyền, nghiệm thu hai tầng và màn
timeline hoàn thành. Đây là bốn điểm phân biệt hệ thống với một ứng dụng CRUD thông thường.

## 11. Tài liệu đối chiếu

- `USE_CASE_STATUS_TRACEABILITY.md`: trạng thái và traceability đầy đủ 35 Use Case.
- `DEMO_GUIDE.md`: dữ liệu seed và hướng dẫn từng ca demo.
- `KICH_BAN_DEMO.md`: thao tác chi tiết và các lỗi dễ vấp.
- `TEST_CHECKLIST.md`: kết quả mong đợi theo từng vai.
- `BUSINESS_RULES.md`: căn cứ và nơi hiện thực từng ràng buộc.
- `QD543_Compliance.md`: đối chiếu quy định QĐ543 với hệ thống.

