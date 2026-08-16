# Rà soát 7 báo cáo Capstone (RP1–RP7) — đối chiếu mẫu FLM + Cẩm nang tránh lỗi

> Soát ngày **16/08/2026**. Đối chiếu ba nguồn: **mẫu chính thức FLM** (`flm down/`),
> **`SEP490 StudentGuide_Fall 2023.docx`** (danh sách nộp chính thức), và
> **`Cam-nang-tranh-loi-Capstone-SE.pdf`**.
>
> Mọi con số dưới đây đọc trực tiếp từ file `.docx`/`.xlsx` và từ repo, không ước lượng.

---

## 1. Trọng số điểm — quyết định thứ tự ưu tiên

Từ StudentGuide §5. **Đây là lý do không nên dồn sức vào RP6.**

| Báo cáo | OGA | TDA | Trạng thái hiện tại |
|---|--:|--:|---|
| RP1 Project Introduction | 4% | 5% | Đủ nội dung |
| RP2 Project Management Plan | 8% | 5% | Đủ nội dung |
| RP3 SRS | 16% | 15% | Đủ nội dung, còn 2 chỗ `<…>` |
| RP4 SDD | 18% | 10% | 18 hình, **0 caption**, 16 tiêu đề còn `<…>` |
| RP5 Test Documentation | 18% | 10% | ⚠️ Xem §3 — số liệu mâu thuẫn |
| RP6 User Guides | 4% | 5% | ✅ Đã hoàn thiện 16/08 |
| **RP7 Final Project Report** | **32%** | **35%** | 🔴 **Phần V và VI rỗng hoàn toàn** |

**RP7 một mình chiếm 32% OGA + 35% TDA — nhiều hơn tổng RP1+RP2+RP3+RP6 cộng lại.**
Mà đây lại đang là file dở nhất.

---

## 2. 🔴 Thiếu file bắt buộc — chưa có trong `docs/report/`

StudentGuide §III.2 liệt kê hai nhóm tài liệu theo dõi **bắt buộc**, hiện chưa có file nào:

| File mẫu FLM | Dùng làm gì | Có chưa |
|---|---|---|
| `Report3_Project Tracking.xlsx` | 4 sheet: **WBS · Issues · Defects · Q&A** | ❌ **Chưa có** |
| `Project Weekly Report_GroupName.xlsx` | Báo cáo tuần nộp cho GVHD | ❌ **Chưa có** |
| `Report2_Sample Project Schedule` | Lịch dự án (MS Project / Project Plan) | ❌ **Chưa có** |

Bảng mốc StudentGuide §4 ghi rõ, mốc 9 (tuần 14): *"Outputs: (1) Report 6: User Guides
**(2) Updated Project Schedule/Tracking**"* — tức là lịch/tracking phải nộp **cùng** RP6.

> Có đường lùi: StudentGuide cho phép *"track that information using any team-convenient
> tools (GitLab, Trello, Asana…)"*. Nếu chọn đường này thì **phải chỉ ra được** bảng
> issue/defect thật khi hội đồng hỏi. Nhưng an toàn nhất vẫn là điền file mẫu.

---

## 3. ⚠️ RP5 — số liệu mâu thuẫn giữa ba nguồn

Đây là chỗ user nghi ngờ *"bạn đưa file cho AI làm và không check lại"*. **Nghi ngờ có cơ sở.**

### 3.1. Ba con số khác nhau cho cùng một thứ

| Nguồn | Số unit test |
|---|--:|
| Chữ trong RP5 | *"99 test scripts matching the source code configuration"* |
| `Report5_Unit_Test_Final.xlsx` → sheet Statistics | **39** (5 function) |
| Repo thật — `dotnet test` | **190** (24 lớp test) |

Không con số nào khớp con số nào. Cẩm nang §6 gọi đúng tên: *"Số liệu tổng hợp trong
test report **sai và không nhất quán**"*.

Con số **46 API controller** thì đúng (đã đếm lại: 46).

### 3.2. Bảng Excel chỉ phủ 5/24 lớp test

`Report5_Unit_Test_Final.xlsx` chỉ có sheet cho: `BudgetCapPolicy`, `ContractLifecycle`,
`CouncilMinutes`, `CycleValidation`, `ProposalSubmission`.

Repo có **24** lớp test. **19 lớp không được ghi vào tài liệu**, trong đó có những lớp
đáng kể: `AcademicWorksTests`, `ContractSigningTests`, `ExportCultureTests`,
`MeetingVisibilityTests`, `PostgresConnectionStringTests`, `ProgressReportEvaluationTests`,
`ResearchOrderWinnerTests`, `SystemSettingServiceTests`…

