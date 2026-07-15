# DB Redesign v3 — Project-centric (theo biên bản Review 2, 03/07/2026)

> **Trạng thái (07/07/2026): PHASE A + PHASE B ĐỀU ĐÃ CODE XONG — ERD v3 áp dụng đầy đủ.**
> Phase A: `project` + versioning + `cycle_track` + order mặc định + `contract_phase` + contract 1-n (migration `PhaseA_ProjectCentric`, reset DB).
> Phase B: `review_round`→cycle_track + `project_rounds` M-N + `council_project_assignments` + score/decision per-project (migration `PhaseB_ReviewMN`, tổng **56 bảng**).
> 36/36 test xanh; smoke test round create→open→close OK; API giữ compat theo proposalId (field optional `projectId` cho council nhiều đề tài).
>
> **ERD đích:** `ERD_v3_Project_Centric.dbml` (dán vào dbdiagram.io).
> **Schema hiện hành (đã áp Phase A/B):** `ERD_v3_Project_Centric.dbml` (~56 bảng).
>
> ⚠️ Doc này **thay thế kết luận cũ trước Review 2** (khi đó chốt "KHÔNG thêm bảng Project" vì thầy Đức mới *hỏi thăm dò*). Tại Review 2, Review Instructor **yêu cầu chính thức trong biên bản**: *"restructuring the logical and physical ERD around Project as the central entity"*. Yêu cầu trong biên bản = phải làm.

---

## 1. Biên bản Review 2 yêu cầu gì

5 điểm (a)–(e) trong mục "Review Instructor's Comments":

| # | Yêu cầu | Ý chính |
|---|---|---|
| (a) | **Project = thực thể trung tâm; Proposal = tài liệu có version** | Proposal chỉ là một tài liệu/thành phần trong Project, có versioning (v1.0 → v1.1). Project giữ: members, supervisors, start/end, phases, deliverables, contract. Các khái niệm con bị tách quá (vd expected products) phải là **bảng con/thuộc tính của Project** ("project deliverables"), không đứng độc lập. |
| (b) | **Cycle chứa nhiều Track** | Quan hệ Cycle–Track đang **thiếu** — 1 đợt đăng ký gồm nhiều track (phần mềm, kinh tế, bán dẫn…), PI nộp vào 1 track cụ thể. |
| (c) | **Round theo Track; Project–Round nhiều-nhiều; nhiều council song song/round** | Mỗi track tự định nghĩa các vòng chấm (sơ loại → chuyên sâu) + hội đồng; 1 round có thể nhiều council chạy song song (do số lượng bài nộp). Mỗi round có bộ tiêu chí (rubric) riêng; project đạt mới vào round sau. |
| (d) | **100% project thuộc 1 Request Order** | Đề tài tự do → Admin tạo order "Nghiên cứu cơ bản" chung (ví dụ minh họa: công bố ISI/Scopus Q2–Q4 + trần kinh phí) để mọi proposal đều map về 1 order. Template tiêu chí chấm định nghĩa theo Order/Track, Admin cấu hình. |
| (e) | **Contract: phase + deliverables + tranches** | Contract chỉ tạo sau khi project được duyệt; phải quản lý **giai đoạn thực hiện + sản phẩm bàn giao**, không chỉ số tiền. Giải ngân chia nhiều tranche gắn phase (applied) hoặc gắn sản phẩm đã nghiệm thu (basic); contract là nơi theo dõi tranche + deliverables + thanh lý. |

**Đối chiếu nhanh với DB hiện tại (52 bảng):** (e) đã có ~70% (`product_deliverable`, `contract_disbursement`, `contract_settlement`, `amendment_request`) — thiếu `contract_phase` + quan hệ 1-n; (d) sửa nhẹ (order_id nullable → bắt buộc + order mặc định); (b) thêm 1 bảng nối; **(a) + (c) là refactor lớn thật sự** — không có `project`, và `review_round`/`review_council` đang neo **ngược** (theo proposal thay vì theo track).

---

## 2. Thay đổi từng thực thể (schema đích v3)

### 2.1 THÊM MỚI (5 bảng)

| Bảng mới | Cột chính | Trace |
|---|---|---|
| **`project`** (guid) | `project_code` unique · `cycle_track_id` FK · `order_id` FK **NOT NULL** · `pi_user_id` · `hosting_unit_id` · `research_type_id` · `title_vi/en` · `status` (PROPOSED → UNDER_REVIEW → APPROVED → IN_PROGRESS → ACCEPTANCE → COMPLETED / CANCELLED / TERMINATED) · `planned_start/end_date` · timestamps | (a): gốc mới; (d): order bắt buộc |
| **`cycle_track`** (int) | `cycle_id` + `track_id`, unique cặp; (tùy chọn) deadline/nộp riêng theo track | (b) |
| **`project_round`** (nối) | `project_id` + `round_id` unique cặp · `status` (PENDING/PASSED/FAILED/REVISION) · `result` · `finalized_at` | (c): M-N, đạt mới sang round sau |
| **`council_project_assignment`** (nối) | `council_id` + `project_id` unique cặp | (c): 1 council chấm 1 nhóm project |
| **`contract_phase`** (int) | `contract_id` FK · `phase_no` · `name` · `start/end_date` · `amount` | (e): giai đoạn thực hiện trong HĐ |

