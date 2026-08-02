# Đối chiếu QĐ 543/QĐ-ĐHFPT ↔ Tính năng FURPMS

> Nguồn: `QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx` (QĐ số 543/QĐ-ĐHFPT ngày 14/5/2025) — **22 Điều + Phụ lục 01 (13 biểu mẫu) + Phụ lục 02 (thù lao hội đồng)**.
> Mục đích: (1) khi làm form thì bê đúng field của biểu mẫu, (2) chứng minh hệ thống bám sát quy định, (3) lộ ra chỗ chưa đáp ứng. Cập nhật: 2026-07-18.

## 1. Bảng đối chiếu 13 biểu mẫu

| Biểu mẫu | Thực thể / API | BE | FE | Ghi chú |
|---|---|---|:--:|:--:|---|
| **BM01** Đề cương nghiên cứu | `Proposal` + `/api/proposals`, export `/export/scientific` | ✅ | ✅ | wizard 5 bước; xuất Word Mẫu 1 |
| **BM02** Lý lịch khoa học | `AcademicProfile` + `/api/proposals/{id}/documents` | ✅ | ❌ | **Điều 6.4 bắt buộc nộp** — FE chưa có UI upload |
| **BM03** Phiếu đánh giá thẩm định đề cương | `ProposalReviewScore` + `RubricTemplate/Criterion` | ✅ | ✅ | reviewer chấm theo rubric |
| **BM04** **Biên bản họp HĐ Xét duyệt** | `CouncilDecision`+`CouncilQaEntry`+`CouncilMemberOpinion` + `/review-scoring/…/minutes` | ✅ | ✅ | **tuần 10: UI xong** — roster auto, 2 phong cách Q&A/tự do, ý kiến TV (chuyên môn/kinh phí), Thư ký soạn→Chủ tịch khóa |
| **BM05** Hợp đồng thực hiện đề tài | `Contract` + `GET /contracts/{id}/export-word` | ✅ | ✅ | **tuần 10: tự sinh Word** để ký ngoài + upload bản ký (minh chứng) |
| **BM06** Báo cáo tiến độ | `ProgressReport` | ✅ | ❌ | FE = Coming Soon |
| **BM07** Phiếu đề nghị thay đổi | `ProposalChangeRequest` | ✅ | ❌ | BE có 4 endpoint |
| **BM08** **Đăng ký seminar** | — | 🔴 | 🔴 | **CHƯA CÓ trong hệ thống** (không bảng, không API) |
| **BM09** Báo cáo tổng kết | `FinalReport` | ✅ | ❌ | FE = Coming Soon |
| **BM10** Nhận xét phản biện | `ReviewerFeedback` | ✅ | ✅ | thang điểm 1–5 |
| **BM11** Phiếu đánh giá nghiệm thu | `AcceptanceEvaluation` | ✅ | ✅ | PASS/FAIL |
| **BM12** Biên bản họp HĐ nghiệm thu | `CouncilDecision` (round ACCEPTANCE) | ✅ | 🔴 | dùng chung luồng biên bản với BM04 |
| **BM13** Biên bản thanh lý hợp đồng | `ContractSettlement` | ✅ | ❌ | ký + xác nhận kế toán/tài sản |

**Phụ lục 02 — thù lao hội đồng** → `CouncilRemunerationRate` (BE có bảng). Mức: xét duyệt CT/TK **750k**, TV **700k**; nghiệm thu CT/TK **800k**, TV **750k**. ⚠️ Bảng có nhưng **chưa seed số liệu này**.

## 2. BM04 ↔ `CouncilDecision` — map field (dùng khi làm UI biên bản)

