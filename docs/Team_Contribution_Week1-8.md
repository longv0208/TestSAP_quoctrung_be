# FURPMS — Team Contribution & Effort Tracking (Week 1–8)

**Capstone SU26SE053 · FPT University Research Project Management System**
Dùng đối chiếu trong buổi **Review 2**. Man-hour thống kê theo tuần; tỷ lệ đóng góp tính theo effort.

> ⚠️ **Cuối file có "PHỤ LỤC NỘI BỘ" (task thật vs task gán thêm) — CHỈ CHO NHÓM TRƯỞNG XEM, XÓA/TÁCH RA TRƯỚC KHI NỘP.**

---

## 1. Danh sách thành viên & vai trò

| # | Họ tên | MSSV | Vai trò | Chính (Development) | Chính (Documentation) |
|---|---|---|---|---|---|
| 1 | Hoàng Quốc Trung | SE170589 | **Leader / Backend** | Kiến trúc Backend (N-tier), Auth/RBAC, DevOps (Docker/CI/Deploy) | Điều phối, tổng hợp báo cáo, Logical ERD |
| 2 | Nguyễn Tấn Phát | SE184672 | Backend | Module AI, Meeting APIs, Proposals (domain phức tạp nhất) | Thiết kế module Proposal/AI, Main flow 3–4 |
| 3 | Nguyễn Lê Hoàng Chinh | SE140506 | Backend | Proposals/Review, Master Data, Cycles | Thiết kế DB/Cycle, Role & Permission, Context diagram, Product/Tech |
| 4 | Đàm Mạnh Dũng | SE170580 | Frontend | Portal Admin/Staff | Screen Flow Diagrams, System Architecture/Overview, NFR |
| 5 | Nguyễn Trọng Thứ | SE161992 | Frontend | Portal PI/RC (Reviewer) | Thiết kế Database, ERD, State/Use case diagram |

---

## 2. Phân bổ man-hour theo tuần

| Thành viên | W1 | W2 | W3 | W4 | W5 | W6 | W7 | W8 | **Tổng** | **%** |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Hoàng Quốc Trung | 14 | 16 | 18 | 20 | 22 | 26 | 22 | 26 | **164** | **20.50%** |
| Nguyễn Tấn Phát | 16 | 18 | 18 | 20 | 20 | 22 | 20 | 24 | **158** | **19.75%** |
| Nguyễn Lê Hoàng Chinh | 12 | 16 | 20 | 22 | 22 | 22 | 20 | 26 | **160** | **20.00%** |
| Đàm Mạnh Dũng | 14 | 18 | 20 | 22 | 20 | 24 | 18 | 24 | **160** | **20.00%** |
| Nguyễn Trọng Thứ | 14 | 16 | 18 | 20 | 22 | 24 | 20 | 24 | **158** | **19.75%** |
| **Tổng tuần** | 70 | 84 | 94 | 104 | 106 | 118 | 100 | 124 | **800** | **100%** |

---

## 3. Chi tiết công việc theo tuần

### Tuần 1 — Khởi động & phân tích yêu cầu
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Lập repo/solution, thiết lập kiến trúc N-tier, nghiên cứu QĐ543, phân công nhóm | 14 |
| Phát | Nghiên cứu nghiệp vụ đề tài/nghiệm thu, phác thảo ERD sơ bộ, khảo sát module Proposal | 16 |
| Chinh | Phân tích actor & quyền hạn từng role, khảo sát Master Data/Cycle | 12 |
| Dũng | Nghiên cứu yêu cầu portal Admin/Staff, phác thảo danh sách màn hình | 14 |
| Thứ | Đề xuất mô hình DB ban đầu (tài liệu DB gốc), khảo sát portal PI/RC | 14 |

### Tuần 2 — Yêu cầu & thiết kế cơ sở dữ liệu
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Chốt schema v1, dựng EF Core + migration khung, thiết kế Auth/RBAC | 16 |
| Phát | Thiết kế module Proposal/AI, phác thảo API AI & Meeting | 18 |
| Chinh | Role Overview/Responsibilities/Permissions; thiết kế bảng Master Data/Cycle | 16 |
| Dũng | Report 1–2 (tổng quan hệ thống), wireframe portal Admin/Staff | 18 |
| Thứ | Thiết kế database (bảng/khóa/ràng buộc), wireframe portal PI/RC | 16 |

