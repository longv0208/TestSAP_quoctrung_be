## Cập nhật 14/08 — sau demo với thầy

| Việc | Trạng thái |
|---|---|
| Góp ý thầy buổi demo 14/08 (12 mục, xem `GOPY_Thay_Demo_1408.md`) | **11/12 xong** — còn #10 Railway là việc hạ tầng của anh |
| Backlog `BACKLOG_Uu_tien.md` | **26/36 (72%)** — hết sạch P0 và P1 |
| Test BE | **173 xanh** |

**Việc lớn xong đợt này:** lý lịch khoa học liệt kê chi tiết theo BM02 · sinh sẵn tóm tắt AI lúc PI nộp · gộp 1 nút AI cho người chấm · Chủ tịch trả biên bản cho Thư ký sửa · thể thức file Word theo Nghị định 30/2020 · dọn sạch 233 chuỗi tiếng Anh/mã nội bộ trong thông báo lỗi.

**Còn 10 mục, đều P2/P3 đánh bóng** — không mục nào chặn luồng chính.

---

# FURPMS Backend — Tiến độ theo nhóm chức năng

> Ảnh chụp % hoàn thiện **so với phạm vi đồ án** (không phải "phần mềm hoàn hảo"). Đây là **ước lượng có cơ sở** (38 controller, Phase A→L đã code, **106/106 test**, luồng core đã E2E) — KHÔNG phải số đo tự động. Cập nhật: **2026-08-06** (sau demo thầy 05/08 — xem `PLAN_Week13_Demo_0508.md`).
>
> Contract (`API_CONTRACT.md` §3–§10) liệt kê "BE cung cấp gì" theo đúng 8 nhóm dưới đây; file này bổ sung cột **% + còn thiếu**.

