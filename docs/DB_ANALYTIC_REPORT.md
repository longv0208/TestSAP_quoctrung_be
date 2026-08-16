


> ⚠️ **CẬP NHẬT 09/07/2026 — SCHEMA HIỆN HÀNH LÀ V2.0 (PROJECT-CENTRIC, ~56 bảng).**
> Nội dung V1.3 bên dưới (44 bảng, proposal-centric) giữ làm **lịch sử**. Sau biên bản Review 2, DB đã refactor sang **Project làm gốc** (Phase A+B+C đã code, 40/40 test).
> **Nguồn schema chính xác:** `ERD_v3_Project_Centric.dbml`. Tóm tắt thay đổi: xem **mục "SCHEMA V2.0" ngay dưới đây**.
>
> ### SCHEMA V2.0 — thay đổi Project-centric (so với V1.3)
> | Nhóm | Thay đổi |
> |---|---|
> | **Thêm bảng** | `project` (GỐC: cycle_track_id, order_id, pi_user_id, status vòng đời tổng) · `cycle_track` (đợt↔lĩnh vực, M-N) · `project_member` · `project_deliverable` (GỘP `proposal_expected_product` + `product_deliverable`) · `project_round` (đề tài↔vòng, M-N) · `council_project_assignment` (hội đồng↔đề tài) · `contract_phase` · `proposal_change_request` |
> | **Proposal** | thành **tài liệu có version**: +`project_id`, `version_no`, `is_current` (unique `project_id`+`version_no`); bỏ các cột neo (cycle/track/order/pi/hosting/type — dời lên `project`) |
> | **Review đổi neo** | `review_round`: bỏ `proposal_id` → `cycle_track_id`; `review_council`: bỏ `proposal_id` → `round_id`; điểm/quyết định/feedback/nghiệm thu +`project_id` (unique 3 chiều) |
> | **Contract đổi neo** | `contract.proposal_id` → `project_id`, **bỏ unique** (1 project → n contract); disbursement +`phase_id`; `final_report`: `contract_id` → `project_id` (1/project) |
> | **Master** | `research_order` +`is_default`, `matched_proposal_id` → `matched_project_id`; `rubric_template` +`track_id`/`order_id` scope |
> | **Soft-delete** | global filter thêm `project` (ngoài `user`, `proposal`) |
>
> ---

TRƯỜNG ĐẠI HỌC FPT
FPT University Research Project Management System
BÁO CÁO PHÂN TÍCH KIẾN TRÚC CƠ SỞ DỮ LIỆU
DATABASE SCHEMA ANALYTIC REPORT
(V1.3 — LỊCH SỬ; xem banner trên cho V2.0 hiện hành)


FURPMS — FPT University Research Project Management System
Hệ thống Quản lý Đề tài Nghiên cứu Khoa học cấp Trường Đại học FPT


Mã Capstone	SU26SE053
Phiên bản Schema	V1.3 (Enterprise SaaS — Configuration Driven)
Database Engine	PostgreSQL 16 (đổi khỏi MS SQL Server 14/08 để deploy được lên Railway)
Căn cứ pháp lý	QĐ 543/QĐ-ĐHFPT ngày 14/5/2025 (Hiệu trưởng Nguyễn Khắc Thành)
Yêu cầu hệ thống	File DOCX SU26SE053 — 9 Task Packages
Supervisor	Phạm Minh Trí (tripm14@fe.edu.vn) | Đặng Ngọc Minh Đức (DucDNM2@fe.edu.vn)
Ngày báo cáo	Tháng 6 năm 2026
 
1. TỔNG QUAN HỆ THỐNG (EXECUTIVE SUMMARY)
FURPMS (FPT University Research Project Management System) là nền tảng phần mềm quản lý toàn bộ vòng đời đề tài nghiên cứu khoa học (NCKH) cấp Trường Đại học FPT — từ giai đoạn đặt hàng, nộp đề cương, xét duyệt hội đồng, ký hợp đồng, theo dõi tiến độ, nghiệm thu cho đến thanh lý — được số hóa hoàn toàn theo Quyết định 543/QĐ-ĐHFPT ngày 14/5/2025.

Cơ sở dữ liệu FURPMS V1.3 được xây dựng theo kiến trúc Enterprise SaaS với ba giá trị cốt lõi:

Configuration-Driven	Toàn bộ tham số tài chính, tiêu chí chấm điểm (Rubric) và loại hình nghiên cứu được lưu trong bảng Master Data, Admin có thể CRUD qua UI mà không cần sửa code.
Lean Table Design	Tách bảng users (định danh) và academic_profiles (học thuật) để tối ưu buffer pool khi phục vụ 3,000+ người dùng đồng thời.
Polymorphic Safety	documents, llm_outputs, notifications dùng entity_id kiểu NVARCHAR(100) để liên kết an toàn với mọi loại thực thể (UUID hoặc INT) mà không gây crash.

Hệ thống bao gồm 10 Domain, 44 Tables, phục vụ 4 nhóm Actor chính:

Administrator	Quản lý người dùng, cấu hình hệ thống, Master Data, báo cáo thống kê.
Staff (QLKH)	Quản lý đợt nhận hồ sơ, lập hội đồng, theo dõi hợp đồng và giải ngân.
Faculty / PI (Chủ nhiệm đề tài)	Soạn và nộp đề cương, báo cáo tiến độ, nộp hồ sơ nghiệm thu.
Review Committee (Giám khảo)	Chấm điểm đề cương và nghiệm thu, phản biện qua giao diện web/mobile.

Tổng quan schema: 44 bảng, ~220 cột, chia thành 10 Domain nghiệp vụ rõ ràng với đầy đủ FK constraints, check constraints và indexes cho hiệu năng truy vấn.
 
2. ĐIỂM SÁNG KIẾN TRÚC V1.3 (ARCHITECTURAL HIGHLIGHTS)
2.1. Sơ đồ tổng quan 10 Domain
Database được chia thành 10 Domain nghiệp vụ độc lập, liên kết với nhau qua Foreign Keys:

#	Domain	Tables chính	Chức năng
D1	Master Data & Config	research_types, research_tracks, product_categories, amendment_categories, llm_configs	Cấu hình hệ thống động — thay thế hard-code
D2	Financial & Rubric Templates	budget_allocation_rules, disbursement_templates, council_remuneration_rates, rubric_templates, rubric_criteria	Quản lý tỷ lệ ngân sách, kịch bản giải ngân và bộ tiêu chí chấm điểm
D3	User Management	roles, users, user_roles, organizational_units, academic_profiles	Định danh, phân quyền RBAC, lý lịch khoa học
D4	Research Cycle & Orders	research_cycles, research_orders	Quản lý đợt nhận hồ sơ và đơn đặt hàng nghiên cứu
D5	Proposals	proposals, proposal_team_members, proposal_budget, proposal_budget_labor_details, proposal_research_contents, proposal_activities, proposal_expected_products	Số hóa Biểu mẫu 01 — Đề cương nghiên cứu
D6	Review Process	review_councils, council_members, council_meetings, meeting_attendances, proposal_review_scores, review_score_details, council_decisions, reviewer_feedbacks, acceptance_evaluations	Lập hội đồng, chấm điểm động, nghiệm thu
D7	Contracts & Disbursements	contracts, contract_disbursements, contract_settlements	Hợp đồng điện tử, giải ngân và thanh lý
D8	Progress & Archival	progress_reports, progress_report_items, amendment_requests, product_deliverables, final_reports	Theo dõi tiến độ, xin gia hạn, nộp sản phẩm
D9	AI / Polymorphic Support	llm_outputs, semantic_search_vectors, documents, notifications	AI tóm tắt, semantic search, quản lý file, thông báo
D10	Logs	audit_logs, email_logs	Lưu vết hệ thống, bảo đảm minh bạch tài chính
 
3. LUỒNG NGHIỆP VỤ & MAPPING BẢNG (BUSINESS FLOWS)
Hệ thống FURPMS vận hành theo 6 luồng nghiệp vụ chính. Mỗi luồng ánh xạ trực tiếp tới các điều khoản trong QĐ 543 và các Task Package trong DOCX yêu cầu:

