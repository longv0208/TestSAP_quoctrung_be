# Use Case Status & Traceability — nội dung dùng cho slide hội đồng

> Cập nhật: 19/08/2026  
> Phạm vi đối chiếu: 35 Use Case đăng ký trong Report 3 với hai repository đang dùng:
> `FURPMS_BEv2` và `FURPMS-Web` trên nhánh `dev`.

## 0. Cách đọc và nguyên tắc chấm trạng thái

| Trạng thái | Ý nghĩa |
|---|---|
| **Completed** | Có luồng BE + FE sử dụng được và đáp ứng mục tiêu nghiệp vụ hiện hành. |
| **Partial** | Có phần lõi nhưng thiếu một phần đúng như mô tả đăng ký ban đầu. |
| **Not completed** | Chưa có endpoint chạy thật hoặc màn hình đã bị khóa vì BE chưa hỗ trợ. |
| **Superseded** | Không tiếp tục làm theo Report 3 vì quy tắc nghiệp vụ/phạm vi đã được chốt lại; không tính là lỗi bỏ quên. |

Các con số dưới đây là **kiểm kê tính năng**, không suy từ phần trăm cảm tính trong `PROGRESS.md`.
“Số bước” là số bước nghiệp vụ người dùng/hệ thống nhìn thấy, **không phải** số câu SQL hay số
database transaction.

---

# PHẦN DÙNG TRỰC TIẾP TRÊN SLIDE

## Slide 5 — Use Case Status Table

### Kết quả đối chiếu Report 3 → hệ thống hiện tại

| Chỉ số | Số lượng | Tỷ lệ trên 35 UC |
|---|---:|---:|
| **Completed** | **23** | **65,7%** |
| **Partial** | **8** | **22,9%** |
| **Not completed** | **2** | **5,7%** |
| **Superseded by approved scope** | **2** | **5,7%** |
| **Có triển khai ít nhất một phần** | **31/35** | **88,6%** |

> Cách nói khi thuyết trình: “Nhóm hoàn thành trọn vẹn 23/35 UC đăng ký. Có 8 UC đã có phần lõi
> nhưng chưa đủ toàn bộ mô tả Report 3; 2 UC chưa triển khai; 2 UC được thay thế do nghiệp vụ đã
> chốt lại. Vì vậy nhóm không trình bày 88,6% là tỷ lệ hoàn thành, mà là tỷ lệ UC có triển khai ít
> nhất một phần.”

### Danh sách rút gọn theo trạng thái

| Trạng thái | Use Case |
|---|---|
| **Completed (23)** | UC-01, 02, 04, 05, 06, 08, 09, 10, 11, 12, 16, 20, 21, 22, 25, 26, 27, 28, 29, 30, 33, 34, 35 |
| **Partial (8)** | UC-03, 13, 14, 15, 17, 23, 31, 32 |
| **Not completed (2)** | UC-18 Calendar sync; UC-24 Semantic search |
| **Superseded (2)** | UC-07 Funding allocation; UC-19 Science→Finance rounds |

### Ba thay đổi phạm vi phải chủ động nói rõ

1. **UC-07:** hệ thống không thực hiện nghiệp vụ kế toán/chuyển tiền; chỉ kiểm trần dự toán, theo
   dõi mốc giải ngân và lưu minh chứng.
2. **UC-19:** bỏ vòng `FINANCE`; quy trình hiện hành có hai loại vòng/hội đồng là **Xét duyệt đề
   cương** và **Nghiệm thu**. Báo cáo tiến độ do Staff đánh giá trực tiếp.
3. **UC-25:** bỏ `WHOLE/PARTIAL`; lịch giải ngân được sinh theo **loại đề tài** và cấu hình mốc
   hiện hành.

---

## Slide 6 — Role-based Features

