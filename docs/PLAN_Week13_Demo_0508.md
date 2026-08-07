# Góp ý demo với thầy — chiều 05/08/2026

> **Nguồn:** 2 bản note của 2 bạn cùng nhóm, ghi độc lập trong cùng buổi demo. Dưới đây là bản
> **GỘP, không nén** — mọi ý của cả hai bản đều được giữ nguyên văn ý nghĩa; chỗ nào hai bản nói
> cùng một việc thì gộp thành 1 dòng và ghi rõ *(cả 2 bản)*, kèm chi tiết riêng của từng bản.
>
> Ký hiệu nguồn: **[B1]** = bản 1 · **[B2]** = bản 2 (bản 2 đánh số sẵn).
> Trạng thái: ✅ đã có · 🔶 có một phần / cần sửa tiếp · ⬜ chưa có · ❓ cần thầy/nhóm chốt thêm.

---


## ✅ Đã trộn PR #1 của nhóm (06/08) — lấp 4 lỗ hổng có sẵn trong doc

PR `immanhdung/FURPMS-Web#1` "4 module ưu tiên" tách nhánh từ `c4b929f` (13 ngày trước) nên `dev`
đã đi rất xa. Trộn lại chỉ xung đột 2 file (`AppRouter.tsx`, `nav.ts`) — **cả hai bên chỉ THÊM
mục, không ai sửa của ai** nên giữ cả hai.

| Module PR mang vào | Lỗ hổng nó lấp |
|---|---|
| `AcademicProfileCard` + `/users/{id}/profile` | **Lý lịch khoa học (QĐ543 Điều 6.4)** — `PROGRESS.md` đang ghi *"BE có chỗ nộp; FE chưa có UI"* |
| `ChangeRequestsPanel` · `CreateChangeRequestSheet` · `PendingChangeRequestsPanel` | **BM07 "Phiếu đề nghị thay đổi"** (`ProposalChangeRequest`) — `PLAN_Week12` §C ghi *"BE có 4 endpoint, FE ❌ chưa có màn nào"* |
| `DocumentRepositoryPage` | Kho tài liệu toàn hệ thống |
| `ProposalExportMenu` | Xuất thuyết minh (Word) / dự toán (Excel) |

⚠️ PR viết khi **typecheck còn hỏng** (xem mục dưới) nên mang theo **9 lỗi kiểu**, đã sửa hết
trước khi trộn — đáng chú ý: `zod .default(0)` làm kiểu **input** lệch kiểu **output** khiến
`resolver` của `react-hook-form` không khớp; và `EmptyState` nhận `icon` là **component** nhưng PR
truyền **element đã render**.

Đã đối chiếu **6 endpoint** PR gọi với BE đang chạy: `/change-requests/pending`, `/documents`,
`/users/{id}/profile`, `/proposals/{id}/change-requests`, `/proposals/{id}/export/scientific`,
`/proposals/{id}/export/budget` — **tất cả trả 200**.

**Ảnh hưởng tới kế hoạch:** mục **C2/F2** (quan hệ giữa `ProposalChangeRequest` — đổi ĐỀ CƯƠNG,
theo BM07 — và `AmendmentRequest` — đổi HỢP ĐỒNG) nay **gấp hơn**, vì cả hai đều đã có UI. Phải
chốt phân vai kẻo người dùng thấy hai chỗ "xin thay đổi" mà không biết dùng cái nào.

## 0. Đọc nhanh — 6 nhóm lớn

| Nhóm | Số ý | Nặng nhất |
|---|:--:|---|
| A. Hội đồng · chấm điểm · biên bản | 11 | Chốt biên bản phải khoá toàn bộ phiếu; thống kê pass/fail từng người chấm |
| B. AI | 6 | AI phải **đọc file** + tự chạy tóm tắt trước khi vào màn chấm |
| C. Hợp đồng · nghiệm thu · giải ngân | 9 | Hệ thống tự sinh HĐ đủ mẫu (PDF tr.22–26) rồi mới đem ký |
| D. Báo cáo tiến độ · tổng kết · gia hạn | 6 | Nộp lại phải có khoảng cách thời gian; đóng đợt sau khi xong |
| E. UI/UX · i18n · dữ liệu demo | 8 | Full tiếng Việt; light mode mặc định; data demo chuẩn |
| F. Business rule · validate · tài liệu | 8 | **Validate lại TOÀN BỘ**; tổng điểm bộ tiêu chí = 100 |

**Tổng: 43 đầu việc** (sau khi gộp trùng giữa 2 bản; trước gộp là 24 + 19 + 5 mục business rule).

---

## A. Hội đồng · chấm điểm · biên bản