Flow / Luồng	Actor	Tables tham gia	Mapping Quy định / Task
Flow 1 Khởi tạo & Cấu hình	Admin	research_types research_tracks rubric_templates rubric_criteria budget_allocation_rules disbursement_templates llm_configs	Điều 4 (phân loại đề tài) Điều 15 (tỷ lệ ngân sách) Điều 16 (kịch bản giải ngân) Phụ lục 02 (thù lao hội đồng) Task Package 5 (LLM config)
Flow 2 Đợt & Nộp Đề cương	Staff, PI	research_cycles research_orders proposals proposal_team_members proposal_budget proposal_budget_labor_details proposal_activities proposal_expected_products	Điều 5 (đặt hàng/đề xuất) Điều 6 (đăng ký thực hiện) Điều 7 (chủ nhiệm đề tài) Biểu mẫu 01, 02 Task Package 1, 2
Flow 3 Lập Hội đồng & Xét duyệt	Staff, Committee	review_councils council_members council_meetings meeting_attendances proposal_review_scores review_score_details council_decisions	Điều 8 (Hội đồng xét duyệt) Biểu mẫu 03, 04 Task Package 3 (invitation token) Task Package 4 (Google Meet/Teams)
Flow 4 Hợp đồng & Tiến độ	Staff, PI	contracts contract_disbursements progress_reports progress_report_items amendment_requests	Điều 9 (ký kết hợp đồng) Điều 10 (báo cáo tiến độ & gia hạn) Điều 16 (giải ngân) Biểu mẫu 05, 06, 07
Flow 5 Nghiệm thu & Thanh lý	Committee, Staff	product_deliverables review_councils (ACCEPTANCE) acceptance_evaluations reviewer_feedbacks final_reports contract_settlements	Điều 11 (tổ chức nghiệm thu) Điều 12 (hội đồng nghiệm thu) Điều 13 (lưu trữ kết quả) Điều 17 (thanh toán & quyết toán) Biểu mẫu 09, 10, 11, 12, 13
Flow 6 AI & Thông báo	System	llm_outputs semantic_search_vectors notifications email_logs documents audit_logs	Task Package 5 (LLM/semantic search) Task Package 6 (dashboards & notifications) Điều 13 (lưu trữ) Minh bạch tài chính (audit)
 
4. PHÂN TÍCH CHI TIẾT TỪNG DOMAIN & TABLE
4.1. DOMAIN 1 — Master Data & Configurations (Cấu hình động)
Mục đích: Cho phép Admin thêm/sửa/xóa các quy định mà không cần can thiệp code. Thay thế hoàn toàn các giá trị hard-code trong hệ thống.

4.1.1. Bảng research_types — Loại hình nghiên cứu
Mapping QĐ543: Điều 4 (Phân loại đề tài cấp Trường — Nghiên cứu ứng dụng/triển khai vs Nghiên cứu cơ bản).
Lý do tạo: Thay thế ENUM hard-code 'APPLIED'/'BASIC' bằng bảng dữ liệu, cho phép Admin bổ sung loại hình NC mới trong tương lai.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
id	INT IDENTITY	NO	Khóa chính tự tăng	PK chuẩn.
code	NVARCHAR(50)	NO	Mã duy nhất, vd: APPLIED, BASIC	Dùng làm reference trong code backend. UNIQUE constraint.
name	NVARCHAR(200)	NO	Tên hiển thị đầy đủ, vd: 'Nghiên cứu ứng dụng/triển khai'	Hiển thị cho người dùng.
max_budget_cap	DECIMAL(15,2)	NO	Trần kinh phí tối đa (VND). VD: 150,000,000 cho đề tài ứng dụng; 100,000,000 cho cơ bản	Điều 14 QĐ543 quy định trần 100M/150M. Lưu vào DB để Admin có thể điều chỉnh khi có QĐ mới mà không cần sửa code.
require_ordering_unit	BIT	NO	Bắt buộc phải có Đơn vị đặt hàng (Khoa/Ban) hay không	Điều 4.1.b: Đề tài ứng dụng BẮT BUỘC do các đơn vị FE đặt hàng. Đề tài cơ bản thì không.
require_publication	BIT	NO	Kết quả nghiệm thu bắt buộc là bài báo ISI/Scopus hay không	Điều 4.2: Đề tài cơ bản tạo ra sản phẩm công bố khoa học. Cờ này dùng để validate hồ sơ nghiệm thu.
is_active	BIT	NO	Trạng thái kích hoạt (Soft delete)	Tránh xóa cứng khi có dữ liệu lịch sử liên quan.

4.1.2. Bảng research_tracks — Hướng nghiên cứu
Mapping: Phân loại hướng nghiên cứu cụ thể (AI/ML, EdTech, FinTech,...). Admin có thể thêm track mới khi Trường mở hướng nghiên cứu ưu tiên mới.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
id	INT IDENTITY	NO	Khóa chính	PK.
code	NVARCHAR(50)	NO	Mã hướng NC. VD: AI_ML, EDTECH	Reference code cho API filter.
name	NVARCHAR(200)	NO	Tên hướng NC đầy đủ	Hiển thị trên UI.
description	NVARCHAR(1000)	YES	Mô tả chi tiết về hướng NC	Giúp PI hiểu rõ phạm vi của track.
is_active	BIT	NO	Trạng thái kích hoạt	Cho phép tắt track khi không còn nhận đề tài theo hướng đó.

4.1.3. Bảng product_categories — Danh mục sản phẩm kỳ vọng
Mapping: Điều 13 QĐ543 & Biểu mẫu 09 — Các loại sản phẩm nộp khi nghiệm thu (phần mềm, bài báo, quy trình, ...). Admin định nghĩa danh mục thay vì hard-code ENUM.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
id	INT IDENTITY	NO	Khóa chính	PK.
code	NVARCHAR(50)	NO	Mã loại sản phẩm. VD: SOFTWARE, PAPER_ISI, PROCESS	Dùng trong logic xác định điều kiện nghiệm thu tự động.
name	NVARCHAR(200)	NO	Tên loại sản phẩm	Hiển thị trong form đề cương và hồ sơ nghiệm thu.
is_active	BIT	NO	Kích hoạt	Soft delete.

4.1.4. Bảng amendment_categories — Danh mục loại xin thay đổi
Mapping: Biểu mẫu 07 — Phiếu đề nghị thay đổi trong quá trình thực hiện đề tài (thay đổi nội dung, gia hạn, thay đổi nhân sự, điều chỉnh kinh phí, ...).

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
id	INT IDENTITY	NO	Khóa chính	PK.
code	NVARCHAR(50)	NO	Mã loại thay đổi. VD: EXTENSION, MEMBER_CHANGE, BUDGET_ADJUST	Backend dùng để phân loại và định tuyến phê duyệt tự động.
name	NVARCHAR(200)	NO	Tên loại thay đổi	Hiển thị cho PI khi chọn loại phiếu đề nghị.
is_active	BIT	NO	Kích hoạt	Soft delete.

4.1.5. Bảng llm_configs — Cấu hình AI/LLM
Mapping: Task Package 5 (DOCX) — Tích hợp xAI API/GPT cho tóm tắt đề cương, gợi ý nhận xét cho giám khảo và semantic search.
Lý do tạo: Cho phép Admin thay đổi AI provider, model và system prompt trực tiếp trên giao diện web mà không cần deploy lại code.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
config_code	NVARCHAR(50)	NO	Mã cấu hình. VD: PROPOSAL_SUMMARY, REVIEWER_SUGGESTION	Backend tra cứu đúng config cho từng tác vụ AI.
provider	NVARCHAR(50)	NO	Nhà cung cấp AI. VD: xAI, OpenAI, Anthropic	Cho phép đổi provider mà không sửa code.
model_name	NVARCHAR(100)	NO	Tên model. VD: grok-3, gpt-4o	Cho phép nâng cấp model khi có phiên bản mới.
system_prompt	NVARCHAR(MAX)	NO	System prompt gốc bằng tiếng Việt/Anh	Admin có thể tinh chỉnh prompt để cải thiện chất lượng đầu ra AI mà không cần dev.
temperature	DECIMAL(3,2)	NO	Độ sáng tạo của model (0.0 - 1.0). Mặc định 0.7	Tóm tắt cần temperature thấp (0.3), gợi ý phản biện cần cao hơn (0.7).
is_active	BIT	NO	Kích hoạt	Cho phép vô hiệu hóa một config AI khi bảo trì.
 
4.2. DOMAIN 2 — Financial & Rubric Templates
Mục đích: Lưu trữ các mẫu tài chính và bộ tiêu chí chấm điểm dưới dạng dữ liệu có thể cấu hình, thay thế các quy định tài chính và rubric cố định trong code.

4.2.1. Bảng budget_allocation_rules — Tỷ lệ phân bổ ngân sách
Mapping: Điều 15 QĐ543 — Dự toán kinh phí: Thù lao NC ≤100%, Thiết bị/vật tư ≤60%, Thuê ngoài ≤60%, Hội nghị/seminar ≤30%, Văn phòng phẩm ≤20%, Phát sinh/sở hữu trí tuệ ≤10%.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
research_type_id	INT FK	NO	FK tới research_types — Quy định áp dụng cho loại NC nào	Điều 15 có thể áp dụng khác nhau cho đề tài ứng dụng và cơ bản.
category_code	NVARCHAR(50)	NO	Mã hạng mục chi. VD: LABOR, EQUIPMENT, EXTERNAL	Backend validate form dự toán của PI theo đúng category.
category_name	NVARCHAR(200)	NO	Tên hạng mục. VD: Thù lao nghiên cứu, Thiết bị vật tư	Hiển thị trên form dự toán.
max_percentage	DECIMAL(5,2)	NO	% tối đa trên tổng KP. VD: 100, 60, 30, 20, 10	Backend dùng để validate: labor_amount / total ≤ max_percentage/100. Linh hoạt khi QĐ thay đổi.