| Vai trò | Chức năng đã triển khai nổi bật | UC liên quan |
|---|---|---|
| **Administrator** | Quản lý tài khoản/vai trò; loại đề tài; đợt; lĩnh vực; đơn vị; bộ tiêu chí; cấu hình upload; analytics | 02–07, 35 |
| **Staff / Phòng QLKH** | Đặt hàng; đọc hồ sơ; lập hội đồng; gán nhiều đề tài; mời thành viên; lịch họp; quản lý hợp đồng, giải ngân, sản phẩm, báo cáo, điều chỉnh và quyết toán | 08, 13–17, 22, 25–32, 35 |
| **PI / Chủ nhiệm** | Nộp/sửa/rút đề cương; AI điền form từ PDF/DOCX; khai thành viên, dự toán, sản phẩm; nộp báo cáo tiến độ/tổng kết; xin điều chỉnh; xem timeline | 04, 09–12, 23, 26, 28, 29, 31, 33, 34 |
| **Review Committee** | Nhận/từ chối lời mời; xem hồ sơ; xác nhận tham gia; chấm rubric; nhận gợi ý AI; phản biện BM10; bỏ phiếu BM11; Thư ký soạn và Chủ tịch khóa biên bản | 04, 15–22, 34 |
| **System / Background jobs** | Kiểm deadline; tạo nhắc hạn; gửi thông báo/email; sinh/cache kết quả AI; đồng bộ trạng thái xuyên suốt Project–Proposal–Round–Contract | 21, 23, 33, 34 |

### Phần phức tạp nhất nên nhấn mạnh

- **Project-centric lifecycle:** một `Project` có nhiều phiên bản `Proposal`, nhiều vòng chấm và
  có thể có nhiều hợp đồng; trạng thái không được cập nhật rời rạc.
- **Hội đồng chấm nhiều đề tài:** quan hệ M–N `Council ↔ Project`; điểm, phản hồi, phiếu nghiệm
  thu và quyết định đều khóa theo bộ ba hội đồng–đề tài–người chấm.
- **COI và quyền truy cập:** chặn thành viên/chủ nhiệm đề tài tham gia hội đồng chấm chính đề tài
  đó ở cả hai cửa gán người và gán đề tài.
- **Khóa quyết định:** Thư ký soạn biên bản → Chủ tịch duyệt/khóa → hệ thống mới cập nhật kết quả;
  không lấy trung bình điểm để tự quyết định.
- **Ràng buộc lịch:** chặn lịch quá khứ, ngoài mốc dự án, trùng người hoặc trùng hội đồng; thời
  gian lưu UTC.
- **AI có kiểm soát:** retry/backoff/fallback, cache kết quả, chỉ gợi ý và không ghi đè quyết định
  của PI/người chấm.

> Không nên claim “distributed system”, “real-time payment” hay “automatic Google Calendar sync”:
> code hiện tại không có các năng lực đó.

---

## Slide 7 — Transaction / Main-flow Traceability

| Luồng chính | Số bước nghiệp vụ | Chuỗi xử lý rút gọn | Điểm kiểm soát quan trọng |
|---|---:|---|---|
| **UC-09 Nộp đề cương** | **7** | Chọn đợt → lĩnh vực/loại suy theo đợt → chọn/upload file → AI prefill tùy chọn → khai nội dung/dự toán/thành viên → lưu nháp → nộp | Deadline, loại–đợt, trần/tỷ lệ kinh phí, file policy, không ghi đè dữ liệu PI |
| **UC-15–17 Lập hội đồng** | **8** | Tạo vòng → thêm đề tài → tạo hội đồng → gán Chair/Secretary/Reviewer → kiểm COI → gửi lời mời theo từng đề tài/hội đồng → thành viên phản hồi → xếp lịch | Hội đồng phải có Chair + Secretary; không mất thư mời khi một hội đồng chấm nhiều đề tài; chặn trùng lịch |
| **UC-20–22 Chấm và quyết định** | **8** | Mở vòng → xác nhận tham gia → xem hồ sơ → AI gợi ý tùy chọn → nộp/sửa phiếu → Thư ký soạn biên bản → Chủ tịch trả sửa hoặc khóa → cập nhật ProjectRound/Proposal | Unique phiếu theo người–đề tài; rubric đúng tổng; quyết định của Chủ tịch; khóa sau duyệt |
| **UC-25–27 Hợp đồng và giải ngân** | **8** | Đề tài được duyệt → tạo hợp đồng → tải/ký bản hợp đồng → sinh mốc theo loại đề tài → PI nộp sản phẩm → Staff đánh giá → tải minh chứng → đánh dấu đã giải ngân | Không phải kế toán thật; mốc chỉ mở khi điều kiện/sản phẩm đạt; hợp đồng chấm dứt không được chi tiếp |
| **UC-28–30 Kết thúc đề tài** | **9** | Tạo kỳ tiến độ → PI nộp → Staff đánh giá → PI nộp tổng kết → lập hồ sơ nghiệm thu → hội đồng bỏ phiếu/khóa kết quả → hoàn tất các mốc chi → xác nhận kế toán/tài sản → ký thanh lý | Nghiệm thu Đạt không đồng nghĩa đã thanh lý; chỉ `SETTLED` sau đủ điều kiện quyết toán |
| **UC-31–32 Điều chỉnh** | **5** | PI chọn hợp đồng/loại yêu cầu → nhập đề nghị + lý do → Staff đọc → duyệt/từ chối → nếu gia hạn thì cập nhật end date và sinh phụ lục | Chặn xử lý lặp; tổng gia hạn không vượt `MaxExtensionMonths`; Rector approval hiện mới là cờ theo dõi |

