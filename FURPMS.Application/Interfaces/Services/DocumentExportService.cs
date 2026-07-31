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