4.2.2. Bảng disbursement_templates — Kịch bản giải ngân mẫu
Mapping: Điều 16 QĐ543 — Đề tài NC ứng dụng giải ngân 4 đợt tỷ lệ 30%-30%-30%-10%. Khi Admin tạo hợp đồng, hệ thống tự động copy kịch bản này thành các dòng contract_disbursements.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
research_type_id	INT FK	NO	FK tới research_types	Đề tài ứng dụng: 4 đợt. Đề tài cơ bản: 1 đợt (sau nghiệm thu đạt).
round_number	INT	NO	Số thứ tự đợt giải ngân (1, 2, 3, 4)	Dùng để sắp xếp và hiển thị đúng thứ tự giải ngân.
percentage	DECIMAL(5,2)	NO	% giải ngân của đợt. VD: 30.00, 10.00	Điều 16: 30%-30%-30%-10%. Lưu vào DB để Admin có thể điều chỉnh khi tỷ lệ thay đổi.
condition_description	NVARCHAR(MAX)	NO	Mô tả điều kiện để được giải ngân đợt này	VD: 'Giải ngân sau khi ký kết Hợp đồng NCKH'. Hiển thị cho Staff và Kế toán.

4.2.3. Bảng council_remuneration_rates — Định mức thù lao Hội đồng
Mapping: Phụ lục 02 QĐ543 — Chủ tịch/Thư ký HĐ xét duyệt: 750,000 VNĐ; Thành viên: 700,000 VNĐ. HĐ nghiệm thu: 800,000/750,000 VNĐ.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
council_type	NVARCHAR(50)	NO	Loại hội đồng. VD: PROPOSAL_REVIEW, ACCEPTANCE	Phụ lục 02 quy định mức thù lao khác nhau cho 2 loại HĐ.
role_in_council	NVARCHAR(50)	NO	Vai trò trong HĐ. VD: CHAIR_SECRETARY, MEMBER	Chủ tịch/Thư ký có mức thù lao cao hơn Thành viên theo Phụ lục 02.
amount	DECIMAL(15,2)	NO	Số tiền thù lao (VNĐ)	Dùng để tính toán kinh phí hoạt động của Hội đồng (Điều 18).
effective_date	DATE	NO	Ngày có hiệu lực	Khi QĐ về thù lao thay đổi, lưu record mới với effective_date mới thay vì xóa record cũ, đảm bảo dữ liệu lịch sử.

4.2.4. Bảng rubric_templates & rubric_criteria — Bộ tiêu chí chấm điểm động
Mapping: Biểu mẫu 03 QĐ543 — Phiếu đánh giá thẩm định đề cương (5 tiêu chí: Mục đích 10đ, PP Nghiên cứu 20đ, Nội dung & Kết quả 40đ, Năng lực PI 20đ, Dự toán KP 10đ).
Lý do tạo: Xóa bỏ 5 cột điểm cứng trong code. Admin có thể tạo template mới với 3/5/10 tiêu chí tùy ý. UI sẽ tự render form chấm điểm tương ứng.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
template_type	NVARCHAR(50)	NO	Loại mẫu: PROPOSAL_REVIEW (Xét duyệt) / ACCEPTANCE (Nghiệm thu)	Mỗi loại HĐ dùng bộ tiêu chí khác nhau.
name	NVARCHAR(200)	NO	Tên bộ tiêu chí. VD: Rubric Xét duyệt Đề cương 2025	Cho phép tạo nhiều phiên bản Rubric cho các năm khác nhau.
max_total_score	DECIMAL(5,2)	NO	Tổng điểm tối đa. Mặc định 100	Biểu mẫu 03: tổng tối đa = 100.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
template_id	INT FK	NO	FK tới rubric_templates	Mỗi bộ tiêu chí có nhiều tiêu chí con.
criterion_name	NVARCHAR(500)	NO	Tên tiêu chí. VD: Mục đích, ý nghĩa của đề tài	Hiển thị trên phiếu chấm điểm của giám khảo.
max_score	DECIMAL(5,2)	NO	Điểm tối đa cho tiêu chí này. VD: 10, 20, 40	Biểu mẫu 03: từng tiêu chí có trọng số điểm khác nhau.
sequence	INT	NO	Thứ tự hiển thị trong phiếu chấm	Đảm bảo thứ tự cố định khi render UI form chấm điểm.
 
4.3. DOMAIN 3 — User Management (Quản lý người dùng & phân quyền)
Mục đích: Quản lý định danh người dùng, phân quyền RBAC (Role-Based Access Control) và lý lịch khoa học cho Faculty/Experts.

4.3.1. Bảng users — Bảng người dùng trung tâm
Mapping: Task Package 1 (DOCX) — Quản lý tài khoản người dùng với 4 role: Admin, Staff, Faculty/PI, Review Committee.
Thiết kế Lean: Chỉ lưu thông tin định danh cốt lõi. Thông tin học thuật được tách sang academic_profiles để tối ưu query performance.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
id	UNIQUEIDENTIFIER	NO	Khóa chính dạng UUID (GUID). VD: 3f2504e0-4f89-11d3-9a0c-0305e82c3301	UUID ngăn enum attack và an toàn hơn INT ID trong hệ thống multi-tenant.
email	NVARCHAR(255) UNIQUE	NO	Email đăng nhập. VD: tripm14@fe.edu.vn	Định danh duy nhất cho SSO và external experts.
password_hash	NVARCHAR(512) NULL	YES	Bcrypt hash mật khẩu. NULL nếu dùng SSO FPT	Cho phép login bằng password (external experts) hoặc SSO (nội bộ FPT).
fpt_employee_id	NVARCHAR(50) NULL	YES	Mã CBGV FPT. VD: FE0001234	Liên kết với hệ thống HR của FPT để đồng bộ thông tin.
fpt_sso_sub	NVARCHAR(255) NULL	YES	Subject token từ FPT SSO (OAuth2)	Dùng để định danh khi CBGV đăng nhập qua SSO FPT mà không cần nhập password.
is_external	BIT	NO	TRUE = Chuyên gia ngoài trường. FALSE = CBGV FPT	Điều 8 QĐ543: HĐ có thể mời chuyên gia ngoài FPT. External experts không có FPT email nên cần cơ chế đăng nhập khác (invitation_token).
unit_id	INT FK NULL	YES	FK tới organizational_units — Đơn vị công tác	Biểu mẫu 01 (Đề cương) yêu cầu Đơn vị chủ trì. Dùng unit_id để tự động điền.
status	NVARCHAR(20)	NO	ACTIVE / INACTIVE / SUSPENDED	Admin có thể tạm khóa tài khoản vi phạm mà không xóa dữ liệu.
is_deleted	BIT	NO	Soft delete flag	GDPR compliance — không xóa cứng dữ liệu người dùng. Dữ liệu lịch sử vẫn tham chiếu được.

4.3.2. Bảng academic_profiles — Lý lịch khoa học
Mapping: Biểu mẫu 02 QĐ543 — Lý lịch khoa học của Chủ nhiệm đề tài và thành viên nhóm nghiên cứu. Điều 7.1: Chủ nhiệm phải có trình độ Thạc sĩ trở lên.
Lý do tách riêng: Tránh làm phình bảng users. Chỉ Faculty và External Experts mới cần Profile này, không phải tất cả 3,000+ users.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
user_id	UNIQUEIDENTIFIER UNIQUE FK	NO	FK 1-1 tới users	One-to-one với users. UNIQUE đảm bảo mỗi user chỉ có một profile.
degree_level	NVARCHAR(20) NULL	YES	Học vị cao nhất. VD: MASTER, PHD, ASSOC_PROF, PROF	Điều 7.1 QĐ543: Chủ nhiệm đề tài phải là CBGV có trình độ Thạc sĩ trở lên. Backend validate is_eligible_pi dựa trên field này.
academic_title	NVARCHAR(20) NULL	YES	Học hàm. VD: GS (Giáo sư), PGS (Phó Giáo sư)	Biểu mẫu 02: Phiếu đánh giá ghi rõ Học hàm, Học vị của người nhận xét.
isi_scopus_count	INT	NO	Số bài báo quốc tế ISI/Scopus	Biểu mẫu 02 mục 14.1. Dùng để đánh giá năng lực nghiên cứu của PI (Rubric tiêu chí 4 trong BM03).
is_eligible_pi	BIT	NO	Đủ điều kiện làm Chủ nhiệm đề tài hay không	Điều 7.1: Phải là CBGV FPT có Thạc sĩ trở lên. Hệ thống tự tính từ degree_level và is_external. Dùng để filter danh sách PI khi Staff tạo đề tài.
total_invitations	INT	NO	Tổng số lần được mời tham gia HĐ	Theo dõi tần suất tham gia để phân phối đều công việc cho các chuyên gia, tránh overload.
institution	NVARCHAR(300) NULL	YES	Tên đơn vị công tác (cho external experts)	Biểu mẫu 02 mục 8. External experts không có unit_id nên cần field riêng.