| # | Việc | Nguồn | Trạng thái hiện tại | Ghi chú thực thi |
|---|---|---|---|---|
| A1 | **Lên lịch họp: nền tảng chỉ còn `online` / `offline`** | *(cả 2 bản)* [B1] · [B2-1] | ✅ **XONG 06/08** | Chỉ còn `IN_PERSON` / `ONLINE` (hằng `MeetingPlatform`). **Không migration**: giá trị cũ `GOOGLE_MEET`/`TEAMS`/`ZOOM` được map về `ONLINE` cả khi ghi lẫn khi đọc — kiểm bằng API: 9 buổi họp cũ đọc ra 5 `IN_PERSON` + 4 `ONLINE`. FE bỏ dropdown 3 lựa chọn và **gỡ hẳn** nút "Tạo link Google Meet" (gắn với nền tảng cụ thể mà nay không còn phân biệt) |
| A2 | **Validate lịch chấm phải nằm TRONG khung giờ buổi họp** | *(cả 2 bản)* [B1] · [B2-2] | ✅ **XONG 06/08** | Chặn 4 ca: chưa có buổi họp · slot ngoài khung · slot tràn ra ngoài vì quá dài · **hai đề tài chồng giờ nhau** (hội đồng chỉ chấm được một đề tài một lúc). Kiểm bằng API với buổi họp 20/08 09:30–11:00: slot 07:00 → 400 · slot 10:30 dài 60′ (tràn 11:30) → 400 · slot 09:45 dài 30′ → 200 |
| A3 | **Thư ký cũng được chấm điểm** | [B1] | ❓ cần xác nhận | Rule #11 hiện tại: *"Reviewer = mọi thành viên hội đồng; chức danh chỉ là field"* ⇒ về nguyên tắc Thư ký chấm được. Phải kiểm màn reviewer có chặn theo `MemberRole == Secretary` không |
| A4 | **Màn Thư ký: hiện bao nhiêu người chấm, bao nhiêu Đạt / Không đạt** | *(cả 2 bản)* [B1] · [B2-7] | ✅ **XONG 06/08** | Xem A5 — cùng một endpoint |
| A5 | **Thống kê chi tiết từng người chấm** (form mẫu PDF tr.34) | [B2-15] | ✅ **XONG 06/08** | Tra ra đúng biểu mẫu: **BM12 mục 10.1** — *"Số phiếu phát ra … thu về … hợp lệ … không hợp lệ; Kết quả đánh giá: Đạt … Không đạt …"*. Thêm `GET /review-scoring/councils/{id}/ballot-tally`; màn biên bản nay hiện 4–6 ô số liệu + **bảng từng thành viên** (tên · vai · điểm/thang · Đạt/Không đạt · chưa nộp · phiếu không hợp lệ). Kiểm bằng API: phát ra 3 · thu về 2 · hợp lệ 2 · TB 58.5, liệt kê đúng 3 người. ⚠️ Còn thiếu mục **"Xuất sắc"** mà BM12 có |
| A6 | **Chốt biên bản = ĐÓNG**; trước khi chốt, toàn bộ phiếu cũng phải đóng | [B1] · [B2-BR] | ✅ **ĐÃ CÓ SẴN** (kiểm 06/08) | `ReviewScoringService.SubmitScoreAsync` dòng 91 đã chặn: `council.Status == DECIDED \|\| lockedDecision` → không sửa điểm. `AcceptanceEvaluationService` chặn tương tự cho phiếu Đạt/Không đạt. Không phải làm gì thêm |
| A7 | **Sau khi chốt biên bản phải đổi trạng thái** — hiện vẫn để `active` | [B2-BR] | ✅ **XONG 06/08** | Rà hết: đề tài → `COMPLETED`/`APPROVED`, đề cương → `APPROVED`, vòng → `PASSED`/`FAILED`, hội đồng → `DECIDED` — **đều đã đổi đúng**. Chỉ **buổi họp** kẹt ở `SCHEDULED` vĩnh viễn vì không ai bấm Bắt đầu/Kết thúc, nên lịch vẫn hiện như sắp họp dù đề tài đã có kết quả. Nay Chủ tịch chốt biên bản = tự đóng mọi buổi họp của hội đồng (`COMPLETED` + `ActualEndAt`) |
| A8 | **Không đủ số thành viên chấm thì KHÔNG được lưu biên bản** | [B2-BR] | ✅ **XONG 06/08** | Tra nguyên văn: **Điều 8.3.b** *"Tham dự của ít nhất **2/3** số thành viên dưới sự chủ trì của Chủ tịch; các thành viên tham dự họp **cần đánh giá thẩm định** đề cương (BM03)"* ⇒ số phiếu đã nộp chính là thước đo số người dự. **Điều 12.3.b** thêm *"và sự tham dự của **thành viên phản biện**"* cho hội đồng nghiệm thu. Nay chặn ở **cả lưu nháp lẫn chốt**; 2/3 làm tròn LÊN (5 người cần 4, không phải 3). Thêm 2 test |
| A9 | **Số thành viên hội đồng phải LẺ** | [B2-8] | ✅ **XONG 08/08** | Cỡ theo QĐ543: xét duyệt **3–5** (Điều 8.2), nghiệm thu **5–7** (Điều 12.2) — bỏ điều kiện ">3" thì hết vướng vì **3 cũng là số lẻ** ⇒ hợp lệ: xét duyệt **3 hoặc 5**, nghiệm thu **5 hoặc 7**. Chặn gửi thư mời khi số chẵn; FE cảnh báo **ngay lúc gán người** kèm lý do, không để tới lúc bấm mới ăn lỗi. |
| A10 | **Điểm chấm nguyên hay thập phân** | [B2-BR] | ✅ **XONG 08/08** | QĐ543 **không quy định**; BM03 để điểm tối đa toàn số nguyên. Thầy chốt: **Admin đặt từ đầu**. Thêm setting `SCORE_DECIMAL_PLACES` (mặc định **0 = số nguyên**); BE chặn ở `SubmitScoreAsync` kèm câu chỉ đường *"Phòng QLKH đổi được ở Cấu hình hệ thống"*; FE lấy `step` từ setting thay vì **hardcode `step="0.5"`** như trước (cẩm nang gọi đúng đây là hardcode tham số nghiệp vụ). **Không hồi tố** — phiếu đã chấm giữ nguyên, chỉ phiếu mới bị soi, cùng nguyên tắc rule #13. Không chia theo đợt/lĩnh vực/vòng vì đây là **định dạng nhập liệu**, không phải luật theo phạm vi. |
| A11 | **Ràng buộc tổng điểm khi chỉnh sửa bộ tiêu chí** | *(cả 2 bản)* [B1-22] · [B2-BR] | ✅ **XONG 06/08** | Tra nguyên văn: QĐ543 **BM03** có 5 mục 10+20+40+20+10, dòng cuối ghi **"Cộng 100"**. Dữ liệu thật đang có bộ **125 điểm**. Nay: `POST/PUT` tiêu chí vượt trần → **400**; **nộp phiếu chấm bằng bộ chưa cộng đủ → 409** (cổng chặn thật, vì bộ phải xây dần mới đủ 100); DTO trả `totalCriteriaScore`/`isTotalValid`, FE hiện "125/100 — chưa dùng chấm được" ngay ở danh sách. Kiểm bằng API: thêm 120 → 400 · 60 ok → thêm 50 → 400 · sửa 60→60 vẫn 200 (không tự tính trùng) · đủ 100 → `isTotalValid=true` |

---