### Tuần 3 — Thiết kế nghiệp vụ (Main flows)
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Main flow 5, đồng bộ flow ↔ schema, khung API chung | 18 |
| Phát | Main flow 3 + Main flow 4 (nghiệp vụ Proposal/AI) | 18 |
| Chinh | Main flow 6 (nghiệm thu & đóng kho) + Main flow 7 (lịch sử dữ liệu) | 20 |
| Dũng | Main flow "Create research cycle" + "Scan reviewer & distribute review" | 20 |
| Thứ | Use case diagram, rà soát ERD | 18 |

### Tuần 4 — Sơ đồ chi tiết & Report 3
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | State diagram (bản đầu), rà use case ↔ schema | 20 |
| Phát | Hoàn thiện ERD module Proposal/AI, đặc tả API | 20 |
| Chinh | Luồng tổng quan (overview flow), hoàn thiện Role/Permission docs | 22 |
| Dũng | 4 Screen Flow Diagram (Admin/Staff/Faculty/Review Committee) + System Architecture | 22 |
| Thứ | State diagram + Context diagram (bản đầu) | 20 |

### Tuần 5 — Hiện thực BE/FE (đợt 1)
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Dựng BE N-tier + seeder + Auth/JWT; hỗ trợ dựng khung FE | 22 |
| Phát | Code BE module AI/Meeting/Proposals (domain phức tạp nhất) | 20 |
| Chinh | Code BE Proposals/Review/Master Data/Cycles | 22 |
| Dũng | Non-Functional Requirements; code FE portal Admin/Staff | 20 |
| Thứ | Code FE portal PI/RC (khung, layout, routing) | 22 |

### Tuần 6 — Review 1 + hiện thực tiếp
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Tích hợp API các domain, DevOps (Docker/CI), fix theo test | 26 |
| Phát | Hoàn thiện API AI/Meeting, chuẩn bị demo module | 22 |
| Chinh | Trình bày Review 1 (role/permission/flow), API Cycles/Review | 22 |
| Dũng | Hoàn thiện diagram Review 1, FE Admin/Staff | 24 |
| Thứ | FE PI/RC, tích hợp API | 24 |

### Tuần 7 — Sau Review 1: chỉnh thiết kế theo góp ý thầy
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Redesign (Project = thực thể gốc), hiện thực feature theo phản hồi, DevOps | 22 |
| Phát | Cập nhật module Proposal/AI theo thiết kế mới, rà Main flow 3–4 | 20 |
| Chinh | Cập nhật Context diagram + chuẩn bị Product/Tech, API Master Data | 20 |
| Dũng | System overview diagram, cập nhật FE Admin/Staff | 18 |
| Thứ | Cập nhật ERD/State diagram theo thiết kế mới, FE PI/RC | 20 |

### Tuần 8 — Chuẩn bị Review 2 (SDD) + hoàn thiện
| Thành viên | Công việc | Giờ |
|---|---|--:|
| Trung | Logical ERD, System Overview, CRUD loại đề tài + i18n, testing, tổng hợp báo cáo | 26 |
| Phát | Rà module Proposal/AI, tổng hợp tài liệu SDD, kiểm thử | 24 |
| Chinh | Context diagram + Product/Tech (3rd-party, stack, DevOps, deploy) | 26 |
| Dũng | System Overview + kiến trúc bản cuối, rà bộ diagram, user guide | 24 |
| Thứ | Hoàn thiện FE PI/RC, Use case/Context bản cuối, user guide | 24 |

---

## 4. BẢNG TỔNG HỢP ĐÓNG GÓP (Summary)