4.3.3. Bảng organizational_units — Cơ cấu tổ chức
Mapping: Điều 5.1 QĐ543 — Phòng QLKH tổng hợp nhu cầu đặt hàng NC từ các đơn vị (Khoa, Viện, Trung tâm, Ban). Cây phân cấp tổ chức của FPT.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
code	NVARCHAR(50) UNIQUE	NO	Mã đơn vị. VD: FEHO_SSET, FEHO_FSB	Dùng trong API và báo cáo thống kê theo đơn vị.
unit_type	NVARCHAR(50)	NO	Loại đơn vị. VD: SCHOOL, DEPARTMENT, CENTER, OFFICE	Điều 4.1.b: Đặt hàng NC phải từ cấp Khối/Viện/Trung tâm/Ban trở lên.
parent_id	INT FK NULL	YES	Self-referencing FK — Đơn vị cha trong cây tổ chức	Cho phép xây dựng cây phân cấp tổ chức FPT (FPT → FE → Campus → Khoa).
head_user_id	UNIQUEIDENTIFIER FK NULL	YES	Lãnh đạo đơn vị (trưởng khoa, giám đốc,...)	Điều 20 (Chương 4): FE phải phối hợp quản lý. Lãnh đạo đơn vị ký xác nhận hồ sơ NCKH.
 
4.4. DOMAIN 4 — Research Cycle & Orders (Đợt & Đặt hàng NC)
Mục đích: Số hóa quy trình Đặt hàng/Đề xuất đề tài theo Điều 5 QĐ543 — Quý IV nhận đề tài ứng dụng, Quý II nhận đề tài cơ bản.

4.4.1. Bảng research_cycles — Đợt nhận hồ sơ
Mapping: Điều 5 & 6 QĐ543 — Quý I (đề tài ứng dụng), Quý II (đề tài cơ bản). Phòng QLKH tổng hợp và công bố danh sách.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
cycle_year	INT	NO	Năm của đợt. VD: 2026	Dùng để filter và báo cáo theo năm.
semester_code	NVARCHAR(20) NULL	YES	Mã học kỳ FPT. VD: FA26, SP26, SU26	FPT dùng hệ thống học kỳ FA/SP/SU thay vì Quý như các trường khác. Field này cho phép quản lý theo học kỳ FPT.
research_type_id	INT FK	NO	FK tới research_types — Đợt này nhận loại NC nào	Điều 5: Quý I nhận ứng dụng, Quý II nhận cơ bản. Mỗi cycle gắn với 1 loại NC.
submission_open_date	DATE	NO	Ngày mở nhận hồ sơ	Hệ thống tự động mở portal nộp hồ sơ vào ngày này.
submission_deadline	DATE	NO	Hạn chót nộp hồ sơ	Điều 6.3: Hồ sơ phải nộp đúng thời hạn. Hệ thống auto-lock form sau deadline.
review_deadline	DATE	NO	Hạn chót hoàn thành xét duyệt	Điều 8.3.a: HĐ phải họp chậm nhất 15 ngày kể từ ngày có QĐ thành lập. Field này cho Staff track deadline.
order_collection_deadline	DATE NULL	YES	Hạn chót thu thập đơn đặt hàng từ các đơn vị	Điều 5.1: Phòng QLKH tổng hợp nhu cầu đặt hàng trước khi mở nhận đề cương.
status	NVARCHAR(20)	NO	PLANNING → OPEN → REVIEWING → CLOSED → ARCHIVED	Trạng thái lifecycle của đợt. Hệ thống auto-transition dựa trên ngày tháng.

4.4.2. Bảng research_orders — Đơn đặt hàng nghiên cứu
Mapping: Điều 4.1.b & 5.1 QĐ543 — Các Khoa/Phòng ban có nhu cầu NC ứng dụng sẽ đăng đơn đặt hàng để Giảng viên 'nhận thầu'.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
cycle_id	INT FK	NO	FK tới research_cycles — Thuộc đợt nào	Mỗi đơn đặt hàng gắn với một đợt nhận hồ sơ cụ thể.
ordering_unit_id	INT FK	NO	FK tới organizational_units — Đơn vị đặt hàng (Khoa/Ban)	Điều 4.1.b: Đặt hàng phải do lãnh đạo FE từ cấp Khối/Viện trở lên. FK đảm bảo tính hợp lệ.
problem_description	NVARCHAR(MAX)	NO	Mô tả vấn đề cần nghiên cứu/giải quyết	PI đọc để quyết định có nộp đề cương theo đơn đặt hàng này không.
matched_proposal_id	UNIQUEIDENTIFIER FK NULL	YES	FK tới proposals — Đề cương đã được chọn để thực hiện đơn hàng này	Sau khi PI nộp đề cương và được chọn, Staff link đề cương vào đơn đặt hàng này để tracking.
status	NVARCHAR(20)	NO	OPEN → MATCHED → CLOSED	Track trạng thái của đơn đặt hàng. MATCHED khi đã có đề cương được phê duyệt.
 
4.5. DOMAIN 5 — Proposals (Hồ sơ Đề cương)
Mục đích: Số hóa 100% Biểu mẫu 01 (Đề cương nghiên cứu đề tài NCKH cấp Trường). Domain này là core business của hệ thống.

4.5.1. Bảng proposals — Thông tin chính Đề cương
Mapping: Biểu mẫu 01, Điều 6 & 7 QĐ543. Lưu toàn bộ phần I (Thông tin chung) và phần II (Mục tiêu, Nội dung, Sản phẩm) của BM01.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
id	UNIQUEIDENTIFIER	NO	Khóa chính UUID	UUID tránh expose total count trong URL. An toàn hơn IDENTITY INT.
pi_user_id	UNIQUEIDENTIFIER FK	NO	FK tới users — Chủ nhiệm đề tài (Principal Investigator)	Biểu mẫu 01 mục 3: Thông tin về chủ nhiệm đề tài. Điều 7: PI phải có Thạc sĩ trở lên (validate qua academic_profiles.is_eligible_pi).
track_id	INT FK	NO	FK tới research_tracks — Hướng nghiên cứu	Phân loại đề tài theo hướng NC để dễ tìm chuyên gia và xét duyệt.
duration_months	INT	NO	Thời gian thực hiện (tháng)	Biểu mẫu 01 mục 2: Thời gian thực hiện KHÔNG QUÁ 12 tháng. Backend validate: duration_months <= 12.
abstract_vi	NVARCHAR(MAX)	NO	Tóm tắt đề tài bằng Tiếng Việt	Bắt buộc theo Biểu mẫu 01. AI feature (Task 5) dùng field này để generate summary.
abstract_en	NVARCHAR(MAX) NULL	YES	Tóm tắt đề tài bằng Tiếng Anh	Điều 6.4.a QĐ543: Trong trường hợp cần thiết, có thể yêu cầu nộp đề cương bằng Tiếng Anh.
methodology	NVARCHAR(MAX) NULL	YES	Phương pháp nghiên cứu	BM01 mục 5: Nêu rõ PPNC gắn với từng nội dung chính. Rubric tiêu chí 2 (PP Nghiên cứu) dựa vào field này.
novelty_originality	NVARCHAR(MAX) NULL	YES	Tính mới, tính độc đáo, tính sáng tạo của đề tài	BM01 mục 5.2: Bắt buộc ghi rõ tính mới. Rubric tiêu chí 3 (Nội dung & Kết quả) đánh giá field này.
status	NVARCHAR(30)	NO	DRAFT → SUBMITTED → UNDER_REVIEW → REVISION_REQUESTED → APPROVED → REJECTED	Quản lý lifecycle của đề cương từ lúc draft đến khi được phê duyệt hoặc từ chối.
revision_deadline	DATETIME2 NULL	YES	Hạn chót PI phải nộp lại sau khi HĐ yêu cầu chỉnh sửa	HĐ xét duyệt có thể yêu cầu PI sửa đổi. Cần deadline để tránh treo trạng thái vô thời hạn.

4.5.2. Bảng proposal_team_members — Danh sách thành viên nhóm NC
Mapping: Biểu mẫu 01 mục 5 — Danh sách Thành viên đề tài (tối đa 10 người kể cả chủ nhiệm).

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
user_id	UNIQUEIDENTIFIER FK NULL	YES	FK tới users. NULL nếu là thành viên ngoài FPT	Cho phép bổ sung thành viên ngoài FPT (external collaborators) theo Điều 1.2.b.
work_months	DECIMAL(4,1)	NO	Số tháng quy đổi tham gia NC. VD: 3.5	BM01 mục 5: Ghi số tháng quy đổi (1 tháng = 22 ngày × 8 giờ). Dùng để tính thù lao trong proposal_budget_labor_details.
is_pi	BIT	NO	TRUE nếu là Chủ nhiệm đề tài	Phân biệt PI với thành viên thường. Hệ thống dùng để validate: chỉ được 1 PI per proposal.
is_secretary	BIT	NO	TRUE nếu là Thư ký đề tài	BM01 mục 4: Phân biệt Thư ký đề tài với thành viên thường.
sequence	INT	NO	Thứ tự trong danh sách (UI order)	Đảm bảo thứ tự nhất quán khi hiển thị danh sách thành viên.