### 3.3. Nội dung thật bị để màu XANH như chưa điền

4 đoạn **nội dung thật** đang mang màu placeholder `#0000ff`, trong đó có **cả tiêu đề
"2. Test Strategy"**. Người chấm mở ra thấy chữ xanh nghiêng sẽ tưởng chưa làm. Đây đúng
là cái "trông xấu, trông lỗi" mà user nhìn thấy.

Các đoạn dính: *Constraints or Assumptions…* · tiêu đề *2. Test Strategy* ·
*Details for Unit Test Cases (99…)* · *Details for Comprehensive System Test Cases (55…)* ·
*The testing cycle was completed with profound depth…*

### 3.4. Văn phong máy sinh

*"completed with **profound depth** covering all project layers"* — cẩm nang §1.4 nêu đích
danh: *"Văn phong chung chung, không gắn với đặc thù đề tài"*. Nên viết lại mộc mạc.

### 3.5. Danh sách gạch đầu dòng không phải bullet thật

Toàn bộ mục "Core Modules in Scope" và "Stages/Levels of Testing" là **đoạn văn thường**,
không dùng bullet. Nhìn thành một khối chữ dày đặc — góp phần vào cảm giác "xấu".

**Cấu trúc mục thì khớp mẫu rỗng 100%** (đã so từng heading) — vấn đề hoàn toàn nằm ở
nội dung và định dạng, không phải bố cục.

---

## 4. 🔴 RP7 — bản đồ chính xác chỗ còn rỗng

| Phần | Trạng thái | Lấy nội dung từ đâu |
|---|---|---|
| I. Project Introduction | ✅ Đủ | — |
| II. Project Management Plan | ✅ Đủ | — |
| III. SRS | ✅ Đủ | — |
| IV. Software Design | ⚠️ 7 tiểu mục `x.2` còn `<…>` | Cần viết mới (sequence/flow từng module) |
| **V. Software Testing** | 🔴 **Rỗng hoàn toàn** — 15 đoạn placeholder | **Copy từ RP5** (sau khi sửa §3) |
| **VI. Release & User Guides** | ✅ **XONG 16/08** — 8 workflow, 19 hình, 3 bảng, caption + cross-ref đầy đủ | đã chép từ RP6 |

Bảy tiêu đề phần IV còn nguyên dấu ngoặc nhọn:
`3.1.2 <Login & Session Initiation>` · `3.2.2 <Create & Open Research Cycle>` ·
`3.3.2 <Proposal Submission & AI Similarity Validation>` ·
`3.4.2 <Reviewer Assignment & Invitation Workflow>` ·
`3.5.2 <Meeting Schedule & Google Meet Link Generation>` ·
`3.6.2 <Proposal Review & Scoring Workflow>` · `3.7.2 <Contract Reporting Workflow>`

**Trang bìa** còn `[FPT University Research Project Management System]` — trong ngoặc vuông.

> ⚠️ `3.5.2 <Meeting Schedule & **Google Meet Link Generation**>` — hệ thống **chưa tích hợp
> Google Meet**. Viết mục này theo đúng tên tiêu đề là mô tả chức năng không tồn tại
> (cẩm nang §1.5). Nên đổi tiêu đề thành *"Meeting Scheduling"*.

---

## 4b. 🔴 RP4 — sơ đồ vẽ sai so với code (soát 16/08)

RP4 chiếm **18% OGA**. Đã trích 18 ảnh ra xem từng cái.

### Sequence diagram — làm tốt về kỹ thuật

Bảng kiểm Cẩm nang §2.2, sơ đồ `3.1.2 Login & Session Initiation` **đạt gần hết**:
đánh số message 1–19 ✅ · activation bar ✅ · đủ tầng Controller → Service → Repository → DB ✅ ·
hộp `alt` chia 2 nhánh có nhãn ✅ · có luồng exception (401) ✅ · mũi tên trả về nét đứt ✅

Chỉ thiếu: **dấu `:` trước tên object** (`LoginView` nên là `:LoginView`) — Cẩm nang §2.2 liệt kê
đích danh *"Thiếu dấu `:` trong tên object"*.

### Nhưng nội dung sai so với repo

