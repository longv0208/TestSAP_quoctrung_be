# FURPMS Frontend (FURPMS-Web) — Đánh giá & việc cần làm

> Đánh giá **FE MỚI** (`core/FURPMS-Web`, của Dũng) so với BE hiện tại + luồng nghiệp vụ chính. Cập nhật: 2026-07-20 (P0 Biên bản đã xong + verify sống).
> **Cơ sở đánh giá** (không đoán): smoke test sống 4 role × ~25 trang qua BE local (0 lỗi API), đối chiếu toàn bộ endpoint FE gọi vs 37 controller BE, đi tay luồng nộp proposal end-to-end (SUBMITTED thành công), đọc code wizard/services/routes.
> Bản đối chiếu API chuẩn: `API_CONTRACT.md`. Tiến độ BE: `PROGRESS.md`.

## Tiến độ theo nhóm chức năng

| # | Nhóm (khớp §API_CONTRACT) | Trạng thái FE | % | Ghi chú |
|---|---|---|:--:|---|
| 1 | Auth & Người dùng | ✅ login 4 role, profile, đổi mật khẩu, users CRUD (admin) | 90% | `ChangePasswordRequest` thiếu `confirmNewPassword` (BE §3) |
| 2 | Master data (cycles/tracks/types/lookup) | ✅ đầy đủ màn admin CRUD | 90% | — |
| 3 | Đề xuất (PI) | ✅ wizard 5 bước, my-proposals, withdraw, **sản phẩm dự kiến** (20/07) | 85% | thiếu upload **tài liệu đính kèm** (BE §6 đã có); change-requests chưa có |
| 4 | Đặt hàng NC | ✅ list/tạo/match | 80% | — |
| 5 | Phản biện & Hội đồng | ✅ rounds/councils/scoring/invitations/meetings + **BIÊN BẢN (Thư ký soạn → Chủ tịch khóa)** | 80% | P0 xong 20/07 (`MinutesPanel.tsx`); còn: đang dùng route legacy per-proposal, chưa dùng Review Board §8.1 |
| 6 | Hợp đồng & sau HĐ | ✅ list/tạo/ký (Dũng) + **giải ngân · sản phẩm · điều chỉnh · quyết toán** (20/07) | 90% | đủ 6 tab trong `ContractDetailSheet`; còn: gắn upload file thật (đang nhập link) |
| 7 | Báo cáo tiến độ / tổng kết | ✅ tiến độ (Dũng) + **báo cáo tổng kết: nộp → yêu cầu sửa → duyệt → lưu trữ** (20/07) | 85% | trang `/final-reports` độc lập vẫn là Coming Soon (đã có trong tab hợp đồng) |
| 8 | Thống kê / Thông báo | ✅ dashboard 3 role (BE thật 15/07), notifications | 90% | — |
| — | Cài đặt hệ thống (Admin) | ✅ theme/demo tools + **giới hạn upload** (`UploadLimitsCard`, 20/07) | 80% | mới lộ 2 key upload; các key khác thêm sau |
| 9 | AI | ⚠️ UI có, chạy mock | 30% | extract/search/similarity/suggest — chờ Gemini key |

**Tổng thể FE mới ≈ 82%.** Luồng chính **đã thông trọn vẹn**: nộp → chấm → biên bản → APPROVED → hợp đồng → ký → giải ngân → sản phẩm → báo cáo tiến độ → tổng kết → quyết toán. Việc còn lại là bù các mảnh phụ (upload file thật, sản phẩm dự kiến trong wizard, change-requests) chứ không còn mắt xích đứt.

> ⚠️ Đánh giá 18/07 chấm nhóm 6+7 là 0% — **sai vì chỉ nhìn nhánh `trung`**. Dũng đã làm màn Hợp đồng + Báo cáo tiến độ trên `mdung`/`dev`. Từ nay đánh giá phải soi nhánh `dev` (đã gộp), không soi nhánh riêng.

## Việc cần làm — theo LUỒNG CHÍNH (ưu tiên)

### P0 — Chốt kết quả xét duyệt bằng BIÊN BẢN (rule #12) — ✅ **XONG 20/07/2026**

Đã làm: tab **Minutes** trong `features/reviewer/proposal-review/MinutesPanel.tsx` (thay `DecisionView.tsx` đã xóa) + `decision.service.saveMinutes/approveMinutes` + `useSaveMinutesMutation/useApproveMinutesMutation`.
Đã verify sống (Playwright, BE :5068 + FE :5173) trên hội đồng `3353749b…`: Thư ký chọn kết quả + nhận xét → **Save draft** (hiện "Draft — not yet approved") → Chủ tịch mở đúng hội đồng đó thấy biên bản read-only + nút **Approve and lock** → bấm xong badge đổi `UNDER_REVIEW → APPROVED`, vòng `PENDING → PASSED`, biên bản **Locked**. Mắt xích tạo hợp đồng đã thông.

<details><summary>Bối cảnh gốc — mắt xích từng đứt ở đâu</summary>

**Bằng chứng cụ thể luồng đứt ở đâu** (đọc code BE, không suy đoán):
1. FE hiện chốt vòng bằng `CloseRoundDialog` → `POST /api/rounds/{id}/close` (đường legacy).
2. `ReviewRoundService.CloseRoundAsync` chỉ đổi status đề tài khi kết quả = **REJECTED** (→ `REJECTED`/`CANCELLED`). Với **APPROVED nó KHÔNG đụng proposal** → đề tài vẫn `SUBMITTED`, project vẫn `UNDER_REVIEW`.
3. Chỉ `ApproveMinutesAsync` (Chủ tịch duyệt biên bản) mới set `proposal → APPROVED` + `project → APPROVED`.
4. `ContractService.CreateContractAsync` chặn: `if (proposal.Status != APPROVED) throw` → *"Cannot create contract: proposal status is 'SUBMITTED', expected APPROVED."*