| Mục trong BM04 | Field BE |
|---|---|
| Tên đề tài / Chủ nhiệm / Đơn vị chủ trì | lấy từ `Project` + `Proposal` |
| Thời gian họp, địa điểm | `CouncilMeeting.ScheduledAt`, `MeetingLink`/địa điểm |
| Danh sách thành viên + nhiệm vụ | `CouncilMember.MemberRole` (Chủ tịch/Thư ký/Phản biện/Thành viên) |
| Tổng số thành viên | `CouncilDecision.TotalMembers` |
| Số có mặt / vắng | `AttendingMembers` (+ `MeetingAttendance`) |
| Số phiếu hợp lệ / không hợp lệ | `ValidBallots` / `InvalidBallots` |
| Điểm trung bình cuối cùng | `AverageScore` |
| Ý kiến thành viên (chuyên môn / kinh phí) | `ProposalReviewScore.GeneralComments` + `ReviewerFeedback` |
| **Kết luận của Hội đồng** | `CouncilComments` + `Result` (APPROVED/REJECTED/REVISION_REQUIRED) |
| Chữ ký **Chủ tịch** + **Thư ký** | `ChairUserId` + `SecretaryUserId`, khóa bằng `FinalizedAt` |

→ Thiết kế BE **khớp gần 1:1 với biểu mẫu chính thức**. Làm UI chỉ cần render đúng các field trên.

## 3. Điều khoản nghiệp vụ ↔ luật đã code

| Điều | Nội dung | Trạng thái |
|---|---|---|
| **Điều 6.1–6.2** | Quý I nhận hồ sơ **Ứng dụng**, Quý II nhận **Cơ bản** | ✅ mô hình 1 đợt = 1 loại (rule #7) |
| **Điều 6.4** | Hồ sơ = đề cương **+ lý lịch khoa học** | ⚠️ BE có chỗ nộp; FE chưa có UI; chưa *bắt buộc* phải có file |
| **Điều 7.1** | CN đề tài phải **thạc sĩ trở lên** | ⚠️ `AcademicProfile.IsEligiblePi` có cột, **chưa enforce khi nộp** |
| **Điều 8.2** | HĐ **3–5 người**; **không** gồm thành viên nhóm đề tài | ✅ COI (rule #5) đã enforce; ✅ Min/Max 3–5 |
| **Điều 8.3.b** | Họp cần **≥2/3 thành viên** | ⚠️ có cột `QuorumNumerator/Denominator` nhưng **chưa code kiểm** |
| **Điều 8.3.c** | **Thư ký ghi biên bản → HĐ thông qua** | ✅ BE đúng (SaveMinutes → ApproveMinutes, rule #12) · 🔴 FE thiếu UI |
| **Điều 9** | Ký kết & triển khai | ✅ BE (`contract/sign` → project IN_PROGRESS) |
| **Điều 10** | Báo cáo tiến độ + điều chỉnh thời gian | ✅ BE (ProgressReport + ChangeRequest) |
| **Điều 11–12** | Nghiệm thu + HĐ nghiệm thu | ✅ BE (round ACCEPTANCE + AcceptanceEvaluation) |
| **Điều 13** | Lưu trữ kết quả & sản phẩm | ⚠️ có `Document`/`ProjectDeliverable`; lưu **đĩa local** (ephemeral khi deploy) |
| **Điều 14–17** | Kinh phí, dự toán, giải ngân, quyết toán | ✅ BE đầy đủ (rule #3/#6) |
| **Điều 18** | Kinh phí hoạt động hội đồng | ⚠️ có bảng `CouncilRemunerationRate`, **chưa seed + chưa dùng** |

## 4. Việc phát sinh từ QĐ 543 (chưa có trong backlog cũ)

| # | Việc | Ưu tiên | Ghi chú |
|---|---|---|---|
| 1 | **UI Biên bản (BM04/BM12)** | **P0** | đã là nút thắt số 1; nay có field chuẩn để làm |
| 2 | FE upload tài liệu (BM02 lý lịch KH) | P2 | Điều 6.4 bắt buộc — nên bắt PI đính kèm trước khi nộp |
| 3 | Seed **thù lao hội đồng** (Phụ lục 02) | P3 | 4 dòng số liệu, có sẵn bảng |
| 4 | Enforce **quorum 2/3** khi chốt biên bản | P3 | Điều 8.3.b |
| 5 | Enforce **CN phải ≥ thạc sĩ** | P3 | Điều 7.1, dùng `IsEligiblePi` |
| 6 | **BM08 Seminar** | P4 | chưa có bảng/API — cân nhắc ngoài phạm vi đồ án |