## B. AI

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| B1 | **AI phải đọc FILE đề cương PI nộp** | *(cả 2 bản)* [B1] · [B2-4] | ✅ **XONG 06/08** | Nối `GetLatestProposalFileAsync` + `GeminiFileInput` vào tóm tắt. Kiểm bằng Gemini thật: `source` trả về `file+form`, `sourceFileName` = tên file .docx PI nộp |
| B2 | **AI đọc file VÀ form để SO SÁNH hai bên** | [B2-4] | ✅ **XONG 06/08** | Prompt v2 lấy file làm nguồn CHÍNH rồi đối chiếu với form; chỗ lệch đưa vào `weaknesses`. Kiểm thật: AI tự bắt được *"có sự không đồng nhất nghiêm trọng giữa thông tin biểu mẫu (phát hiện gian lận giao dịch dùng LSTM) và nội dung file đính kèm (hệ thống quản lý nghiên cứu)"* |
| B3 | **Tóm tắt AI TỰ CHẠY trước khi vào màn chấm** | *(cả 2 bản)* [B1] · [B2-4] | ✅ **XONG 06/08** | `AiSummaryCard` thêm cờ `autoGenerate`, bật ở **màn chấm điểm**: chưa có bản cache thì tự gọi 1 lần khi mở. Màn PI vẫn để tự bấm — không gọi Gemini mỗi lần PI mở đề cương của mình |
| B4 | **Prompt trả đủ tên đề tài · tóm tắt · ưu điểm · nhược điểm** | [B2-5] | ✅ **XONG 06/08** | Prompt v2 trả JSON `{title, summary, strengths[], weaknesses[]}`. Lưu vào `llm_outputs.Content` (gộp thêm `source`/`sourceFileName` để **khỏi migration**); `Map` đọc được cả bản v1 văn xuôi lẫn v2 JSON. FE render thành mục Ưu điểm / Nhược điểm |
| B5 | **Gợi ý chấm điểm phải bám đúng bộ tiêu chí** | [B2-4] | ✅ **XONG 06/08** | Kiểm ra phần resolve bộ theo vòng + clamp `[0, MaxScore]` + trả đủ tiêu chí **đã đúng sẵn**. Thiếu là ở chỗ **cũng chỉ đọc form** như B1 — chấm dựa trên mấy dòng gõ vội thì điểm không có căn cứ. Nay đọc file, và prompt nêu rõ: đủ N tiêu chí, tổng thang bao nhiêu, lý do phải nói đúng khía cạnh của tiêu chí (không nhận xét chung chung), thiếu thông tin thì cho điểm thấp và nói rõ thiếu gì. Kiểm thật: 7 nhận xét đều dẫn chứng từ file |
| B6 | **AI chạy local thay vì phụ thuộc server** | [B1] | ❓ **cần cân nhắc kỹ** | Nghĩa là chạy mô hình cục bộ (Ollama…) thay Gemini. Đổi được nhưng: máy demo phải đủ RAM/VRAM, chất lượng tiếng Việt của mô hình nhỏ **kém hơn Gemini rõ rệt**, và tốn thời gian dựng. Ưu điểm: không phụ thuộc mạng/quota lúc demo. **Đề xuất: giữ Gemini, thêm cache** (B3 đã giải quyết phần lớn nỗi lo "demo gãy vì mạng") — nhưng đây là ý thầy nên cần hỏi lại rõ động cơ |

---

## C. Hợp đồng · nghiệm thu · giải ngân

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| C1 | **Hệ thống tự tổng hợp file hợp đồng ĐẦY ĐỦ rồi mới đem ký** | *(cả 2 bản)* [B1] · [B2-9] | ✅ **XONG 08/08** — bản Word nay có đủ **căn cứ pháp lý + Bên A/Bên B + Điều 1–7 + ô ký**, dữ liệu thật điền vào đúng chỗ (tên đề tài, mã số, chủ nhiệm, đơn vị, thời gian, kinh phí, **bảng sản phẩm**, **bảng đợt giải ngân**). Nhãn gia hạn tự hiện khi . Kiểm bằng file xuất thật: 80 dòng, đúng thứ tự mẫu. **Chừa trống** số tài khoản + CCCD của Bên B vì chưa chốt C3. Cũ: Đã có `GET /contracts/{id}/export-word` nhưng bản Word **mới chỉ có 1 bảng tóm tắt + ô ký**, chưa có Điều 1–7. Đối chiếu BM05 còn thiếu: **mã số đề tài** · **Bên B: đơn vị công tác, điện thoại, địa chỉ** · **số tài khoản + ngân hàng** · **số CMND/CCCD + ngày cấp + nơi cấp** |
| C2 | **Xem lại chức năng "Phạm vi ký" (`scopeTitle`)** | *(cả 2 bản)* [B1] · [B2-10] | ✅ **XONG 08/08 — BỎ** | Rà hết mẫu BM05: **không có mục nào tên "phạm vi ký"**. Khái niệm gần nhất là **Điều 1 "Nội dung công việc"** (tên đề tài + mã số) và **Điều 2 "Sản phẩm của đề tài"** — cả hai hệ thống đã tự sinh. Ô này là do nhóm tự thêm hồi Review 2, không có căn cứ. Đã gỡ khỏi **form tạo hợp đồng** và **bản Word**; cột `ScopeTitle` giữ trong DB cho dữ liệu cũ (rule: strip chứ không xoá bảng). |
| C3 | **Trường của BÊN B**: tài khoản ngân hàng, CCCD | [B2-9] | ⬜ — **ĐÃ CHỐT HƯỚNG 08/08** | **Có thu thập**, và để **TUỲ CHỌN** (user chốt): PI chưa khai thì bản Word chừa trống như bản giấy, bổ sung sau cũng được — **không chặn** việc lập hợp đồng. Cách làm: migration thêm cột · card trong hồ sơ cá nhân cho **PI tự khai** (Staff không gõ hộ) · **che `****1234`** khi hiển thị · bản đầy đủ chỉ đổ vào file Word lúc xuất · ghi `audit_log`. Căn cứ trả lời khi bị hỏi: BM05 **Điều 7.2** — *"ủy quyền cho Trường ĐH FPT khai báo thông tin định danh để **cấp chứng thư số**"*. ≈ nửa buổi |
| C4 | **Hồ sơ nghiệm thu phải có: link sản phẩm + TOÀN BỘ thông tin sản phẩm và đề tài + file Word/PDF** | [B1] | 🔶 | 05/08 đã thêm `GET /councils/{cid}/proposals/{pid}/dossier` + panel hồ sơ nghiệm thu. Cần bổ sung: thông tin đề tài đầy đủ, và mở được file |
| C5 | **Hồ sơ nghiệm thu phải chi tiết TỪNG LẦN báo cáo tiến độ**: ai chấm · role gì · bao nhiêu điểm · xem lại được **tất cả file của các lần trước** | [B2-14] | ⬜ | Hiện dossier mới trả % + đánh giá của Staff, **không có** người chấm/role/điểm từng lần, cũng chưa gom file các kỳ |
| C6 | **Chưa có giải ngân đợt CUỐI trước khi chốt nghiệm thu (kết thúc hợp đồng)** | [B2-16] | ⬜ | BM05 Điều 4.2: *"Đợt 4: giải ngân kinh phí còn lại sau khi đề tài được công nhận kết quả Đạt"*. Phải nối: nghiệm thu Đạt → mở đợt cuối → giải ngân → mới cho đóng hợp đồng |
| C7 | **Từng đợt sản phẩm sau khi xong phải ĐÓNG lại** | [B1] | ✅ **XONG 06/08** | Sản phẩm nghiệm thu ĐẠT nay không nộp lại được (409). Xem D5 |
| C8 | **Page "Tiến trình đề tài" quá sơ sài** | [B2-17] | ✅ **XONG 06/08** | Mỗi dòng nay có: tên đề tài · số hợp đồng · **chủ nhiệm** · **lĩnh vực** · **thời gian thực hiện**. Lấy từ `useMyContractsQuery` (thêm map `proposalById`) nên không phát sinh request mới |
| C9 | **Đổi nhãn "Duyệt (Chuyên viên)" → "Nội dung kiểm tra"** | [B2-12] | ✅ **XONG 06/08** | `contract.finalReport.reviewStaff` ở cả vi lẫn en |

---