| Sơ đồ vẽ | Thực tế trong code |
|---|---|
| Lifeline **`UnitOfWork`** | ❌ **Không tồn tại** — `grep UnitOfWork` = 0 file. Kiến trúc thật là Controller → Service → Repository → DbContext (CLAUDE.md) |
| *"Enter **username** & password"*, `Users.GetByUsernameAsync`, `SELECT … WHERE Username = @username` | ❌ Đăng nhập bằng **EMAIL**. `LoginRequest` có `[Required, EmailAddress] public string Email` |
| `AuthenticateAsync(loginDto)` | ⚠️ Tên thật là `LoginAsync(LoginRequest)` |
| `UserRepo`, `AuthStore` | ✅ Có thật (`UserRepository.cs`, FE `authStore`) |

Cẩm nang §13 câu 3 hỏi thẳng: *"Chỉ vào sơ đồ kiến trúc và **mở đoạn code tương ứng**"* —
với `UnitOfWork` thì không mở được gì.

### 🔴 Sơ đồ System Architecture (§1.1) — sai 6 chỗ

Cẩm nang §2.4 mở đầu: *"Đây là sơ đồ **bị hỏi xoáy nhiều nhất**, vì nó được dùng để kiểm tra
bạn có thật sự hiểu hệ thống của mình không."*

| Sơ đồ vẽ | Thực tế |
|---|---|
| **SQL Server** | ❌ **PostgreSQL** — `UseNpgsql(...)`, chuỗi kết nối `Host=…` |
| **Google Meet** | ❌ 0 file. Không tích hợp |
| **Google Calendar** | ❌ 0 file. Không tích hợp |
| **Google SMTP Email** | ❌ Dùng **Brevo** — `smtp-relay.brevo.com` |
| *(không có)* | ❌ **Thiếu Cloudinary** — 7 file dùng, là nơi lưu mọi file upload. Cẩm nang §2.4: *"Thiếu đường nối tới third-party service"* |
| **React Native + Expo (mobile)** | ❌ Chỉ có repo `FURPMS-Web`. Không có mã nguồn mobile nào |

Ngoài ra sơ đồ đang là **bộ sưu tập logo**, không phải sơ đồ kiến trúc: mọi khối nối vào một
hộp giữa tên "FPT University research project management system" — không thể hiện được ai gọi ai.
Cẩm nang §2.4 còn nêu: *"Vẽ third-party chỉ nối với Backend trong khi thực tế Frontend cũng gọi"*.

**Đây là chỗ nguy hiểm nhất trong toàn bộ hồ sơ**: sơ đồ bị hỏi nhiều nhất, lại sai nhiều nhất,
và mỗi cái sai đều kiểm chứng được trong 10 giây bằng cách mở repo.

**Việc cần làm:** vẽ lại — PostgreSQL thay SQL Server · bỏ Google Meet + Google Calendar ·
đổi SMTP thành Brevo · thêm Cloudinary · **quyết định bỏ hẳn nhánh mobile hay giữ**
(RP3 cũng đang ghi *"web and mobile application"*, sẽ bị hỏi *"cho xem app mobile"*).

---

## 5. Lỗi chung cả 7 báo cáo — theo Cẩm nang §1.2 và §1.6

| Hạng mục cẩm nang bắt buộc | RP1 | RP2 | RP3 | RP4 | RP5 | RP6 | RP7 |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| **List of Figures** | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| **List of Tables** | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Caption cho hình (đặt **dưới** hình) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ 19 | ❌ |
| Caption cho bảng (đặt **trên** bảng) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ 3 | ❌ |
| Cross-reference "xem Figure N" trong bài | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ 18 | ❌ |
| Số hình trong file | 2 | 3 | 8 | 18 | 1 | 19 | 29 |

**RP4 đáng lo nhất trong nhóm này**: 18 hình, không hình nào có caption, không hình nào
được nhắc tới trong bài. Cẩm nang §1.6 mở đầu bằng câu *"lỗi kỹ thuật trình bày rất hay
bị bỏ qua nhưng **luôn bị bắt**"*. RP4 chiếm 18% OGA.

---

## 6. Sơ đồ luồng của nhóm vs hệ thống thật

Nhóm đã vẽ 7 "MAIN FLOW". Đối chiếu với code, **hai chỗ mô tả chức năng không tồn tại**:

| Sơ đồ | Node vẽ | Thực tế trong code |
|---|---|---|
| MAIN FLOW 2 | *"Use AI to find reviewer"* → *"View recommended list"* | ❌ **Không có.** Không endpoint nào gợi ý reviewer. AI chỉ có: tóm tắt đề cương, nhận xét, kiểm tra nhất quán, gợi ý điểm, review-kit |
| MAIN FLOW 7 | *"Generate AI insight"* | ❌ **Không có.** `AnalyticsController` chỉ có overview / by-track / funnel / 3 dashboard |
| MAIN FLOW 3 | *"Calculate avg score"* → quyết định | ⚠️ Hệ thống **có tính** điểm trung bình nhưng **không tự chốt kết quả** — quyết định là của Chủ tịch (rule #12). Sơ đồ đang ngụ ý hệ thống tự quyết |
| MAIN FLOW 5 | *"Calculate maximum extension"* | ⚠️ **Chưa hiện thực.** QĐ543 Điều 10.4 giới hạn gia hạn ≤ 1/2 thời gian, code không kiểm |
| MAIN FLOW 4 | *"Create online meeting"* | ⚠️ Hệ thống hỗ trợ **cả** trực tiếp lẫn trực tuyến; sơ đồ chỉ vẽ online |

Cẩm nang §1.5 và §2.6 đều phạt nặng chỗ này. **Hai lựa chọn:** sửa sơ đồ cho khớp code,
hoặc làm thêm chức năng. Với thời gian còn lại, **sửa sơ đồ** là lựa chọn đúng.

---

## 7. Việc phải làm — xếp theo điểm ÷ công

| # | Việc | Vì sao | Ai |
|---|---|---|---|
| 1 | **RP7 phần VI** ← copy nguyên từ RP6 | 32% OGA. Nội dung đã có sẵn, chỉ dán | Có thể tự động hoá |
| 2 | **RP7 phần V** ← copy từ RP5 *(sau khi sửa §3)* | 32% OGA | Sau khi RP5 sạch |
| 3 | **Sửa 3 con số RP5** cho khớp repo (190 test / 24 lớp) | Người chấm mở repo đối chiếu là lộ | Người |
| 4 | **Bỏ màu xanh** khỏi 4 đoạn nội dung thật RP5 | 30 giây, mà đang làm cả file trông như chưa xong | Người / tự động |
| 5 | **Thêm caption + cross-ref cho RP4** (18 hình) | 18% OGA, "luôn bị bắt" | Tự động hoá được |
| 6 | **Điền `Report3_Project Tracking.xlsx`** | File bắt buộc, đang thiếu hẳn | Người |
| 7 | **Sửa 7 tiêu đề `<…>` phần IV RP7** + bỏ Google Meet | Placeholder lộ + mô tả chức năng không có | Người |
| 8 | **Sửa 2 sơ đồ** (bỏ AI find reviewer, AI insight) | Mô tả chức năng không tồn tại | Người |
| 9 | List of Figures/Tables cho RP1–RP5, RP7 | Cẩm nang §1.2 nêu đích danh | Tự động hoá được |

---

## 8. Đã làm — RP6 (16/08/2026)

Từ bản mẫu FLM gốc, điền toàn bộ, **0 placeholder còn sót**:

- **Mục 1** — bảng Deliverable Package: điền đủ 9 dòng Description bằng tên file/thư mục thật
- **Mục 2.1** — System Requirements: .NET 8 · PostgreSQL 16 · Node 18+ · Docker · Gemini (tuỳ chọn)
- **Mục 2.2** — 5 bước cài đặt thật, chạy được từ máy trắng
- **Mục 3** — **8 workflow** phủ trọn vòng đời: mở đợt → nộp đề cương → lập hội đồng →
  chấm → hợp đồng → báo cáo tiến độ → nghiệm thu → thống kê
- **19 hình chụp trực tiếp trên hệ thống đang chạy, giao diện tiếng Anh**
- Caption hình **dưới** hình, caption bảng **trên** bảng (đúng cẩm nang §1.6)
- Đánh số bằng **trường SEQ** — thêm/bớt hình Word tự đánh lại, không lệch
- **18 cross-reference** trong thân bài
- **List of Figures + List of Tables** bằng trường TOC tự động
- Mục lục cũ gõ tay đã sai (vẫn ghi "Workflow 1/2") → thay bằng trường TOC
- Record of Changes: sửa dòng mô tả nhầm sang nội dung của RP5

**Sau khi mở file: bấm `Ctrl+A` rồi `F9`** để Word dựng mục lục, danh mục hình/bảng và số trang.

---

*Liên quan: `RA_SOAT_LUONG_HAPPY_CASE.md` (lỗ hổng nghiệp vụ) ·
`HANDOFF_HIEN_HANH.md` (hiện trạng hệ thống + deploy) · `../CLAUDE.md` (business rules).*
