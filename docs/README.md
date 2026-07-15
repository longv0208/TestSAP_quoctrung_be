# FURPMS — Mục lục tài liệu & Việc còn lại

> Điểm vào cho thư mục `docs/`. Cập nhật: 2026-07-03.
> Lưu ý: KHÔNG đổi tên / di chuyển các file (CLAUDE.md + docs khác đang tham chiếu path) — file này để tra cứu + theo dõi việc dở.

---

## 🔧 Việc còn lại / đang dang dở (backlog)

### A. Code chưa làm / làm dở
- **Phase 5 — Versioning + pin biểu mẫu chấm (RubricTemplate)**: chưa làm. Cần migration + rewire chỗ resolve template khi chấm (FE `RubricForm` đang lấy template active đầu tiên). Rủi ro vừa → làm phiên riêng.
- **i18n incremental**: mới key qua `t()` ở 2 màn (CycleManagement, TrackWorkspace). Các màn khác đã Việt hoá "track"→"lĩnh vực" **inline** → chuyển dần sang key i18n.
- **Dropdown loại đề tài ở form nộp** (`ProposalSubmission`) còn cứng 2 lựa chọn (Ứng dụng/Cơ bản). Loại mới hiện ở dropdown *tạo đợt* nhưng chưa ở *form nộp* (đề tài lấy loại theo đợt nên không chặn — chỉ là chỉnh hiển thị).
- **Thùng rác toàn hệ thống**: mới làm *mẫu* trên ResearchType. Nhân rộng = thống nhất `IsDeleted` + EF global query filter + 1 màn recycle-bin tổng. Để sau.

### B. Kết quả AUDIT FE↔BE toàn bộ route (07/07/2026 — 81 route FE so với 117 route BE)
- ✅ **Đã fix trong đợt audit:** (1) `my-memberships`/`meetings` trả **id bản đề cương hiện hành** thay vì projectId (trước đó reviewer bấm vào đề tài sẽ 404); (2) thêm 2 endpoint FE đang gọi mà BE thiếu: `PATCH /api/users/{id}/toggle-active` + `POST /api/users/{id}/reset-password` (reset về `Furpms@123456`) — đã smoke test sống.
- ✅ **`changeRequestService` (4 route) — ĐÃ IMPLEMENT BE (09/07/2026):** bảng `proposal_change_requests` (migration `PhaseC_ChangeRequests`) + `ChangeRequestService` + `ChangeRequestsController` + DTO khớp FE (type/status PascalCase). 40/40 test. **E2E UI đầy đủ:** PI gửi "Gia hạn thời gian" → admin thấy trong queue → duyệt → queue trống; 3 endpoint đều 200.
- ❌ **404 còn lại (pre-existing):** `POST /api/assignments/{id}/ai-feedback` + `ai-rubric-assessment` (aiService, gọi từ ReviewerInterface/AiSummaryPanel) — tính năng AI gợi ý nhận xét khi chấm. → implement qua GeminiService khi có key, hoặc ẩn nút.
- `AcceptanceEvaluationsController` (`/api/councils/{id}/acceptance`) → FE không gọi → orphan (luồng nghiệm thu FE dùng review-scoring).

