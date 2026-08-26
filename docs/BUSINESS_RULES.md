# FURPMS — Bộ quy tắc nghiệp vụ (tra cứu)

> **Mục đích:** trả lời được câu *"quy tắc này ở đâu ra, và nó nằm ở dòng code nào?"* trong vòng
> mười giây. Mỗi dòng gồm **luật → căn cứ → nơi thực thi → cách kiểm**.
>
> **Không phải nơi ghi quyết định.** Quyết định nghiệp vụ nằm ở `CLAUDE.md` §Business Rules
> (rule #1–#23). File này chỉ **soi chiếu** các quyết định đó xuống code — nếu hai bên lệch nhau,
> `CLAUDE.md` đúng và code sai.
>
> Rà lần gần nhất: **09/08/2026** · BE **115 test** xanh · **277** chốt chặn trong service/controller.

---

## Cách đọc

| Cột | Nghĩa |
|---|---|
| **Luật** | Phát biểu bằng tiếng Việt, đúng cái người dùng gặp |
| **Căn cứ** | QĐ 543/QĐ-ĐHFPT (điều/biểu mẫu) · hoặc buổi chốt với thầy (tuần mấy) |
| **Thực thi** | File + phương thức chặn. Không có nghĩa là **chưa được thực thi** |
| **Mã lỗi** | 400 `ArgumentException` · 403 `ForbiddenException` · 404 `KeyNotFoundException` · 409 `InvalidOperationException` |

---

## A. Đợt & đề cương

| # | Luật | Căn cứ | Thực thi | Mã |
|---|---|---|---|---|
| A1 | **1 đợt = đúng 1 loại đề tài.** Mở cả Ứng dụng lẫn Cơ bản ⇒ tạo 2 đợt độc lập | rule #7 (tuần 6) | `ResearchCycle.ResearchTypeId`; loại đề tài **suy từ đợt**, PI không chọn | — |
| A2 | Đợt dùng **năm dương lịch** (`2026`), không có khái niệm "năm học" | QĐ543 dùng *"Quý I hằng năm…"*, biểu mẫu ghi *"NĂM 20…"* | `CycleService.CreateCycleAsync` (chỉ nhận số nguyên) + FE regex 4 chữ số | 400 |
| A3 | Quá hạn nộp của đợt ⇒ **không nộp được** | QĐ543 Điều 6 | `ProposalService.SubmitAsync` | 409 |
| A4 | **Gia hạn deadline = GHI THÊM, không ghi đè.** Deadline hiệu lực = bản mới nhất, mốc gốc giữ nguyên | rule #19 (tuần 10) | bảng `deadline_extension`; `CycleService` | — |
| A5 | Chỉ **chủ nhiệm** mới sửa/nộp/rút đề cương của mình | rule #11 | `ProposalService` (Edit/Submit/Withdraw) | 403 |
| A6 | Chỉ **bản nháp** mới sửa/nộp được; muốn sửa bản đã nộp thì **rút lại trước** | QĐ543 Điều 6 | `ProposalService.UpdateAsync/SubmitAsync` | 409 |
| A7 | Trước khi nộp phải **cập nhật CV** hoặc xác nhận CV vẫn đúng (cũ > 6 tháng ⇒ nhắc) | rule tuần 6 | `ProposalService.SubmitAsync` | 409 |
| A8 | Upload + AI là **TÙY CHỌN** — nhập tay ngang hàng, không ép dùng AI | rule #10, #20 | 2 đường song song ở wizard | — |
| A9 | **Trần kinh phí đề tài: cơ bản ≤ 100tr · ứng dụng/triển khai ≤ 150tr.** Chặn ở **cả 4 cửa**: tạo đề cương · sửa đề cương · tạo bản chỉnh sửa · sửa dự toán — và kiểm lại lần cuối khi **nộp** | QĐ543 **Điều 14.1** | `BudgetPolicyService.AssertWithinCapAsync`, gọi từ `ProposalService.SyncBudgetItemsAsync` + `SubmitProposalAsync` + `ProposalBudgetService.UpdateBudgetAsync` | 400 |
| A10 | Trần **để ở master data** `research_types.max_budget_cap`, không cắm số vào code — vì Điều 14.3 cho phép Hiệu trưởng duyệt vượt trần; khi đó Phòng QLKH nâng trần (có dấu vết) chứ hệ thống không tự mở | QĐ543 **Điều 14.3** | `ResearchType.MaxBudgetCap`; `PUT /research-types/{id}` | — |
| A11 | Đơn **đặt hàng** đặt trần riêng thì lấy trần **nghiêm ngặt hơn**; đơn nới rộng hơn **không** phá được trần Điều 14 | rule #8 + Điều 14 | `BudgetPolicyService.GetCapAsync` (lấy `Min`) | 400 |
| A12 | Chủ nhiệm **KHÔNG chọn "phương thức khoán chi"** — lịch giải ngân do loại đề tài quyết định (xem D12). Ô này đã gỡ khỏi wizard | QĐ543 không có khái niệm này | `Step3Details.tsx` | — |
| A13 | **Dự toán chỉ gồm 06 hạng mục của Điều 15**: thù lao · thiết bị/vật tư · thuê ngoài · hội nghị/hội thảo · VPP & chi khác · phát sinh/SHTT | QĐ543 **Điều 15.1** | `DatabaseSeeder.SeedBudgetExpenseCategoriesAsync`; 12 hạng mục cũ (mẫu cấp Bộ) chuyển `IsActive = false`, **không xoá** để dự toán cũ không gãy FK | — |
| A14 | **Tỷ lệ tối đa từng hạng mục trên TỔNG dự toán**: 100 · 60 · 60 · 30 · 20 · 10 (%) | QĐ543 **Điều 15.1** | `BudgetPolicyService.AssertCategoryLimitsAsync` — gọi từ `SyncBudgetItemsAsync` + `ProposalBudgetService.UpdateBudgetAsync`; lỗi liệt kê **tất cả** hạng mục vi phạm một lần | 400 |
| A15 | Tổng dự toán = **tổng các hạng mục**, không nhập tay | tránh hai con số đá nhau | `SyncBudgetItemsAsync`; FE `BudgetBreakdownTable` tự cộng | — |
| A16 | **Hiển thị kinh phí là BẮT BUỘC, không phải tuỳ chọn.** Hệ thống quản *hồ sơ kinh phí* (dự toán · trần · giá trị HĐ · lịch giải ngân · quyết toán) — chỉ KHÔNG quản *dòng tiền* (không thanh toán, không nối ngân hàng) | rule #15 **viết lại 25/08** sau biên bản bảo vệ lần 2 | `GET /api/projects/{projectId}/budget` → `ProjectBudgetOverviewService`; FE `BudgetOverviewPanel.tsx` | — |
| A17 | **Đợt đã đánh dấu chi mà chưa có số thực chi ⇒ tổng là TẠM TÍNH theo kế hoạch, và phải nói ra.** Hệ thống không tự suy diễn thay kế toán | rule #15 (ghi nhận lại, không tự tính) | cờ `hasUnreportedActuals` trong `ProjectBudgetOverviewService`; FE hiện câu cảnh báo dưới thanh tiến độ | — |
| A18 | **Còn lại = giá trị HỢP ĐỒNG − đã chi**, KHÔNG phải dự toán − đã chi. Dự toán là số *xin*, hợp đồng mới là số *cam kết chi* | phân biệt hai giai đoạn hồ sơ | `ProjectBudgetOverviewService.GetAsync` (`RemainingTotal`) | — |
| A19 | **Báo cáo nghiệm thu phải nộp trước ngày kết thúc đề tài ít nhất 30 ngày** | QĐ543 **Điều 11.2.a** (nguyên văn) | `FinalReportService.SubmitAsync` ghi `final_reports.deadline = contract.EndDate − FINAL_REPORT_LEAD_DAYS`; hiện trên dòng thời gian và scanner nhắc hạn | — |
| A20 | **Mỗi giai đoạn của đề tài phải có hạn, và hạn phải nói được LẤY TỪ ĐÂU** | biên bản bảo vệ lần 2 (25/08) | `ProjectTimelineService` — mỗi mốc mang `DeadlineSource` + `DeadlineBasis`; `GET /api/projects/{id}/timeline` | — |
| A21 | **Giai đoạn chấm có hạn**; mở vòng là tự đặt theo `SCORING_WINDOW_DAYS`. Quá hạn thì **gắn cờ, KHÔNG tự đóng vòng** — kết luận là của Chủ tịch | rule #12 + biên bản 25/08 | `ReviewRoundService.OpenRoundAsync` / `SetRoundDeadlineAsync`; cờ `IsScoringOverdue` | 400/409 |
| A22 | **Dời hạn chấm phải nêu lý do**, và ghi thành log chứ không ghi đè ngày gốc | rule #19 | `SetRoundDeadlineAsync` → `deadline_extensions` (`target_type = REVIEW_ROUND`) | 400 |
| A23 | **Giải ngân KHÔNG có hạn theo ngày** — đợt mở khoá theo điều kiện (nghiệm thu sản phẩm, duyệt báo cáo). Dòng thời gian hiện `NO_DEADLINE` kèm điều kiện, **không bịa ngày** | bản chất nghiệp vụ | `ProjectTimelineService.AddContractStagesAsync` | — |
| A24 | **Đếm ngược tới hạn do MÁY CHỦ tính, giao diện không được trừ ngày** — mọi DTO có hạn đều trả `daysLeft` (âm = quá hạn, `null` = chưa đặt hạn) | máy chủ chạy UTC, trình duyệt chạy giờ máy người dùng ⇒ sát nửa đêm lệch nguyên 1 ngày; đã xảy ra thật 25/08 | `Application/Common/DeadlineMath.cs` (qua `IClock`) | — |
| A25 | **Việc quá hạn luôn hiện trên thẻ nhắc việc**, kể cả khi đã quá xa cửa sổ `days` | lọc thuần theo "trong N ngày tới" sẽ làm việc trễ lâu nhất biến mất hẳn — đúng thứ cần thấy nhất | `ProjectTimelineService.GetUpcomingAsync` | — |
| A26 | **Đề tài đã đóng (`COMPLETED/CANCELLED/TERMINATED`) không vào thẻ nhắc việc** | hạn cũ của đề tài đã xong không còn ý nghĩa, để lẫn chỉ làm thẻ đầy rác | `ProjectTimelineService.GetUpcomingAsync` | — |
| A27 | **Mọi quyết định với đề tài đều được ghi vào sổ `project_decisions`** — 12 điểm chốt trong luồng thật | hội đồng bảo vệ lần 2 yêu cầu *"lưu trữ lại các quyết định liên quan đến đề tài"* | `IDecisionLogger` gắn ở 7 service | — |
| A28 | **Chức danh người quyết định được chép cứng tại thời điểm chốt**, không suy ra lúc đọc | người ta đổi vai/nghỉ việc/bị gỡ quyền — hồ sơ cũ phải giữ nguyên "Chủ tịch hội đồng" chứ không biến thành vai hiện tại | `ProjectDecision.DecidedByRole` | — |
| A29 | **Ghi sổ quyết định hỏng thì nuốt lỗi, không chặn nghiệp vụ chính** | chặn một kết luận hợp lệ của Chủ tịch chỉ vì không ghi nổi một dòng sổ phụ là đánh đổi sai hướng | `DecisionLogger.Log` try/catch + ghi log | — |
| A30 | **Sổ quyết định là sổ MỎNG, trỏ ngược bản gốc** — không chép lại nội dung | chép lại là tạo hai nguồn sự thật, sửa một bên thì bên kia sai | `SourceEntityType + SourceEntityId` | — |

## B. Hội đồng

| # | Luật | Căn cứ | Thực thi | Mã |
|---|---|---|---|---|
| B1 | Hội đồng **xét duyệt 3–5** người, **nghiệm thu 5–7** người | QĐ543 **Điều 8.2** / **Điều 12.2** | `CouncilService.CouncilSizeFor` | 409 |
| B2 | Số thành viên phải **LẺ** — để bỏ phiếu luôn có chênh lệch làm căn cứ cho kết luận | thầy chốt 08/08 | `CouncilService` (chặn lúc gửi thư mời) + FE cảnh báo ngay lúc gán người | 409 |
| B3 | Phải có **đủ Chủ tịch + Thư ký + lịch họp** (đủ ngày/giờ + địa điểm hoặc link) mới hiện nút **Gửi thư mời** | rule #17 (tuần 10) | `CouncilService.SendInvitationsAsync` | 409 |
| B4 | Gán reviewer **hết** rồi mới gửi thư mời **một lượt** — tránh spam khi còn sửa | rule #13 | `CouncilService` (mỗi người chỉ gửi 1 lần) | 409 |
| B5 | **COI:** người có tên trong nhóm nghiên cứu của đề tài **không được** làm thành viên hội đồng chấm chính đề tài đó | rule #5 | `CouncilService.AddMemberAsync` | 400 |
| B6 | Chưa **xác nhận** lời mời thì chưa được chấm / soạn / duyệt biên bản | rule #13 | `ReviewScoringService.AssertConfirmed` | 403 |
| B7 | Reviewer từ chối ⇒ báo Staff đi tìm người thay; quá hạn xác nhận ⇒ hết tư cách | rule #4, #13 | `CouncilService` (`DECLINED` / `EXPIRED`) | 409 |
| B8 | Hội đồng **đã có phiếu / biên bản / đánh giá** thì **không xoá được** | an toàn dữ liệu | `CouncilService.DeleteAsync` | 409 |
| B9 | Cảnh báo **trùng lịch** giảng viên giữa hai hội đồng giao giờ | rule #17 | `GET /councils/{id}/schedule-conflicts` | — |
| B10 | Khung giờ chấm từng đề tài phải **nằm trong** buổi họp và **không chồng nhau** | rule #17 | `CouncilService.SaveCouncilSlotsAsync` | 409 |
| B11 | Thay người / đổi lịch được **bất kỳ lúc nào** — không đóng băng hội đồng | rule #17 | không có khoá nào chặn | — |

## C. Chấm điểm & biên bản

| # | Luật | Căn cứ | Thực thi | Mã |
|---|---|---|---|---|
| C1 | Bộ tiêu chí phải **cộng đúng** tổng điểm tối đa mới dùng chấm được | QĐ543 **BM03** (10+20+40+20+10 = *"Cộng 100"*) | `RubricTemplatesController` (thêm/sửa) + `ReviewScoringService.SubmitScoreAsync` (nộp phiếu) | 400 / 409 |
| C2 | Điểm từng tiêu chí trong `[0, điểm tối đa của tiêu chí]` | BM03 | `ReviewScoringService.SubmitScoreAsync` | 400 |
| C3 | **Bước nhảy điểm do Admin đặt** (`SCORE_DECIMAL_PLACES`, mặc định 0 = số nguyên). **Không hồi tố** — chỉ soi phiếu mới | thầy chốt 08/08; QĐ543 không quy định | `ReviewScoringService` + FE lấy `step` từ setting | 400 |
| C4 | **Mọi thành viên đều chấm**, kể cả Thư ký | QĐ543 **Điều 8.3.b** (*"các thành viên tham dự họp **cần đánh giá thẩm định** đề cương"*) + **Điều 8.3.c** (*"Thư ký ghi biên bản… và **các thành viên của Hội đồng** thông qua"* ⇒ Thư ký là thành viên) | không có nhánh nào loại Thư ký; test `SubmitScore_BySecretary_Succeeds` khoá lại | — |
| C5 | **Quorum 2/3** (làm tròn LÊN: 5 người cần 4) mới **lưu nháp lẫn chốt** được biên bản | QĐ543 **Điều 8.3.b** | `ReviewScoringService.AssertQuorumAsync` | 409 |
| C6 | Quorum đếm **cả** phiếu chấm điểm (`review_scores`) **và** phiếu Đạt/Không đạt (`acceptance_evaluations`) | vòng nghiệm thu không chấm điểm (BM11) | `AssertQuorumAsync` — *sửa 08/08, trước đó chỉ đếm bảng đầu nên hội đồng nghiệm thu không bao giờ chốt được biên bản* | 409 |
| C7 | Nghiệm thu phải có **phản biện dự họp và cho ý kiến** | QĐ543 **Điều 12.3.b** | `AssertQuorumAsync` | 409 |
| C8 | **Thư ký soạn** biên bản → **Chủ tịch duyệt = KHOÁ** → mới đổi trạng thái đề tài. Người khác chỉ xem | rule #12 (tuần 7) | `SaveMinutesAsync` / `ApproveMinutesAsync`; cờ khoá = `CouncilDecision.FinalizedAt` | 403 / 409 |
| C9 | **Kết quả = quyết định của Chủ tịch.** Hệ thống **đếm phiếu để làm căn cứ**, KHÔNG tự chốt | rule #12 | `GetBallotTallyAsync` chỉ trả số liệu; không có nhánh nào tự quyết | — |
| C10 | Biên bản đã khoá ⇒ **không sửa điểm, không sửa biên bản** | rule #12 | `SubmitScoreAsync`, `SaveMinutesAsync` | 409 |
| C11 | Số liệu biên bản theo **BM12 mục 10.1**: phát ra / thu về / hợp lệ / không hợp lệ; nghiệm thu để **null** điểm trung bình | QĐ543 **BM12 §10.1** | `ComputeTallyAsync`, `GetBallotTallyAsync` | — |
| C12 | Chỉ còn **2 loại vòng**: `REVIEW` (xét duyệt đề cương) và `ACCEPTANCE` (nghiệm thu). Bỏ `SCREENING` | rule #16 (tuần 10) | `ReviewRoundService`, `ReviewBoardService` | 400 |
| C13 | Chỉ còn **phương diện SCIENCE**. Phương diện TÀI CHÍNH đã bỏ | rule #16 | `ReviewRoundService`, `ReviewBoardService` — *sửa 09/08* | 400 |
| C14 | Vòng có **vòng tiên quyết** ⇒ đề tài phải **ĐẠT** vòng đó mới vào được (chưa từng tham gia = chưa đạt) | rule #2 (tổng quát hoá) | cả hai đường `ReviewRoundService` + `ReviewBoardService` — *sửa 09/08, trước đó là code chết vì gắn với FINANCE* | 409 |
| C15 | `REJECTED` ⇒ đề tài kết thúc. `REVISION_REQUIRED` ⇒ PI sửa & nộp lại thì **vòng mở lại**, giữ điểm cũ | rule #1 | `ReviewRoundFinalizer`, `ProposalService.SubmitAsync` | — |

## D. Hợp đồng & giải ngân

| # | Luật | Căn cứ | Thực thi | Mã |
|---|---|---|---|---|
| D1 | **Hệ thống KHÔNG quản tiền.** Chỉ theo dõi **mốc** giải ngân + lưu **minh chứng** | rule #15 (tuần 10) | số tiền là tuỳ chọn khi xác nhận; không có phép tính tiền nào | — |
| D2 | Gia hạn **tối đa ½ thời gian thực hiện** (12 tháng ⇒ tối đa 6) | QĐ543 **Điều 10.4** | `ContractService.ValidateMaxExtension` (cả tạo lẫn sửa) | 400 |
| D3 | Đợt giải ngân **có gắn sản phẩm minh chứng** ⇒ sản phẩm phải **nghiệm thu Đạt** mới đánh dấu đã chi | QĐ543 Điều 16 | `DisbursementService.ConfirmAsync` | 409 |
| D4 | **Đợt CUỐI** chỉ chi sau khi đề tài được **công nhận Đạt**; áp cả đề tài cơ bản chỉ có **1 đợt 100%** | QĐ543 **Điều 16.1.d, 16.2** + BM05 Điều 4.2 | `DisbursementService.AssertMilestoneUnlockedAsync` | 409 |
| D5 | Còn đợt chưa chi ⇒ **không lập được quyết toán** (quyết toán = bước đóng hợp đồng) | suy ra từ D4 | `ContractSettlementService.CreateAsync` | 409 |
| D6 | Hợp đồng **chưa ký** mới xoá được; đã có sản phẩm/báo cáo/quyết toán ⇒ không xoá | an toàn dữ liệu | `ContractService.DeleteAsync` | 409 |
| D7 | Hợp đồng đã ký **không sửa đè bản gốc** — mỗi điều chỉnh phải có **phụ lục** riêng ghi nội dung đã được duyệt | QĐ543 **BM05 Điều 6.1** (báo trước 15 ngày) | `DocumentExportService.ExportAmendmentDocAsync` | 409 nếu đơn chưa duyệt |
| D8 | Đề tài đã **nghiệm thu xong / huỷ / chấm dứt** ⇒ không còn gì để điều chỉnh | thầy bắt lúc demo 05/08 | `AmendmentService.EnsureContractStillOpen` (cả tạo lẫn duyệt) | 409 |
| D9 | Yêu cầu gia hạn phải ghi **SỐ THÁNG**; gõ "3 tháng" ⇒ báo lỗi rõ, không im lặng bỏ qua | tự phát hiện | `AmendmentService.ApplyExtensionIfNeededAsync` | 400 |
| D10 | Số tài khoản / CCCD của Bên B: **chính chủ tự khai**, đọc ra **luôn che**, **tuỳ chọn** không chặn lập hợp đồng | QĐ543 **BM05 Điều 7.2** (chứng thư số) + user chốt 08/08 | `ContractIdentityController` (chỉ đường `/me`) | 400 |
| D11 | Tự sinh Word hợp đồng đủ mẫu BM05 → ký ngoài → **upload bản ký làm minh chứng** | rule #21 (tuần 10) | `ExportContractDocAsync` + `Document` polymorphic | — |
| D12 | **Lịch giải ngân do LOẠI ĐỀ TÀI quyết định, PI không được chọn.** Ứng dụng **4 đợt 30–30–30–10**; Cơ bản **1 đợt 100% sau nghiệm thu "Đạt"** | QĐ543 **Điều 16** | `DisbursementService.GenerateFromTemplateAsync` đọc `disbursement_templates` theo `ResearchTypeId`; seed ở `DatabaseSeeder.SeedDisbursementTemplatesAsync` | — |
| D13 | Tỷ lệ từng đợt **để trong master data**, không cắm số vào code; **đợt cuối lấy phần còn lại** để tổng luôn khớp giá trị hợp đồng | tránh lệch tiền do làm tròn | `GenerateFromTemplateAsync` | — |
| D14 | Xác nhận mốc giải ngân phải đúng thứ tự điều kiện: hợp đồng **đã ký**; Ứng dụng đợt 2/3 cần tiến độ GĐ1/GĐ2 **Đạt**; đợt cuối cần nghiệm thu **Đạt** | QĐ543 **Điều 16** | `DisbursementService.AssertMilestoneUnlockedAsync` | 409 |
| D15 | BM07 chỉ chia **4 nhóm**: nội dung/tên đề tài · tiến độ/thời gian · dự toán kinh phí · thay đổi khác. Không ép mọi nhóm khai cặp “giá trị hiện tại → đề nghị”; chỉ gia hạn cần số tháng cấu trúc | QĐ543 **BM07**, Điều 10.2 | danh mục `amendment_categories`; form PI; `DocumentExportService.ExportAmendmentDocAsync` | — |
| D16 | Nghiệm thu `Đạt` ⇒ **Project = COMPLETED**, nhưng hợp đồng chưa phải đã thanh lý | QĐ543 Điều 13.1–13.2 | `ReviewScoringService`; `ContractDto.ProjectStatus` | — |
| D17 | Chỉ lập quyết toán sau nghiệm thu Đạt và chi xong các đợt; chỉ ký BM13 sau xác nhận quyết toán kinh phí + xử lý tài sản; ký xong ⇒ **Contract = SETTLED** | QĐ543 Điều 13.1.e–13.2, BM13 | `ContractSettlementService` | 409 |
| D18 | Hợp đồng đang hiệu lực, đề tài chưa hoàn thành chỉ được **TERMINATED** để *ghi nhận quyết định/căn cứ dừng*; lý do tối thiểu 20 ký tự, đồng thời Project = TERMINATED. Đã nghiệm thu Đạt thì phải đi thanh lý BM13. Không có toggle hoàn tác trực tiếp; phục hồi nếu có phải là nghiệp vụ thu hồi quyết định có audit riêng | QĐ543 Điều 10.3 (Hiệu trưởng quyết định đình chỉ) + BM05 Điều 5.1.h | `ContractService.TerminateAsync` | 400 / 409 |
| D19 | Danh sách hợp đồng phải trả và hiển thị đủ **loại đề tài · đợt · lĩnh vực**, vì loại đề tài quyết định trực tiếp lịch giải ngân D12 | QĐ543 Điều 16 | `ContractService.GetListAsync`; FE `ContractsPage` | — |

> ⚠️ **"Phương thức khoán chi" (WHOLE/PARTIAL) KHÔNG có trong QĐ543** — rà toàn văn, chữ "khoán" chỉ xuất hiện ở *"thuê khoán chuyên môn"* và *"giao khoán"*. Khái niệm này đến từ mẫu thuyết minh cấp Bộ (`Mau-1_Thuyet-minh`). Cột `Proposal.FundingMethod` **vẫn còn trong DB** để đọc dữ liệu cũ nhưng **không còn quyết định số đợt giải ngân** (D12).

## E. Thực hiện & nghiệm thu

| # | Luật | Căn cứ | Thực thi | Mã |
|---|---|---|---|---|
| E1 | Báo cáo tiến độ giữa kỳ do **Staff duyệt trực tiếp, KHÔNG lập hội đồng** ⇒ **không có thang điểm** | rule #16 (tuần 10) | `ProgressReportService.EvaluateAsync`; dossier trả `progressReportNote` nói rõ | — |
| E2 | Kỳ báo cáo phải **tuần tự**: kỳ N−1 chưa được đánh giá thì kỳ N chưa nộp được | QĐ543 **Điều 10.1** | `ProgressReportService.SubmitAsync` | 409 |
| E3 | Chỉ **chủ nhiệm** tạo/sửa/nộp báo cáo; chỉ **bản nháp** mới nộp được | rule #11 | `ProgressReportService` | 403 / 409 |
| E4 | Báo cáo phải có **file hoặc link** thì Staff mới đánh giá được | thầy 29/07 | `ProgressReportService` + FE khoá nút | 409 |
| E5 | Sản phẩm đã **nghiệm thu Đạt** ⇒ **không nộp lại**, không gia hạn thêm | thầy 05/08 | `DeliverableService`, `ChangeRequestService` | 409 |
| E6 | Nghiệm thu chỉ **Đạt / Không đạt**, không chấm điểm; phiếu Không đạt phải ghi lý do | QĐ543 **BM11** | `AcceptanceEvaluationService` | 400 |
| E7 | Nghiệm thu **Đạt** ⇒ đề tài `COMPLETED`; **chưa đạt** ⇒ quay lại `IN_PROGRESS` để làm tiếp | Process_Spec §Giai đoạn 8 | `ReviewScoringService.ApproveMinutesAsync` | — |
| E8 | Hồ sơ nghiệm thu phải mở được **file thật** của từng kỳ, không chỉ cờ "có file" | thầy 05/08 (C4/C5) | `AcceptanceDossierController` | — |

## F. Hệ thống

| # | Luật | Căn cứ | Thực thi | Mã |
|---|---|---|---|---|
| F1 | **PI là actor duy nhất** đại diện đề tài; thành viên nhóm chỉ là dữ liệu, không đăng nhập | rule #11 | không cấp tài khoản cho `ProjectMember` | — |
| F2 | **Reviewer = mọi thành viên hội đồng.** Chức danh (Chủ tịch/Thư ký/Phản biện) chỉ là **field khi gán**, không tách vai trò đăng nhập | rule #11 | `CouncilMember.MemberRole` | — |
| F3 | **Đa vai:** một người có nhiều vai, đổi vai ở dropdown header, chỉ hiện vai thực có | rule #23 (tuần 10) | `UserRole` nhiều-nhiều | — |
| F4 | Biểu mẫu **pin version active tại thời điểm tạo**; đổi bộ active chỉ áp đề tài mới | rule #13 | `ReviewRound.RubricTemplateId` | — |
| F5 | Giới hạn file upload & định dạng do **Admin cấu hình**, đọc lại mỗi lần upload (không cần khởi động lại) | vận hành | `SystemSettingService.GetUploadPolicyAsync` | 400 |
| F6 | Dữ liệu demo tắt được bằng `DEMO_DATA_ENABLED`; seeder hỏng **không được** chặn ứng dụng khởi động | vận hành | `DemoScenarioSeeder` (giao dịch riêng từng kịch bản) + `Program.cs` | — |
| F7 | Mã lỗi: 400 sai dữ liệu · **401 chỉ dùng cho lỗi xác thực** (FE thấy 401 là đăng xuất) · 403 thiếu quyền · 404 không tìm thấy · 409 xung đột trạng thái | quy ước dự án | `GlobalExceptionMiddleware` | — |
| F8 | **Mọi thông báo lỗi nghiệp vụ đều bằng tiếng Việt** — rà 09/08, dịch 86 câu còn sót | thầy 05/08 (E3) | toàn bộ `FURPMS.Infrastructure/Services` | — |
| F9 | AI dùng `llm_outputs` làm nguồn cache tập trung; retry lỗi 429/5xx theo exponential backoff + jitter rồi fallback model nhẹ. Reviewer đọc tóm tắt/gợi ý đã cache, chạy lại thành công mới ghi đè; lỗi không xoá bản tốt gần nhất | yêu cầu demo 19/08 + khuyến nghị Gemini API | `GeminiService`, `AiSummaryPregenerationService`, `AiAdvisorService` | — |

---

## Chỗ đã rà và KHÔNG có luật (ghi lại để khỏi rà lại)

| Câu hỏi | Kết luận |
|---|---|
| Điểm chấm nguyên hay thập phân? | **QĐ543 không quy định.** BM03 để điểm tối đa toàn số nguyên ⇒ mặc định số nguyên, Admin đổi được (C3) |
| Có "hạng quý" đề tài không? | **Bỏ** — rule #7 |
| Báo cáo tiến độ chấm mấy điểm? | **Không có điểm** — Staff duyệt trực tiếp (E1). Ghi chú của nhóm có nêu "bao nhiêu điểm" nhưng mâu thuẫn rule #16; muốn có điểm phải bàn lại rule #16 trước |
| "Phạm vi ký" hợp đồng? | **Không có trong QĐ543** ⇒ đã bỏ khỏi bản Word |
| Năm học `2025-2026`? | **Không có khái niệm năm học** trong QĐ543 ⇒ dùng năm dương lịch (A2) |

---

## Độ phủ kiểm thử

| Nhóm | Số test |
|---|---|
| Vòng chấm & hội đồng (`ReviewRounds`, `Councils`, `Review`) | 51 |
| Hợp đồng, giải ngân, phụ lục (`Contracts`) | 19 |
| Đề cương & tài liệu (`Proposals`) | 11 |
| Cấu hình hệ thống (`MasterData`) | 10 |
| Nhắc hạn (`Reminders`) | 5 |
| Đề nghị điều chỉnh (`ChangeRequests`) | 4 |
| Dự toán (`Budget`) | 3 |
| Gia hạn đợt (`Cycles`) | 2 |
| **Tổng** | **115** |

**Chưa có test tự động** (đã kiểm bằng API/file thật, ghi lại để biết chỗ mỏng): COI (B5) ·
trùng lịch (B9) · dossier nghiệm thu (E8) · sinh Word hợp đồng BM05 (D11) · luồng AI (B-nhóm).

---

## Liên quan

- **Quyết định nghiệp vụ (nguồn):** `CLAUDE.md` §Business Rule Decisions — rule #1–#23
- **Văn bản gốc:** `QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx`
- **Đối chiếu QĐ543 ↔ hệ thống:** `QD543_Compliance.md`
- **Hợp đồng FE↔BE:** `API_CONTRACT.md`
- **Luồng nghiệp vụ:** `Process_Spec_v2.md`