## D. Báo cáo tiến độ · tổng kết · gia hạn

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| D1 | **Nộp báo cáo tiến độ dồn nhiều kỳ cùng lúc** | [B1] | ✅ **XONG 06/08** | Đọc lại thì không phải chuyện số ngày: `SubmitAsync` chỉ kiểm `DRAFT` ⇒ PI nộp kỳ 1 xong **nộp dồn luôn kỳ 2, 3**. Chặn theo **KỲ** (QĐ543 Điều 10.1 — báo cáo *định kỳ*), không bịa số ngày: kỳ N chỉ nộp được khi kỳ N−1 **đã được Staff đánh giá** và đã tới `reportingPeriodStart` của kỳ N. Kiểm bằng API với 3 kỳ: nộp kỳ 3 khi kỳ 2 chưa duyệt → 409 *"Kỳ 2 đã nộp nhưng phòng QLKH chưa đánh giá"*; duyệt kỳ 1+2 xong nộp kỳ 3 → 200 |
| D2 | **Sau bước báo cáo tiến độ phải có một bước DUYỆT (ví dụ Staff duyệt)** | [B2-11] | ✅ **đã có** | `POST /progress-reports/{id}/evaluate` — Staff chấm Đạt/Không đạt/Có điều kiện (rule #16: không cần hội đồng). Có thể ý thầy là **luồng chưa rõ trên UI** → cần làm nổi bước này lên |
| D3 | **Báo cáo tổng kết mới chỉ có link sản phẩm — phải thêm file Word/PDF** | [B1] | 🔶 | 31/07 đã đổi sang upload file thật (BM09). Cần kiểm lại màn có còn ô dán link đơn thuần không |
| D4 | **Gia hạn: ghi rõ gia hạn LÚC NÀO và BAO LÂU** | [B1] | ✅ **XONG 06/08** | Đủ 3 phần: nhãn *"Đã gia hạn — hạn gốc {ngày}"* (05/08) · trần ≤ ½ thời gian thực hiện, QĐ543 Điều 10.4 (05/08) · **ngày duyệt** hiện ở danh sách đơn (06/08 — BE đã trả `reviewedAt` xuống list DTO) |
| D5 | 🔴 **Sản phẩm ĐÃ NGHIỆM THU rồi mà vẫn gia hạn thêm thời gian được** | [B1] | ✅ **XONG 06/08** | Hoá ra là **hai lỗi tách biệt**: ① `AmendmentService` không kiểm trạng thái đề tài (trong khi `ChangeRequestService`/BM07 đã kiểm đúng từ trước — hai cơ chế song song mà luật lệch nhau) → nay chặn ở **cả gửi lẫn duyệt**; ② `DeliverableService.SubmitAsync` không kiểm `AcceptanceStatus`, nộp lại sản phẩm đã ĐẠT sẽ **đặt lại về PENDING = xoá kết quả nghiệm thu** và khoá lại đợt giải ngân đã mở — FE ẩn nút nhưng gọi thẳng API là lọt. Kiểm bằng API: nộp lại sản phẩm đã ĐẠT → 409; sản phẩm chưa đạt → vẫn 200. Thêm 4 test (104/104) |
| D6 | **Ô nhập thông tin proposal phải tải lên được từ file Word** | [B2-19] | ✅ **đã có** | Đường B (upload + AI trích xuất → prefill form) — `/ai/extract`, nối lại 04/08. Nếu thầy vẫn nói thiếu ⇒ **UI chưa lộ rõ**, phải làm nổi nút upload ở bước 1 wizard |

---

## E. UI/UX · i18n · dữ liệu demo

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| E1 | **Lĩnh vực: cho chọn NHIỀU lĩnh vực cùng lúc**, thay vì chọn 1 → OK → chọn tiếp | [B1] | ⬜ | Màn gắn lĩnh vực vào đợt. Đổi sang multi-select |
| E2 | **Sửa lại cách hiển thị năm học** — `2025-2026`, hoặc chỉ 1 năm dương `2026` | [B1] | ⬜ | Entity đang có `CycleYear` (int) + `SemesterCode` (string). Phải chốt định dạng rồi thống nhất mọi nơi hiển thị |
| E3 | **Giao diện tiếng Việt phải FULL tiếng Việt** — còn chỗ để nguyên `passed` | [B1] | ✅ **XONG 06/08** | Đúng là `StatusBadge` render **thẳng** giá trị enum của BE (`{status}`), nên mọi nơi đều hiện `PASSED`, `IN_PROGRESS`, `PENDING_SIGNATURE`… Nay tra bảng `status.*` — **42 nhãn** phủ hết enum trong `DomainStatus.cs`; key thiếu thì vẫn hiện enum để lộ ra mà bổ sung, không hiện trống |
| E4 | **Mặc định để màu trắng (light mode)** | [B2-6] | ✅ **XONG 06/08** | `ui.store` mặc định `"system"` — máy chấm để dark thì cả hệ thống hiện tối. Đổi sang `"light"` |
| E5 | **UI/UX phải sửa lại, cải thiện nhiều** | [B1] | ❓ chung chung | Cần hỏi lại bạn ghi note xem thầy chỉ cụ thể màn nào. Các điểm cụ thể đã tách thành E1–E4, C8, F5 |
| E6 | **Page "Tiêu chí chấm": gom nhóm bộ lọc** | [B2-18] | ✅ **XONG 06/08** | Màn này vốn **không có bộ lọc nào**. Thêm 1 hàng: loại đề tài (Cơ bản/Ứng dụng) + loại vòng, kèm đếm "Hiện x/y bộ" |
| E7 | **Chuẩn bị DATA DEMO chuẩn thật**: sẵn 1 đợt có **nhiều đề tài**, **nhiều proposal đã nộp** | [B2-3] | ⬜ | Mở rộng `DatabaseSeeder` (vẫn phải idempotent) |
| E8 | **Sửa mật khẩu tất cả tài khoản thành `password`** | [B2-13] | ⬜ | Seeder đang dùng `Admin@123456`, `Faculty@123456`… ⚠️ Đổi thì phải sửa **CLAUDE.md + mọi doc + script test** đang chép mật khẩu cũ, kẻo doc lệch |

---

## F. Business rule · validate · tài liệu

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| F1 | 🔴 **VALIDATE LẠI TOÀN BỘ** — rà mọi ràng buộc | *(cả 2 bản)* [B1] · [B2] | ⬜ | Đây là ý **lặp lại ở cả hai bản và được nhấn mạnh**. Xem F2 để biết cách chia nhỏ |
| F2 | **Thiết kế lại chi tiết business rule**, ít nhất gồm 5 điểm bản 2 nêu: ① điểm chấm nguyên hay thập phân ② quy tắc lưu/chốt biên bản ③ không đủ thành viên thì không lưu được biên bản ④ mọi điều chỉnh phải validate thời gian ⑤ ràng buộc tổng điểm bộ tiêu chí | [B2-BR] | ⬜ | Đã tách thành A6–A11, D1, D5. Nên viết thành **một mục "Business rules" có số hiệu** trong `CLAUDE.md` như các rule #1–#23 hiện có |
| F3 | **Điều chỉnh phải validate thời gian** (ví dụ nêu trong note: đề tài 12 tháng ⇒ gia hạn ≤ 6 tháng) | [B2-BR] | ✅ **đã làm 05/08** | `ContractService.ValidateMaxExtension` — QĐ543 Điều 10.4 (≤ ½ thời gian thực hiện), chặn ở cả tạo lẫn sửa hợp đồng. Kiểm bằng API: nhập 9 → 400 |
| F4 | **Phụ lục** | [B1] | ❓ | Note chỉ ghi đúng 2 chữ. Đoán: **phụ lục hợp đồng** (BM05 có mục "1.5.2. Gia hạn (nếu có)") hoặc **phụ lục của báo cáo**. **Phải hỏi lại** |
| F5 | **Tạo rõ KỊCH BẢN để demo sản phẩm** | [B1] | ⬜ | Đi kèm E7 (data demo). Viết thành `docs/DEMO_SCRIPT.md`: đóng vai nào, bấm gì, ra kết quả gì |
| F6 | **Viết doc** | [B1] | 🔶 | Đã có bộ docs khá dày. Cần hỏi rõ thầy muốn loại doc nào (SRS? báo cáo đồ án? doc kỹ thuật?) |

---


### 📌 Phát hiện khi tra QĐ543 cho A11: **phiếu nghiệm thu KHÔNG chấm điểm**
- **BM03** (thẩm định đề cương) = bảng điểm 5 mục, **Cộng 100**.
- **BM11** (đánh giá nghiệm thu) = **không có thang điểm nào**, chỉ `☐ Đạt` / `☐ Không Đạt` + lý do.

Nghĩa là vòng **REVIEW** chấm điểm, vòng **ACCEPTANCE** chỉ Đạt/Không đạt — hệ thống đang làm
đúng (`AcceptanceEvaluation` là pass/fail). ⚠️ Nhưng dữ liệu hiện có một bộ tiêu chí tên
*"Phiếu đánh giá nghiệm thu (BM12)"* gắn 100 điểm — **sai tên và sai bản chất**, cần rà lại.

Kèm 2 ràng buộc QĐ543 chưa code (bổ sung cho A8):
- *"Tham dự của **ít nhất 2/3** số thành viên dưới sự chủ trì của Chủ tịch"*
- *"và sự tham dự của **thành viên phản biện**"*


## 🧩 Bốn câu treo — đã tra nguồn, chốt hướng (06/08)

### D1 — "nộp báo cáo phải qua một thời gian mới cho nộp lần 2"
Nguyên văn [B1]: *"Nộp báo cáo thì phải được duyệt xong phải qua 1 tg mới cho nộp lần 2, hiện tại
đang có thể nộp 2 lần cùng lúc."*

Đọc lại code thì **"nộp 2 lần cùng lúc"** không phải lỗi thời gian mà là: PI nộp kỳ 1 xong **nộp
luôn kỳ 2** dù kỳ 2 chưa tới hạn, vì `SubmitAsync` không kiểm gì về kỳ.

⇒ **Đề xuất (không cần thầy chốt con số ngày):** chặn theo **KỲ**, không theo số ngày —
*"kỳ N chỉ nộp được khi kỳ N-1 đã được Staff đánh giá, và đã tới `reportingPeriodStart` của kỳ N"*.
Bám đúng QĐ543 Điều 10 (báo cáo **định kỳ**) và tránh bịa ra con số ngày không có trong văn bản.

### B6 — "AI chạy local" = **DỰ PHÒNG khi mất mạng**, không phải đổi hẳn mô hình
Cách hiểu của user (xác nhận 06/08): thầy lo **demo gãy vì Gemini không vào được**, chứ không phải
lo chi phí (đang dùng free tier).

Hiện trạng: `GeminiService` gặp lỗi mạng/quota là **ném `InvalidOperationException` → 409**, cả
tính năng chết, không có đường lui.

⇒ **Đề xuất 3 lớp, rẻ hơn hẳn dựng mô hình local:**
1. **Sinh sẵn + cache** (đã có `llm_outputs`): tóm tắt tạo từ lúc PI nộp / Staff mở vòng chấm ⇒
   lúc demo chỉ đọc DB, **không gọi mạng**. Đây là lớp quan trọng nhất.
2. **Suy biến êm**: Gemini lỗi mà đã có bản cache thì trả bản cache kèm cờ `stale`, thay vì 409.
3. Chỉ khi vẫn thấy thiếu thì mới tính tới mô hình local (Ollama…) — nhưng cần máy đủ mạnh và
   chất lượng tiếng Việt của mô hình nhỏ kém hơn Gemini rõ rệt.

### C3 — thu thập số tài khoản / CCCD của Bên B: làm sao cho đúng
BM05 **có** các mục này nên không thu thập thì không xuất được hợp đồng. Nhưng cẩm nang Capstone
cảnh báo đúng nhóm lỗi này: *"thu thập thông tin quá mức cần thiết (CCCD khi không cần)"* và
*"nhập một trường định danh thì hệ thống hiển thị luôn thông tin cá nhân → có thể dò người khác"*.

⇒ **Cách làm chuẩn, gói gọn:**
- **Thu đúng lúc cần**: chỉ hỏi khi lập hợp đồng, KHÔNG hỏi lúc đăng ký tài khoản.
- **Chính chủ tự khai**: PI nhập trong hồ sơ cá nhân; Staff **không** gõ hộ.
- **Che khi hiển thị**: chỉ hiện 4 số cuối (`****1234`); bản đầy đủ chỉ đổ vào file Word lúc xuất.
- **Giới hạn quyền đọc**: chỉ PI (của chính mình) + Staff/Admin.
- **Ghi log truy cập** vào `audit_log` — cẩm nang khuyên có mục *Legal & Compliance* trong tài liệu.
- ⚠️ **Điều 7.2/7.3** nói hợp đồng ký qua **Econtract**, và *"ủy quyền cho Trường ĐH FPT khai báo
  thông tin định danh để cấp chứng thư số"* ⇒ CCCD là để **cấp chữ ký số**, có căn cứ rõ ràng.

### F4 — "phụ lục" là **PHỤ LỤC HỢP ĐỒNG**
Tra QĐ543: chữ "Phụ lục" xuất hiện đúng 1 lần và là *"Phụ lục 02"* — bảng thù lao hội đồng của
**chính quy định**, không phải của hợp đồng. Nhưng **Điều 6.1 của BM05** ghi:

> *"Trong quá trình thực hiện Hợp đồng, nếu một trong hai bên có yêu cầu **sửa đổi, bổ sung nội
> dung** … phải thông báo cho bên kia ít nhất 15 ngày…"*

Sửa đổi hợp đồng đã ký thì phải ký **phụ lục hợp đồng** — đó là mắt xích hệ thống đang thiếu:
- **BM07 / `AmendmentRequest`** = *đơn xin* thay đổi (đã có, đã duyệt được).
- **Phụ lục hợp đồng** = *văn bản kết quả* hai bên ký sau khi duyệt (**chưa có**).

⇒ **Đề xuất:** duyệt đơn gia hạn/điều chỉnh xong thì sinh **phụ lục .docx** (số phụ lục, hợp đồng
gốc số mấy, điều khoản nào đổi, giá trị cũ → mới, hiệu lực) — dùng lại đúng cơ chế xuất Word của
C1, rồi upload bản ký làm minh chứng như hợp đồng gốc. **Gộp vào C1 làm một đợt.**


## 🗂 BACKLOG HỢP NHẤT — note KHÔNG phải toàn bộ (06/08)

Note của nhóm chỉ là **1 trong 4 nguồn việc**. Gộp hết lại để không sót:

### Nguồn 1 — note demo 05/08 (file này): 43 việc, **xong 18**
### Nguồn 2 — `PLAN_Week12.md` §"DANH SÁCH VIỆC" (rà CRUD 05/08)
| Việc | Trạng thái |
|---|---|
| Hợp đồng SỬA + XOÁ | ✅ 05/08 |
| Lịch họp SỬA + XOÁ | ✅ 06/08 |
| **Rà `DisbursementsPanel` + `SettlementPanel`** xem có sai vai như 3 panel kia | ⬜ |
| **XOÁ**: sản phẩm · kỳ báo cáo · thành viên đề tài | ⬜ |
| **XOÁ master data** (5 màn Admin: đơn vị · loại sản phẩm · vai trò nhân sự · hạng mục chi · cấu hình tài chính) | ⬜ |
| **Chốt 4 loại điều chỉnh** (kinh phí/nội dung/thành viên/khác) + quan hệ với BM07 | ⬜ **gấp hơn** từ khi merge PR #1 — nay cả 2 cơ chế đều có UI |
| **4 việc UI lớn**: màn quản lý hội đồng CRUD · gom nhóm + số liệu ở màn Xét duyệt · **trang chi tiết đề tài** (gỡ bớt khỏi sheet hợp đồng đang ngột ngạt) · lọc danh sách hợp đồng theo đề tài đã qua vòng 1 | ⬜ |

### Nguồn 3 — `README.md` §"Code chưa làm / làm dở"
| Việc | Ghi chú |
|---|---|
| **Pin biểu mẫu chấm theo version** (rule #13: *"biểu mẫu pin version active tại thời điểm tạo đề tài"*) | ⬜ Cần migration. **Liên quan trực tiếp tới B6**: đổi bộ tiêu chí giữa chừng thì gợi ý chấm đã cache bị lệch |
| Dropdown loại đề tài ở form nộp còn cứng 2 lựa chọn | ⬜ chỉ là hiển thị |
| Thùng rác toàn hệ thống (mới làm mẫu ở ResearchType) | ⬜ để sau |
| i18n incremental các màn cũ | 🔶 |

### Nguồn 4 — `PLAN_Week12.md` §"Chờ user quyết định" (Q1–Q5)
| | Việc | Trạng thái |
|---|---|---|
| Q1 | Tìm kiếm ngữ nghĩa: bỏ / thay bằng tìm kiếm nâng cao / làm thật | ⏸ **khuyến nghị: bỏ, thay bằng tìm kiếm nâng cao** |
| Q2 | AI theo từng role | ✅ đã xử lý |
| Q3 | Chuẩn hoá validate ở FE (12 schema zod / 17 form) | ⬜ |
| Q4 | Google Calendar + Meet | ⏸ khuyến nghị để cuối — ⚠️ **A1 đã gộp nền tảng còn online/offline nên việc này càng ít giá trị** |
| Q5 | Deploy: ① storage ✅ Cloudinary · ② **không upload nhiều file một lần** (7 ô chọn file, 0 ô có `multiple`) · ③ Render ngủ sau 15′ | ⬜ ② và ③ |

**Tổng còn lại ≈ 40 đầu việc** trên cả 4 nguồn.

## 📅 Làm rõ: LỊCH HỌP vs LỊCH CHẤM (06/08)

| | Là gì | Có trong QĐ543? |
|---|---|---|
| **Lịch họp** (`CouncilMeeting`) | Buổi họp của hội đồng: ngày/giờ, thời lượng, địa điểm hoặc link | ✅ **BM04/BM12 mục 4–5**: *"Thời gian họp: … h, ngày … "* + *"Địa điểm"* |
| **Lịch chấm** (slot con — `CouncilProjectAssignment.SlotStartAt`) | Khung giờ **từng đề tài** bên trong buổi họp: 9:00 đề tài A, 9:30 đề tài B | ❌ **KHÔNG có**. Nguồn duy nhất là **rule #17** (thầy Đức tuần 10) |

**Quan hệ:** 1 lịch họp → N lịch chấm, mỗi lịch chấm 1 đề tài. Chỉ có nghĩa khi hội đồng chấm
**≥2 đề tài** trong một buổi; hội đồng 1 đề tài thì slot trùng luôn buổi họp ⇒ thừa.

**Gate gửi thư mời** chỉ kiểm 3 thứ: có Chủ tịch · có Thư ký · **có lịch họp**. Slot KHÔNG nằm
trong gate ⇒ không tạo slot vẫn gửi mời được (đúng như user quan sát).

### ⚠️ Vấn đề thứ tự do user phát hiện — có thật
Hội đồng được tạo **kèm sẵn đề tài** (`CreateCouncilAsync` bắt buộc `proposalId`; đường
"package" ở review-board nhận `ProjectIds[]` nhiều đề tài). Nhưng:

- Staff đặt **lịch họp** (giờ + **thời lượng**) rồi **mới** gán thêm đề tài 2, 3 vào hội đồng đó.
- **Không có gì kiểm** buổi họp có đủ dài cho số đề tài hay không, cũng không nhắc chia lại slot
  khi thêm đề tài. Buổi họp 90 phút gán 5 đề tài vẫn lưu được.

**Đề xuất:**
1. Gán thêm đề tài vào hội đồng đã có lịch họp ⇒ **cảnh báo** nếu tổng slot đã đặt + đề tài mới
   vượt thời lượng buổi họp (cảnh báo, không chặn — rule #17 cho đổi lịch bất kỳ lúc nào).
2. **Ẩn phần lịch chấm khi hội đồng chỉ có 1 đề tài** — bớt rối cho ca thường gặp nhất.


## 🎬 E7 — KỊCH BẢN DATA DEMO (thiết kế 06/08, làm sau)

**Vấn đề:** data hiện tại là `abc`, `abc4`, `abc5`, `t01`, `t06 tc` — thầy nhìn vào là biết chưa
chuẩn bị. Và **mọi đề tài đang ở cùng một chỗ**, nên demo màn nào cũng phải bấm từ đầu.

**Nguyên tắc:** mỗi màn/mỗi bước của quy trình phải có **sẵn ít nhất 1 đề tài đứng đúng ở đó**,
để mở màn nào là demo được ngay màn đó — không phải chạy lại cả vòng đời.

### 2 đợt (rule #7: 1 đợt = 1 loại đề tài)
| Đợt | Loại | Trạng thái |
|---|---|---|
| `2026-UD` | Ứng dụng | Đang mở nhận đề cương (còn hạn) |
| `2026-CB` | Cơ bản | Đã đóng nhận, đang xét duyệt |

### 8 đề tài — mỗi cái đứng ở một bước khác nhau
| # | Tên (thật, không phải abc) | Trạng thái | Demo được màn nào |
|---|---|---|---|
| 1 | *Ứng dụng học sâu phát hiện đạo văn trong bài báo khoa học* | `DRAFT` | PI đang soạn dở → demo **wizard nộp đề cương** + **upload file + AI trích xuất** |
| 2 | *Hệ thống giám sát môi trường khuôn viên bằng IoT* | `SUBMITTED`, **chưa vào vòng nào** | Staff **tạo vòng chấm + gán đề tài** |
| 3 | *Mô hình dự báo tỷ lệ bỏ học bằng học máy tổ hợp* | `SUBMITTED`, **đã vào vòng 1**, hội đồng đã lập + đã mời + **các TV đã xác nhận**, đã có lịch họp | ⭐ **Màn chính**: reviewer **chấm điểm** · Thư ký **soạn biên bản** · Chủ tịch **chốt** |
| 4 | *Nền tảng học liệu cá nhân hoá* | `REVISION_REQUIRED` sau vòng 1 | PI **sửa & nộp lại**, vòng **mở lại** (rule #1) |
| 5 | *Phân tích cảm xúc phản hồi sinh viên* | `APPROVED`, **chưa có hợp đồng** | Staff **lập hợp đồng** + **xuất Word** |
| 6 | *Tối ưu lịch thi bằng thuật toán di truyền* | Có **hợp đồng ACTIVE**, **kỳ 1 đã nộp chưa duyệt** | Staff **duyệt báo cáo tiến độ** · PI **xin gia hạn** |
| 7 | *Nhận dạng chữ viết tay tiếng Việt* | Hợp đồng ACTIVE, **sản phẩm đã nộp**, **vòng NGHIỆM THU đang mở** | Hội đồng **nghiệm thu** (BM11 Đạt/Không đạt) + **hồ sơ nghiệm thu** |
| 8 | *Hệ thống khuyến nghị môn học tự chọn* | `COMPLETED` (nghiệm thu Đạt) | **Vòng đời trọn vẹn**: timeline, giải ngân đủ 4 đợt, quyết toán |

### Kèm theo
- **Bộ tiêu chí HỢP LỆ**: 1 bộ Xét duyệt đúng **BM03** (10+20+40+20+10 = **100**), 1 bộ Nghiệm thu.
  ⚠️ **Xoá bộ 125 điểm** đang có — nay đã bị chặn không chấm được.
- **Hội đồng đúng cỡ QĐ543**: xét duyệt **5 người** (Điều 8.2), nghiệm thu **5–7** (Điều 12.2),
  có đủ Chủ tịch/Thư ký/Phản biện.
- **Đề tài #3 và #7 nên gán CHUNG một hội đồng nhiều đề tài** → mới demo được **lịch chấm (slot con)**,
  thứ hiện không thấy tác dụng vì hội đồng nào cũng chỉ 1 đề tài.
- Mật khẩu tất cả tài khoản = `password` (E8). ⚠️ Hiện **không đồng nhất**: `reviewer3.demo` dùng
  `Reviewer@123456` còn `pi.demo` dùng khác — sửa thì phải sửa cả doc đang chép mật khẩu cũ.

### Cách làm
Mở rộng `DatabaseSeeder.SeedAsync()` — **vẫn phải idempotent** (kiểm tồn tại trước khi insert).
Tách thành `SeedDemoScenarioAsync()` gọi sau seeder gốc, bật/tắt bằng `SystemSetting` để bản deploy
thật không dính data giả.


### 📌 Chốt thêm 08/08 sau khi tra QĐ543

**E2 — năm học:** QĐ543 **không có khái niệm "năm học"**. Văn bản dùng **năm dương lịch**:
*"**Quý I hằng năm**, Phòng QLKH tiếp nhận hồ sơ… **Quý II hằng năm**…"*, và mọi biểu mẫu ghi
**"NĂM 20…"** (một năm). ⇒ Dùng **`2026`**, bỏ `2025-2026`.

**A10 — điểm chấm:** QĐ543 **không quy định** nguyên hay thập phân. BM03 để điểm tối đa toàn số
nguyên (10/20/40/20/10). ⇒ Đề xuất: **điểm từng tiêu chí = số nguyên** (khớp thang biểu mẫu),
**điểm trung bình = thập phân** (phép chia tự nhiên). **Không** cho user tự cấu hình — phải thống
nhất trong một hội đồng, cho chỉnh thì mỗi người chấm một kiểu.

**A9 — ✅ ĐÃ CHỐT 08/08 (user xác nhận với thầy): ÉP SỐ LẺ.**
Mâu thuẫn mình nêu trước đó **đã được giải**: hệ thống **vẫn đếm phiếu**, Chủ tịch **vẫn chốt** —
hai thứ **bổ sung** nhau chứ không loại trừ. Cơ chế thật:
1. **Mọi thành viên đều chấm** (kể cả Thư ký — Điều 8.3.b, đã sửa ở A3).
2. **Thư ký** dựa vào **chênh lệch phiếu Đạt/Không đạt** để soạn kết luận.
3. **Chủ tịch** xem lại, thấy hợp lý mới ký thông qua — *"quyết định cũng phải có căn cứ"*.
4. Người ngoài soi vào **đối chiếu được** kết luận với số phiếu.

Số chẵn ⇒ có thể hoà ⇒ **không có căn cứ nào để viết kết luận**. Rule #12 vẫn đúng: hệ thống
**không tự chốt** kết quả, chỉ cung cấp căn cứ.

**Form tạo hợp đồng vs BM05:** rà xong — **không thiếu gì**. Form hỏi số HĐ · thời gian · gia hạn
tối đa · đại diện Bên A; mọi mục khác của BM05 đều **tự lấy** (tên đề tài, chủ nhiệm, đơn vị,
điện thoại, email, tổng kinh phí, bảng sản phẩm, bảng giải ngân). Chỉ thiếu đúng **nhóm C3**.


### ⚠️ E7 — mục đích thật, và cái bẫy lớn nhất (làm rõ 08/08)

**Mục đích KHÔNG phải "màn nào cũng có dữ liệu"** (cách diễn đạt sai ở bản trước). Mục đích là:
**mỗi bước của quy trình có một đề tài đứng NGAY TRƯỚC bước đó**, để lúc demo **bấm thật được
ngay**, không mất 20 phút dựng tiền đề trước mỗi thao tác. Thầy muốn xem *bấm và nó chạy*, không
phải xem màn hình có chữ.

⇒ Đề tài #3 phải ở trạng thái *hội đồng đã lập + đã mời + mọi người đã xác nhận + đã có lịch họp*
để **chấm điểm / soạn biên bản / chốt** đều diễn live. Đề tài #8 `COMPLETED` mới là "cảnh nền" —
để trả lời *"xong rồi thì nhìn thế nào"* mà không phải diễn lại cả vòng đời.

**🔴 Cái bẫy: data seed phải QUA ĐƯỢC chính các chốt chặn thêm ngày 06–08/08**, nếu không demo
gãy ngay trên sân khấu:

| Chốt chặn | Seed phải đảm bảo |
|---|---|
| Hội đồng **số lẻ** + 3–5 (xét duyệt) / 5–7 (nghiệm thu) | Đúng 5 người, không phải 4 |
| Quorum **2/3** mới lưu/chốt biên bản | ≥ 4/5 phiếu đã nộp |
| Nghiệm thu phải có **phản biện dự** | Có vai `Opponent` **và** người đó đã chấm |
| Bộ tiêu chí **cộng đúng `MaxTotalScore`** | Dùng bộ 100 điểm, **không** dùng bộ 125 điểm cũ |
| Kỳ báo cáo **tuần tự** (kỳ N-1 phải được đánh giá) | Kỳ 1 seed sẵn trạng thái đã đánh giá |
| Slot chấm phải **trong khung giờ họp**, không chồng nhau | Nếu seed slot thì phải khớp buổi họp |
| Sản phẩm đã **PASSED** thì không nộp lại được | Đề tài #7 để `PENDING` nếu muốn demo nộp |

Đây mới là phần khó, không phải chuyện gõ tên đề tài.

**Cách dựng:** `SeedDemoScenarioAsync()` chạy sau seeder gốc, **idempotent**, bật/tắt bằng setting
để bản deploy thật không dính data giả. Đặt thẳng trạng thái **nhưng dựng kèm đủ bản ghi phụ trợ**
(`CouncilDecision`, phiếu chấm, đợt giải ngân…) — nếu không màn đó hiện trống lúc demo.

**File đính kèm:** dùng lại `DocumentExportService` sinh Word thuyết minh **khớp nội dung từng đề
tài**, lưu qua `IFileStorage`, tạo `Document` row ⇒ demo **AI đọc file → tóm tắt → đối chiếu form**
chạy thật, không phải upload tay.

**User cho xoá sạch DB (08/08)** ⇒ seeder dựng lại từ đầu, không phải né dữ liệu cũ (`abc`, `abc4`,
`t01`, bộ tiêu chí 125 điểm).

**F5 viết SAU E7** — `DEMO_SCRIPT.md` phải trỏ vào dữ liệu thật (tên đề tài, tài khoản, mật khẩu);
viết trước thì toàn placeholder.

## 🔢 Thứ tự đề xuất

**Nhóm 1 — lỗi nghiệp vụ thật, sai kết quả** *(làm trước)*
1. ~~**A11** tổng điểm bộ tiêu chí > 100 vẫn tạo được~~ ✅
2. ~~**D5** sản phẩm đã nghiệm thu vẫn gia hạn được~~ ✅
3. **D1** nộp báo cáo 2 lần cùng lúc
4. ~~**A6 + A7 + A8**~~ ✅ (A6 vốn đã có sẵn)
5. ~~**A2** lịch chấm phải nằm trong khung giờ họp~~ ✅

**Nhóm 2 — thầy nhìn thấy ngay khi demo**
6. ~~**E3** full tiếng Việt (status badge) · **E4** light mode mặc định~~ ✅
7. ~~**A1** nền tảng họp chỉ online/offline~~ ✅
8. ~~**A4 + A5** thống kê pass/fail từng người chấm~~ ✅
9. ~~**C8** trang tiến trình đề tài · **C9** đổi nhãn~~ ✅ · ~~**E6** gom filter~~ ✅

**Nhóm 3 — AI** *(gộp làm một đợt, vì cùng đụng prompt + luồng)*
10. ~~**B1 + B2 + B3 + B4**~~ ✅ · ~~**B5**~~ ✅ — **cả nhóm AI xong, trừ B6 chờ thầy**

**Nhóm 4 — hợp đồng & nghiệm thu** *(nặng nhất, cần migration)*
11. **C1 + C2 + C3** dựng lại bản Word đủ mẫu BM05 + nhóm trường Bên B
12. **C5 + C4** hồ sơ nghiệm thu chi tiết từng kỳ
13. **C6** giải ngân đợt cuối

**Nhóm 5 — chuẩn bị bảo vệ**
14. **E7 + E8** data demo + mật khẩu · **F5** kịch bản demo · **F2** viết lại business rule · **F6** doc

---

## ⏸ Phải hỏi lại trước khi làm

| Mục | Câu hỏi |
|---|---|
| **A9** | "Lẻ và > 3" là **đúng 5 người**, hay `{5, 7, 9…}`? Vì QĐ543 Điều 8.2 ghi hội đồng **3–5 người** — nếu bắt lẻ và >3 thì chỉ còn đúng 5 |
| **A10** | Điểm chấm: số nguyên hay cho thập phân? Nếu thập phân thì mấy chữ số? |
| **B6** | "AI chạy local" — thầy muốn giải quyết nỗi lo gì? Không phụ thuộc mạng lúc demo, hay vấn đề chi phí/bảo mật dữ liệu? Cách xử lý khác nhau hẳn |
| **D1** | "Qua một thời gian mới cho nộp lần 2" — bao lâu? Hay ý là phải **sang kỳ báo cáo kế tiếp**? |
| **F4** | "Phụ lục" là phụ lục **hợp đồng** hay phụ lục **báo cáo**? |
| **E5** | "UI/UX sửa nhiều" — thầy chỉ cụ thể màn nào? |
| **F6** | "Viết doc" — loại doc nào? |
| **C2** | Bỏ hẳn "Phạm vi ký", hay đổi tên thành "Nội dung công việc" cho khớp Điều 1 của BM05? |

---

## 📎 Đối chiếu tài liệu gốc

- **File hợp đồng mẫu (BM05): PDF trang 22–26** — cần cho C1, C2, C3.
- **Form mẫu biên bản/kết quả: PDF trang 34** — cần cho A5.
- QĐ543 **Điều 8.2** (hội đồng 3–5 người) — liên quan A9.
- QĐ543 **Điều 10.4** (gia hạn ≤ ½ thời gian thực hiện) — F3, đã làm.
- QĐ543 **Điều 13.1** (minh chứng thử nghiệm) — liên quan C4.

---

*Ghi 06/08/2026, gộp từ 2 bản note của nhóm sau buổi demo chiều 05/08/2026.*