⇒ **Hệ quả:** Staff đóng vòng "Đạt" xong, đề tài **không bao giờ thành APPROVED** → **không tạo được hợp đồng** → khóa luôn cả giai đoạn ký HĐ → giải ngân → sản phẩm → báo cáo → nghiệm thu. Đây là **nút thắt số 1 của toàn hệ thống**.
- Thư ký: form soạn biên bản → `POST /api/review-scoring/councils/{id}/minutes` (nháp).
- Chủ tịch: xem nháp → duyệt/khóa → `POST .../minutes/approve` (BE tự sync proposal/project/round).
- Lưu ý: `POST .../decision` đã bị BE khóa (409) — đừng dùng; `rounds/close` chỉ là công cụ tay.
- Chi tiết luồng + bảng kết quả: `API_CONTRACT.md` §8.5.
</details>

> Còn nợ nhỏ: nút "Close round" cũ (`CloseRoundDialog`) bên màn Staff vẫn gọi `/rounds/{id}/close` — bypass rule #12 và không bao giờ set APPROVED. Nên ẩn/ghi rõ "công cụ tay" khi làm §8.1.

### P1 — Giai đoạn SAU DUYỆT (nhóm 6 + 7) — *đang làm dở*
Thứ tự nghiệp vụ: hợp đồng → ký → **giải ngân** → PI nộp sản phẩm → đánh giá → báo cáo tiến độ → final report → nghiệm thu → quyết toán. Endpoint BE đủ hết (§9).

| Bước | Trạng thái |
|---|---|
| Hợp đồng: list / tạo / ký | ✅ Dũng |
| Báo cáo tiến độ: tạo kỳ / lên lịch / đánh giá | ✅ Dũng |
| **Giải ngân: sinh lịch + xác nhận chi** | ✅ 21/07 (`DisbursementsPanel` + `ConfirmDisbursementDialog`), đã verify sống |
| Sản phẩm (deliverables): PI nộp → Staff đánh giá | ✅ 20/07 (`DeliverablesPanel` + 2 dialog) |
| Báo cáo tổng kết (final report) | ✅ 20/07 (`FinalReportPanel`), verify sống trọn vòng SUBMITTED → ACCEPTED → ARCHIVED |
| Điều chỉnh (amendments) | ✅ 20/07 (`AmendmentsPanel`), verify sống tạo → duyệt |
| Quyết toán (settlement) | ✅ 20/07 (`SettlementPanel`), verify sống lập → ký → kế toán → tài sản |

| Sản phẩm dự kiến (PI khai trong đề cương) | ✅ 20/07 (`ExpectedProductsCard` trong trang chi tiết đề xuất) |

**Đặt ở trang chi tiết đề xuất, không nhét vào wizard** — để tránh đụng wizard 5 bước của Dũng (chỉ chèn 1 dòng vào `ProposalDetailPage`). PI khai sản phẩm khi đề cương còn `DRAFT`; nộp xong thì khóa. Card tự cảnh báo nếu đề tài PARTIAL mà chưa khai sản phẩm nào.

### P2 — Hoàn thiện nhánh nộp đề tài
- Upload tài liệu đính kèm proposal (`/api/proposals/{id}/documents` — BE có thật, mock list của FE ghi nhầm là chưa).
  Khi làm form upload: gọi `useUploadPolicyQuery()` (`GET /api/system-settings/upload-policy`) để chặn sớm và hiện đúng mức giới hạn Admin đang đặt, thay vì hard-code 10 MB.
- Change requests (PI gửi yêu cầu thay đổi → admin duyệt, §6).

### P3 — Nâng cấp/khi có điều kiện
- Chuyển màn phân công sang **Review Board cấp track** (§8.1) — tránh lặp lại bài học UX của FE cũ.
- Nối AI thật khi có Gemini key: đổi `/ai/extract` → `POST /api/proposals/extract`; summary đã đúng route. `/ai/search|similarity|suggest-reviewers` + google-meet giữ mock.
- i18n song ngữ (đã chốt để sau).

## Bug nhỏ đã biết (sửa nhanh)
| Bug | Chỗ | Hậu quả |
|---|---|---|
| `Cycle.id`/`ResearchType.id` khai `number` nhưng API trả **string** | `types/*.ts` | set thẳng vào form → zod chặn im lặng (đã dính 1 lần ở nút sample-fill) |
| `ChangePasswordRequest` thiếu `confirmNewPassword` | `types/auth.ts` | BE validate → 400 |
| `INVITATION_STATUS` thiếu `ASSIGNED`/`EXPIRED` | `constants/statuses.ts` | member mới tạo (chưa mời) hiển thị sai |
| Mock list trong CLAUDE.md FE lỗi thời | `FURPMS-Web/CLAUDE.md` §Mock Strategy | Research Types / Proposal Documents / Notifications **BE đã có thật** — nên gỡ khỏi mock + trỏ nguồn contract về `FURPMS_BE/docs/API_CONTRACT.md` |

## Đã làm trong đợt tích hợp 15–16/07 (đã commit bên FE)
Wizard nộp proposal: validation đảo đúng chiều BE (titleVI + objectives), Funding Method → Select WHOLE/PARTIAL, copy 2 loại đề tài đúng rule #8/#9, Step 2 ghi rõ optional (dual intake), Step 3 chia section, Research Field scope theo đợt (hết track mồ côi), nút "Fill with sample data" (điền hết + nhảy Preview, đã verify nộp end-to-end), trang Settings + toggle, fix cờ `VITE_USE_MOCK_API` bị bỏ qua trong `main.tsx`.