4.5.3. Bảng proposal_budget & proposal_budget_labor_details — Dự toán kinh phí
Mapping: Biểu mẫu 01 Phần IV — Kinh phí thực hiện. proposal_budget lưu tổng hợp, proposal_budget_labor_details lưu chi tiết thù lao từng người.
Điểm đặc biệt: Dùng team_member_id (FK) thay vì nhập tên tự do, đảm bảo tiền thù lao chỉ trả cho người có trong danh sách đề tài.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
team_member_id	INT FK	NO	FK tới proposal_team_members — Người nhận thù lao	QUAN TRỌNG: Bảo vệ tính toàn vẹn tài chính. Ép buộc tiền lương chỉ được trả cho thành viên chính thức trong danh sách đề tài (Biểu mẫu 01 mục 5). Tránh gian lận.
total_research_hours	DECIMAL(10,2)	NO	Tổng số giờ tham gia NC	BM01 Bảng dự trù chi tiết thù lao NC.
hourly_rate	DECIMAL(10,2)	NO	Mức thù lao trên 1 giờ NC (VNĐ/giờ)	Theo QĐ 187/QĐ-CTGDFPT về Quy định chi trả thu nhập cho giảng viên.
total_amount	AS (total_research_hours * hourly_rate) PERSISTED	NO	Thành tiền — Computed column tự tính	Persisted computed column: tự động tính total_amount = hours × rate khi INSERT/UPDATE, không cần code logic riêng.

4.5.4. Bảng proposal_activities — Kế hoạch triển khai
Mapping: Biểu mẫu 01 mục 8 — Tóm tắt kế hoạch và lộ trình triển khai NC.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
content_id	INT FK	NO	FK tới proposal_research_contents — Thuộc Nội dung NC nào	BM01 cấu trúc: Nội dung 1 → Hoạt động 1.1, 1.2,... Quan hệ cha-con.
activity_type	NVARCHAR(30)	NO	Loại hoạt động. NORMAL / SEMINAR / MILESTONE	Tích hợp đăng ký Seminar (Biểu mẫu 08) trực tiếp vào bảng hoạt động, tránh tạo bảng riêng.
requires_approval	BIT	NO	Hoạt động này cần Staff phê duyệt không	VD: SEMINAR type cần Staff phê duyệt (Biểu mẫu 08 cần chữ ký 3 bên). NORMAL activities không cần.
 
4.6. DOMAIN 6 — Review Process (Xét duyệt & Nghiệm thu)
Mục đích: Số hóa quy trình lập hội đồng, mời chuyên gia ngoài, họp online và chấm điểm theo Điều 8 và 12 QĐ543.

4.6.1. Bảng review_councils — Quản lý Hội đồng
Mapping: Điều 8 (HĐ Xét duyệt) và Điều 12 (HĐ Nghiệm thu) QĐ543. Một bảng phục vụ cả 2 loại hội đồng để tái sử dụng logic.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
council_type	NVARCHAR(30)	NO	PROPOSAL_REVIEW (Xét duyệt đề cương) hoặc ACCEPTANCE (Nghiệm thu)	Phân biệt 2 loại HĐ theo Điều 8 và 12. Mỗi loại có cấu trúc thành viên và quy trình khác nhau.
meeting_deadline	DATE NULL	YES	Hạn chót phải hoàn thành phiên họp HĐ	Điều 8.3.a: Phải tổ chức họp HĐ chậm nhất 15 ngày làm việc sau QĐ thành lập. Field này cho hệ thống gửi reminder tự động.
min_members_required	INT	NO	Số thành viên tối thiểu. Mặc định 3	Điều 8.2: HĐ xét duyệt từ 03-05 thành viên. Điều 12.2: HĐ nghiệm thu từ 05-07 thành viên.
quorum_numerator / quorum_denominator	INT	NO	Tỷ lệ số đại biểu tối thiểu để họp hợp lệ. VD: 2/3	Điều 8.3.b & 12.3.b: Tham dự ít nhất 2/3 số thành viên. Lưu linh hoạt để có thể thay đổi nếu QĐ mới.

4.6.2. Bảng council_members — Thành viên Hội đồng
Mapping: Điều 8.2 & 12.2 QĐ543 — Mời chuyên gia nội/ngoại. Task Package 3 (DOCX) — External expert invitation workflow.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
member_role	NVARCHAR(30)	NO	CHAIR (Chủ tịch) / SECRETARY (Thư ký) / REVIEWER / ATTENDEE	Phân biệt vai trò trong HĐ để áp dụng mức thù lao đúng (Phụ lục 02) và quyền chấm điểm.
invitation_token	NVARCHAR(500) UNIQUE NULL	YES	Token bảo mật gửi qua email cho chuyên gia ngoài	QUAN TRỌNG: Task Package 3. External experts không có email FPT nên không thể dùng SSO. Hệ thống sinh token duy nhất gửi qua email để họ click vào chấm điểm trực tiếp mà không cần đăng ký tài khoản.
token_expires_at	DATETIME2 NULL	YES	Thời hạn hết hiệu lực của invitation token	Token hết hạn sau X ngày để bảo mật. Staff có thể resend token mới.
is_external	BIT	NO	TRUE = Chuyên gia ngoài FPT	Điều 8.2: HĐ gồm chuyên gia có trình độ, kinh nghiệm trong lĩnh vực (không bắt buộc là CBGV FPT).
status	NVARCHAR(20)	NO	INVITED → CONFIRMED / DECLINED → SCORING_DONE	Track trạng thái từng thành viên HĐ. Nếu declined, Staff cần mời người khác bù.

4.6.3. Bảng council_meetings — Lịch họp Hội đồng
Mapping: Điều 8.3 & 12.3 QĐ543 — Phương thức họp HĐ. Task Package 4 (DOCX) — Tích hợp Google Meet/Microsoft Teams API.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
platform	NVARCHAR(20)	NO	IN_PERSON / GOOGLE_MEET / MS_TEAMS	Task Package 4: Hệ thống hỗ trợ họp online qua Google Meet hoặc Teams. Cho phép Committee họp từ xa.
external_meeting_id	NVARCHAR(500) NULL	YES	ID cuộc họp từ Google Meet/Teams API. VD: meet.google.com/abc-xyz-123	Dùng để gọi API hủy, cập nhật lịch họp trên nền tảng tương ứng khi có thay đổi.
calendar_event_id	NVARCHAR(500) NULL	YES	ID sự kiện trên Google Calendar/Outlook để sync	Task Package 4: Sync với Google Calendar/Microsoft Outlook để tạo reminder tự động cho các thành viên HĐ.
agenda_documents	NVARCHAR(MAX) NULL	YES	JSON list URL các tài liệu đính kèm trong lời mời họp	Điều 8.3 & 12.3: HĐ cần xem trước đề cương/hồ sơ nghiệm thu trước khi họp. Gửi kèm link qua email mời.

4.6.4. Bảng proposal_review_scores & review_score_details — Phiếu chấm điểm
Mapping: Biểu mẫu 03 QĐ543 — Phiếu đánh giá thẩm định Đề cương (100 điểm chia thành 5 tiêu chí). Thiết kế động: score_details lưu điểm từng tiêu chí theo Rubric động.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
template_id	INT FK	NO	FK tới rubric_templates — Dùng bộ tiêu chí nào để chấm	Liên kết với Rubric động. Nếu Staff chọn template khác (vd: năm 2027 Rubric mới 7 tiêu chí), chấm điểm vẫn hoạt động đúng mà không cần sửa code.
ai_feedback_suggestion	NVARCHAR(MAX) NULL	YES	Gợi ý nhận xét do AI tạo ra cho Giám khảo	Task Package 5: LLM phân tích đề cương và gợi ý nhận xét dựa trên Rubric để hỗ trợ Giám khảo chưa quen với lĩnh vực.
is_valid_ballot	BIT	NO	Phiếu chấm hợp lệ hay không	Biểu mẫu 04: Phân biệt Phiếu hợp lệ / Không hợp lệ. Dùng để tính điểm trung bình cuối cùng của HĐ.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
criterion_id	INT FK	NO	FK tới rubric_criteria — Tiêu chí nào được chấm	Lưu điểm từng tiêu chí riêng biệt thay vì tổng điểm. Cho phép phân tích: PI yếu ở tiêu chí nào, cần cải thiện gì.
given_score	DECIMAL(5,2)	NO	Điểm Giám khảo cho tiêu chí này	Biểu mẫu 03: Cột Điểm đánh giá. Validate: 0 ≤ given_score ≤ rubric_criteria.max_score.
comments	NVARCHAR(MAX) NULL	YES	Nhận xét chi tiết của Giám khảo cho tiêu chí này	Cho PI biết cụ thể cần cải thiện điểm nào. Cũng là dữ liệu để AI học cách gợi ý nhận xét tốt hơn.