### Traceability kỹ thuật cho 6 luồng chính

| Luồng | Backend | Frontend |
|---|---|---|
| Nộp đề cương | `ProposalsController`, `ProposalDocumentsController`, `ProposalExtractionService`, `ProposalService` | `ProposalWizardPage`, `Step2ResearchContent`, `Step3Details`, `Step4TeamMembers` |
| Hội đồng | `ReviewBoardController`, `CouncilsController`, `CouncilMeetingsController`, `ReviewShared` | `ReviewBoardPage`, `CreateCouncilSheet`, `ScheduleMeetingSheet`, `InvitationsPage` |
| Chấm/biên bản | `ReviewScoringController`, `AcceptanceEvaluationsController`, `ReviewScoringService` | `ProposalReviewWorkspace`, các tab Chấm điểm/Biên bản |
| Hợp đồng/giải ngân | `ContractsController`, `DeliverablesController`, `DisbursementsController` | `ContractsPage`, `ContractDetailSheet`, `DeliverablesPage` |
| Báo cáo/nghiệm thu/thanh lý | `ProgressReportsController`, `FinalReportsController`, `AcceptanceDossierController`, `ContractSettlementsController` | `ProgressReportsPage`, `FinalReportsPage`, `SettlementPanel` |
| Điều chỉnh | `AmendmentsController`, `ChangeRequestsController`, `AmendmentService` | `MyAmendmentsPage`, trang Staff Yêu cầu thay đổi |

---

# PHỤ LỤC — BẢNG ĐỐI CHIẾU ĐẦY ĐỦ 35 USE CASE