### 2.2 ĐỔI NEO / ĐỔI CẤU TRÚC

| Bảng | Đổi gì | Trace |
|---|---|---|
| **`proposal`** | + `project_id` FK, + `version_no`, + `is_current`; unique `(project_id, version_no)`. **Bỏ** các cột neo (cycle_id, track_id, order_id, pi_user_id, hosting_unit_id, research_type_id — dời lên `project`). Giữ toàn bộ content (title, abstract, objectives, methodology…) + status tài liệu (DRAFT/SUBMITTED/REVISION/APPROVED) + funding_method. REVISION → tạo bản v(n+1), giữ lịch sử. | (a) versioning |
| **`review_round`** | **Bỏ `proposal_id`** → neo `cycle_track_id`. Giữ round_number, `round_type` (SCREENING/SPECIALIZED/ACCEPTANCE), `dimension` (SCIENCE/FINANCE — giữ rule #2), `rubric_template_id` (bộ tiêu chí riêng mỗi round), `prerequisite_round_id`, status. | (c) |
| **`review_council`** | **Bỏ `proposal_id`** → chỉ còn `round_id`. Nhiều council/round chạy song song. | (c) |
| **`proposal_review_score` → `project_review_score`** | Thêm chiều project: unique `(council_id, project_id, evaluator_member_id)` | (c) |
| **`council_decision`** | Unique `(council_id, project_id)` thay vì chỉ council_id — Chủ tịch chốt **từng đề tài** trong phiên; giữ `finalized_at` (khóa biên bản) + workflow Thư ký soạn → Chủ tịch duyệt | (c) |
| **`reviewer_feedback`, `acceptance_evaluation`** | Tương tự: thêm `project_id` vào unique | (c) |
| **`contract`** | `proposal_id` → **`project_id`**, **bỏ unique** → 1 project → n contract. + `scope_title` (ký hạng mục gì) | (a)(e) |
| **`contract_disbursement`** | + `phase_id` FK nullable → `contract_phase` (tranche theo phase — applied; hoặc theo deliverable nghiệm thu — basic, qua `deliverable_id` sẵn có) | (e) |
| **`final_report`** | `contract_id` → **`project_id`** unique (nghiệm thu cuối là của ĐỀ TÀI, không phải từng hợp đồng) | (a) |
| **`research_order`** | + `is_default` (order "Nghiên cứu cơ bản" chung, Admin/seeder tạo mỗi cycle; ví dụ ISI/Scopus Q2–Q4 trong biên bản là **minh họa**, trần Basic thật theo QĐ 543 = 200tr qua `research_type.max_budget_cap`) | (d) |
| **`rubric_template`** | + `track_id`/`order_id` nullable (scope: template tiêu chí theo Track/Order, Admin cấu hình; round pick template) | (d) |

### 2.3 ĐỔI TÊN / GỘP

| Cũ | Mới | Lý do |
|---|---|---|
| `proposal_team_member` | **`project_member`** (FK `project_id`) | (a): thành viên thuộc Project, không theo từng bản proposal. Giữ nguyên cột (full_name, is_pi, is_secretary, member_role_code, salary_coefficient…) |
| `proposal_expected_product` + `product_deliverable` | **gộp thành `project_deliverable`** | (a): thầy nói rõ expected products đừng tách riêng — là "project deliverables" con của Project. Cột: `project_id` NOT NULL · `contract_id`/`contract_phase_id` nullable (HĐ ký 1 tập con — đúng kịch bản ký từng phần) · name, category_id, scientific_requirements, due_date, submitted_at, file_url, acceptance_status |

### 2.4 GIỮ NGUYÊN (+ lý do)

| Nhóm | Bảng | Lý do giữ |
|---|---|---|
| Master data | research_type, research_track, product_category, amendment_category, llm_config, personnel_role_type, system_financial_config, budget_expense_category | Lookup thuần, không dính neo |
| Financial | budget_allocation_rule, disbursement_template, council_remuneration_rate, rubric_criterion | Cấu hình theo research_type/template — không đổi |
| Users | role, user, user_role, organizational_unit, academic_profile | Không dính |
| Budget của đề cương | proposal_budget, proposal_budget_labor_detail, proposal_budget_item | Kinh phí là **nội dung của bản đề cương** → theo proposal-version (đề xuất; xem câu hỏi mở Q2). Riêng labor_detail đổi FK team_member → `project_member` |
| Nội dung đề cương | proposal_research_content, proposal_activity | Vẫn theo proposal (nội dung tài liệu) |
| Hội đồng — phần trong | council_member, council_meeting, meeting_attendance, review_score_detail | Cấu trúc nội bộ council/score không đổi |
| Contract phụ | contract_settlement (1-1 contract), amendment_request | Đã đúng mức contract |
| Progress | progress_report (+items) giữ theo `contract_id` | Tiến độ là của hợp đồng-giai đoạn |
| AI/Logs | llm_output, semantic_search_vector, document, notification, audit_log, email_log | Polymorphic — chỉ thêm giá trị mới cho entity_type ("PROJECT") |

**Kết quả:** 52 bảng → **~56 bảng** (thêm 5, gộp 2 thành 1).

### 2.5 Chuỗi neo mới (xương sống)

```
cycle ──< cycle_track >── track
              │
              ▼
           project ◄─── order (NOT NULL; có order mặc định "Cơ bản")
           │  │  │  │
           │  │  │  └──< project_member
           │  │  └──< proposal (v1, v2, … — is_current)  ──< budget/content/activity
           │  └──< project_deliverable  (contract_id/phase_id nullable)
           └──< contract ──< contract_phase ──< disbursement(tranche)
           │        └── settlement (1-1)         └─ hoặc gắn deliverable
           ├──< project_round >── review_round (theo cycle_track, rubric riêng)
           │                          └──< review_council (song song) ──< council_project_assignment
           │                                    └── member/meeting/score(+project)/decision(+project)
           └── final_report (1-1 project)
```

---

## 3. Roadmap code (đợt sau — SAU khi thầy duyệt ERD v3)

> Chiến lược: **reset DB** (schema mới + `DatabaseSeeder` viết lại), không data-migration. Migration EF = 1 bản mới sạch (`Phase8_ProjectCentric` hoặc re-init).

### Phase A — Project root + versioning + cycle_track + order + contract phase
Làm trước vì mở khóa mọi thứ khác, và **không** phá luồng chấm hiện có (round/council tạm giữ neo cũ qua project thay proposal — compat mỏng).
- Entities: + `Project`, `CycleTrack`, `ContractPhase`; sửa `Proposal` (version), rename `ProposalTeamMember`→`ProjectMember`, gộp deliverable; sửa `Contract`, `FinalReport`, `ResearchOrder`, `RubricTemplate`.
- DbContext: neo mới + unique mới; Seeder: order mặc định/cycle, cycle_track, demo project.
- Services phải rewire (~13): ProposalService (create → tạo Project+Proposal v1; submit; revision → bản mới), ContractService, DisbursementService, FinalReportService, ProgressReportService, TeamMemberService, DeliverableService, ResearchOrderService, CycleService (track theo cycle), ExportService…
- FE tối thiểu: form nộp (chọn track trong cycle, order), màn project detail bọc proposal versions.
- **Độ đụng chạm: LỚN** (~13 service + DTO + FE forms) nhưng cơ học, ít rẽ nhánh logic.

### Phase B — Review M-N (điểm c)
- `ReviewRound` → cycle_track; `ProjectRound`, `CouncilProjectAssignment`; score/decision/feedback thêm chiều project; luồng mở round/gán council/chấm/biên bản viết lại (ReviewRoundService, CouncilService, ReviewScoringService, AcceptanceEvaluation…).
- FE: màn chấm điểm, gán hội đồng, biên bản — viết lại phần chọn project trong council.
- **Độ đụng chạm: RẤT LỚN + đổi hành vi** → tách riêng, làm sau khi Phase A chạy ổn. Rule #12 (Chủ tịch quyết, Thư ký soạn) + rule #2 (FINANCE sau SCIENCE) + rule #5 (COI) giữ nguyên ngữ nghĩa, chỉ đổi chỗ neo.

---

## 4. Câu hỏi mở — cần chốt (với thầy hoặc nội bộ nhóm) trước khi code

| # | Câu hỏi | Đề xuất hiện tại trong ERD v3 |
|---|---|---|
| Q1 | `final_report` neo project (1 bản cuối/đề tài) hay per-contract (mỗi HĐ giai đoạn 1 bản)? | **Neo project** — nghiệm thu cuối là của đề tài; nghiệm thu từng giai đoạn đã có qua deliverable/phase |
| Q2 | Budget theo proposal-version (mỗi bản đề cương 1 dự toán) hay theo project? | **Theo version** — dự toán là nội dung đề cương, sửa đề cương = sửa dự toán; số chốt cuối nằm ở contract |
| Q3 | `project.status` có gộp cả trạng thái review không, hay chỉ vòng đời lớn? | Chỉ vòng đời lớn; trạng thái chấm chi tiết nằm ở `project_round.status` |
| Q4 | Council chấm theo assignment (1 council – n project) — điểm/biên bản có cần khóa theo phiên họp (meeting) không? | Chưa — decision khóa theo (council, project); meeting giữ như hiện tại |
| Q5 | Supervisors trong biên bản ("Project holds members, supervisors") — capstone context hay cần field? | Map = `project_member.member_role_code` (không tạo bảng riêng) |

---

## 5. Việc docs kéo theo (khi code xong Phase A/B)

State machine Proposal/Project (§3–§4 Review2_Diagrams.md), Activity §9, Use Case §8, `RP4_Diagrams.md` (class/sequence), `API_CONTRACT.md` — đều phải cập nhật theo model mới. Ghi vào backlog `docs/README.md`.