4.6.5. Bảng acceptance_evaluations & reviewer_feedbacks — Đánh giá nghiệm thu
Mapping: Biểu mẫu 11 (Phiếu đánh giá nghiệm thu — Đạt/Không đạt) và Biểu mẫu 10 (Nhận xét đề tài dành cho người phản biện).

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
result	NVARCHAR(20)	NO	PASS (Đạt) hoặc FAIL (Không đạt)	Biểu mẫu 11: Phiếu đánh giá nghiệm thu chỉ có 2 kết quả Đạt/Không đạt theo Điều 12.3.b.
fail_reason	NVARCHAR(MAX) NULL	YES	Lý do Không đạt (bắt buộc khi result=FAIL)	Bắt buộc ghi lý do khi kết quả là FAIL để PI biết cần bổ sung gì. Backend validate: fail_reason NOT NULL khi result='FAIL'.
is_valid_ballot	BIT	NO	Phiếu hợp lệ hay không	Biểu mẫu 12: Phân biệt phiếu hợp lệ/không hợp lệ khi kiểm phiếu. Dùng để tính kết quả chính thức của HĐ.
 
4.7. DOMAIN 7 — Contracts & Disbursements (Hợp đồng & Giải ngân)
Mục đích: Số hóa quy trình ký kết, giải ngân và thanh lý hợp đồng NCKH theo Điều 9, 16, 17 QĐ543.

4.7.1. Bảng contracts — Hợp đồng NCKH điện tử
Mapping: Điều 9 QĐ543 & Biểu mẫu 05 — Hợp đồng Nghiên cứu Khoa học cấp Trường. Hợp đồng được ký điện tử qua nền tảng Econtract.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
proposal_id	UNIQUEIDENTIFIER UNIQUE FK	NO	FK 1-1 tới proposals. Mỗi đề cương chỉ có 1 hợp đồng	Điều 9.1: Hiệu trưởng phê duyệt xong → Phòng QLKH ký kết Hợp đồng với từng Chủ nhiệm đề tài.
original_end_date	DATE	NO	Ngày kết thúc gốc theo hợp đồng ban đầu	Điều 10.4: Gia hạn tối đa 1/2 tổng thời gian thực hiện. Cần lưu ngày gốc để tính max_extension_months chính xác khi có nhiều lần gia hạn.
max_extension_months	INT	NO	Số tháng gia hạn tối đa theo luật	Điều 10.4: = duration_months / 2. Tính tự động và lưu vào đây để backend validate khi PI xin gia hạn.
econtract_id	NVARCHAR(500) NULL	YES	ID hợp đồng trên nền tảng Econtract	Biểu mẫu 05 Điều 7: Hợp đồng được ký điện tử qua phần mềm Econtract. Field này cho phép tra cứu/verify trên hệ thống Econtract.
side_a_representative	NVARCHAR(200)	YES	Đại diện Bên A (FPT). Mặc định: Nguyễn Kim Ánh	Biểu mẫu 05: BÊN GIAO THỰC HIỆN ĐỀ TÀI đại diện là bà Nguyễn Kim Ánh (Trưởng ban NC&PT). Lưu tên để in hợp đồng.
terminated_reason	NVARCHAR(MAX) NULL	YES	Lý do chấm dứt hợp đồng trước thời hạn	Biểu mẫu 05 Điều 5.1.h: Bên A có quyền chấm dứt HĐ nếu PI vi phạm. Cần ghi rõ lý do để audit.

4.7.2. Bảng contract_disbursements — Giải ngân theo đợt
Mapping: Điều 16 QĐ543 — Quy trình giải ngân: Đề tài ứng dụng 4 đợt (30%-30%-30%-10%). Đề tài cơ bản: 1 đợt sau nghiệm thu Đạt.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
round_number	INT	NO	Số thứ tự đợt giải ngân (1-4 cho ứng dụng, 1 cho cơ bản)	UNIQUE(contract_id, round_number) đảm bảo không trùng lặp đợt.
condition_description	NVARCHAR(MAX)	NO	Điều kiện để được giải ngân đợt này	Copy từ disbursement_templates khi tạo hợp đồng. VD Đợt 2: 'Sau khi giai đoạn 1 đánh giá tiến độ Đạt'.
condition_met_by	UNIQUEIDENTIFIER FK NULL	YES	Staff QLKH xác nhận điều kiện giải ngân đã được đáp ứng	QUAN TRỌNG: Điều 16.1.b: Phải có xác nhận 'Đạt' mới giải ngân đợt tiếp. Staff phải click confirm trên hệ thống trước khi Kế toán xử lý tiền.
bank_reference	NVARCHAR(200) NULL	YES	Mã giao dịch ngân hàng khi giải ngân thực tế	Điều 13.1.d: Bộ hồ sơ KP bao gồm toàn bộ hoá đơn, chứng từ. Lưu bank reference để đối chiếu kế toán.

4.7.3. Bảng contract_settlements — Biên bản thanh lý hợp đồng
Mapping: Biểu mẫu 13 QĐ543 — Biên bản nghiệm thu & thanh lý Hợp đồng NCKH cấp Trường.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
total_returned_amount	DECIMAL(15,2)	NO	Tổng số tiền phải hoàn trả (nếu đề tài bị đình chỉ)	Biểu mẫu 05 Điều 5.2.h: PI hoàn trả kinh phí chưa sử dụng nếu HĐ bị chấm dứt. Kế toán dùng để xử lý hoàn tiền.
settlement_deadline	DATE NULL	YES	Hạn chót hoàn thành thủ tục thanh lý	Điều 13.3: Toàn bộ thủ tục giao nộp phải hoàn tất trong 60 ngày làm việc kể từ ngày kết thúc nghiệm thu.
accounting_cleared_at	DATE NULL	YES	Ngày Ban Kế toán xác nhận đã quyết toán	Điều 13.1.e: Cần xác nhận của Ban Kế toán về việc đề tài đã quyết toán KP và xử lý tài sản.
 
4.8. DOMAIN 8 — Progress Monitoring & Archival (Tiến độ & Gia hạn)
Mục đích: Theo dõi tiến độ thực hiện đề tài, xử lý yêu cầu gia hạn/thay đổi và quản lý hồ sơ nghiệm thu.

4.8.1. Bảng progress_reports — Báo cáo tiến độ định kỳ
Mapping: Điều 10 & Biểu mẫu 06 QĐ543 — Báo cáo tiến độ định kỳ: 2 lần/đề tài ứng dụng, 1 lần/đề tài cơ bản.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
report_round	INT	NO	Số thứ tự báo cáo tiến độ (1, 2,...)	Điều 10.1: Đánh giá tiến độ 2 lần đối với ứng dụng, 1 lần với cơ bản. UNIQUE(contract_id, report_round) đảm bảo không trùng lặp.
overall_completion_pct	DECIMAL(5,2)	NO	% hoàn thành tổng thể (0-100)	BM06 mục 4: Bảng tổng hợp tỷ lệ hoàn thành. Dùng để Staff đánh giá có đủ điều kiện giải ngân đợt tiếp theo không.
expenditure_to_date	DECIMAL(15,2)	NO	Tổng kinh phí đã chi đến thời điểm báo cáo	BM06 mục 5: Kinh phí đã chi. Kế toán đối chiếu với số giải ngân đã thực hiện để phát hiện bất thường.
evaluation_result	NVARCHAR(20) NULL	YES	Kết quả đánh giá: PASS (Đạt) / FAIL (Không đạt) / CONDITIONAL	Điều 10.3: Kết quả đánh giá tiến độ là căn cứ để Hiệu trưởng quyết định giải ngân đợt tiếp theo. FK ràng buộc với contract_disbursements.condition_met_by.