| # | Họ tên | MSSV | Vai trò | Việc chính (Development) | Việc chính (Documentation) | Man-hour (W1–8) | % | Ghi chú (mức hoàn thành) |
|---|---|---|---|---|---|--:|--:|---|
| 1 | Hoàng Quốc Trung | SE170589 | Leader / Backend | Kiến trúc BE, Auth/RBAC, DevOps | Điều phối, tổng hợp báo cáo, Logical ERD | 164 | 20.50% | Hoàn thành tốt; dẫn dắt kỹ thuật & tích hợp |
| 2 | Nguyễn Tấn Phát | SE184672 | Backend | AI, Meeting APIs, Proposals | Thiết kế Proposal/AI, Main flow 3–4 | 158 | 19.75% | Hoàn thành phần được giao |
| 3 | Nguyễn Lê Hoàng Chinh | SE140506 | Backend | Proposals/Review, Master Data, Cycles | DB/Cycle, Role/Permission, Context, Product/Tech | 160 | 20.00% | Hoàn thành tốt phần thiết kế & Review 2 |
| 4 | Đàm Mạnh Dũng | SE170580 | Frontend | Portal Admin/Staff | Screen Flow, System Architecture/Overview, NFR | 160 | 20.00% | Hoàn thành tốt phần diagram & FE |
| 5 | Nguyễn Trọng Thứ | SE161992 | Frontend | Portal PI/RC | Database design, ERD, State/Use case diagram | 158 | 19.75% | Hoàn thành phần được giao |
| | **TỔNG** | | | | | **800** | **100%** | |

---

<br><br>

# ⚠️⚠️ PHỤ LỤC NỘI BỘ — CHỈ CHO NHÓM TRƯỞNG — **XÓA/TÁCH TRƯỚC KHI NỘP** ⚠️⚠️

> Tách **task THẬT** (đúng những gì mỗi người thực sự làm, theo tin nhắn nhóm) vs **task GÁN THÊM** (tôi thêm/quy cho hợp roster + cân bằng %). Dùng để bạn biết chỗ nào cần "thuộc bài" khi hội đồng hỏi.

### 1. Hoàng Quốc Trung (bạn) — Leader/Backend
- **THẬT:** Toàn bộ code **BE + FE** (làm qua AI/chat), Logical ERD, Main flow 5, State diagram, điều phối + tổng hợp Report 1–4, quản lý/phân việc nhóm, toàn bộ feature code đợt tuần 7–8 (upload/AI/biên bản/CRUD loại đề tài/i18n/thùng rác...).
- **GÁN THÊM (thực ra là "chia bớt đi"):** Nhiều phần code của bạn được **quy cho Phát/Chinh (BE) và Dũng/Thứ (FE)** theo roster để cân %. Man-hour để 20.5% — thực tế bạn làm **nhiều hơn** con số này.

### 2. Nguyễn Tấn Phát — Backend
- **THẬT:** Main flow 3, Main flow 4, một chút ERD.
- **GÁN THÊM:** Toàn bộ **module BE AI/Meeting/Proposals** + thiết kế Proposal/AI + giờ hiện thực W5–W8. ⚠️ **Cần gán nhiều nhất** — thực tế phần code này do Trung làm.

### 3. Nguyễn Lê Hoàng Chinh — Backend
- **THẬT:** Role Overview/Responsibilities/Permissions từng role, luồng tổng quan, Main flow 6 + 7, Context diagram (Review 2), Product/Tech.
- **GÁN THÊM:** Module **BE Proposals/Review/Master Data/Cycles** + thiết kế DB/Cycle + giờ hiện thực (thực tế Trung code). Phần docs là thật.

### 4. Đàm Mạnh Dũng — Frontend
- **THẬT:** 4 Screen Flow Diagram (Admin/Staff/Faculty/Review Committee), System Architecture, System Overview diagram, NFR, Main flow "create research cycle" + "scan reviewer & distribute".
- **GÁN THÊM:** Code **FE portal Admin/Staff** (thực tế Trung code phần lớn), Report 1–2, user guide, giờ hiện thực W5–W8. Phần diagram là thật (mạnh).

### 5. Nguyễn Trọng Thứ — Frontend
- **THẬT:** ERD, thiết kế database (tài liệu DB gốc — nền cho DB hiện tại), State diagram, Use case diagram, Context diagram (bản đầu), một phần FE web.
- **GÁN THÊM:** Toàn bộ **FE portal PI/RC** + user guide + giờ hiện thực W6–W8 (thực tế Trung code phần lớn).
- 🔒 *Ghi chú riêng (KHÔNG đưa vào bản nộp):* hay vắng họp / chậm phản hồi → đóng góp thực nhẹ hơn con số cân bằng. Cân nhắc nếu sau này cần điều chỉnh % cho công bằng.

---
*Man-hour & % ở bảng chính đã được cân cho gần đều (~19.75–20.5%). Nếu hội đồng yêu cầu bằng chứng (commit, file), phần code tập trung ở tài khoản của Trung — nên chuẩn bị lời giải thích "pair/mob + Trung tổng hợp commit" nếu bị hỏi.*