### B2. DB Redesign v3 — PROJECT-CENTRIC (biên bản Review 2 — xem `DB_Redesign_v3_PostReview2.md`)
- ✅ **Phase A ĐÃ CODE XONG (07/07/2026):** bảng `project` (gốc) + proposal versioning (`project_id`+`version_no`+`is_current`, REVISION tạo bản mới) + `project_member` + gộp `project_deliverable` + `cycle_track` + order mặc định "Nghiên cứu tự do" (100% project có order) + `contract`→project **1-n** + `contract_phase` + `final_report`→project + review round/council tạm neo project. **DB đã reset** (migrations xóa sạch → 1 bản `PhaseA_ProjectCentric`, 54 bảng; FURPMS_V2 drop + tạo lại + seed). Build 0 lỗi · **36/36 test xanh** · smoke test API OK (login/proposals/detail/contracts) · FE tsc+build xanh (2 type patch).
- ✅ **Phase B ĐÃ CODE XONG (07/07/2026):** `review_round`→**cycle_track** (unique cycle_track+số vòng+dimension; nhiều project cùng track DÙNG CHUNG round — tự reuse khi tạo) + bảng nối **`project_rounds`** (kết quả TỪNG đề tài; rule #2 check per-project) + **`council_project_assignments`** (council chấm nhóm đề tài; COI check với TẤT CẢ đề tài được gán) + score/decision/feedback/acceptance thêm chiều `project_id` (unique 3 chiều; biên bản Chủ tịch khóa TỪNG đề tài; council DECIDED khi mọi đề tài có biên bản khóa). Migration `PhaseB_ReviewMN` (56 bảng). Build 0 lỗi · 36/36 test · smoke test round create→open→close PASSED. **API compat giữ nguyên**: request thêm field optional `projectId`/`proposalProjectId` — council/round 1 đề tài thì tự suy, FE chưa cần sửa.
- ✅ **Phase C — Yêu cầu thay đổi đề tài (09/07/2026):** bảng `proposal_change_requests` + service/controller/DTO + 4 endpoint (§6 API_CONTRACT). 40/40 test + E2E UI. → tổng **~56 bảng**.
- ✅ **Docs kéo theo ĐÃ CẬP NHẬT (09/07/2026):** `Review2_Diagrams.md` (§0 status có Project, §3 versioning, §4 Project SM cột thật, §5b project_round, §8 use case +change-request, §9 activity 9.2/9.4/9.6, banner §7), `RP4_Diagrams.md` (DB 56 bảng + class/sequence Project-centric), `API_CONTRACT.md` (banner + DTO mới + endpoint change-request/users), `DB_ANALYTIC_REPORT.md` (banner V2.0 + bảng thay đổi). `ERD_v3_Project_Centric.dbml` đã có banner từ trước.

### B3. Redesign màn "Hội đồng & Chấm" cấp Track (15/07/2026 — theo góp ý user test tay)
- **Bối cảnh:** user test màn Staff "Đề xuất & Phân công" thấy rối + lỗi tùm lum. Soi code xác nhận 4 bug thật: (1) thiếu nút "Mở vòng" dù FE có sẵn `roundService.openRound()` nhưng không nơi nào gọi → bấm Đạt/Từ chối luôn 409; (2) không có endpoint xóa vòng → vòng test rác tồn tại mãi; (3) nút "Đạt/Từ chối" gọi thẳng `close` — bypass biên bản Thư ký→Chủ tịch (vi phạm rule #12), đồng thời `ApproveMinutesAsync` (đường đúng) không cập nhật `project_rounds` → 2 nguồn dữ liệu lệch nhau; (4) UX nhét phân công vào modal TỪNG đề xuất trong khi DB đã đúng mô hình round-theo-track/hội-đồng-chấm-nhóm.
- ✅ **BE:** `IReviewBoardService`/`ReviewBoardService` (mới) + `ReviewBoardController` — endpoint `GET/POST .../tracks/{trackId}/rounds`, `DELETE /api/rounds/{id}`, `POST/DELETE .../projects`, `POST .../councils` (tạo hội đồng trọn gói, COI check trước khi tạo — vi phạm thì không tạo nửa vời). `ApproveMinutesAsync` fix nối mạch `project_round` + tự đóng round khi mọi đề tài xong (xem API_CONTRACT §8). 11 test mới, 51/51 xanh.
- ✅ **FE:** component mới `ReviewBoard.tsx` (+ `reviewBoardService.ts`) — chọn Đợt → Lĩnh vực → board: đề tài + rounds + hội đồng, có Mở vòng/Xóa vòng/Thêm-gỡ đề tài/Tạo hội đồng trọn gói (không còn nút chốt trực tiếp). Tab "Hội đồng & Chấm" thêm vào cả Staff + Admin dashboard. `ReviewRoundsPanel.tsx` (cũ) đã **xóa hẳn** — modal chi tiết đề xuất giờ chỉ hiện badge trạng thái vòng (read-only) + link sang tab mới.
- ✅ **E2E qua Playwright MCP (dữ liệu thật, không mock):** board load đúng 4 đề tài + 4 round có sẵn từ lần test trước của user → xóa thành công 2 vòng rác (Vòng 3 Sàng lọc, Vòng 4 Nghiệm thu — PENDING, không hội đồng) → "Mở vòng" trên Vòng 1 Tài chính chuyển PENDING→OPEN thành công (đúng bug user report) → link "Quản lý ở Hội đồng & Chấm →" từ modal đề xuất điều hướng kèm cycleId/trackId, board tự chọn đúng. Đồng thời fix luôn UX nhỏ tồn đọng ở dòng dưới (C2, "Gửi thư mời bị disabled tới khi reload") vì board mới refetch toàn bộ sau mỗi thao tác.
- ✅ **Code-review (8 góc) + fix bug BE (15/07):** rà lại chính diff redesign, sửa 5 vấn đề thật ở **BE thuần** (không đụng FE):
  - **#1+#5** gom logic đóng round vào `ReviewRoundFinalizer` (dùng chung `ApproveMinutesAsync` + `CloseRoundAsync`): `REVISION_REQUIRED` **không** còn khóa `project_round` thành FAILED (giữ OPEN cho PI nộp lại — rule #1); `round.status/result` suy nhất quán từ "tất cả PASSED?" nên hết mâu thuẫn/phụ thuộc thứ tự duyệt.
  - **#2** `CreateCouncilPackageAsync` bắt buộc ≥1 Chair + ≥1 Secretary (thiếu → 400) — tránh hội đồng "chết" không chốt được.
  - **#4** `FinalizeDecisionAsync` (`POST .../decision`) **khóa lại → 409**: đường tắt bỏ qua biên bản + không đồng bộ project_round. Chốt chỉ qua minutes (Thư ký→Chủ tịch).
  - **#3** thay reviewer bị DECLINED (rule #4): BE **đã có sẵn** `POST /councils/{id}/members` + `DELETE /council-members/{id}` → không cần sửa BE, FE mới gọi (contract §8.1/§8.3).
  - 53/53 test (thêm test REVISION-reopenable + Missing-Secretary). Cập nhật `API_CONTRACT.md §8`.
- ✅ **Dọn nợ kỹ thuật (15/07):** gộp N+1 (tạo vòng theo track + COI tạo hội đồng), projection `GET review-board` (4 field thay full entity), **siết quyền board = Admin/Staff** (đóng lỗ lộ danh tính hội đồng). Build + 53/53 test.
- ✅ **P0 reopen REVISION (15/07):** `ReviewRoundService.ReopenAfterResubmitAsync` — PI nộp lại bản REVISION (submit v2+) → council `DECIDED`→`FORMING`, biên bản về nháp, project_round→OPEN, **giữ điểm cũ** (rule #1). Hook vào `SubmitProposalAsync`. 54/54 test.
- ✅ **P2 gom code trùng (15/07):** `ReviewShared` — COI (rule #5) + MapMember + tạo/tái-dùng round gom về 1 chỗ, 3 service (ReviewBoard/ReviewRound/Council) dùng chung. 54/54 test.
- ⚠️ **Follow-up còn lại** (xem `PROGRESS.md`): (a) **P1** — config Gemini/SMTP key để demo trọn (hạ tầng, không phải code); (b) track mồ côi "a.i" (id=2, chưa gắn cycle).
- 🧊 **FE đóng băng:** FE hiện tại (`ReviewBoard.tsx`…) **giữ nguyên, không sửa tiếp** — sẽ có FE mới do thành viên khác làm. `API_CONTRACT.md` là bản giao kèo để FE mới code khớp BE.

### C2. E2E browser test (07/07/2026)
- ✅ **Đã test UI (Playwright + Chrome headless):** login 4 role · PI **tạo đề tài mới qua UI** (điền mẫu → wizard 5 bước → lưu nháp → tạo project thật) · PI nộp (DRAFT→SUBMITTED) · thấy order mặc định "Nghiên cứu tự do" (điểm d) · reviewer nhận phân công + mở đề tài (chỗ từng bug projectId — nay HTTP 200) + chấm **86/100** · staff mở chi tiết → panel **Vòng phản biện (2)** hiển thị đúng: SCIENCE "Đã duyệt/Đạt", FINANCE có reviewer1 badge **Accepted** (gán+mời+nhận round-trip OK) · thống kê 3 đề tài đúng.
- ✅ **Bug tìm được + đã fix:** `RoundMeetings.tsx` fallback `councilId ?? roundId` → vòng chưa lập hội đồng gọi `/api/councils/{roundId}/meetings` → **404**. Fix: chỉ fetch khi có `councilId` (vòng chưa có hội đồng thì không có lịch họp). tsc xanh.
- ✅ **LUỒNG CORE FULL-UI HOÀN TẤT (08/07/2026, qua Playwright MCP):** staff tạo vòng → gán **Chủ tịch/Thư ký/Phản biện** → gửi 3 thư mời (alert "Đã gửi 3 thư mời") → Thư ký (reviewer2) + Chủ tịch (reviewer1) nhận → **Thư ký soạn biên bản** (nháp, proposal chưa đổi — đúng rule #12) → **Chủ tịch duyệt & khóa** ("Đã khóa", kết quả Đạt) → **proposal APPROVED** → staff **tạo hợp đồng HD-2026-003** → **ký kết → ACTIVE** → **project IN_PROGRESS**. Verify API xác nhận từng bước. 0 lỗi API.
- ✅ **GIẢI NGÂN FULL-UI (08/07):** staff "Tạo lịch giải ngân" HD-2026-003 → tự sinh **3 đợt 33/33/34%** (WHOLE ≥3 — rule #6, đều PENDING) · HD-2026-002 (PARTIAL): **PI nộp sản phẩm** (URL file) → **Staff đánh giá PASSED** → điều kiện đợt 3 tự đạt → nút "Xác nhận giải ngân" CHỈ hiện ở đợt đó (rule #3 — tiền không tự chuyển) → confirm với mã NH → **DISBURSED 90M**. Chuỗi nghiệm thu→giải ngân trọn vẹn qua UI.
- ✅ **3 bug fix trong đợt MCP:** (1) BE `AddRoundMemberAsync` bổ sung `Secretary` vào whitelist (trước gán Thư ký 400); (2) FE `ROLE_LABEL` thêm `Secretary: 'Thư ký'`; (3) FE nút "Đánh giá" sản phẩm đòi `!acceptanceStatus` nhưng BE set `PENDING` khi PI nộp → **staff không bao giờ thấy nút** — sửa thành `PENDING` cũng hiện (`ContractManagement.tsx`).
- ✅ **UX nhỏ đã fix gián tiếp (xem B3):** sau khi gán người đầu, "Gửi thư mời" từng bị disabled tới khi reload (state FE cũ không cập nhật `councilId`) — màn `ReviewBoard.tsx` mới refetch toàn bộ board sau mỗi thao tác nên không còn tái hiện.
- Skill tái sử dụng: `.claude/skills/fe-e2e/SKILL.md` + Playwright MCP (scope user). Screenshot cũ: `Fefurpmsv0/.e2e/shots/` (gitignore).

### C. Vận hành / môi trường
- **Docker BE**: restart để nạp fix mới nhất (ResearchType create Code-optional, track chống trùng mã, appsettings.json có lại connection dev) — `docker compose restart backend`.
- **`appsettings.Development.json` bị mất** → Gemini AI key cũng mất theo → tính năng AI trích xuất báo "chưa cấu hình" (không crash, vẫn nhập tay được). Muốn AI chạy lại: tạo lại file với `GeminiAI:ApiKey`, hoặc set qua env.
- **Deploy BE (Render + site4now)**: push code mới → redeploy → test login trong Swagger; set env var (`ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `GeminiAI__ApiKey`). **Đổi secret đã lộ** (JWT/SMTP/SQL password — từng commit trong git history).
- **Nhiều thay đổi BE+FE+docs trong phiên này chưa commit** (xem `git status` cả 2 repo): BE (ResearchType CRUD + fix create + appsettings), FE (i18n + track→Lĩnh vực + màn quản lý loại + dropdown reload fix), docs (§2b System Overview, §6b Logical ERD, fix erDiagram labels, README này, cập nhật handoff).

### D. Nhóm tự làm (tài liệu Review 2)
- **Document 1–4** (prose Report 4 Design / Report 5 Testing).
- ~~Bảng Team Contribution Week 1–8~~ → **đã có bản nháp** `Team_Contribution_Week1-8.md` (roster thật + % cân ~20/người + chi tiết tuần). ⚠️ Còn: rà lại số giờ cho hợp lý + **XÓA phụ lục nội bộ trước khi nộp**.

### E. Ngoài phạm vi (đã chốt KHÔNG làm)
- Multi-winner Applied (ghi âm thầy chốt **1 winner**).
- Dual-intake AI nâng cao ngoài phần đã có.

---

## 📚 Danh mục tài liệu

### 🧭 Thứ tự đọc (người mới vào)
1. `../CLAUDE.md` — conventions + **business rules #1–14** (nền nghiệp vụ, đọc trước tiên).
2. `Process_Spec_v2.md` — quy trình nghiệp vụ theo giai đoạn (bức tranh lớn: ai làm gì, đầu ra gì).
3. `DB_Redesign_v3_PostReview2.md` → `ERD_v3_Project_Centric.dbml` — mô hình dữ liệu Project-centric (cấu trúc).
4. `API_CONTRACT.md` — endpoint + payload (để code/tích hợp FE↔BE). **Bản giao kèo chính.**
5. Khi cần diagram: `Review2_Diagrams.md` (context/architecture/use-case/activity) · `RP4_Diagrams.md` (SDD: package/class/sequence).
6. Tham khảo sâu: `DB_ANALYTIC_REPORT.md` (mô tả cột) · `Review2_Tech_Stack.md` · guides `DEMO_GUIDE.md`/`EXPORT_TEST_GUIDE.md`.

### Spec hiện hành — đọc trước khi code
| File | Nội dung |
|---|---|
| `../CLAUDE.md` | Conventions + kiến trúc + **business rules** (rule #1–14). Nguồn gốc nghiệp vụ. |
| `Process_Spec_v2.md` | Quy trình nghiệp vụ chi tiết theo giai đoạn. |
| `API_CONTRACT.md` | Hợp đồng API (endpoint + payload) — **8 nhóm chức năng**. |
| `PROGRESS.md` | **Tiến độ BE theo nhóm chức năng (% + còn thiếu + việc nên làm tiếp).** |
| `FURPMS_DB_Change_Spec_v1.4.md` | Spec thay đổi DB. |

### Chuẩn bị Review 2 (mới nhất)
| File | Nội dung |
|---|---|
| `DB_Redesign_v3_PostReview2.md` | **★ Thiết kế DB v3 Project-centric theo biên bản Review 2** (a)–(e) → schema đích · bảng thay đổi từng thực thể · roadmap code Phase A/B · câu hỏi mở Q1–Q5. |
| `ERD_v3_Project_Centric.dbml` | **★ ERD hiện hành (đã áp Phase A/B)** — dán vào dbdiagram.io (~56 bảng: +project, cycle_track, project_round, council_project_assignment, contract_phase). **Sơ đồ DB DUY NHẤT** (các bản .dbml/.sql cũ đã xóa 15/07 — xem git nếu cần lịch sử). |
| `Review2_Diagrams.md` | Bộ diagram: Context · Architecture · **System Overview (§2b)** · State Machine · ERD rút gọn (§6) + **Logical ERD đầy đủ (§6b — có sản phẩm/giải ngân, Proposal→n Contract)** · Use Case · **Activity (§9)** + đánh giá DB/code (§7). |
| `Review2_Tech_Stack.md` | 4 danh sách Product/Tech (3rd-party · stack · DevOps · deploy). |
| `RP4_Diagrams.md` | **Diagram theo khung Report 4 (SDD)**: Package Diagram + Class Diagram + Sequence Diagram (3 feature lõi). Architecture/ERD trỏ về Review2_Diagrams. |
| `Review2_Context_Handoff.md` | Context để dán vào phiên AI khác lo SDD/report. |
| `Team_Contribution_Week1-8.md` | Bảng đóng góp thành viên W1–8 (roster thật + % + chi tiết tuần). ⚠️ có phụ lục nội bộ — xóa trước khi nộp. |

### Báo cáo & phân tích DB (tham khảo)
| File | Nội dung |
|---|---|
| `DB_ANALYTIC_REPORT.md` | Phân tích kiến trúc DB (10 domain · ~56 bảng · mô tả cột). Cho mục "Database". |
| `../../Fefurpmsv0/DATABASE_DESIGN.md` | Schema chi tiết (CREATE TABLE SQL + quan hệ + enum). |

### Hướng dẫn
| File | Nội dung |
|---|---|
| `DEMO_GUIDE.md` | Hướng dẫn demo. |
| `EXPORT_TEST_GUIDE.md` | Hướng dẫn test export tài liệu. |