| ID | Use Case Report 3 | Status | Thực tế hiện tại / khoảng trống | Traceability chính |
|---|---|---|---|---|
| UC-01 | Log in / Log out | **Completed** | JWT login, logout phía client, lấy hồ sơ hiện tại, đổi/quên/đặt lại mật khẩu. | `AuthController`; auth store/pages |
| UC-02 | Manage users | **Completed** | Tạo, sửa, vô hiệu hóa, reset mật khẩu, xóa mềm và gán vai trò. | `UsersController`; `UsersPage` |
| UC-03 | Manage roles & permissions | **Partial** | RBAC và gán nhiều vai đã có; Admin chưa tự tạo vai hoặc chỉnh ma trận permission động, quyền được khai trong code. | `[Authorize(Roles=...)]`; `NAV_ITEMS`; user roles |
| UC-04 | Manage academic profile | **Completed** | PI/Reviewer quản lý học hàm, học vị, chuyên môn và công trình khoa học. | `AcademicProfilesController`, `AcademicWorksController`; Profile page |
| UC-05 | Manage research type | **Completed** | CRUD loại Cơ bản/Ứng dụng, trần kinh phí, yêu cầu đơn vị đặt hàng. | Research Type endpoints/page |
| UC-06 | Manage research cycle | **Completed** | CRUD/mở/đóng/gia hạn hạn nộp; gắn loại đề tài và lĩnh vực theo đợt. | `CyclesController`; `CyclesPage` |
| UC-07 | Configure funding allocation | **Superseded** | Chỉ cấu hình trần/tỷ lệ dự toán và mốc giải ngân; không quản sổ tiền hay phân bổ kế toán. | Budget policy; disbursement templates |
| UC-08 | Manage research order | **Completed** | CRUD đề tài đặt hàng, lọc theo đợt; đề tài ứng dụng liên kết order. Multi-winner ngoài phạm vi. | `ResearchOrdersController`; `ResearchOrdersPage` |
| UC-09 | Submit proposal | **Completed** | Wizard 5 bước, draft/submit, PDF/DOCX, AI prefill, kiểm deadline và policy file. | `ProposalsController`; proposal wizard |
| UC-10 | Manage proposal team members | **Completed** | Thêm/sửa/xóa trong draft, khai thư ký/vai trò/số tháng; PI là actor đăng nhập duy nhất. | `ProposalTeamMembersController`; `Step4TeamMembers` |
| UC-11 | Define expected products | **Completed** | Khai sản phẩm/kết quả dự kiến và danh sách deliverable. | proposal content/deliverables UI + API |
| UC-12 | Edit / withdraw proposal | **Completed** | Sửa draft; rút proposal đã nộp/đang xét; revision tạo phiên bản mới. | Proposal update/withdraw/resubmit |
| UC-13 | Screen proposal eligibility | **Partial** | Staff đọc, lọc và kiểm hồ sơ/file; chưa có một quyết định “eligibility screening” độc lập trước hội đồng. | `ProposalReviewsPage`; proposal detail |
| UC-14 | Manage reviewers & experts | **Partial** | Có user/profile/công trình và tập ứng viên nội bộ; chưa có expert pool ngoài trường độc lập. | Users + academic profile; council candidate filter |
| UC-15 | Assign reviewers / invite experts | **Partial** | Gán/mời/nhận/từ chối và bảo vệ tài liệu đã có; external expert bằng secure token/account riêng chưa có. | Council member/invitation endpoints/pages |
| UC-16 | Form review council | **Completed** | Lập hội đồng chấm nhiều đề tài, vai Chair/Secretary/Reviewer, COI và điều kiện thành phần. | `ReviewBoardController`; `CreateCouncilSheet` |
| UC-17 | Schedule committee meeting | **Partial** | CRUD lịch, link online nhập tay, agenda, attendance, notify và chặn trùng; chưa tự tạo Google Meet/Teams. | Meeting controllers/pages |
| UC-18 | Sync calendar | **Not completed** | Có lịch nội bộ nhưng không có OAuth/sync Google Calendar hoặc Outlook. | Không có integration endpoint chạy thật |
| UC-19 | Manage Science→Finance rounds | **Superseded** | Bỏ vòng Finance; dùng REVIEW và ACCEPTANCE, đều dimension SCIENCE. | `CreateRoundSheet`; rule #16 |
| UC-20 | Review & score proposal | **Completed** | Rubric động, nộp/sửa điểm/nhận xét, kiểm tổng điểm và quyền thành viên. | `ReviewScoringController`; reviewer workspace |
| UC-21 | View AI feedback suggestions | **Completed** | AI feedback/review-kit và gợi ý từng tiêu chí, có cache và nút áp dụng; người chấm quyết định cuối. | `AiController`; AI panel |
| UC-22 | Record council decision | **Completed** | Thư ký soạn, Chủ tịch trả sửa/khóa; PASSED/REVISION_REQUIRED/REJECTED cập nhật xuyên luồng. | minutes/decision trong `ReviewScoringService` |
| UC-23 | Generate AI summary | **Partial** | Có summary proposal, consistency check và reviewer kit; chưa tự tóm tắt progress/final report như mô tả Report 3. | `AiController`; `AiSummaryService` |
| UC-24 | Perform semantic search | **Not completed** | FE/page cũ tồn tại nhưng route bị ẩn và BE `/api/ai/search` chưa implement. | `SemanticSearchPage` (inactive); API contract §AI |
| UC-25 | Create contract | **Completed** | Tạo/sửa/xóa trước ký, upload bản ký, activate/terminate; một Project có thể có nhiều Contract. `WHOLE/PARTIAL` đã bỏ theo scope mới. | `ContractsController`; Contracts pages |
| UC-26 | Manage deliverables | **Completed** | PI nộp file/link/minh chứng; Staff đánh giá từng sản phẩm; gắn sản phẩm với mốc. | `DeliverablesController`; Deliverables page |
| UC-27 | Record disbursement | **Completed** | Sinh mốc theo loại đề tài, gắn sản phẩm/minh chứng, kiểm điều kiện rồi đánh dấu đã chi. Chỉ theo dõi, không chuyển tiền thật. | Disbursement controllers/panel |
| UC-28 | Submit progress report | **Completed** | Kỳ linh hoạt, file hoặc link, nội dung BM06, Staff đặt lịch/đánh giá PASS/FAIL/CONDITIONAL. | `ProgressReportsController`; Progress pages |
| UC-29 | Submit final report | **Completed** | PI nộp/nộp lại file hoặc link báo cáo + tóm tắt, chọn ngôn ngữ; Staff xem đúng file. | `FinalReportsController`; Final Reports pages |
| UC-30 | Record contract settlement | **Completed** | Kiểm nghiệm thu, đủ giải ngân, kế toán/tài sản, ký BM13 rồi chuyển `SETTLED`. | `ContractSettlementsController`; `SettlementPanel` |
| UC-31 | Request extension / amendment | **Partial** | Gửi yêu cầu điều chỉnh/gia hạn và phụ lục đã có; giới hạn theo `MaxExtensionMonths`, không dùng quy tắc “scope ≤50%” như Report 3. | `AmendmentsController`; PI amendment page |
| UC-32 | Approve amendment | **Partial** | Staff/Admin duyệt/từ chối và có cờ `RequiresRectorApproval`; chưa có actor/màn phê duyệt Hiệu trưởng riêng. | `AmendmentService`; Staff change-request UI |
| UC-33 | Track project status | **Completed** | Timeline và trạng thái Project–Proposal–Round–Contract–Report cho các vai theo quyền. “Real-time” ở đây là dữ liệu API hiện hành, không phải WebSocket. | timeline/dashboard/detail pages |
| UC-34 | Receive notifications | **Completed** | Thông báo DB + email cho nộp, lời mời, lịch, quyết định, hợp đồng và deadline; đọc từng cái/đọc tất cả. | `NotificationsController`, notifier/scanner; bell/page |
| UC-35 | View analytics dashboard | **Completed** | Dashboard theo vai và analytics theo đợt/lĩnh vực/kết quả. | `AnalyticsController`; dashboard/analytics pages |

