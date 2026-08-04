# Rà soát hệ thống + Backlog (ghi lại để không quên)

> Cập nhật: tuần 11. Nguồn tham chiếu: **QĐ 543/QĐ-ĐHFPT** (Điều 9 hợp đồng, Điều 10 báo cáo tiến độ, Điều 16 giải ngân) · `docs/DB_ANALYTIC_REPORT.md` · `docs/QD543_Compliance.md` · `CLAUDE.md` (Business Rules #1–23) · `docs/HANDOFF_Week10.md`.
> Mục đích: chốt lại **cái đã đúng · cái thừa · cái chưa ổn · ý tưởng để sau** để không bị lãng quên giữa các phiên.

## 0. Trạng thái các mảng lớn (đã kiểm code, không phải phỏng đoán)

| Mảng | Trạng thái thực | Ghi chú |
|---|---|---|
| **Thông báo** | ✅ **Đủ stack cả 2 đầu** | BE `NotificationsController`: GET list + `/count` + `/{id}/read` + `/read-all`. Bắn ở **5 nơi**: mời hội đồng (`CouncilService`), sản phẩm ×2 (`DeliverableService`), mở vòng (`ReviewRoundService`), nhắc deadline (`DeadlineReminderScanner`). FE: `notification.service`/`useNotifications`/`notification.store`/`NotificationBell`(header)/`NotificationsPage`. **Không phải "chưa hoạt động".** ✅ **04/08: gộp chuông + EMAIL vào `INotifier`** — trước đó chỉ thư mời & nhắc hạn có mail; **kết quả xét duyệt gửi PI** và **sản phẩm đạt/không đạt** chỉ bắn chuông (PI không mở app thì không biết gì). Nay mọi thông báo đi kèm mail, vẫn chịu công tắc Admin `EMAIL_ENABLED`. Còn thiếu: **auto-poll/real-time** (chuông chưa tự làm mới) + thêm loại sự kiện (được phân công chấm…). |
| **Báo cáo tiến độ** | ✅ theo QĐ543 Điều 10 | Số kỳ cố định (Ứng dụng 2 / Cơ bản 1) · Staff `generate` mở kỳ · PI điền kỳ có sẵn · Staff đánh giá. |
| **Tài chính = minh chứng** | ✅ (không quản tiền, rule #15) | Mốc giải ngân + upload chứng từ + đánh dấu đã giải ngân. |
| **Hội đồng & chấm** | ✅ | 2 hội đồng · slot theo đề tài · trùng lịch · gate mời · biên bản Thư ký→Chủ tịch khóa. |

## 0b. 🔒 Cái CỐ Ý fix cứng — đừng "linh hoạt hoá" (chốt 01/08)
Không phải thứ gì cũng nên cấu hình được. Mấy mã sau **là logic, không phải nhãn**:

| Mã | Vì sao phải cứng |
|---|---|
| **`RoundType = ACCEPTANCE`** | `ReviewScoringService` dựa vào đúng chữ này để chuyển đề tài → **`COMPLETED`** khi nghiệm thu Đạt (và → `IN_PROGRESS` khi chưa đạt). Cho Admin tạo loại vòng mới tự do ⇒ hệ thống **không biết** chấm xong thì đề tài đi đâu ⇒ **đề tài kẹt, không bao giờ COMPLETED** (bug im lặng). |
| **`ProjectStatus` / `ProposalStatus`** | State machine trong `Process_Spec_v2`. |
| **`fundingMethod` WHOLE/PARTIAL** | Quyết định cách sinh đợt giải ngân. |

**Được phép linh hoạt (đã hỗ trợ):** **tên hiển thị** của loại vòng — sửa tại `src/i18n/locales/{vi,en}.ts → reviewBoard.type.*`, không đụng code, không ảnh hưởng logic.

> Nếu sau này thật sự cần **thêm loại vòng mới**: phải làm kiểu *"tên linh hoạt, hành vi có neo"* — bảng loại vòng có cột **hành vi khi Đạt** (duyệt-để-ký-HĐ / kết-thúc-đề-tài) để hệ thống biết xử sự. **Không** để free-form.

## 1. ✂️ Thừa / nợ kỹ thuật (nên dọn)
- **Dimension FINANCE + FINANCE round** — superseded rule #16 (chỉ 2 hội đồng). Đã ẩn FE, code còn → nên strip.
- **Tính tiền / % giải ngân / budget cap** — superseded #15. Bảng budget + `TotalBudget` còn tính; nav ẩn.
- **Endpoint chốt trực tiếp** `POST councils/{id}/decision` — đã khóa 409, luôn dùng luồng biên bản. Dead endpoint (giữ tra cứu).
- **Quorum** (`QuorumNumerator/Denominator`) — rule #12 (quyết định Chủ tịch, không đếm phiếu) → vô dụng.
- **SCREENING round type** — đã bỏ FE; kiểm còn sót BE/DB enum.

## 2. ⚠️ Chưa ổn / rủi ro (nên xử)
1. **🔴 IDOR các endpoint con của proposal** — `GET /proposals/{id}` **đã fix** (tuần 11), nhưng `GET .../documents`, `/budget`, `/team-members`, `/research-contents` phần lớn `[Authorize]` **trơn, chưa kiểm chủ sở hữu** → cùng lỗ. **Rà + fix đồng loạt.**
2. **🟠 Đa vai chưa role-aware toàn cục** — header đổi vai chỉ FE; BE cấp quyền theo **mọi role trong token**. Đã ép đúng cho **`/proposals/my`** + **`/contracts?mine=true`** (trang PI báo cáo tiến độ/sản phẩm/tổng kết chỉ thấy HĐ mình — tránh 403 "Only the PI may edit" khi acc đa vai). Còn lại vẫn cần chuẩn: gửi "vai đang chọn" xuống BE, hoặc tách tài khoản.
3. **✅ Giải ngân ↔ sản phẩm minh chứng (P5, 04/08)** — mọi đợt gắn được sản phẩm (`PUT /disbursements/{id}/deliverable`), `confirm` chặn 409 khi sản phẩm chưa `PASSED`, `EvaluateAsync` mở khoá theo **liên kết** thay vì `fundingMethod`. **Còn lại:** nối **báo cáo tiến độ** (khác sản phẩm) vào `ConditionMetAt` — vẫn cần thầy/leader chốt mapping đợt↔kỳ.
4. **🟠 Báo cáo tiến độ ↔ giải ngân (P6→P1)** — đánh giá "Đạt" chưa set `ConditionMetAt` để mở đợt giải ngân sau (QĐ543 Điều 10.3). Giờ đã có kỳ cố định → **khả thi**, nhưng **cần thầy/leader chốt mapping đợt↔kỳ**.
4. **🟠 Dữ liệu seed rác** — `sideARepresentative` / `econtractUrl` = **"12"** (không phải tên/URL). Gây "Hợp đồng điện tử: 12" + mốc timeline bấm không ra gì (đã guard linkify tuần 11, nhưng **nên seed dữ liệu thật**).
5. **🟡 Sản phẩm (Deliverables)** — ✅ tuần 11: Staff **thêm sản phẩm** (`POST /contracts/{id}/deliverables`) + **PI có trang "Sản phẩm" riêng** (`/deliverables`, nav Faculty) để **nộp file** (trước đây nút Nộp chỉ nằm trong màn Staff → PI không nộp được). **Còn thiếu để tự động hoá:** (a) **wizard chưa dùng** `POST /proposals/{id}/expected-products` (PI chưa khai sản phẩm dự kiến lúc nộp đề cương); (b) khi tạo hợp đồng **chưa tự gắn** `project_deliverable` (ContractId=null) vào hợp đồng. → Làm 2 cái này thì sản phẩm tự chảy từ đề cương, khỏi nhập tay.
6. **🟡 Rich text** — thuyết minh + biên bản đang textarea trơn → **xuất Word mất định dạng**. Chưa làm (thầy có nhắc).
6. **🟡 Reschedule / thay người sau khi gửi mời** — BE hỗ trợ, FE chưa có nút. **Decline-on-behalf** chưa có (mới có accept-on-behalf).
7. **🟡 AI phụ thuộc Gemini config** — thiếu key thì nút AI (trích xuất/tóm tắt/semantic) hỏng; nên fallback rõ. (AI để phase sau.)
8. **🟡 Thuật ngữ** — **vi đã chuẩn "đề cương"** (tuần 11); **en + docs còn lẫn** "proposal/đề xuất".
9. **🟡 Test** — 90 test cấp service; **chưa E2E UI** thường xuyên. Seed mỏng → demo dễ vấp.
10. **🟡 Báo cáo tiến độ khóa quá gắt** — nộp xong **KHÓA cứng** (UpdateAsync chỉ cho DRAFT), dù còn trong hạn `dueDate` & chưa ai đánh giá → PI lỡ sai không sửa được. **KHÔNG nhất quán** với sản phẩm (cho nộp lại đến khi ĐẠT) + báo cáo tổng kết (cho re-submit + request-revision). **Cần:** cho PI sửa/nộp lại kỳ báo cáo **khi chưa bị Staff đánh giá** (đánh giá xong mới khóa).
11. **🟡 Form nộp sản phẩm thiếu trường** — chỉ có `fileUrl` + `description`. Entity `ProjectDeliverable` **đã có** `TrialEvidenceUrl` (minh chứng thử nghiệm) + `ScientificRequirements` nhưng form không cho nhập → chưa đủ hồ sơ nghiệm thu (QĐ543 Điều 13.1). **Cần:** thêm ô minh chứng thử nghiệm khi nộp sản phẩm.

## 3. 💡 Ý tưởng để sau (backlog)
- **Lịch tổng trong app** — đã làm **agenda nhẹ** (gom buổi họp theo ngày, tuần 11). **Google Calendar + Meet thật** = OAuth + Google Cloud + token → **phức tạp, phase sau**.
- **Danh sách tổng "báo cáo cần duyệt"** cho Staff (thay vì vào từng hợp đồng mới thấy).
- **Thông báo**: auto-poll chuông + thêm loại sự kiện.
- **Cột PI** đã thêm ở màn Staff; cân nhắc thêm ở các list khác nếu cần.

## 4. ✅ Đã làm tuần 11 (commit trên `dev`)
> BE `083a70e` · FE `8a678dc` (+ FE timeline linkify guard sau đó).
- **Bảo mật:** fix IDOR `GET /proposals/{id}`; `/proposals/my` luôn của người gọi; dọn role thừa reviewer demo (chỉ ReviewCommittee).
- **Báo cáo tiến độ:** QĐ543 Điều 10 (số kỳ 2/1) — Staff generate, PI điền kỳ có sẵn; fix lỗi nộp (preventDefault).
- **Hội đồng:** chặn trùng người 1 vị trí · xác nhận thay (confirm-on-behalf) · xóa hội đồng · sao chép thành viên · thêm đề tài cả khi vòng OPEN.
- **Gia hạn deadline:** hiện hạn hiệu lực + nút tua nhanh tuần/tháng/năm.
- **Chấm điểm:** bỏ số 0 đầu ô điểm + validate rõ tiêu chí.
- **Wizard nộp đề cương:** stepper bấm nhảy bước; Step 2 nhãn "đính kèm tài liệu" (bỏ hiểu nhầm AI).
- **UI:** cột PI màn Staff · lịch agenda · chi tiết hợp đồng i18n hoá + nới rộng + tab lưới + guard linkify e-contract.