4.8.2. Bảng amendment_requests — Phiếu xin thay đổi/gia hạn
Mapping: Điều 10 & Biểu mẫu 07 QĐ543 — Phiếu đề nghị thay đổi nội dung, nhân sự, thời gian, kinh phí trong quá trình thực hiện đề tài.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
category_id	INT FK	NO	FK tới amendment_categories — Loại thay đổi (gia hạn, đổi nhân sự, điều chỉnh KP,...)	Động hóa loại thay đổi. Backend định tuyến đúng workflow phê duyệt theo loại.
change_percentage	DECIMAL(5,2) NULL	YES	% nội dung nghiên cứu bị thay đổi (0-100)	Điều 10.5: Nếu thay đổi từ 50% nội dung trở lên → cần trình Hiệu trưởng duyệt. Backend tự tính và set requires_rector_approval=1 khi field này ≥ 50.
requires_rector_approval	BIT	NO	TRUE = Cần Hiệu trưởng phê duyệt (thay vì Staff duyệt)	Điều 10.5 QĐ543: Thay đổi ≥50% nội dung → Phòng QLKH PHẢI trình Hiệu trưởng xem xét. Cờ này tự động đẩy hồ sơ lên workflow cấp cao hơn.
old_value / new_value	NVARCHAR(MAX) NULL	YES	Giá trị cũ và mới của nội dung cần thay đổi (JSON format)	BM07 mục 5,6,7: Ghi rõ những thay đổi về nội dung NC, tiến độ, dự toán KP. Lưu dạng JSON để linh hoạt với mọi loại thay đổi.

4.8.3. Bảng final_reports — Báo cáo tổng kết đề tài
Mapping: Điều 11 & Biểu mẫu 09 QĐ543 — Báo cáo tổng kết đề tài (Hồ sơ nghiệm thu bắt buộc phải có).

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
language	NVARCHAR(10)	NO	Ngôn ngữ báo cáo: VI hoặc EN	Điều 13.1.a: Có thể viết bằng tiếng Việt hoặc tiếng Anh. BM09 cũng ghi chú tương tự.
archival_deadline	DATE NULL	YES	Hạn chót nộp lưu trữ (60 ngày từ ngày nghiệm thu)	Điều 13.3: Tất cả thủ tục phải hoàn tất trong 60 ngày. Hệ thống tự tính deadline và gửi reminder.
revision_notes	NVARCHAR(MAX) NULL	YES	Ghi chú của HĐ yêu cầu PI chỉnh sửa báo cáo	Điều 13.1.a: Báo cáo được chỉnh sửa và bổ sung theo ý kiến HĐ nghiệm thu trước khi nộp lưu.
 
4.9. DOMAIN 9 — AI/LLM & Polymorphic Supporting Tables
Mục đích: Hỗ trợ tích hợp AI (Task 5), quản lý file tập trung (Azure Blob), gửi thông báo và hỗ trợ semantic search.

4.9.1. Bảng llm_outputs — Kết quả AI
Mapping: Task Package 5 (DOCX) — LLM tóm tắt đề cương, gợi ý nhận xét cho Giám khảo, phân tích tiến độ.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
entity_type + entity_id	NVARCHAR(50/100)	NO	Polymorphic FK: đây là output của thực thể nào. entity_type='PROPOSAL', entity_id='{uuid}'	Một bảng phục vụ tất cả loại AI output (tóm tắt đề cương, gợi ý nhận xét, phân tích báo cáo) thay vì tạo nhiều bảng riêng.
output_type	NVARCHAR(50)	NO	Loại output AI. VD: PROPOSAL_SUMMARY, REVIEWER_SUGGESTION, PROGRESS_ANALYSIS	Phân loại để query: lấy tất cả summary của 1 đề cương, hoặc tất cả gợi ý cho 1 giám khảo.
tokens_input / tokens_output	INT NULL	YES	Số token đầu vào và đầu ra	Đo lường chi phí gọi API của xAI/OpenAI (tính tiền theo token). Dùng để báo cáo cost analytics cho Admin.
is_reviewed_by_human	BIT	NO	Đã được con người review và xác nhận hay chưa	AI output có thể sai. Flag này giúp Staff biết output nào cần review. Các output chưa review không được hiển thị công khai.

4.9.2. Bảng semantic_search_vectors — Chỉ mục tìm kiếm ngữ nghĩa
Mapping: Task Package 5 — Semantic search để tìm đề tài tương tự, expert phù hợp, feedback liên quan. Kết nối với Elasticsearch/Vector DB.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
content_hash	NVARCHAR(64)	NO	SHA-256 hash của nội dung đã index	Khi nội dung đề cương thay đổi (PI cập nhật abstract), hệ thống so sánh hash để biết cần re-index vector hay không. Tránh index thừa, tiết kiệm chi phí.
elasticsearch_doc_id	NVARCHAR(200) NULL	YES	Document ID trong Elasticsearch/Vector DB	Dùng để update/delete document trên Elasticsearch khi nội dung thay đổi hoặc entity bị xóa.
index_status	NVARCHAR(20)	NO	PENDING → INDEXED → FAILED	Async indexing: khi PI nộp đề cương, hệ thống queue job index. Status này track tiến trình job.

4.9.3. Bảng documents — Quản lý File tập trung
Mapping: Điều 13 QĐ543 — Lưu trữ tất cả loại tài liệu (PDF đề cương, PDF hợp đồng, biên bản họp, sản phẩm nghiệm thu,...) trên Azure Blob Storage.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
entity_type + entity_id	NVARCHAR(50/100)	NO	Polymorphic FK: entity_type='PROPOSAL' / 'CONTRACT' / 'MEETING', entity_id=ID của thực thể	1 bảng quản lý tất cả file thay vì mỗi domain tự có bảng file riêng. entity_id NVARCHAR(100) đảm bảo hoạt động với cả UUID và INT.
document_category	NVARCHAR(100)	NO	Loại tài liệu. VD: PROPOSAL_FORM, CONTRACT_SIGNED, MEETING_MINUTES, FINAL_REPORT	Filter tài liệu theo loại. VD: 'Hiển thị tất cả hợp đồng đã ký' hay 'Tất cả biên bản họp HĐ'.
storage_container + storage_blob_name	NVARCHAR(200/1000)	NO	Container và blob name trên Azure Blob Storage	Không lưu file trực tiếp trong DB (quá nặng). Lưu metadata trong SQL, file lên Azure Blob. Truy cập qua storage_url.
is_confidential	BIT	NO	Tài liệu mật (chỉ Staff/Admin mới xem được)	Một số tài liệu HĐ có thông tin tài chính nhạy cảm, không phải mọi thành viên đề tài đều được xem.

4.9.4. Bảng notifications — Thông báo hệ thống
Mapping: Task Package 6 (DOCX) — Real-time notifications cho deadline, meeting invitations, feedback availability, acceptance outcomes.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
notification_type	NVARCHAR(100)	NO	Loại thông báo. VD: SUBMISSION_DEADLINE, MEETING_INVITE, SCORE_AVAILABLE, DISBURSEMENT_APPROVED	Frontend dùng để render icon và màu sắc phù hợp theo từng loại thông báo.
action_url	NVARCHAR(1000) NULL	YES	Deep link URL dẫn đến màn hình liên quan	Khi người dùng click thông báo, navigate thẳng đến trang liên quan (vd: /proposals/{id}/review) thay vì trang chủ.
related_entity_id	NVARCHAR(100) NULL	YES	Polymorphic: ID của thực thể liên quan	Đã FIX POLYMORPHIC BUG: NVARCHAR(100) chứa được cả UUID (36 ký tự) và INT, không bị lỗi type mismatch.
priority	NVARCHAR(20)	NO	Mức độ ưu tiên: LOW / NORMAL / HIGH / URGENT	URGENT (hết hạn nộp trong 24h) hiển thị màu đỏ và push notification. NORMAL chỉ hiển thị trong notification feed.
 
4.10. DOMAIN 10 — Logs (Nhật ký hệ thống)
Mục đích: Lưu vết toàn bộ thay đổi dữ liệu và hoạt động email để đảm bảo tính minh bạch, kiểm tra được (auditability).

4.10.1. Bảng audit_logs — Nhật ký thay đổi dữ liệu
Mapping: Task Package 6 (Analytics & Reporting) — Đảm bảo tính minh bạch tuyệt đối cho nền tảng quản lý ngân sách Trường.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
action	NVARCHAR(100)	NO	Hành động. VD: CREATE, UPDATE, DELETE, APPROVE, REJECT, DISBURSE	Phân loại hành động để query nhanh: 'Tất cả lần approve đề tài trong tháng 5'.
old_values / new_values	NVARCHAR(MAX) NULL	YES	Snapshot JSON của dữ liệu trước và sau khi thay đổi	QUAN TRỌNG: Bất kỳ ai (kể cả Admin) lén sửa điểm thi hoặc số tiền dự toán đều bị ghi log vĩnh viễn với old_values/new_values. Đây là bằng chứng pháp lý.
ip_address	NVARCHAR(50) NULL	YES	IP của người thực hiện hành động	Trace xâm nhập bất hợp pháp hoặc hành vi đáng ngờ theo địa chỉ IP.

4.10.2. Bảng email_logs — Nhật ký email gửi đi
Mapping: Task Package 6 — Automated notifications cho deadlines, meeting invitations, feedback availability theo DOCX requirements.