---

## Câu hỏi hội đồng dễ hỏi và câu trả lời ngắn

### “Tại sao chỉ 23/35 Completed?”

Vì nhóm dùng chuẩn nghiêm ngặt: UC chỉ được ghi Completed khi đủ cả mục tiêu nghiệp vụ hiện hành
và đường sử dụng FE–BE. Tám UC Partial vẫn có phần lõi chạy thật, nhưng nhóm không nhập nhằng phần
thiếu như external expert token, calendar sync hay Rector approval thành “đã xong”.

### “Hai UC Superseded có phải nhóm không làm kịp?”

Không. Đây là hai mô tả Report 3 đã bị thay thế sau khi chốt lại quy trình: hệ thống không làm kế
toán và không còn vòng Finance. Code cũ liên quan đã được bỏ/ẩn để không trình diễn sai nghiệp vụ.

### “Hệ thống có xử lý concurrency/distributed không?”

Không claim distributed. Hệ thống bảo vệ nhất quán bằng state transition ở service, unique
constraint ở database, kiểm khóa biên bản/quyết toán và thao tác `SaveChanges` nguyên tử. Đây là
transactional consistency trong một ứng dụng .NET + PostgreSQL, không phải distributed transaction.

### “AI có tự quyết định điểm hoặc kết quả không?”

Không. AI chỉ prefill/gợi ý, có retry–fallback–cache. PI và thành viên hội đồng phải kiểm tra; kết
quả hội đồng chỉ thay đổi khi Chủ tịch khóa biên bản.

## Nguồn kiểm chứng

- `docs/API_CONTRACT.md` — endpoint, quyền, trạng thái và ràng buộc hiện hành.
- `docs/Process_Spec_v2.md` — quy trình nghiệp vụ theo giai đoạn.
- `CLAUDE.md` — các quyết định nghiệp vụ đã chốt của nhóm.
- `docs/PROGRESS.md` — phần còn thiếu đã công khai.
- `FURPMS.API/Controllers`, `FURPMS.Infrastructure/Services` — implementation backend.
- `FURPMS-Web/src/constants/nav.ts`, `src/features`, `src/services/api` — route và implementation frontend.
