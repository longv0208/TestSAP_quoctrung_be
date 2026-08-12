using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FURPMS.Application.Constants;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class DocumentExportService : IDocumentExportService
{
    private readonly IProposalRepository _proposals;
    private readonly IMasterDataRepository _masterData;
    private readonly IContractRepository _contracts;

    public DocumentExportService(
        IProposalRepository proposals,
        IMasterDataRepository masterData,
        IContractRepository contracts)
    {
        _proposals = proposals;
        _masterData = masterData;
        _contracts = contracts;
    }

    // ── Scientific (.docx) ──────────────────────────────────────────────────

    public async Task<(byte[] Content, string FileName)> ExportScientificDocAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query()
            .IgnoreQueryFilters()
            .Include(p => p.Project).ThenInclude(pr => pr.PiUser).ThenInclude(u => u.AcademicProfile)
            .Include(p => p.Project).ThenInclude(pr => pr.PiUser).ThenInclude(u => u.Unit)
            .Include(p => p.Project).ThenInclude(pr => pr.HostingUnit)
            .Include(p => p.Project).ThenInclude(pr => pr.ResearchType)
            .Include(p => p.Budget)
            .Include(p => p.Project).ThenInclude(pr => pr.Members)
            .Include(p => p.ResearchContents).ThenInclude(c => c.Activities)
            .Include(p => p.Project).ThenInclude(pr => pr.Deliverables).ThenInclude(ep => ep.Category)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException($"Proposal {proposalId} not found.");

        var secretary = proposal.Project.Members.FirstOrDefault(m => m.IsSecretary);

        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            // ── Cover ────────────────────────────────────────────────────────
            AppendHeading(body, "THUYẾT MINH NHIỆM VỤ NGHIÊN CỨU KHOA HỌC VÀ CÔNG NGHỆ", 24, true, JustificationValues.Center);
            AppendParagraph(body, $"Tên nhiệm vụ: {proposal.TitleVi}", bold: true, fontSize: 14);
            AppendParagraph(body, $"Chủ nhiệm nhiệm vụ: {proposal.Project.PiUser.FullName}", bold: true, fontSize: 12);
            AppendParagraph(body, "");

            // ── Abstract ─────────────────────────────────────────────────────
            AppendHeading(body, "TÓM TẮT NỘI DUNG NHIỆM VỤ", 14, bold: true);
            AppendParagraph(body, proposal.AbstractVi);
            if (!string.IsNullOrWhiteSpace(proposal.AbstractEn))
                AppendParagraph(body, proposal.AbstractEn);
            AppendParagraph(body, "");

            // ── Part I: General information ───────────────────────────────────
            AppendHeading(body, "PHẦN I. THÔNG TIN CHUNG VỀ NHIỆM VỤ", 13, bold: true);

            var generalTable = CreateTable(body, new[] { "Mục", "Nội dung" }, new[] { 2000, 7000 });
            AddTableRow(generalTable, "1. Tên nhiệm vụ", proposal.TitleVi, bold0: true);
            AddTableRow(generalTable, "2. Dạng nhiệm vụ", proposal.Project.ResearchType?.Name ?? "—", bold0: true);
            AddTableRow(generalTable, "4. Thời gian thực hiện",
                $"{proposal.DurationMonths} tháng ({proposal.PlannedStartDate:MM/yyyy} – {proposal.PlannedEndDate:MM/yyyy})",
                bold0: true);
            AddTableRow(generalTable, "6. Cơ quan chủ trì", proposal.Project.HostingUnit?.Name ?? "—", bold0: true);
            AddTableRow(generalTable, "7. Tổng kinh phí",
                proposal.Budget != null
                    ? $"{proposal.Budget.TotalAmount:N0} VNĐ"
                    : "Chưa nhập",
                bold0: true);
            AddTableRow(generalTable, "8. Phương thức khoán chi",
                proposal.FundingMethod == FundingMethod.Whole ? "☑ Khoán đến sản phẩm cuối cùng  ☐ Khoán từng phần"
                : proposal.FundingMethod == FundingMethod.Partial ? "☐ Khoán đến sản phẩm cuối cùng  ☑ Khoán từng phần"
                : "—",
                bold0: true);

            AppendParagraph(body, "");

            // ── PI info ───────────────────────────────────────────────────────
            AppendHeading(body, "9. Chủ nhiệm nhiệm vụ", 12, bold: true);
            var pi = proposal.Project.PiUser;
            var piProfile = pi.AcademicProfile;
            var piTable = CreateTable(body, new[] { "Thông tin", "Giá trị" }, new[] { 3000, 6000 });
            AddTableRow(piTable, "Họ và tên", pi.FullName);
            AddTableRow(piTable, "Ngày tháng năm sinh", piProfile?.DateOfBirth?.ToString("dd/MM/yyyy") ?? "—");
            AddTableRow(piTable, "Giới tính", piProfile?.Gender ?? "—");
            AddTableRow(piTable, "Học hàm, học vị", piProfile?.AcademicTitle ?? "—");
            AddTableRow(piTable, "Chuyên ngành", piProfile?.Specialization ?? "—");
            AddTableRow(piTable, "Tên cơ quan đang công tác", piProfile?.Institution ?? pi.Unit?.Name ?? "—");
            AddTableRow(piTable, "Địa chỉ cơ quan", piProfile?.InstitutionAddress ?? "—");
            AddTableRow(piTable, "Điện thoại di động", pi.Phone ?? "—");
            AddTableRow(piTable, "Email", pi.Email);

            AppendParagraph(body, "");

            // ── Secretary info ────────────────────────────────────────────────
            AppendHeading(body, "10. Thư ký nhiệm vụ", 12, bold: true);
            if (secretary != null)
            {
                var secTable = CreateTable(body, new[] { "Thông tin", "Giá trị" }, new[] { 3000, 6000 });
                AddTableRow(secTable, "Họ và tên", secretary.FullName);
                AddTableRow(secTable, "Học hàm, học vị", secretary.AcademicTitle ?? "—");
                AddTableRow(secTable, "Cơ quan công tác", secretary.UnitName ?? "—");
                AddTableRow(secTable, "Nội dung công việc", secretary.WorkContent);
            }
            else
            {
                AppendParagraph(body, "(Chưa có thư ký)");
            }

            AppendParagraph(body, "");

            // ── Team members table ────────────────────────────────────────────
            AppendHeading(body, "13. Danh sách cá nhân thực hiện nhiệm vụ", 12, bold: true);
            var teamTable = CreateTable(body,
                new[] { "STT", "Họ và tên, Chức danh", "Vai trò", "Đơn vị", "Nội dung công việc", "Số tháng" },
                new[] { 500, 2200, 1200, 2000, 2500, 600 });

            int seq = 1;
            foreach (var m in proposal.Project.Members.OrderBy(m => m.Sequence))
            {
                var role = m.IsPi ? "Chủ nhiệm" : m.IsSecretary ? "Thư ký" : m.MemberRoleCode ?? "Thành viên";
                AddTableRowMulti(teamTable, new[]
                {
                    seq.ToString(),
                    string.IsNullOrWhiteSpace(m.AcademicTitle) ? m.FullName : $"{m.AcademicTitle}. {m.FullName}",
                    role,
                    m.UnitName ?? "—",
                    m.WorkContent,
                    m.WorkMonths.ToString("G")
                });
                seq++;
            }

            AppendParagraph(body, "");

            // ── Part II: Task description ─────────────────────────────────────
            AppendHeading(body, "PHẦN II. MÔ TẢ NHIỆM VỤ", 13, bold: true);

            // 14 — Tổng quan tình hình nghiên cứu, tính cấp thiết
            AppendSectionIfAny(body, "14. Tổng quan tình hình nghiên cứu, luận giải tính cấp thiết", proposal.LiteratureReview);

            // 15 — Objectives
            AppendHeading(body, "15. Mục tiêu nhiệm vụ", 12, bold: true);
            AppendParagraph(body, proposal.ResearchObjectives);
            AppendParagraph(body, "");

            // 16 — Tính mới, sáng tạo
            AppendSectionIfAny(body, "16. Tính mới, tính sáng tạo", proposal.NoveltyOriginality);

            // 17 — Research contents
            AppendHeading(body, "17. Nội dung nghiên cứu", 12, bold: true);
            foreach (var rc in proposal.ResearchContents.OrderBy(c => c.Sequence))
            {
                AppendParagraph(body, $"• Nội dung {rc.ContentNumber}: {rc.Title}", bold: true);
                if (!string.IsNullOrWhiteSpace(rc.Description))
                    AppendParagraph(body, rc.Description);
            }
            AppendParagraph(body, "");

            // 18 — Timeline + Gantt
            AppendHeading(body, "18. Nội dung nghiên cứu, tiến độ thực hiện", 12, bold: true);
            var activities = proposal.ResearchContents
                .OrderBy(c => c.Sequence)
                .SelectMany(c => c.Activities.OrderBy(a => a.Sequence))
                .ToList();

            if (activities.Count > 0)
            {
                var timeTable = CreateTable(body,
                    new[] { "STT", "Tên nội dung nghiên cứu", "Kết quả cần đạt", "Thời gian (tháng)" },
                    new[] { 500, 3500, 3500, 1500 });
                int ai = 1;
                foreach (var a in activities)
                {
                    AddTableRowMulti(timeTable, new[]
                    {
                        ai.ToString(),
                        a.ActivityName,
                        a.ExpectedResult,
                        $"{a.StartMonth}–{a.EndMonth}"
                    });
                    ai++;
                }

                // Gantt header — up to DurationMonths, max 24
                AppendParagraph(body, "");
                AppendParagraph(body, "Thể hiện bằng đồ thị Gantt:", bold: true);
                int maxMonth = Math.Min(proposal.DurationMonths, 24);
                var ganttCols = new[] { "TT", "Công việc" }.Concat(
                    Enumerable.Range(1, maxMonth).Select(m => m.ToString())).ToArray();
                var ganttWidths = new[] { 400, 2500 }.Concat(
                    Enumerable.Repeat(400, maxMonth)).ToArray();
                var ganttTable = CreateTable(body, ganttCols, ganttWidths);

                ai = 1;
                foreach (var a in activities)
                {
                    var ganttRow = new string[2 + maxMonth];
                    ganttRow[0] = ai.ToString();
                    ganttRow[1] = a.ActivityName;
                    for (int m = 1; m <= maxMonth; m++)
                        ganttRow[m + 1] = (m >= a.StartMonth && m <= a.EndMonth) ? "X" : "";
                    AddTableRowMulti(ganttTable, ganttRow);
                    ai++;
                }
            }
            else
            {
                AppendParagraph(body, "(Chưa có hoạt động)");
            }

            AppendParagraph(body, "");

            // 21 — Expected products
            AppendHeading(body, "21. Dự kiến kết quả nghiên cứu của nhiệm vụ", 12, bold: true);
            if (proposal.Project.Deliverables.Any())
            {
                var prodTable = CreateTable(body,
                    new[] { "STT", "Tên sản phẩm", "Tiêu chí đánh giá", "Dạng sản phẩm" },
                    new[] { 500, 3500, 3000, 2000 });
                int pi2 = 1;
                foreach (var ep in proposal.Project.Deliverables.OrderBy(p => p.Sequence))
                {
                    AddTableRowMulti(prodTable, new[]
                    {
                        pi2.ToString(),
                        ep.ProductName,
                        ep.ScientificRequirements ?? "—",
                        ep.Category?.Name ?? ep.Notes ?? "—"
                    });
                    pi2++;
                }
            }
            else
            {
                AppendParagraph(body, "(Chưa có sản phẩm dự kiến)");
            }
            AppendParagraph(body, "");

            // 22–24 — Khả năng ứng dụng / chuyển giao / cơ sở vật chất
            AppendSectionIfAny(body, "22. Khả năng ứng dụng kết quả nghiên cứu", proposal.ApplicationPotential);
            AppendSectionIfAny(body, "23. Khả năng chuyển giao công nghệ", proposal.TransferPotential);
            AppendSectionIfAny(body, "24. Cơ sở vật chất, trang thiết bị phục vụ nghiên cứu", proposal.FacilitiesEquipment);

            mainPart.Document.Save();
        }

        var code = proposal.Project?.ProjectCode ?? proposalId.ToString("N")[..8].ToUpper();
        return (ms.ToArray(), $"ThuyetMinh_{code}.docx");
    }

    // ── Budget (.xlsx) ──────────────────────────────────────────────────────

    public async Task<(byte[] Content, string FileName)> ExportBudgetDocAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query()
            .IgnoreQueryFilters()
            .Include(p => p.Budget)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException($"Proposal {proposalId} not found.");

        var categories = await _masterData.BudgetExpenseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Sequence)
            .ToListAsync();

        var items = await _proposals.BudgetItems
            .Where(i => i.ProposalId == proposalId)
            .ToListAsync();

        var laborDetails = await _proposals.LaborDetails
            .Include(d => d.ProjectMember)
            .Where(d => d.ProposalId == proposalId)
            .OrderBy(d => d.Sequence)
            .ToListAsync();

        var disbursements = await _contracts.Disbursements
            .Include(d => d.Contract)
            .Where(d => d.Contract.Project.Proposals.Any(pp => pp.Id == proposalId))
            .OrderBy(d => d.RoundNumber)
            .ToListAsync();

        using var wb = new XLWorkbook();

        // ── Sheet 1: Tong hop ─────────────────────────────────────────────────
        var ws1 = wb.Worksheets.Add("Tong hop");

        ws1.Cell(1, 1).Value = "III. PHÂN BỔ KINH PHÍ THỰC HIỆN";
        ws1.Cell(1, 1).Style.Font.Bold = true;
        ws1.Cell(2, 1).Value = "24. Cơ cấu phân bổ kinh phí";
        ws1.Cell(3, 1).Value = "Đơn vị tính: Đồng";

        // header row
        int r = 4;
        ws1.Cell(r, 1).Value = "STT";
        ws1.Cell(r, 2).Value = "Nội dung các khoản chi";
        ws1.Cell(r, 3).Value = "Tổng kinh phí";
        ws1.Cell(r, 4).Value = "Khoán chi";
        ws1.Cell(r, 5).Value = "Ngoài khoán";
        ws1.Cell(r, 6).Value = "NSNN";
        ws1.Cell(r, 7).Value = "Khác";
        var headerRange = ws1.Range(r, 1, r, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r++;

        // build lookup: CategoryId → BudgetItem
        var itemLookup = items.ToDictionary(i => i.CategoryId);

        decimal grandTotal = 0m;
        foreach (var cat in categories)
        {
            itemLookup.TryGetValue(cat.Id, out var item);
            decimal total = item?.Amount ?? 0m;
            grandTotal += total;

            ws1.Cell(r, 1).Value = cat.Sequence;
            ws1.Cell(r, 2).Value = cat.Name;
            ws1.Cell(r, 3).Value = (double)total;
            ws1.Cell(r, 4).Value = (double)(item?.SourceKhoan ?? 0m);
            ws1.Cell(r, 5).Value = (double)(item?.SourceNgoaiKhoan ?? 0m);
            ws1.Cell(r, 6).Value = (double)(item?.SourceNsnn ?? 0m);
            ws1.Cell(r, 7).Value = (double)(item?.SourceOther ?? 0m);

            // number format
            for (int c = 3; c <= 7; c++)
                ws1.Cell(r, c).Style.NumberFormat.Format = "#,##0";
            r++;
        }

        // total row
        ws1.Cell(r, 2).Value = "Tổng cộng";
        ws1.Cell(r, 2).Style.Font.Bold = true;
        ws1.Cell(r, 3).Value = (double)grandTotal;
        ws1.Cell(r, 3).Style.Font.Bold = true;
        ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
        r++;

        // disbursement schedule
        if (disbursements.Count > 0)
        {
            r++;
            ws1.Cell(r, 1).Value = "25. Kế hoạch phân bổ kinh phí";
            ws1.Cell(r, 1).Style.Font.Bold = true;
            r++;
            ws1.Cell(r, 2).Value = "Phân bổ kinh phí";
            ws1.Cell(r, 2).Style.Font.Bold = true;
            for (int d = 0; d < disbursements.Count; d++)
                ws1.Cell(r, 3 + d).Value = $"Đợt {disbursements[d].RoundNumber}";
            r++;
            ws1.Cell(r, 2).Value = "Ngân sách Nhà nước";
            for (int d = 0; d < disbursements.Count; d++)
            {
                ws1.Cell(r, 3 + d).Value = (double)disbursements[d].PlannedAmount;
                ws1.Cell(r, 3 + d).Style.NumberFormat.Format = "#,##0";
            }
            r++;
            ws1.Cell(r, 2).Value = "Tổng cộng";
            ws1.Cell(r, 2).Style.Font.Bold = true;
            for (int d = 0; d < disbursements.Count; d++)
            {
                ws1.Cell(r, 3 + d).Value = (double)disbursements[d].PlannedAmount;
                ws1.Cell(r, 3 + d).Style.NumberFormat.Format = "#,##0";
                ws1.Cell(r, 3 + d).Style.Font.Bold = true;
            }
        }

        ws1.Columns().AdjustToContents();

        // ── Sheet 2: Tong hop tien cong ───────────────────────────────────────
        var ws2 = wb.Worksheets.Add("Tong hop tien cong");

        ws2.Cell(1, 1).Value = "V. GIẢI TRÌNH CÁC KHOẢN CHI";
        ws2.Cell(1, 1).Style.Font.Bold = true;
        ws2.Cell(2, 1).Value = "BẢNG TỔNG HỢP THANH TOÁN TIỀN CÔNG TRỰC TIẾP";
        ws2.Cell(2, 1).Style.Font.Bold = true;

        r = 4;
        ws2.Cell(r, 1).Value = "STT";
        ws2.Cell(r, 2).Value = "Họ và tên";
        ws2.Cell(r, 3).Value = "Chức danh";
        ws2.Cell(r, 4).Value = "Tổng số ngày công";
        ws2.Cell(r, 5).Value = "Hệ số tiền công/ngày";
        ws2.Cell(r, 6).Value = "Mức lương cơ bản ngày (đồng)";
        ws2.Cell(r, 7).Value = "Đơn giá ngày (đồng)";
        ws2.Cell(r, 8).Value = "Tổng tiền (đồng)";
        var h2Range = ws2.Range(r, 1, r, 8);
        h2Range.Style.Font.Bold = true;
        h2Range.Style.Fill.BackgroundColor = XLColor.LightGray;
        h2Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r++;

        decimal totalLaborCost = 0m;
        int lSeq = 1;

        // Try to get BASE_DAILY_SALARY for reference
        var baseDaily = await _masterData.SystemFinancialConfigs
            .Where(c => c.Code == "BASE_DAILY_SALARY" && c.IsActive)
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        foreach (var detail in laborDetails)
        {
            decimal dailyRate = detail.DailyRate ?? 0m;
            decimal computedTotal = (detail.WorkDays ?? 0m) * dailyRate;
            totalLaborCost += computedTotal;

            ws2.Cell(r, 1).Value = lSeq;
            ws2.Cell(r, 2).Value = detail.ProjectMember.FullName;
            ws2.Cell(r, 3).Value = detail.ProjectMember.MemberRoleCode ?? "—";
            ws2.Cell(r, 4).Value = (double)(detail.WorkDays ?? 0m);
            ws2.Cell(r, 5).Value = (double)(detail.Coefficient ?? 0m);
            if (baseDaily > 0m) ws2.Cell(r, 6).Value = (double)baseDaily;
            else ws2.Cell(r, 6).SetValue("—");
            if (dailyRate > 0m) ws2.Cell(r, 7).Value = (double)dailyRate;
            else ws2.Cell(r, 7).SetValue("—");
            ws2.Cell(r, 8).Value = (double)computedTotal;

            ws2.Cell(r, 6).Style.NumberFormat.Format = "#,##0";
            ws2.Cell(r, 7).Style.NumberFormat.Format = "#,##0";
            ws2.Cell(r, 8).Style.NumberFormat.Format = "#,##0";
            r++;
            lSeq++;
        }

        // total row
        ws2.Cell(r, 2).Value = "Tổng cộng";
        ws2.Cell(r, 2).Style.Font.Bold = true;
        ws2.Cell(r, 8).Value = (double)totalLaborCost;
        ws2.Cell(r, 8).Style.Font.Bold = true;
        ws2.Cell(r, 8).Style.NumberFormat.Format = "#,##0";

        ws2.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var code = proposal.Project?.ProjectCode ?? proposalId.ToString("N")[..8].ToUpper();
        return (ms.ToArray(), $"DuToan_{code}.xlsx");
    }

    // ── OpenXml helpers ─────────────────────────────────────────────────────

    // ── Contract (.docx) — BM05 ─────────────────────────────────────────────
    public async Task<(byte[] Content, string FileName)> ExportContractDocAsync(Guid contractId)
    {
        var c = await _contracts.Query()
            .Include(x => x.Project).ThenInclude(p => p.PiUser).ThenInclude(u => u.AcademicProfile)
            .Include(x => x.Project).ThenInclude(p => p.HostingUnit)
            .FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        // Điều 2 (sản phẩm) và Điều 4 (đợt giải ngân) phải là dữ liệu THẬT của hợp đồng này,
        // không phải câu mẫu chung chung.
        var deliverables = await _contracts.Deliverables
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.Sequence).ToListAsync();
        var disbursements = await _contracts.Disbursements
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.RoundNumber).ToListAsync();

        using var ms = new MemoryStream();
        using (var docx = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = docx.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            /*
             * BM05 — Hợp đồng nghiên cứu khoa học cấp trường (QĐ543, PDF tr.22-26).
             *
             * Trước đây bản xuất ra chỉ là MỘT BẢNG TÓM TẮT 6 dòng + ô ký — không phải hợp đồng,
             * đem đi ký không được. Thầy 05/08: "phải tự tạo hợp đồng xong có thể kiểm tra lại xong
             * xuất ra mới đưa cho bên ký, chứ không phải người ta tự tạo tự ký ở ngoài rồi nộp lên".
             *
             * Nay giữ NGUYÊN VĂN toàn bộ chữ của mẫu (căn cứ pháp lý, Điều 1-7, ô ký) và điền dữ
             * liệu hệ thống vào đúng chỗ trống. Mục nào hệ thống CHƯA có dữ liệu (số tài khoản,
             * CCCD của Bên B — DB chưa có cột) thì để dấu chấm lửng như bản giấy, ký ngoài điền tay.
             */
            var pi = c.Project?.PiUser;
            var titleVi = c.Project?.TitleVi ?? c.ScopeTitle ?? "…";
            var money = c.TotalAmount > 0 ? $"{c.TotalAmount:N0}" : "…………";
            const string Blank = "……………………………";

            AppendParagraph(body, "CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM", bold: true, fontSize: 12);
            AppendParagraph(body, "Độc lập – Tự do – Hạnh phúc");
            AppendParagraph(body, "***");
            AppendParagraph(body, "");
            AppendHeading(body, $"HỢP ĐỒNG NGHIÊN CỨU KHOA HỌC CẤP TRƯỜNG NĂM {c.StartDate.Year}",
                16, bold: true, justify: JustificationValues.Center);
            AppendParagraph(body, $"Số: {c.ContractNumber}/QLKH-FEHO");
            AppendParagraph(body, "(Dùng cho việc giao khoán thực hiện Đề tài NCKH cấp Trường với Chủ nhiệm đề tài)");
            AppendParagraph(body, "");

            foreach (var can in new[]
            {
                "Căn cứ Bộ luật Dân sự số 91/2015/QH13 ngày 24/11/2015;",
                "Căn cứ Luật Khoa học và Công nghệ số 29/2013/QH13 ngày 18/6/2013;",
                "Căn cứ Luật Sở hữu trí tuệ số 50/2005/QH11 ngày 29/11/2005 và Luật Sửa đổi, bổ sung một số điều của Luật Sở hữu trí tuệ số 07/2022/QH15 ngày 16/6/2022;",
                "Căn cứ Quyết định số 543/QĐ-ĐHFPT của Hiệu trưởng Trường Đại học FPT về Quy định quản lý đề tài nghiên cứu khoa học cấp Trường;",
                "Căn cứ thuyết minh đề cương nghiên cứu của đề tài đã được phê duyệt."
            })
                AppendParagraph(body, can);

            AppendParagraph(body, "");
            AppendParagraph(body, "Chúng tôi gồm:", bold: true);
            AppendParagraph(body, "BÊN GIAO THỰC HIỆN ĐỀ TÀI (BÊN A): TRƯỜNG ĐẠI HỌC FPT", bold: true);
            AppendParagraph(body, $"Đại diện là: {c.SideARepresentative ?? Blank}");
            AppendParagraph(body, "Chức vụ: Trưởng ban Nghiên cứu và Phát triển                Mã số thuế: 0102100740");
            AppendParagraph(body, "Địa chỉ: Khu Giáo dục và Đào tạo, Khu Công nghệ cao Hoà Lạc, Km29 Đại lộ Thăng Long, huyện Thạch Thất, Hà Nội.");
            AppendParagraph(body, "");
            AppendParagraph(body, "BÊN NHẬN TỔ CHỨC CHỦ TRÌ THỰC HIỆN ĐỀ TÀI (BÊN B):", bold: true);
            AppendParagraph(body, $"CHỦ NHIỆM ĐỀ TÀI: {pi?.FullName ?? Blank}");
            AppendParagraph(body, $"Đơn vị công tác: {c.Project?.HostingUnit?.Name ?? Blank}");
            AppendParagraph(body, $"Điện thoại: {pi?.Phone ?? "………………"}          Email: {pi?.Email ?? "………………"}");
            // C3 — tài khoản ngân hàng & CCCD của Bên B do CHÍNH CHỦ tự khai trong hồ sơ cá nhân.
            // Đây là chỗ DUY NHẤT số đầy đủ rời khỏi hệ thống; mọi đường đọc qua API đều bị che.
            // Chưa khai thì để dấu chấm lửng đúng như bản giấy, ký ngoài điền tay — TUỲ CHỌN, không
            // chặn việc lập hợp đồng (chốt 08/08). Căn cứ thu thập: BM05 Điều 7.2 (chứng thư số).
            var identity = pi?.AcademicProfile;
            var bankNo = string.IsNullOrWhiteSpace(identity?.BankAccountNumber) ? Blank : identity!.BankAccountNumber!;
            // Mẫu BM05 đã in sẵn chữ "tại Ngân hàng …", mà người khai thường gõ cả cụm
            // "Ngân hàng TMCP …" ⇒ ghép thẳng ra "tại Ngân hàng Ngân hàng TMCP…". Bỏ tiền tố trùng.
            var bankRaw = identity?.BankName?.Trim();
            if (!string.IsNullOrEmpty(bankRaw) && bankRaw.StartsWith("Ngân hàng ", StringComparison.OrdinalIgnoreCase))
                bankRaw = bankRaw["Ngân hàng ".Length..].TrimStart();
            var bankName = string.IsNullOrWhiteSpace(bankRaw) ? Blank : bankRaw;
            var idNo = string.IsNullOrWhiteSpace(identity?.NationalId) ? Blank : identity!.NationalId!;
            var idPlace = string.IsNullOrWhiteSpace(identity?.NationalIdIssuedPlace) ? Blank : identity!.NationalIdIssuedPlace!;
            var idDate = identity?.NationalIdIssuedDate is { } issued
                ? $"Cấp ngày {issued:dd} tháng {issued:MM} năm {issued:yyyy}"
                : "Cấp ngày … tháng … năm ……";
            AppendParagraph(body, $"Số tài khoản: {bankNo} tại Ngân hàng {bankName}");
            AppendParagraph(body, $"Số CCCD: {idNo} {idDate} tại {idPlace}");
            AppendParagraph(body, "");
            AppendParagraph(body, "Cùng thỏa thuận và thống nhất ký kết hợp đồng thực hiện Đề tài nghiên cứu khoa học cấp Trường (sau đây gọi tắt là Hợp đồng) với những điều khoản như sau:");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 1. NỘI DUNG CÔNG VIỆC", 13);
            AppendParagraph(body, $"Bên B cam kết tổ chức triển khai thực hiện đề tài nghiên cứu khoa học cấp Trường năm {c.StartDate.Year} theo đúng tiến độ đã đăng ký trong thuyết minh đề cương được phê duyệt.");
            AppendParagraph(body, $"- Tên đề tài: {titleVi}");
            AppendParagraph(body, $"- Mã số: {c.ContractNumber}");
            AppendParagraph(body, "Đề cương là bộ phận không tách rời của Hợp đồng.");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 2. SẢN PHẨM CỦA ĐỀ TÀI", 13);
            AppendParagraph(body, "Bên B phải nộp cho Bên A sản phẩm nghiên cứu khoa học bao gồm:");
            AppendParagraph(body, "- 01 bản mềm Báo cáo tổng kết đề tài;");
            AppendParagraph(body, "- 01 bản mềm toàn bộ nội dung sản phẩm;");
            AppendParagraph(body, "- Xác nhận bằng văn bản của đơn vị thụ hưởng về việc tiếp nhận, thử nghiệm sản phẩm và đánh giá chất lượng.");
            if (deliverables.Count > 0)
            {
                AppendParagraph(body, "Danh mục sản phẩm đã đăng ký trong đề cương:");
                var td = CreateTable(body, new[] { "TT", "Tên sản phẩm", "Thời hạn nộp" }, new[] { 800, 6200, 2000 });
                for (int i = 0; i < deliverables.Count; i++)
                    AddTableRowMulti(td, new[]
                    {
                        (i + 1).ToString(),
                        deliverables[i].ProductName,
                        deliverables[i].DueDate?.ToString("dd/MM/yyyy") ?? "—"
                    });
            }
            AppendParagraph(body, "Toàn bộ thủ tục giao nộp sản phẩm phải hoàn tất trong 30 ngày kể từ ngày nghiệm thu.");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 3. THỜI GIAN THỰC HIỆN HỢP ĐỒNG", 13);
            var months = ((c.EndDate.Year - c.StartDate.Year) * 12) + c.EndDate.Month - c.StartDate.Month;
            AppendParagraph(body, $"Thời gian thực hiện Đề tài là: {(months > 0 ? months : 12)} tháng");
            AppendParagraph(body, $"Từ tháng {c.StartDate.Month} năm {c.StartDate.Year} đến tháng {c.EndDate.Month} năm {c.EndDate.Year}.");
            AppendParagraph(body, "Thời gian trên đã bao gồm thời gian nghiệm thu sản phẩm.");
            if (c.EndDate != c.OriginalEndDate)
                AppendParagraph(body, $"(Đã gia hạn — thời hạn theo hợp đồng gốc: {c.OriginalEndDate:dd/MM/yyyy})");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 4. GIÁ TRỊ HỢP ĐỒNG VÀ PHƯƠNG THỨC THANH TOÁN", 13);
            AppendParagraph(body, $"4.1. Tổng giá trị Hợp đồng: tổng kinh phí thực hiện đề tài là {money} đồng.");
            AppendParagraph(body, "4.2. Kinh phí này bao gồm các khoản đóng góp nghĩa vụ theo quy định hiện hành và được chia làm các đợt giải ngân:");
            if (disbursements.Count > 0)
            {
                var tt = CreateTable(body, new[] { "Đợt", "Điều kiện giải ngân", "Thời điểm dự kiến" }, new[] { 900, 6100, 2000 });
                foreach (var d in disbursements)
                    AddTableRowMulti(tt, new[]
                    {
                        d.RoundNumber.ToString(),
                        string.IsNullOrWhiteSpace(d.ConditionDescription) ? "Theo tiến độ thực hiện" : d.ConditionDescription,
                        d.ConditionMetAt?.ToString("dd/MM/yyyy") ?? "—"
                    });
            }
            else
            {
                AppendParagraph(body, "- Đợt 1: Tối đa 30% tổng kinh phí, để Bên B thực hiện nội dung theo Đề cương.");
                AppendParagraph(body, "- Đợt 2, Đợt 3: Tối đa 30% mỗi đợt, sau khi đánh giá tiến độ đạt yêu cầu.");
                AppendParagraph(body, "- Đợt 4: Kinh phí còn lại, sau khi đề tài được công nhận kết quả \"Đạt\".");
            }
            AppendParagraph(body, "4.3. Phương thức thanh toán: thanh toán từng đợt theo kết quả đánh giá tiến độ; việc thanh quyết toán thực hiện theo quy trình của Ban Kế toán.");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 5. TRÁCH NHIỆM CỦA MỖI BÊN", 13);
            AppendParagraph(body, "5.1. Bên A: cung cấp thông tin cần thiết; cấp kinh phí theo tiến độ khi Bên B đáp ứng yêu cầu của Đề cương; kiểm tra định kỳ hoặc đột xuất; tổ chức đánh giá, nghiệm thu và thanh lý Hợp đồng theo quy định.");
            AppendParagraph(body, "5.2. Bên B: tổ chức thực hiện đúng nội dung, tiến độ và kinh phí đã đăng ký; báo cáo tiến độ định kỳ; giao nộp sản phẩm đúng hạn; hoàn trả kinh phí chưa sử dụng nếu Đề tài bị đình chỉ hoặc Hợp đồng bị chấm dứt do lỗi của Bên B.");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 6. ĐIỀU KHOẢN CHUNG", 13);
            AppendParagraph(body, "6.1. Trong quá trình thực hiện Hợp đồng, nếu một trong hai bên có yêu cầu sửa đổi, bổ sung nội dung hoặc có căn cứ để chấm dứt thực hiện Hợp đồng phải thông báo cho bên kia ít nhất 15 ngày trước.");
            AppendParagraph(body, "6.2. Hai bên cam kết thực hiện đúng các quy định của Hợp đồng và hợp tác giải quyết các vướng mắc phát sinh.");
            AppendParagraph(body, "6.3. Mọi tranh chấp được các bên thương lượng hoà giải; không hoà giải được thì đưa ra cơ quan có thẩm quyền giải quyết.");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 7. HIỆU LỰC CỦA HỢP ĐỒNG", 13);
            AppendParagraph(body, "7.1. Hợp đồng này có hiệu lực từ ngày ký.");
            AppendParagraph(body, "7.2. Hợp đồng được thực hiện qua phương thức ký điện tử trên phần mềm Econtract; Bên B ủy quyền cho Trường Đại học FPT khai báo thông tin định danh để cấp chứng thư số.");
            AppendParagraph(body, "7.3. Các bên tự bảo quản và lưu trữ Hợp đồng trên thiết bị điện tử của từng Bên./.");
            AppendParagraph(body, "");
            AppendParagraph(body, "");

            var sign = CreateTable(body, new[] { "ĐẠI DIỆN BÊN A", "CHỦ NHIỆM ĐỀ TÀI (BÊN B)" }, new[] { 4500, 4500 });
            AddTableRow(sign, "(Ký, ghi rõ họ tên)", "(Ký, ghi rõ họ tên)");
            AddTableRow(sign, "\n\n\n" + (c.SideARepresentative ?? ""), "\n\n\n" + (pi?.FullName ?? ""));
        }

        var code = c.ContractNumber.Replace("/", "-").Replace(" ", "_");
        return (ms.ToArray(), $"HopDong_{code}.docx");
    }

    // ── Biên bản nghiệm thu & thanh lý hợp đồng — BM13 ──────────────────────

    /// <summary>
    /// Sinh <b>Biên bản nghiệm thu &amp; thanh lý hợp đồng (BM13)</b> — QĐ543 <b>Điều 13.2</b>:
    /// <i>"Phòng QLKH … tổ chức ký kết Biên bản thanh lý hợp đồng thực hiện đề tài NCKH (Biểu mẫu 13)"</i>.
    /// <para>
    /// Trước đây quyết toán chỉ có hai nút đánh dấu, <b>không có văn bản nào để ký</b> — mà Điều 13.2
    /// coi việc ký biên bản này mới là chốt sổ đề tài. Giữ nguyên văn chữ của mẫu, điền dữ liệu thật
    /// vào chỗ trống; chỗ nào hệ thống chưa có thì để dấu chấm lửng như bản giấy để điền tay.
    /// </para>
    /// </summary>
    public async Task<(byte[] Content, string FileName)> ExportSettlementDocAsync(Guid contractId)
    {
        var c = await _contracts.Query()
            .Include(x => x.Project).ThenInclude(p => p.PiUser)
            .Include(x => x.Project).ThenInclude(p => p.HostingUnit)
            .FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        var settlement = await _contracts.Settlements
            .FirstOrDefaultAsync(x => x.ContractId == contractId);

        var deliverables = await _contracts.Deliverables
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.Sequence).ToListAsync();

        var disbursed = await _contracts.Disbursements
            .Where(d => d.ContractId == contractId && d.Status == "DISBURSED")
            .SumAsync(d => (decimal?)(d.ActualAmount ?? d.PlannedAmount)) ?? 0m;

        using var ms = new MemoryStream();
        using (var docx = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = docx.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            var pi = c.Project?.PiUser;
            const string Blank = "……………………………";
            var today = DateTime.UtcNow;
            var signedOn = c.SignedAt ?? today;

            AppendParagraph(body, "CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM", bold: true, fontSize: 12);
            AppendParagraph(body, "Độc Lập – Tự Do – Hạnh Phúc");
            AppendParagraph(body, "***");
            AppendParagraph(body, "");
            AppendHeading(body, "BIÊN BẢN NGHIỆM THU & THANH LÝ HỢP ĐỒNG", 16, bold: true,
                justify: JustificationValues.Center);
            AppendHeading(body, $"NGHIÊN CỨU KHOA HỌC CẤP TRƯỜNG NĂM {c.EndDate.Year}", 14, bold: true,
                justify: JustificationValues.Center);
            AppendParagraph(body, "");

            AppendParagraph(body, "Căn cứ Bộ luật Dân sự số 91/2015/QH13 ngày 24/11/2015;");
            AppendParagraph(body, "Căn cứ Luật Khoa học và Công nghệ số 29/2013/QH13 ngày 18/6/2013;");
            AppendParagraph(body,
                $"Căn cứ Hợp đồng NCKH số: {c.ContractNumber}/QLKH-FEHO giữa Trường Đại học FPT với " +
                $"Chủ nhiệm đề tài ký ngày {signedOn:dd} tháng {signedOn:MM} năm {signedOn:yyyy}.");
            AppendParagraph(body, "");
            AppendParagraph(body,
                $"Hôm nay, ngày {today:dd} tháng {today:MM} năm {today:yyyy}, tại Trường Đại học FPT, chúng tôi gồm:");
            AppendParagraph(body, "");

            AppendParagraph(body, "BÊN GIAO THỰC HIỆN ĐỀ TÀI (BÊN A): TRƯỜNG ĐẠI HỌC FPT", bold: true);
            AppendParagraph(body, $"Đại diện là: {c.SideARepresentative ?? Blank}");
            AppendParagraph(body, "Chức vụ: Trưởng ban Nghiên cứu và Phát triển    Mã số thuế: 0102100740");
            AppendParagraph(body,
                "Địa chỉ: Khu Giáo dục và Đào tạo, Khu Công nghệ cao Hoà Lạc, Km29 Đại lộ Thăng Long, huyện Thạch Thất, Hà Nội.");
            AppendParagraph(body, "");

            AppendParagraph(body, "BÊN NHẬN TỔ CHỨC CHỦ TRÌ THỰC HIỆN ĐỀ TÀI (BÊN B):", bold: true);
            AppendParagraph(body, $"Chủ nhiệm đề tài: {pi?.FullName ?? Blank}");
            AppendParagraph(body, $"Đơn vị công tác: {c.Project?.HostingUnit?.Name ?? Blank}");
            AppendParagraph(body, $"Điện thoại: {pi?.Phone ?? Blank}    Email: {pi?.Email ?? Blank}");
            AppendParagraph(body, $"Số tài khoản: {Blank} tại Ngân hàng {Blank}");
            AppendParagraph(body, "");

            AppendParagraph(body,
                "Cùng thỏa thuận và thống nhất ký kết Biên bản thanh lý Hợp đồng nghiên cứu khoa học cấp trường " +
                $"số {c.ContractNumber} ký ngày {signedOn:dd} tháng {signedOn:MM} năm {signedOn:yyyy} với những điều khoản như sau:");
            AppendParagraph(body, $"Đề tài: {c.Project?.TitleVi ?? c.ScopeTitle ?? Blank}");
            AppendParagraph(body, $"Mã số đề tài: {c.Project?.ProjectCode ?? Blank}");
            AppendParagraph(body, "");

            AppendParagraph(body, "ĐIỀU 1.", bold: true);
            AppendParagraph(body,
                "Bên A xác nhận Bên B đã giao nộp sản phẩm NCKH được hoàn thiện theo ý kiến đánh giá và yêu cầu " +
                "của Hội đồng nghiệm thu.");
            AppendParagraph(body,
                "Toàn văn báo cáo tổng kết đề tài và các tài liệu, minh chứng Bên B giao nộp là bộ phận không tách rời " +
                "của Biên bản thanh lý này.");
            AppendParagraph(body, "- Sản phẩm giao nộp:");
            if (deliverables.Count == 0)
            {
                AppendParagraph(body, $"   {Blank}");
            }
            else
            {
                foreach (var d in deliverables)
                    AppendParagraph(body, $"   • {d.ProductName} — {FURPMS.Application.Constants.StatusText.Vi(d.AcceptanceStatus)}");
            }
            AppendParagraph(body,
                $"- Đề tài được đánh giá và xếp loại: {(c.Project?.Status == "COMPLETED" ? "Đạt" : Blank)}");
            AppendParagraph(body, "");

            AppendParagraph(body, "ĐIỀU 2.", bold: true);
            AppendParagraph(body,
                $"Tổng giá trị Hợp đồng là {c.TotalAmount:N0} đồng. Bên B đã được nhận số tiền là {disbursed:N0} đồng. " +
                "Bên B có trách nhiệm quyết toán kinh phí thực hiện Đề tài với Ban Kế toán trong vòng 07 ngày làm việc " +
                "kể từ ngày ký Biên bản nghiệm thu và thanh lý Hợp đồng.");
            if (settlement != null)
            {
                var acc = settlement.AccountingClearedAt.HasValue
                    ? settlement.AccountingClearedAt.Value.ToString("dd/MM/yyyy") : "chưa xác nhận";
                var ast = settlement.AssetsClearedAt.HasValue
                    ? settlement.AssetsClearedAt.Value.ToString("dd/MM/yyyy") : "chưa xác nhận";
                AppendParagraph(body, $"Kế toán xác nhận đã quyết toán kinh phí: {acc}.");
                AppendParagraph(body, $"Xác nhận đã xử lý tài sản: {ast}.");
            }
            AppendParagraph(body, "");

            AppendParagraph(body, "ĐIỀU 3.", bold: true);
            AppendParagraph(body,
                $"Biên bản này nghiệm thu và thanh lý Hợp đồng nghiên cứu khoa học cấp trường số {c.ContractNumber} " +
                $"ký ngày {signedOn:dd} tháng {signedOn:MM} năm {signedOn:yyyy}.");
            AppendParagraph(body, "");

            AppendParagraph(body, "ĐIỀU 4.", bold: true);
            AppendParagraph(body,
                "Biên bản nghiệm thu và thanh lý Hợp đồng này được thực hiện qua phương thức ký điện tử trên phần mềm " +
                "Econtract; các bên tự bảo quản và lưu trữ trên thiết bị điện tử của từng Bên./.");
            AppendParagraph(body, "");
            AppendParagraph(body, "");

            var sign = CreateTable(body, new[] { "ĐẠI DIỆN BÊN A (Bên giao)", "BÊN B (Bên nhận)" }, new[] { 4500, 4500 });
            AddTableRow(sign, "(Ký, ghi rõ họ tên)", "(Ký, ghi rõ họ tên)");
            AddTableRow(sign, "\n\n\n" + (c.SideARepresentative ?? ""), "\n\n\n" + (pi?.FullName ?? ""));
        }

        var code = c.ContractNumber.Replace("/", "-").Replace(" ", "_");
        return (ms.ToArray(), $"BienBanThanhLy_{code}.docx");
    }

    // ── Phụ lục hợp đồng (F4) ───────────────────────────────────────────────

    /// <summary>
    /// Hợp đồng đã ký thì KHÔNG sửa đè lên bản gốc — mỗi điều chỉnh phải có văn bản riêng đính
    /// kèm, dẫn chiếu hợp đồng gốc và ghi rõ <em>trước → sau</em>. Trước đây duyệt điều chỉnh xong
    /// chỉ đổi vài dòng trong cơ sở dữ liệu, không có giấy tờ nào đem đi ký, nên hồ sơ quyết toán
    /// không giải thích được vì sao thời gian/nội dung khác với hợp đồng gốc.
    /// <para>
    /// Căn cứ BM05 **Điều 6.1**: *"nếu một trong hai bên có yêu cầu sửa đổi, bổ sung nội dung …
    /// phải thông báo cho bên kia ít nhất 15 ngày trước"* — ngày duyệt được in ra để đối chiếu mốc này.
    /// </para>
    /// </summary>
    public async Task<(byte[] Content, string FileName)> ExportAmendmentDocAsync(Guid amendmentId)
    {
        // IgnoreQueryFilters: `RequestedByUser` và `Contract.Project` là navigation BẮT BUỘC, mà
        // User/Project đều có global query filter — EF nối INNER JOIN nên chỉ cần một mắt xích bị
        // lọc là mất luôn bản ghi gốc, báo "không tìm thấy" trong khi nó vẫn nằm đó.
        var a = await _contracts.Amendments
            .IgnoreQueryFilters()
            .Include(x => x.Category)
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser)
            .Include(x => x.Contract).ThenInclude(c => c.Project).ThenInclude(p => p.PiUser)
            .FirstOrDefaultAsync(x => x.Id == amendmentId)
            ?? throw new KeyNotFoundException($"Amendment {amendmentId} not found.");

        // Đơn vị chủ trì tra riêng, KHÔNG Include: đây là navigation bắt buộc nên EF nối INNER JOIN,
        // thiếu đơn vị là mất luôn bản ghi gốc và người dùng nhận 404 khó hiểu thay vì một ô trống.
        var hostingUnitName = a.Contract?.Project == null
            ? null
            : await _masterData.OrganizationalUnits
                .Where(u => u.Id == a.Contract.Project.HostingUnitId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();

        // Chỉ xuất phụ lục cho đề nghị ĐÃ DUYỆT — bản chờ duyệt mà in ra thì thành giấy tờ khống.
        if (!string.Equals(a.Status, AmendmentStatus.Approved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Đề nghị điều chỉnh đang ở trạng thái \"{FURPMS.Application.Constants.StatusText.Vi(a.Status)}\" — chỉ xuất phụ lục sau khi đã được duyệt.");

        var c = a.Contract;
        var pi = c.Project?.PiUser;
        const string Blank = "……………………………";

        using var ms = new MemoryStream();
        using (var docx = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = docx.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            AppendParagraph(body, "CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM", bold: true, fontSize: 12);
            AppendParagraph(body, "Độc lập - Tự do - Hạnh phúc", bold: true, fontSize: 12);
            AppendParagraph(body, "");
            AppendHeading(body, "PHỤ LỤC HỢP ĐỒNG", 15, bold: true, justify: JustificationValues.Center);
            AppendHeading(body, "NGHIÊN CỨU KHOA HỌC CẤP TRƯỜNG", 13, bold: true, justify: JustificationValues.Center);
            AppendParagraph(body, "");
            AppendParagraph(body, $"(Kèm theo Hợp đồng số {c.ContractNumber}" +
                                  $"{(c.SignedAt.HasValue ? $" ký ngày {c.SignedAt.Value:dd/MM/yyyy}" : "")})");
            AppendParagraph(body, "");

            AppendParagraph(body, "Căn cứ Quyết định số 543/QĐ-ĐHFPT ngày 30/5/2024 của Hiệu trưởng Trường Đại học FPT ban hành Quy định quản lý đề tài nghiên cứu khoa học cấp Trường;");
            AppendParagraph(body, $"Căn cứ Hợp đồng nghiên cứu khoa học cấp Trường số {c.ContractNumber};");
            AppendParagraph(body, "Căn cứ Điều 6.1 của Hợp đồng về việc sửa đổi, bổ sung nội dung Hợp đồng;");
            AppendParagraph(body, $"Căn cứ đề nghị điều chỉnh của Chủ nhiệm đề tài ngày {a.RequestedAt:dd/MM/yyyy}" +
                                  $"{(a.ReviewedAt.HasValue ? $" và ý kiến phê duyệt ngày {a.ReviewedAt.Value:dd/MM/yyyy}" : "")};");
            AppendParagraph(body, "");

            AppendParagraph(body, $"Hôm nay, ngày {(a.ReviewedAt ?? a.RequestedAt):dd} tháng {(a.ReviewedAt ?? a.RequestedAt):MM} năm {(a.ReviewedAt ?? a.RequestedAt):yyyy}, chúng tôi gồm:");
            AppendParagraph(body, "");
            AppendParagraph(body, "BÊN A: TRƯỜNG ĐẠI HỌC FPT", bold: true);
            AppendParagraph(body, $"Đại diện: {c.SideARepresentative ?? Blank}");
            AppendParagraph(body, "");
            AppendParagraph(body, "BÊN B: CHỦ NHIỆM ĐỀ TÀI", bold: true);
            AppendParagraph(body, $"Ông/Bà: {pi?.FullName ?? Blank}");
            AppendParagraph(body, $"Đơn vị công tác: {(string.IsNullOrWhiteSpace(hostingUnitName) ? Blank : hostingUnitName)}");
            AppendParagraph(body, $"Điện thoại: {(string.IsNullOrWhiteSpace(pi?.Phone) ? Blank : pi!.Phone)}    Email: {pi?.Email ?? Blank}");
            AppendParagraph(body, "");
            AppendParagraph(body, "Hai bên thống nhất ký Phụ lục Hợp đồng với các nội dung sau:");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 1. TÊN ĐỀ TÀI VÀ HỢP ĐỒNG ĐƯỢC ĐIỀU CHỈNH", 13);
            AppendParagraph(body, $"Tên đề tài: {c.Project?.TitleVi ?? c.ScopeTitle ?? Blank}");
            AppendParagraph(body, $"Mã số đề tài: {c.Project?.ProjectCode ?? Blank}");
            AppendParagraph(body, $"Hợp đồng số: {c.ContractNumber}");
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 2. NỘI DUNG ĐIỀU CHỈNH", 13);
            AppendParagraph(body, $"Loại điều chỉnh: {a.Category?.Name ?? Blank}");
            AppendParagraph(body, $"Nội dung: {a.ChangeDescription}");
            AppendParagraph(body, "");

            // Trước → sau là phần cốt lõi: người đọc quyết toán phải thấy ngay cái gì đã đổi.
            var diff = CreateTable(body, new[] { "Nội dung", "Theo Hợp đồng đã ký", "Sau điều chỉnh" },
                                   new[] { 2600, 3200, 3200 });

            // Với gia hạn, hệ thống lưu NewValue là SỐ THÁNG (yêu cầu của AmendmentService). In trần
            // "0 → 3" vào văn bản đem ký thì vô nghĩa — phải quy ra MỐC THỜI GIAN thật của hợp đồng.
            var isExtension = string.Equals(a.Category?.Code, "EXTENSION", StringComparison.OrdinalIgnoreCase);
            if (isExtension && int.TryParse(a.NewValue, out var months) && months > 0)
            {
                var newEnd = c.EndDate;
                var oldEnd = newEnd.AddMonths(-months);
                AddTableRowMulti(diff, new[]
                {
                    "Thời gian thực hiện",
                    $"{c.StartDate:dd/MM/yyyy} – {oldEnd:dd/MM/yyyy}",
                    $"{c.StartDate:dd/MM/yyyy} – {newEnd:dd/MM/yyyy}"
                });
                AddTableRowMulti(diff, new[] { "Số tháng gia hạn", "—", $"{months} tháng" });
            }
            else
            {
                AddTableRowMulti(diff, new[]
                {
                    a.Category?.Name ?? "Nội dung điều chỉnh",
                    string.IsNullOrWhiteSpace(a.OldValue) ? Blank : a.OldValue!,
                    string.IsNullOrWhiteSpace(a.NewValue) ? Blank : a.NewValue!
                });
            }

            if (a.ChangePercentage.HasValue)
                AddTableRowMulti(diff, new[] { "Tỷ lệ thay đổi", "—", $"{a.ChangePercentage.Value:0.##}%" });
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 3. LÝ DO ĐIỀU CHỈNH", 13);
            AppendParagraph(body, a.Justification);
            AppendParagraph(body, "");

            AppendHeading(body, "ĐIỀU 4. HIỆU LỰC", 13);
            AppendParagraph(body, "4.1. Phụ lục này là bộ phận không tách rời của Hợp đồng đã ký; các nội dung khác của Hợp đồng không đề cập trong Phụ lục này vẫn giữ nguyên hiệu lực.");
            AppendParagraph(body, "4.2. Phụ lục có hiệu lực kể từ ngày hai bên ký.");
            if (a.RequiresRectorApproval)
                AppendParagraph(body, "4.3. Nội dung điều chỉnh thuộc thẩm quyền phê duyệt của Hiệu trưởng; Phụ lục chỉ có hiệu lực sau khi được Hiệu trưởng phê duyệt.");
            AppendParagraph(body, "4.4. Phụ lục được thực hiện qua phương thức ký điện tử trên phần mềm Econtract; các bên tự bảo quản và lưu trữ trên thiết bị điện tử của từng Bên./.");
            AppendParagraph(body, "");
            AppendParagraph(body, "");

            var sign = CreateTable(body, new[] { "ĐẠI DIỆN BÊN A", "CHỦ NHIỆM ĐỀ TÀI (BÊN B)" }, new[] { 4500, 4500 });
            AddTableRow(sign, "(Ký, ghi rõ họ tên)", "(Ký, ghi rõ họ tên)");
            AddTableRow(sign, "\n\n\n" + (c.SideARepresentative ?? ""), "\n\n\n" + (pi?.FullName ?? ""));
        }

        var contractCode = c.ContractNumber.Replace("/", "-").Replace(" ", "_");
        return (ms.ToArray(), $"PhuLucHopDong_{contractCode}_{a.RequestedAt:yyyyMMdd}.docx");
    }

    private static void AppendParagraph(Body body, string text, bool bold = false, int fontSize = 11)
    {
        var para = new Paragraph();
        var run = new Run();
        var rpr = new RunProperties();
        if (bold) rpr.Append(new Bold());
        rpr.Append(new FontSize { Val = (fontSize * 2).ToString() });
        run.Append(rpr);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.Append(run);
        body.Append(para);
    }

    // Chỉ in mục (heading + nội dung) khi có dữ liệu — tránh để trống trong file Mẫu 1.
    private static void AppendSectionIfAny(Body body, string heading, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        AppendHeading(body, heading, 12, bold: true);
        AppendParagraph(body, content);
        AppendParagraph(body, "");
    }

    private static void AppendHeading(Body body, string text, int fontSize = 13, bool bold = true,
        JustificationValues justify = JustificationValues.Left)
    {
        var para = new Paragraph();
        var ppr = new ParagraphProperties(new Justification { Val = justify });
        para.Append(ppr);
        var run = new Run();
        var rpr = new RunProperties();
        if (bold) rpr.Append(new Bold());
        rpr.Append(new FontSize { Val = (fontSize * 2).ToString() });
        run.Append(rpr);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.Append(run);
        body.Append(para);
    }

    private static Table CreateTable(Body body, string[] headers, int[] widths)
    {
        var table = new Table();

        var tblPr = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ));
        table.Append(tblPr);

        var grid = new TableGrid(widths.Select(w => new GridColumn { Width = w.ToString() }));
        table.Append(grid);

        var headerRow = new TableRow();
        for (int i = 0; i < headers.Length; i++)
        {
            var tc = new TableCell(new TableCellProperties(new TableCellWidth { Width = widths[i].ToString(), Type = TableWidthUnitValues.Dxa }),
                                   MakeParagraph(headers[i], bold: true));
            headerRow.Append(tc);
        }
        table.Append(headerRow);
        body.Append(table);
        return table;
    }

    private static void AddTableRow(Table table, string col0, string col1, bool bold0 = false)
    {
        var row = new TableRow();
        row.Append(new TableCell(MakeParagraph(col0, bold: bold0)));
        row.Append(new TableCell(MakeParagraph(col1)));
        table.Append(row);
    }

    private static void AddTableRowMulti(Table table, string[] cells)
    {
        var row = new TableRow();
        foreach (var c in cells)
            row.Append(new TableCell(MakeParagraph(c)));
        table.Append(row);
    }

    private static Paragraph MakeParagraph(string text, bool bold = false)
    {
        var para = new Paragraph();
        var run = new Run();
        if (bold)
        {
            var rpr = new RunProperties(new Bold());
            run.Append(rpr);
        }
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.Append(run);
        return para;
    }
}