Field Name	Data Type	Nullable	Description	Lý do / Mapping QĐ543
email_type	NVARCHAR(100)	NO	Loại email. VD: COUNCIL_INVITATION, DEADLINE_REMINDER, CONTRACT_SIGNED	Phân loại để báo cáo: 'Đã gửi bao nhiêu email mời HĐ trong quý này?'
template_name	NVARCHAR(100) NULL	YES	Tên email template đã dùng	Dễ identify khi cần debug lỗi nội dung email hoặc A/B test template.
status	NVARCHAR(20)	NO	QUEUED → SENT / FAILED	Track trạng thái gửi. Nếu FAILED, có thể retry. Staff xem dashboard biết email invitation đã tới tay chuyên gia chưa.
provider_message_id	NVARCHAR(500) NULL	YES	Message ID từ email provider (SendGrid/Azure Email)	Dùng để tra cứu delivery status trực tiếp trên SendGrid/Azure nếu cần debug.
 
5. BẢNG MAPPING TUÂN THỦ QĐ 543/QĐ-ĐHFPT (COMPLIANCE MATRIX)
Bảng dưới đây thể hiện từng Điều khoản trong QĐ 543 và Biểu mẫu được số hóa trong database FURPMS V1.3:

Điều khoản / Biểu mẫu	Tables ánh xạ	Nội dung số hóa
Điều 4 — Phân loại đề tài cấp Trường	research_types, research_tracks	Loại hình NC (Ứng dụng/Cơ bản), max_budget_cap, require_ordering_unit, require_publication
Điều 5 — Đặt hàng/Đề xuất đề tài	research_cycles, research_orders	Đợt nhận hồ sơ (Quý I/II), đơn đặt hàng từ các đơn vị FE, deadline
Điều 6 — Đăng ký thực hiện đề tài	research_cycles, proposals, documents	Submission_open_date, deadline, hồ sơ (BM01+BM02), nộp đề cương tiếng Anh (abstract_en)
Điều 7 — Chủ nhiệm đề tài	academic_profiles, users	is_eligible_pi (validate Thạc sĩ trở lên), phối hợp với QLKH, is_external
Điều 8 — Hội đồng Xét duyệt đề cương	review_councils, council_members, council_meetings, proposal_review_scores, rubric_templates, rubric_criteria, review_score_details	QĐ thành lập HĐ, 3-5 thành viên, meeting_deadline (15 ngày), quorum (2/3), chấm điểm động, invitation_token cho external
Điều 9 — Ký kết và triển khai đề tài	contracts, contract_disbursements	Hợp đồng điện tử (Econtract), side_a_representative, điều khoản từ BM05
Điều 10 — Báo cáo tiến độ & Điều chỉnh	progress_reports, progress_report_items, amendment_requests	Báo cáo định kỳ (BM06), xin thay đổi (BM07), gia hạn tối đa 1/2 thời gian, requires_rector_approval khi ≥50% nội dung
Điều 11 — Tổ chức Nghiệm thu	product_deliverables, final_reports, review_councils (ACCEPTANCE)	Nộp sản phẩm, báo cáo tổng kết (BM09), hồ sơ nghiệm thu đầy đủ
Điều 12 — Hội đồng nghiệm thu	review_councils, acceptance_evaluations, reviewer_feedbacks, council_decisions	HĐ nghiệm thu 5-7 thành viên, bỏ phiếu Đạt/Không đạt (BM11), biên bản họp (BM12)
Điều 13 — Lưu trữ kết quả & sản phẩm	contract_settlements, documents, final_reports	Nộp bản mềm, sản phẩm, hồ sơ KP, xác nhận Kế toán, trong 60 ngày (archival_deadline)
Điều 14 — Kinh phí cấp cho Đề tài	research_types.max_budget_cap	100M (cơ bản) / 150M (ứng dụng). Hiệu trưởng phê duyệt vượt mức
Điều 15 — Dự toán kinh phí	budget_allocation_rules, proposal_budget, proposal_budget_labor_details	Tỷ lệ phân bổ linh hoạt (Thù lao 100%, TB/VT 60%,...), validate PI form, team_member_id integrity
Điều 16 — Giải ngân, thanh toán, quyết toán	disbursement_templates, contract_disbursements	Kịch bản giải ngân mẫu (4 đợt 30%-30%-30%-10%), condition_met_by Staff xác nhận trước khi giải ngân
Điều 17 & 18 — Thanh toán, thù lao HĐ	contract_settlements, council_remuneration_rates	Quyết toán trong năm tài chính (15/12), mức thù lao HĐ xét duyệt/nghiệm thu từ Phụ lục 02
Biểu mẫu 01 — Đề cương NC	proposals, proposal_team_members, proposal_budget, proposal_budget_labor_details, proposal_research_contents, proposal_activities, proposal_expected_products	100% Phần I-IV của BM01: Thông tin chung, Mục tiêu, Sản phẩm, Kinh phí
Biểu mẫu 02 — Lý lịch khoa học	academic_profiles	Toàn bộ BM02: học hàm/vị, ISI/Scopus count, quá trình đào tạo, công trình KH
Biểu mẫu 03 — Phiếu thẩm định Đề cương	rubric_templates, rubric_criteria, proposal_review_scores, review_score_details	5 tiêu chí động, điểm thành phần, nhận xét chung, AI suggestion
Biểu mẫu 04 — Biên bản họp HĐ Xét duyệt	council_meetings, council_decisions, meeting_attendances	Thành phần HĐ, kết quả bỏ phiếu, phiếu hợp lệ/không hợp lệ, kết luận HĐ
Biểu mẫu 05 — Hợp đồng NCKH	contracts, contract_disbursements	Nội dung công việc, sản phẩm, thời gian 12 tháng, giải ngân 4 đợt, Econtract
Biểu mẫu 06 — Báo cáo tiến độ	progress_reports, progress_report_items	Nội dung đã/chưa hoàn thành, % hoàn thành, kinh phí đã chi, kiến nghị
Biểu mẫu 07 — Phiếu đề nghị thay đổi	amendment_requests	Thay đổi nội dung/tiến độ/nhân sự/KP, requires_rector_approval khi ≥50%
Biểu mẫu 08 — Đăng ký seminar	proposal_activities (activity_type=SEMINAR)	Tích hợp vào bảng hoạt động, chủ đề, thời gian, kinh phí, requires_approval
Biểu mẫu 09 — Báo cáo tổng kết	final_reports	File báo cáo, file tóm tắt, ngôn ngữ VI/EN, archival_deadline
Biểu mẫu 10 — Nhận xét nghiệm thu (PB)	reviewer_feedbacks	Tính cấp thiết, đóng góp KH, ý nghĩa thực tiễn, kết quả thực tế vs kỳ vọng
Biểu mẫu 11 — Phiếu đánh giá nghiệm thu	acceptance_evaluations	Đánh giá Đạt/Không đạt, lý do không đạt, phiếu hợp lệ
Biểu mẫu 12 — Biên bản họp HĐ Nghiệm thu	council_decisions (ACCEPTANCE type)	Thành viên có mặt/vắng, bỏ phiếu, kết quả, kiến nghị
Biểu mẫu 13 — Biên bản thanh lý HĐ	contract_settlements	Sản phẩm giao nộp, tổng KP, số tiền hoàn trả, xác nhận Kế toán, Econtract
 
6. TỔNG KẾT (SUMMARY)

Tổng số Domain	10 Domain nghiệp vụ độc lập
Tổng số Tables	44 bảng dữ liệu
Ước tính số cột	~220 cột với đầy đủ data type và constraints
Primary Keys	UNIQUEIDENTIFIER (UUID) cho thực thể nghiệp vụ chính; INT IDENTITY cho lookup/config tables
Foreign Keys	Đầy đủ FK constraints với ON DELETE/UPDATE rules phù hợp
Indexes	Composite indexes trên (entity_type, entity_id) cho polymorphic tables; index trên (user_id, is_read) cho notifications
Collation	Vietnamese_CI_AS — Hỗ trợ tiếng Việt có dấu, case-insensitive
Soft Delete	is_deleted + deleted_at + deleted_by cho users và proposals
Audit Trail	audit_logs với old_values/new_values JSON cho mọi thay đổi quan trọng
Tích hợp AI	llm_outputs + semantic_search_vectors + llm_configs (Task Package 5)
Tích hợp Online Meeting	council_meetings.platform/external_meeting_id/calendar_event_id (Task Package 4)
External Expert Access	council_members.invitation_token (Task Package 3) — không cần SSO FPT
Tuân thủ QĐ543	100% Điều 4-18 và 15 Biểu mẫu được số hóa


Cơ sở dữ liệu FURPMS V1.3 không chỉ đáp ứng 100% các yêu cầu functional trong tài liệu Capstone SU26SE053 mà còn xử lý trọn vẹn các quy định hành chính và luật tài chính khắt khe nhất của QĐ 543/QĐ-ĐHFPT. Với kiến trúc Configuration-Driven, hệ thống sẵn sàng mở rộng và triển khai cho các đơn vị giáo dục khác dưới dạng một mô hình Enterprise SaaS đích thực.

─── Hết Báo cáo ───
