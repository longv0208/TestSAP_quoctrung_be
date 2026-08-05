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
| A1 | **Lên lịch họp: nền tảng chỉ còn `online` / `offline`** — bỏ Google Meet / MS Teams / Zoom | *(cả 2 bản)* [B1] · [B2-1] | ⬜ BE đang có 4 giá trị: `IN_PERSON`, `GOOGLE_MEET`, `TEAMS`, `ZOOM` (`CouncilMeetingService.ValidateAndNormalize`) | Gộp về 2 giá trị. Cân nhắc **giữ cột cũ**, chỉ map `GOOGLE_MEET/TEAMS/ZOOM → ONLINE` khi đọc, để không phải migration dữ liệu cũ. Bỏ luôn nút "Tạo link Google Meet" trên form nếu không còn phân biệt nền tảng |
| A2 | **Validate lịch chấm phải nằm TRONG khung giờ buổi họp** | *(cả 2 bản)* [B1] · [B2-2] | ⬜ | Đã có slot con theo đề tài (`PUT /councils/{id}/slots`) và `CouncilMeeting.ScheduledAt + DurationMinutes`. Cần chặn: `SlotStartAt ≥ ScheduledAt` và `SlotStartAt + SlotDuration ≤ ScheduledAt + DurationMinutes`. Đang **không** kiểm gì |
| A3 | **Thư ký cũng được chấm điểm** | [B1] | ❓ cần xác nhận | Rule #11 hiện tại: *"Reviewer = mọi thành viên hội đồng; chức danh chỉ là field"* ⇒ về nguyên tắc Thư ký chấm được. Phải kiểm màn reviewer có chặn theo `MemberRole == Secretary` không |
| A4 | **Màn Thư ký: hiện bao nhiêu người chấm, bao nhiêu Đạt / bao nhiêu Không đạt** | *(cả 2 bản)* [B1] · [B2-7] | 🔶 | 05/08 đã sửa: điểm từng thành viên nay hiện đúng tên (trước hiện `—: 58.0`), và thêm ô "Số liệu cuộc họp" tính tại chỗ. **Còn thiếu: đếm số phiếu Đạt / Không đạt** |
| A5 | **Trang ra quyết định cuối: thống kê chi tiết từng người chấm** — ai chấm, bao nhiêu điểm, Đạt hay Không đạt, tổng bao nhiêu người chấm Đạt / bao nhiêu Không đạt. Đối chiếu **form mẫu PDF trang 34** | [B2-15] | ⬜ | Hiện chỉ có 4 ô: thành viên · có mặt · phiếu hợp lệ · điểm TB |
| A6 | **Chốt biên bản = ĐÓNG, không cho sửa nữa**; và **trước khi chốt, toàn bộ phiếu chấm trước đó cũng phải đóng hết** | [B1] · [B2-BR] | 🔶 | `CouncilDecision.FinalizedAt` đã khoá biên bản (rule #12) và `AcceptanceEvaluationService` đã chặn sửa phiếu sau khi chốt. **Chưa có**: khoá phiếu *trước* khi chốt, và không cho sửa Đạt/Không đạt |
| A7 | **Sau khi chốt biên bản phải đổi trạng thái** — hiện vẫn để `active` | [B2-BR] | ⬜ | Kiểm `CouncilDecision`/`ReviewRound`/`Council.Status` sau `ApproveMinutesAsync` |
| A8 | **Không đủ số thành viên chấm thì KHÔNG được lưu biên bản** | [B2-BR] | ⬜ | Đối chiếu `MinMembersRequired` của hội đồng với số phiếu đã nộp |
| A9 | **Tổng số thành viên hội đồng phải là số LẺ và > 3** | [B2-8] | ⬜ | Hiện chỉ có `MinMembersRequired`/`MaxMembersAllowed`, **không kiểm tính chẵn lẻ**. ⚠️ Lưu ý: QĐ543 Điều 8.2 ghi *"hội đồng 3–5 người"* — "lẻ và >3" nghĩa là **đúng 5**. Cần chốt lại: 5 cố định, hay `{5, 7, 9…}` |
| A10 | **Điểm chấm là số nguyên hay thập phân?** | [B2-BR] | ❓ | Hiện `decimal`. Phải chốt và validate thống nhất cả BE lẫn ô nhập ở FE |
| A11 | **Ràng buộc tổng điểm khi chỉnh sửa bộ tiêu chí** | *(cả 2 bản)* [B1-22] · [B2-BR] | ✅ **XONG 06/08** | Tra nguyên văn: QĐ543 **BM03** có 5 mục 10+20+40+20+10, dòng cuối ghi **"Cộng 100"**. Dữ liệu thật đang có bộ **125 điểm**. Nay: `POST/PUT` tiêu chí vượt trần → **400**; **nộp phiếu chấm bằng bộ chưa cộng đủ → 409** (cổng chặn thật, vì bộ phải xây dần mới đủ 100); DTO trả `totalCriteriaScore`/`isTotalValid`, FE hiện "125/100 — chưa dùng chấm được" ngay ở danh sách. Kiểm bằng API: thêm 120 → 400 · 60 ok → thêm 50 → 400 · sửa 60→60 vẫn 200 (không tự tính trùng) · đủ 100 → `isTotalValid=true` |

---

## B. AI

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| B1 | **AI phải đọc FILE đề cương PI nộp**, không chỉ đọc các trường gõ trên web | *(cả 2 bản)* [B1] · [B2-4] | ⬜ **đúng như đã ghi 05/08** | `AiSummaryService.BuildPrompt` chỉ ghép từ trường cấu trúc, DTO trả `source: "textFields"`. Ống dẫn đọc file **đã có sẵn** (`GeminiFileInput`: PDF gửi inline, .docx bóc text OpenXml) — chỉ cần nối |
| B2 | **AI đọc file VÀ tên/nội dung đề tài để SO SÁNH hai bên** | [B2-4] | 🔶 | Đã có `/ai/proposals/{id}/consistency` (đối chiếu form ↔ file) từ 04/08 nhưng **tách rời** khỏi tóm tắt. Ý thầy: tóm tắt cũng phải dựa trên cả hai nguồn |
| B3 | **Tóm tắt AI TỰ CHẠY trước khi vào màn chấm** — bỏ nút "Tạo tóm tắt" | *(cả 2 bản)* [B1] · [B2-4] | ⬜ | Hiện phải bấm nút. Cần sinh sẵn (lúc PI nộp, hoặc lúc Staff mở vòng chấm) rồi cache vào `llm_outputs` — reviewer mở màn là có ngay, không chờ và không tốn quota lặp |
| B4 | **Prompt tóm tắt phải trả kết quả tốt hơn**, gồm đủ: **tên đề tài · tóm tắt thông tin · ưu điểm · nhược điểm** | [B2-5] | ⬜ | Prompt hiện tại yêu cầu 5–7 câu văn xuôi, **không tách ưu/nhược**. Phải đổi sang trả cấu trúc để FE render thành mục |
| B5 | **Gợi ý chấm điểm phải bám đúng bộ tiêu chí** | [B2-4] | 🔶 | Đã có `/ai/…/scoring-suggestion`. Cần kiểm: có truyền đúng bộ tiêu chí của **vòng đó** (3 tầng `IRubricResolver`) vào prompt không, và điểm gợi ý có nằm trong `MaxScore` từng tiêu chí không |
| B6 | **AI chạy local thay vì phụ thuộc server** | [B1] | ❓ **cần cân nhắc kỹ** | Nghĩa là chạy mô hình cục bộ (Ollama…) thay Gemini. Đổi được nhưng: máy demo phải đủ RAM/VRAM, chất lượng tiếng Việt của mô hình nhỏ **kém hơn Gemini rõ rệt**, và tốn thời gian dựng. Ưu điểm: không phụ thuộc mạng/quota lúc demo. **Đề xuất: giữ Gemini, thêm cache** (B3 đã giải quyết phần lớn nỗi lo "demo gãy vì mạng") — nhưng đây là ý thầy nên cần hỏi lại rõ động cơ |

---

## C. Hợp đồng · nghiệm thu · giải ngân

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| C1 | **Hệ thống tự tổng hợp file hợp đồng ĐẦY ĐỦ → xem lại → xuất ra → mới đem ký**, chứ không phải người ta tự tạo tự ký ngoài rồi nộp lên. Mẫu ở **PDF trang 22–26** | *(cả 2 bản)* [B1] · [B2-9] | 🔶 | Đã có `GET /contracts/{id}/export-word` nhưng bản Word **mới chỉ có 1 bảng tóm tắt + ô ký**, chưa có Điều 1–7. Đối chiếu BM05 còn thiếu: **mã số đề tài** · **Bên B: đơn vị công tác, điện thoại, địa chỉ** · **số tài khoản + ngân hàng** · **số CMND/CCCD + ngày cấp + nơi cấp** |
| C2 | **Xem lại chức năng "Phạm vi ký" (`scopeTitle`)** — trường này để làm gì, có đúng mẫu không; rà cả Entity `Contract`, các DTO (`ContractDto`, `CreateContractDto`) và API liên quan | *(cả 2 bản)* [B1] · [B2-10] | ⬜ | Hiện là ô chữ tự do, không map vào mục nào của BM05. Mẫu hợp đồng **không có** mục "phạm vi ký"; khái niệm gần nhất là Điều 1 *"Nội dung công việc"* + Điều 2 *"Sản phẩm của đề tài"*. **Nhiều khả năng nên bỏ hoặc đổi tên** |
| C3 | **Thiếu trường của BÊN B** (kéo theo từ C1): tài khoản ngân hàng, CCCD, đơn vị công tác, điện thoại, địa chỉ | [B2-9] | ⬜ | Cần **migration** + màn cho PI tự khai trong hồ sơ cá nhân (không nên để Staff gõ hộ thông tin định danh người khác) |
| C4 | **Hồ sơ nghiệm thu phải có: link sản phẩm + TOÀN BỘ thông tin sản phẩm và đề tài + file Word/PDF** | [B1] | 🔶 | 05/08 đã thêm `GET /councils/{cid}/proposals/{pid}/dossier` + panel hồ sơ nghiệm thu. Cần bổ sung: thông tin đề tài đầy đủ, và mở được file |
| C5 | **Hồ sơ nghiệm thu phải chi tiết TỪNG LẦN báo cáo tiến độ**: ai chấm · role gì · bao nhiêu điểm · xem lại được **tất cả file của các lần trước** | [B2-14] | ⬜ | Hiện dossier mới trả % + đánh giá của Staff, **không có** người chấm/role/điểm từng lần, cũng chưa gom file các kỳ |
| C6 | **Chưa có giải ngân đợt CUỐI trước khi chốt nghiệm thu (kết thúc hợp đồng)** | [B2-16] | ⬜ | BM05 Điều 4.2: *"Đợt 4: giải ngân kinh phí còn lại sau khi đề tài được công nhận kết quả Đạt"*. Phải nối: nghiệm thu Đạt → mở đợt cuối → giải ngân → mới cho đóng hợp đồng |
| C7 | **Từng đợt sản phẩm sau khi xong phải ĐÓNG lại** | [B1] | ✅ **XONG 06/08** | Sản phẩm nghiệm thu ĐẠT nay không nộp lại được (409). Xem D5 |
| C8 | **Page "Tiến trình đề tài" quá sơ sài** — hiện chỉ có `"abc06 Hợp đồng 06"`. Phải có **tên đề tài · ai là PI · mô tả đề tài** | [B2-17] | ⬜ | |
| C9 | **Tab "Báo cáo tổng kết" ở page hợp đồng: đổi tên trường "Duyệt (chuyên viên)" → "Nội dung kiểm tra"** | [B2-12] | ⬜ | Sửa nhãn i18n, việc nhỏ |

---

## D. Báo cáo tiến độ · tổng kết · gia hạn

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| D1 | **Nộp báo cáo: duyệt xong phải qua MỘT KHOẢNG THỜI GIAN mới cho nộp lần 2** — hiện có thể nộp 2 lần cùng lúc | [B1] | ⬜ **lỗi thật** | Cần chốt khoảng cách tối thiểu (theo kỳ báo cáo? theo số ngày?) rồi chặn ở `SubmitAsync` |
| D2 | **Sau bước báo cáo tiến độ phải có một bước DUYỆT (ví dụ Staff duyệt)** | [B2-11] | ✅ **đã có** | `POST /progress-reports/{id}/evaluate` — Staff chấm Đạt/Không đạt/Có điều kiện (rule #16: không cần hội đồng). Có thể ý thầy là **luồng chưa rõ trên UI** → cần làm nổi bước này lên |
| D3 | **Báo cáo tổng kết mới chỉ có link sản phẩm — phải thêm file Word/PDF** | [B1] | 🔶 | 31/07 đã đổi sang upload file thật (BM09). Cần kiểm lại màn có còn ô dán link đơn thuần không |
| D4 | **Gia hạn: phải ghi rõ được gia hạn LÚC NÀO và gia hạn TRONG BAO LÂU** | [B1] | 🔶 | Đã có: `OriginalEndDate` + nhãn *"Đã gia hạn — hạn gốc {ngày}"*; và 05/08 đã chặn trần theo **QĐ543 Điều 10.4** (≤ ½ thời gian thực hiện; đề tài 12 tháng ⇒ ≤ 6 tháng). **Còn thiếu**: log *thời điểm* duyệt gia hạn hiện ra cho người dùng thấy |
| D5 | 🔴 **Sản phẩm ĐÃ NGHIỆM THU rồi mà vẫn gia hạn thêm thời gian được** | [B1] | ✅ **XONG 06/08** | Hoá ra là **hai lỗi tách biệt**: ① `AmendmentService` không kiểm trạng thái đề tài (trong khi `ChangeRequestService`/BM07 đã kiểm đúng từ trước — hai cơ chế song song mà luật lệch nhau) → nay chặn ở **cả gửi lẫn duyệt**; ② `DeliverableService.SubmitAsync` không kiểm `AcceptanceStatus`, nộp lại sản phẩm đã ĐẠT sẽ **đặt lại về PENDING = xoá kết quả nghiệm thu** và khoá lại đợt giải ngân đã mở — FE ẩn nút nhưng gọi thẳng API là lọt. Kiểm bằng API: nộp lại sản phẩm đã ĐẠT → 409; sản phẩm chưa đạt → vẫn 200. Thêm 4 test (104/104) |
| D6 | **Ô nhập thông tin proposal phải tải lên được từ file Word** | [B2-19] | ✅ **đã có** | Đường B (upload + AI trích xuất → prefill form) — `/ai/extract`, nối lại 04/08. Nếu thầy vẫn nói thiếu ⇒ **UI chưa lộ rõ**, phải làm nổi nút upload ở bước 1 wizard |

---

## E. UI/UX · i18n · dữ liệu demo

| # | Việc | Nguồn | Trạng thái | Ghi chú thực thi |
|---|---|---|---|---|
| E1 | **Lĩnh vực: cho chọn NHIỀU lĩnh vực cùng lúc**, thay vì chọn 1 → OK → chọn tiếp | [B1] | ⬜ | Màn gắn lĩnh vực vào đợt. Đổi sang multi-select |
| E2 | **Sửa lại cách hiển thị năm học** — `2025-2026`, hoặc chỉ 1 năm dương `2026` | [B1] | ⬜ | Entity đang có `CycleYear` (int) + `SemesterCode` (string). Phải chốt định dạng rồi thống nhất mọi nơi hiển thị |
| E3 | **Giao diện tiếng Việt phải FULL tiếng Việt** — còn chỗ để nguyên `passed` | [B1] | ⬜ **lỗi thật** | Đó là các **status badge** đang render thẳng giá trị enum của BE. Phải map toàn bộ status qua i18n, không hiện chuỗi thô |
| E4 | **Mặc định để màu trắng (light mode)** | [B2-6] | ⬜ | Đang mặc định theo hệ thống hoặc dark. Đổi default sang light |
| E5 | **UI/UX phải sửa lại, cải thiện nhiều** | [B1] | ❓ chung chung | Cần hỏi lại bạn ghi note xem thầy chỉ cụ thể màn nào. Các điểm cụ thể đã tách thành E1–E4, C8, F5 |
| E6 | **Page "Tiêu chí chấm": gom nhóm bộ lọc** — filter năm, filter Ứng dụng/Cơ bản; cái nào cùng filter thì nhóm lại | [B2-18] | ⬜ | |
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

## 🔢 Thứ tự đề xuất

**Nhóm 1 — lỗi nghiệp vụ thật, sai kết quả** *(làm trước)*
1. ~~**A11** tổng điểm bộ tiêu chí > 100 vẫn tạo được~~ ✅
2. ~~**D5** sản phẩm đã nghiệm thu vẫn gia hạn được~~ ✅
3. **D1** nộp báo cáo 2 lần cùng lúc
4. **A6 + A7 + A8** khoá phiếu trước khi chốt biên bản · đổi status sau khi chốt · thiếu người thì không cho lưu
5. **A2** lịch chấm phải nằm trong khung giờ họp

**Nhóm 2 — thầy nhìn thấy ngay khi demo**
6. **E3** full tiếng Việt (status badge) · **E4** light mode mặc định
7. **A1** nền tảng họp chỉ online/offline
8. **A4 + A5** thống kê pass/fail từng người chấm (2 màn)
9. **C8** trang tiến trình đề tài · **C9** đổi nhãn · **E6** gom filter

**Nhóm 3 — AI** *(gộp làm một đợt, vì cùng đụng prompt + luồng)*
10. **B1 + B2 + B3 + B4 + B5** đọc file, tự chạy trước, prompt có ưu/nhược, gợi ý bám tiêu chí

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