| # | Nhóm chức năng | Controller chính | % | Còn thiếu / ghi chú |
|---|---|---|:--:|---|
| 1 | **Auth & Người dùng** | Auth, Users, AcademicProfiles | 95% | refresh-token / rate-limit / email-verify (không bắt buộc cho đồ án) |
| 2 | **Cấu hình / Master data** | Cycles+ResearchTypes+Tracks (**lĩnh vực toàn cục + gắn/gỡ theo đợt, rule #6, 22/07**), 6 lookup, **system_settings (Admin chỉnh giới hạn upload, 20/07)** | 98% | xóa đợt (chưa có endpoint DELETE /cycles/{id}) |
| 3 | **Đề xuất & nội dung** | Proposals, Budget, Contents, TeamMembers, Documents, Export, ChangeRequests, AI-summary | 92% | upload siết theo cấu hình Admin: mặc định ≤10MB + whitelist đuôi file (18/07, chuyển sang `system_settings` 20/07). **12/08 — trần kinh phí QĐ543 Điều 14** (cơ bản ≤100tr · ứng dụng ≤150tr): chặn ở cả 4 đường ghi + cửa nộp (`BudgetPolicyService`), trần để ở master data vì Điều 14.3 cho phép Hiệu trưởng duyệt vượt; wizard bỏ ô "Phương thức cấp kinh phí" (không có trong QĐ543), thay bằng **Tổng dự toán** hiện trần tại chỗ; `GET /proposals/{id}/budget/cap`. **12/08 — Điều 15**: dự toán đổi sang đúng **06 hạng mục** của quy định (bỏ bộ 12 lấy từ mẫu cấp Bộ), chặn tỷ lệ tối đa từng hạng mục trên tổng, tổng tự cộng từ hạng mục; wizard có **bảng dự toán** hiện trần + tỷ lệ từng dòng, bản xem lại và màn reviewer cũng hiện bảng này; 6 cột tổng hợp `proposal_budgets` nay được ghi (trước luôn 0) |
| 4 | **Đặt hàng NC (Applied)** | ResearchOrders | 70% | multi-winner (đã chốt để sau — epic tương lai) |
| 5 | **Phản biện, Hội đồng & Chấm** | ReviewBoard, Rounds, Councils, Meetings, Scoring, Feedback, Acceptance, **RubricTemplates** | 95% | **tuần 12:** fix 3 lỗi chấm nghiệm thu (403 reviewer · lệch kiểu mảng/object · không sửa được phiếu) · nối mạch ACCEPTANCE→`COMPLETED` · **Bộ tiêu chí** (gắn loại đề tài + nhiều đợt/lĩnh vực, sao chép, gắn riêng từng vòng) · chỉ cho tạo vòng REVIEW/ACCEPTANCE · **PI xem lịch họp** (`GET /meetings/my`). **11/08:** thống nhất nhãn vai hội đồng & loại vòng toàn bộ màn Reviewer (`RoleBadge` dùng chung — trước đây badge in thẳng "Chair"/"ACCEPTANCE", và 3 chỗ tra nhầm namespace `councils.role.*` vốn không tồn tại). Còn: AI gợi ý chấm điểm |
| 6 | **Hợp đồng & sau HĐ** | Contracts, Disbursements, Deliverables, Amendments, ProgressReports, FinalReports, Settlements | 93% | **tuần 12:** upload file thật (BM06 báo cáo tiến độ · BM09 tổng kết) thay dán URL · Staff phải xem file mới đánh giá được · số kỳ báo cáo **linh hoạt** + đặt tên đợt (PhaseK) · bảng tiến độ theo hoạt động (BM06) · fix từ vựng đánh giá `PASS/FAIL/CONDITIONAL` (trước lệch 3 kiểu → Staff **luôn 400**). **P5 (04/08):** mọi đợt giải ngân gắn được sản phẩm minh chứng (`PUT /disbursements/{id}/deliverable`), chặn đánh dấu giải ngân khi sản phẩm chưa nghiệm thu Đạt. **05/08:** nộp sản phẩm bằng **file thật** + `TrialEvidenceUrl` (QĐ543 Điều 13.1) · **chuẩn hoá URL sản phẩm** (thiếu scheme → tự thêm `https://`, rác → 400) · báo cáo tiến độ nhận **link thay file** (PhaseM) · **hợp đồng có SỬA + XOÁ** (`PUT`/`DELETE /contracts/{id}`) — trước đây nhập sai là kẹt vĩnh viễn. **11/08 — bám QĐ543 Điều 16:** lịch giải ngân sinh theo **LOẠI ĐỀ TÀI** (Ứng dụng 4 đợt 30–30–30–10 · Cơ bản 1 đợt sau nghiệm thu), tỷ lệ để ở master data `disbursement_templates` chứ không cắm số vào code; bỏ nhánh chia theo `FundingMethod` (WHOLE/PARTIAL — khái niệm không có trong QĐ543). Dữ liệu demo sinh lại theo đúng lịch này, ngày chi đợt cuối đặt sau mốc nghiệm thu. UI: timeline hợp đồng hết lòi enum tiếng Anh (DISBURSED/SUBMITTED/ACTIVE), panel báo cáo tiến độ hết nút "Schedule/Evaluate", bảng hợp đồng hiện tên đề tài thay GUID. Còn: xoá sản phẩm · xoá kỳ báo cáo · xoá lịch họp · xoá thành viên đề tài · xoá 5 màn master data |
| 7 | **Thống kê / Thông báo / Dev-tools** | Analytics (+3 dashboard theo role 15/07), Notifications, Admin, Documents | 93% | **tuần 12:** nhắc hạn thêm mốc **T-3** + scanner quét cả **báo cáo tiến độ** (trước chỉ quét sản phẩm). **12/08:** thêm **6 loại thông báo** cho các mốc trước đây im lặng (nộp đề cương / nộp lại · ký hợp đồng · Chủ tịch khoá biên bản → cả hội đồng · kết quả nghiệm thu → chủ nhiệm · gia hạn hạn nộp → mọi chủ nhiệm trong đợt) — tổng **14 loại**, đã kiểm end-to-end. Sửa bẫy `Notifier` chỉ `AddAsync` mà không lưu (nơi gọi quên `SaveChanges` là thông báo biến mất im lặng). Bảng điều khiển 4 vai có số liệu thật. Doc: `THONG_BAO_VA_EMAIL.md`. Còn: chuông auto-poll · báo khi xác nhận giải ngân / duyệt báo cáo tiến độ · **quên mật khẩu qua email chưa có** |
| 8 | **Hạ tầng nền** | Middleware, JWT, Email/SMTP, DeadlineReminder, Gemini, Seeder | 90% | **04/08:** SMTP Brevo đã chạy thật (mail vào Spam do `FromEmail` là @gmail gửi qua relay — giới hạn hạ tầng); Gemini key mới đã test 200. Còn: `/ai/search` (semantic) + `/ai/suggest-reviewers` chưa có |

**Tổng thể ≈ 88–92%** cho phạm vi capstone. Lõi (đề cương → xét duyệt → hợp đồng → giải ngân → **nghiệm thu → COMPLETED**) chạy thông end-to-end.

## Đối chiếu góp ý thầy (demo 05/08) — `PLAN_Week13_Demo_0508.md`
**43 đầu việc**, xong **24** (≈56%) trong ngày 06/08. Hết **nhóm 1 (lỗi nghiệp vụ)** và
**nhóm 2 (thứ thầy nhìn thấy ngay)**, trừ D1 đang chờ chốt "bao lâu mới cho nộp lại".

Xong: A1 hình thức họp còn online/offline · A2 lịch chấm trong khung giờ họp · A4+A5 kết quả bỏ
phiếu BM12 mục 10.1 · A6 (vốn đã có) · A7 đóng buổi họp khi chốt biên bản · A8 quorum 2/3 +
phản biện · A11 tổng điểm bộ tiêu chí · C7+D5 đóng sản phẩm đã nghiệm thu · C8 trang tiến trình ·
C9 đổi nhãn · E3 full tiếng Việt · E4 light mode · E6 gom bộ lọc · F3 trần gia hạn Điều 10.4.

Xong thêm chiều 06/08: **nhóm AI B1–B5** (đọc file, đối chiếu form, tự chạy trước màn chấm,
prompt ưu/nhược, gợi ý bám tiêu chí) · **D1** nộp dồn kỳ báo cáo · **D4** ngày duyệt gia hạn ·
**A3** Thư ký được chấm · **A9** cỡ hội đồng theo Điều 8.2/12.2 · ẩn lịch chấm khi 1 đề tài +
hiện quỹ giờ buổi họp.

Xong thêm 08/08: **C1** xuất Word hợp đồng đủ mẫu BM05 · **A9** hội đồng số lẻ · **A10** bước
nhảy điểm do Admin đặt · **E7 data demo** (`DemoScenarioSeeder`: 2 đợt + 8 đề tài `NCKH-2026-001..008`
đứng ở 8 bước, 3 hội đồng × 5 người, 3 slot chấm chung một buổi họp, 22 phiếu, 3 biên bản đã chốt,
6 file Word thuyết minh sinh sẵn) · **F5 kịch bản demo** (viết vào `DEMO_GUIDE.md` §3–4).
Dựng data demo lôi ra **2 lỗi thật** đã sửa luôn: **A12** hội đồng nghiệm thu không chốt được biên
bản dù đủ phiếu (quorum chỉ đếm phiếu chấm điểm, bỏ qua phiếu Đạt/Không đạt) · **A13** biên bản
nghiệm thu luôn hiện 0 phiếu hợp lệ. Cả hai chỉ lộ khi có dữ liệu đứng đúng ở bước nghiệm thu.

Xong thêm 08/08 (đợt 2): **C4+C5 hồ sơ nghiệm thu** (thông tin đề tài đầy đủ · file mở được thật ·
từng kỳ báo cáo có ai duyệt/vai gì/file của chính kỳ đó) · **C6** đợt giải ngân cuối khoá theo kết
quả nghiệm thu + chưa chi hết thì không quyết toán · **E2** bỏ "năm học", dùng năm dương lịch.
Riêng C6 bản đầu viết luật quá rộng, **test đỏ lôi ra** hợp đồng 1 đợt bị cấm luôn tiền tạm ứng ⇒
sửa luật (chỉ áp khi ≥2 đợt), không sửa test.

Xong thêm 08/08 (đợt 3): **F4 phụ lục hợp đồng** — `GET /amendments/{id}/export-word` sinh .docx cho
đề nghị điều chỉnh đã duyệt, có bảng *trước → sau*; riêng gia hạn quy số tháng ra mốc thời gian thật.

Xong thêm 08/08 (đợt 4): **C3 trường Bên B** — migration `PhaseN_PiContractIdentity`, PI **tự khai**
số tài khoản/CCCD (Staff không gõ hộ), đọc ra luôn che `****1234`, số đầy đủ chỉ đổ vào file Word
hợp đồng, mỗi lần đổi ghi `audit_logs`. Phải sửa kèm `GET /users/{id}/profile` vì nó trả **thẳng
entity** — thêm cột là lộ nguyên số cho Admin/Staff.
⇒ **Nhóm hợp đồng BM05 (C1–C3, F4) đã đủ**: bản Word xuất ra không còn ô nào để trống vì thiếu dữ liệu.

Xong thêm 09/08: **F1 rà toàn bộ validate** — soi 277 chốt chặn, ra **5 lỗi thật** (phương diện
FINANCE còn sót · chốt "phải đạt vòng tiên quyết" thành **code chết** nên đề tài trượt vẫn vào được
vòng nghiệm thu · hai đường xử lý khác nhau · luật "phải có bản báo cáo mới đánh giá" **chỉ khoá ở
FE** · seeder ghi sai giá trị `EvaluationResult`), dịch nốt **88 câu lỗi tiếng Anh** ⇒ còn **0**.
**F2**: viết `docs/BUSINESS_RULES.md` — 62 luật, tra được **luật → căn cứ QĐ543 → dòng code → mã lỗi**.

Còn: **B6** cache AI + pin version bộ tiêu chí (chờ ý thầy) · **E5/F6** cần hỏi lại thầy ·
và **backlog 4 nguồn** (xem `PLAN_Week13` §BACKLOG HỢP NHẤT, ~26 việc).

## Đối chiếu góp ý thầy (demo 05/08) — kế hoạch: `PLAN_Week13_Demo_0508.md`

**46/50 dòng việc = 92%** (đếm ngày 09/08; note gốc 43 dòng, phát sinh thêm 7 dòng lỗi tự phát hiện).

| Nhóm | Xong | Còn |
|---|---|---|
| **A** Hội đồng · chấm điểm · biên bản | 14/15 | A3 |
| **B** AI | 5/6 | B6 |
| **C** Hợp đồng · nghiệm thu · giải ngân | **9/9** | — |
| **D** Báo cáo tiến độ · tổng kết · gia hạn | **6/6** | — |
| **E** UI/UX · i18n · dữ liệu demo | 7/8 | E5 |
| **F** Business rule · validate · tài liệu | 5/6 | F6 |

**Cả 4 việc còn lại đều phải chờ thầy trả lời**, không tự làm được:
A3 *(xác nhận Thư ký được chấm — code đã cho phép theo Điều 8.3.b)* ·
B6 *(AI chạy local thay Gemini)* · E5 *("UI/UX sửa nhiều" — cần biết màn nào)* ·
F6 *(SRS / báo cáo đồ án / doc kỹ thuật?)*.

## Đối chiếu góp ý thầy (demo 29/07) — kế hoạch: `PLAN_Week12.md`
**≈17/18 ý (94%).** Xong: P0 lỗi nghiệm thu · P1 upload PDF + Staff xem file mới chấm · P2 số đợt/tên đợt linh hoạt · P3 nhắc hạn T-3 & quá hạn · P4 Bộ tiêu chí theo group · **P5 giải ngân gắn sản phẩm minh chứng** · P6 dashboard PI hiện đợt đang mở · P7 chuẩn hoá ngôn ngữ (vi=en=1341 key).
**P8 AI — đang làm:** ✅ nối lại Đường B (`/ai/extract` FE gọi sai đường dẫn ⇒ chưa từng chạy) · ✅ **đối chiếu form ↔ file** (đúng yêu cầu thầy) · ✅ góp ý AI · ✅ tóm tắt (fix crash `highlights.map` + **đưa sang màn reviewer**, nơi thật sự cần) · ✅ **AI gợi ý chấm điểm theo từng tiêu chí**. ⬜ còn `/ai/search` (đang **chờ user chốt** — xem PLAN §Q1), `/ai/suggest-reviewers`, xoá `similarity-check`. ⚠️ toàn bộ AI **chưa test bằng dữ liệu thật**.

## Vì sao KHÔNG nhóm nào 100%?
- **% là ước lượng, không phải đo được.** Trần 95% là chủ ý: không claim "provably complete" khi chưa verify mọi nhánh (edge case, coverage, polish). 100% sẽ là overclaim.
- **"Xong để demo" ≠ "production-complete".** Đồ án đủ chạy luồng chính, nhưng vẫn còn edge/nice-to-have (refresh token, i18n đầy đủ, thùng rác toàn hệ thống…).
- **Có phần cố ý để sau** (đã chốt ngoài phạm vi): multi-winner Applied, AI nâng cao.

## Việc BE nên làm tiếp (ưu tiên)
- **P1 — cấu hình để demo trọn:** đặt `GeminiAI:ApiKey` + SMTP thật (AI trích xuất/tóm tắt + email mời/nhắc hạn). *(Cấu hình/hạ tầng, không phải code.)*
- **✅ Đã xử lý 15/07:** reopen council khi resubmit REVISION (rule #1, `ReopenAfterResubmitAsync` hook vào `SubmitProposalAsync`); gom COI/MapMember/tạo-round trùng ở 3 service về `ReviewShared`; N+1 (tạo vòng, COI hội đồng); projection `GET review-board`; siết quyền board = Admin/Staff. → **hết nợ kỹ thuật ở nhóm review.**

## Nợ kỹ thuật — trạng thái
| Món | Trạng thái |
|---|---|
| N+1 tạo vòng / COI tạo hội đồng | ✅ đã gộp query (15/07) |
| `GET review-board` nạp full entity | ✅ đã projection 4 field (15/07) |
| `GET review-board` lộ danh tính hội đồng | ✅ đã siết Admin/Staff (15/07) |
| Trùng logic đóng round (2 đường) | ✅ đã gom `ReviewRoundFinalizer` (15/07) |
| reopen council khi resubmit REVISION | ✅ đã làm (15/07) — `ReopenAfterResubmitAsync` |
| Trùng COI/MapMember/round-create ở 3 service | ✅ đã gom (15/07) — `ReviewShared` |
