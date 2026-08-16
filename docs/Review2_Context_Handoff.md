# FURPMS — Context bàn giao (phiên Claude chat: Diagram + Report)

> Dùng để **dán vào một phiên Claude chat riêng** hỗ trợ sửa UML + viết Report 4 (Design) / Report 5 (Testing).
> Phần code bám repo chạy ở Claude Code; phiên chat tập trung thiết kế & tài liệu (không cần repo).
> Cập nhật: tuần 8 (chuẩn bị Review 2 theo checklist Khoa). Bộ diagram đầy đủ ở `Review2_Diagrams.md` (Context/Architecture/State Machine/ERD/Use Case/**Activity**); danh sách công nghệ ở `Review2_Tech_Stack.md`. Trạng thái checklist Khoa xem §10.

## 1. Dự án
- FURPMS = Hệ thống quản lý đề tài NCKH cấp trường (FPTU). Capstone SU26SE053, theo QĐ 543/QĐ-ĐHFPT.
- Kiến trúc code: **N-tier .NET 8** (Controller → Service → Repository → DbContext), **React/TypeScript** (web), **PostgreSQL 16** (đổi khỏi SQL Server 14/08). Có **Mobile app** (chỉ PI/Staff).
- Actor: **Admin** (Trưởng phòng QLKH), **Staff** (CB phòng), **PI** (Giảng viên chủ nhiệm), **Reviewer** (thành viên hội đồng). TUYỆT ĐỐI không dùng "System User".

## 2. Mô hình thực thể CHỐT (thầy, tuần 7) — quan trọng nhất
- **Project (Đề tài) = thực thể GỐC.**
- 1 PI → nhiều Project; **1 Project chỉ 1 PI** (không đồng chủ nhiệm).
- 1 Project gồm: **1 Proposal (đề cương)**, **nhiều Progress Report (đợt 1, 2, 3…)**, **1 Final Report (nghiệm thu)**.
- 1 Project đi qua **nhiều Hội đồng khác nhau**: HĐ duyệt đề cương → HĐ tiến độ → HĐ nghiệm thu.
- Proposal được **Approve + ký hợp đồng = KẾT THÚC vòng đời Proposal** → Project chuyển sang trạng thái "đang thực hiện".

> ⚠️ Lưu ý khớp code: hiện **chưa có bảng `Project` riêng**. Code lấy **`Proposal` làm gốc**; các artifact sau duyệt (ProgressReport, FinalReport, giải ngân) **neo vào `Contract`** (Contract 1–1 Proposal). Quan hệ thầy mô tả đã có, chỉ neo khác chỗ. Chi tiết + khuyến nghị xem `Review2_Diagrams.md` §"Đánh giá DB/Code".

## 3. Góp ý UML của thầy (tuần 7) — phải sửa trước khi nộp Report 4 & 5
### System Architecture
- Phân biệt mũi tên: **Request = nét liền**, **Response = nét đứt**. Phải đủ cả 2 chiều.
### State Machine
- Vẽ RIÊNG state cho **Proposal**, **Final Report**, và **Project (tổng)** — không gộp.
- Proposal: Draft → Submitted → (Under Review) → Approved / Rejected / Revision. Approved + ký HĐ → đóng vòng đời Proposal.
- **BỎ NGAY state của "Hội đồng"** (đang vẽ "ráp hội đồng", "triệu tập đủ người"…). Hội đồng họp ra biên bản là xong việc — **bản thân hội đồng KHÔNG có trạng thái**.
### Context Diagram
- Nhãn trên mũi tên = **danh từ / cụm danh từ** (dữ liệu truyền đi), **KHÔNG dùng động từ**.
- Actor cụ thể (Admin/Staff/PI/Reviewer), không "System User".

## 4. Luồng demo Review 2 (bắt buộc mượt)
Admin mở đợt (Applied hoặc Basic) → PI chọn đợt → PI **upload file đề cương (Word/PDF)** → **AI đọc & trích xuất** (tóm tắt, kinh phí…) tự điền vào hệ thống → PI dò lại → Nộp.
⚠️ Thầy nhấn mạnh: **KHÔNG bắt PI điền tay form dài**. AI prefill là điểm chấm chính. (Nhập tay vẫn giữ làm fallback khi AI lỗi — "dual intake".)

## 5. Phạm vi Mobile App
- Chỉ **PI + Staff**. Tính năng: xem trạng thái hồ sơ, xem lịch họp, **nhận Notification nhắc deadline**.
- KHÔNG đưa chức năng phức tạp (vd hội đồng chấm điểm) lên mobile.

## 6. Rule nghiệp vụ liên quan diagram
- 1 đợt (cycle) = đúng 1 loại (Applied HOẶC Basic). "Mở cả 2" = 2 cycle độc lập.
- **Applied** = đặt hàng: Staff đăng danh mục → nhiều PI cạnh tranh 1 đề tài → **1 winner**. Hội đồng có thêm "người đặt hàng".
- **Basic** = PI tự đề xuất → 1 đề tài 1 PI.
- **Đạt/Trượt = Chủ tịch HĐ quyết** sau họp kín; Thư ký soạn biên bản → Chủ tịch duyệt = khóa. Hệ thống KHÔNG tự đếm phiếu (điểm/phiếu chỉ tham khảo).
- Giải ngân KHÔNG tự động: deliverable PASSED → set điều kiện + báo Staff → Staff xác nhận tay.
- WHOLE ≥ 3 đợt giải ngân (đầu/giữa/cuối); PARTIAL = 1 đợt/mốc nghiệm thu.

## 7. Mốc thời gian & tài liệu
- Hiện: **tuần 7**. **Review 2: ~30/06–04/07 (tuần 8)**. Giám khảo dự kiến: **thầy Phương, thầy Trương Long**.
- Đang viết **Report 4 (Design)** + **Report 5 (Testing)**. Unit test phải làm thật, **không chế số liệu** (BE hiện có 26 test xanh dùng EF InMemory).

## 8. Mình cần hỗ trợ ở phiên này (tick khi dán):
- [ ] Vẽ lại State Machine (Proposal / Project / Final Report) — Mermaid/PlantUML
- [ ] Rà Context Diagram (đổi động từ→danh từ, actor cụ thể)
- [ ] Sửa System Architecture (Request nét liền / Response nét đứt)
- [ ] Use Case Diagram (actor đúng, hướng include/extend)
- [ ] Viết/biên tập mục … trong Report 4 / Report 5

---

## 9. Tài liệu đã có sẵn trong repo (đính kèm cho phiên này)
> Đều ở `FURPMS_BE/docs/`. Mở ra copy Mermaid để chỉnh/render (mermaid.live hoặc VS Code).
- **`Review2_Diagrams.md`** — bộ diagram chuẩn theo DB thật:
  - §1 Context · §2 System Architecture (request nét liền / response nét đứt) · **§2b System Overview** (mức cao: component + deployment, gồm Render/site4now/Gemini/Brevo)
  - §3–5 State Machine: Proposal · Project · Final Report
  - §5b State Machine bổ sung: Contract · Review Round · Progress Report (Cycle ghi text)
  - §6 ERD rút gọn · **§6b Logical ERD đầy đủ** (thực thể + PK/FK + trường chính) · §7 Đánh giá DB/Code · §8 Use Case (Mermaid + PlantUML)
  - §9 **Activity Diagrams** 5 luồng có actor: nộp đề cương · xét duyệt HĐ · đặt hàng Applied · hợp đồng+giải ngân · báo cáo+nghiệm thu
- **`Review2_Tech_Stack.md`** — 4 danh sách Product/Tech: dịch vụ 3rd-party · tech stack (FE/BE/Mobile/DB) · source/DevOps · môi trường deploy.
- **`README.md`** — mục lục toàn bộ docs (gom nhóm cũ/mới) + backlog việc còn lại.

## 10. Checklist Review 2 (Khoa KTPM) — trạng thái
**1. Document**
- ✅ System Overview / Architecture — `Review2_Diagrams §2` (chi tiết) + `§2b` (mức cao)
- ✅ Logical ERD + Database — **`§6b` (Logical ERD đầy đủ PK/FK)** + `§6` (rút gọn) + `DB_ANALYTIC_REPORT.md`, `DATABASE_DESIGN.md`
- ✅ State machine main entities — `§3–5`, `§5b`
- ✅ Activity Diagrams có actor — `§9`
- ⏳ Document 1–4 (văn xuôi báo cáo) — **NHÓM viết**
**2. Product/Tech** — ✅ `Review2_Tech_Stack.md` (điền nốt stack mobile + nơi deploy FE)
**3. Team Contribution (Week 1–8)** — ⏳ **NHÓM tự điền**: tên/vai trò/man-hour/%/ghi chú (đối chiếu trong buổi review)

## 11. Cần hỗ trợ ở phiên này (gợi ý)
- Đánh bóng diễn giải diagram → dán vào file SDD (Word).
- Viết phần mô tả Activity/State Machine bằng văn xuôi cho **Report 4 (Design)**.
- Soạn mẫu bảng **Team Contribution** + nội dung **Report 5 (Testing)**: liệt kê **36 unit test BE** đã có (xcounit, EF InMemory) — làm thật, không chế số liệu.
