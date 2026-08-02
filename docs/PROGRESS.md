# FURPMS Backend — Tiến độ theo nhóm chức năng

> Ảnh chụp % hoàn thiện **so với phạm vi đồ án** (không phải "phần mềm hoàn hảo"). Đây là **ước lượng có cơ sở** (37 controller, Phase A/B/C đã code, 53/53 test, luồng core đã E2E) — KHÔNG phải số đo tự động. Cập nhật: 2026-07-15.
>
> Contract (`API_CONTRACT.md` §3–§10) liệt kê "BE cung cấp gì" theo đúng 8 nhóm dưới đây; file này bổ sung cột **% + còn thiếu**.

| # | Nhóm chức năng | Controller chính | % | Còn thiếu / ghi chú |
|---|---|---|:--:|---|
| 1 | **Auth & Người dùng** | Auth, Users, AcademicProfiles | 95% | refresh-token / rate-limit / email-verify (không bắt buộc cho đồ án) |
| 2 | **Cấu hình / Master data** | Cycles+ResearchTypes+Tracks (**lĩnh vực toàn cục + gắn/gỡ theo đợt, rule #6, 22/07**), 6 lookup, **system_settings (Admin chỉnh giới hạn upload, 20/07)** | 98% | xóa đợt (chưa có endpoint DELETE /cycles/{id}) |
| 3 | **Đề xuất & nội dung** | Proposals, Budget, Contents, TeamMembers, Documents, Export, ChangeRequests, AI-summary | 92% | upload siết theo cấu hình Admin: mặc định ≤10MB + whitelist đuôi file (18/07, chuyển sang `system_settings` 20/07) |
| 4 | **Đặt hàng NC (Applied)** | ResearchOrders | 70% | multi-winner (đã chốt để sau — epic tương lai) |
| 5 | **Phản biện, Hội đồng & Chấm** | ReviewBoard, Rounds, Councils, Meetings, Scoring, Feedback, Acceptance | 92% | **tuần 10:** UI biên bản (roster/Q&A/ý kiến TV), lịch họp offline+địa điểm, cảnh báo trùng lịch. Backlog: slot theo đề tài, gate gửi mời, điểm danh |
| 6 | **Hợp đồng & sau HĐ** | Contracts, Disbursements, Deliverables, Amendments, ProgressReports, FinalReports, Settlements | 90% | **tuần 10:** tài chính=minh chứng (không quản tiền), timeline mốc hợp đồng, minh chứng giải ngân, tự sinh Word HĐ, gia hạn deadline log |
| 7 | **Thống kê / Thông báo / Dev-tools** | Analytics (+3 dashboard theo role 15/07), Notifications, Admin, Documents | 92% | — |
| 8 | **Hạ tầng nền** | Middleware, JWT, Email/SMTP, DeadlineReminder, Gemini, Seeder | 85% | Email/Gemini cần config key thật để chạy đầy đủ |

**Tổng thể ≈ 85–90%** cho phạm vi capstone. Lõi (đề xuất → xét duyệt → hợp đồng → giải ngân → nghiệm thu) chạy thông end-to-end.

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
