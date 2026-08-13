using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Sau Review 2 (Project-centric): tạo đề tài = tạo PROJECT (gốc) + Proposal v1 (tài liệu).
// Route/DTO giữ theo proposalId để FE không phải sửa lớn; service tự resolve project.
public class ProposalService : IProposalService
{
    private readonly IProposalRepository _proposals;
    private readonly ICycleRepository _cycles;
    private readonly IMasterDataRepository _masterData;
    private readonly IUserRepository _users;
    private readonly IClock _clock;
    private readonly IReviewRoundService _reviewRounds;
    private readonly IReviewRepository _review;
    private readonly IBudgetPolicyService _budgetPolicy;
    private readonly INotifier _notifier;
    private readonly IAiSummaryQueue _summaryQueue;

    public ProposalService(
        IProposalRepository proposals,
        ICycleRepository cycles,
        IMasterDataRepository masterData,
        IUserRepository users,
        IClock clock,
        IReviewRoundService reviewRounds,
        IReviewRepository review,
        IBudgetPolicyService budgetPolicy,
        INotifier notifier,
        IAiSummaryQueue summaryQueue)
    {
        _budgetPolicy = budgetPolicy;
        _notifier = notifier;
        _summaryQueue = summaryQueue;
        _proposals = proposals;
        _cycles = cycles;
        _masterData = masterData;
        _users = users;
        _clock = clock;
        _reviewRounds = reviewRounds;
        _review = review;
    }

    private IQueryable<Proposal> QueryWithProject() => _proposals.Query()
        .IgnoreQueryFilters()
        .Include(p => p.Project).ThenInclude(pr => pr.CycleTrack).ThenInclude(ct => ct.Track)
        .Include(p => p.Project).ThenInclude(pr => pr.CycleTrack).ThenInclude(ct => ct.Cycle)
        .Include(p => p.Project).ThenInclude(pr => pr.PiUser)
        .Include(p => p.Project).ThenInclude(pr => pr.ResearchType)
        .Include(p => p.Budget);

    public async Task<IEnumerable<ProposalSummaryDto>> GetProposalsAsync(
        ProposalQueryParams queryParams,
        Guid requesterId,
        IEnumerable<string> requesterRoles,
        bool ownOnly = false)
    {
        var query = QueryWithProject()
            .Where(p => p.IsCurrent)     // danh sách chỉ hiện bản hiện hành của mỗi project
            .AsQueryable();

        // "Đề cương của tôi" (ownOnly) LUÔN chỉ của người gọi — kể cả khi họ là Admin/Staff
        // (đa vai: đang "làm PI" thì phải thấy đúng đề cương mình nộp, không phải toàn hệ thống).
        var roleSet = requesterRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ownOnly || (!roleSet.Contains("Admin") && !roleSet.Contains("Staff")))
            query = query.Where(p => p.Project.PiUserId == requesterId && !p.IsDeleted && !p.Project.IsDeleted);
        else
            query = query.Where(p => !p.IsDeleted && !p.Project.IsDeleted);

        if (!string.IsNullOrWhiteSpace(queryParams.CycleId) && int.TryParse(queryParams.CycleId, out int cycleId))
            query = query.Where(p => p.Project.CycleTrack.CycleId == cycleId);

        if (!string.IsNullOrWhiteSpace(queryParams.TrackId) && int.TryParse(queryParams.TrackId, out int trackId))
            query = query.Where(p => p.Project.CycleTrack.TrackId == trackId);

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
            query = query.Where(p => p.Status == queryParams.Status.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.ToLower();
            query = query.Where(p => p.TitleVi.ToLower().Contains(search)
                || (p.TitleEn != null && p.TitleEn.ToLower().Contains(search)));
        }

        var proposals = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return proposals.Select(MapSummary);
    }

    // Có kiểm quyền: Staff/Admin xem tất cả; PI xem đề cương của mình; reviewer xem đề cương mà
    // hội đồng họ tham gia được gán chấm. Còn lại 403 (chống IDOR — trước đây ai đăng nhập cũng đọc được).
    public async Task<ProposalDto> GetProposalByIdAsync(Guid proposalId, Guid callerId, IEnumerable<string> callerRoles)
    {
        var roleSet = callerRoles as ISet<string> ?? callerRoles.ToHashSet();
        if (!roleSet.Contains("Admin") && !roleSet.Contains("Staff"))
        {
            var info = await _proposals.Query().IgnoreQueryFilters()
                .Where(p => p.Id == proposalId)
                .Select(p => new { p.Project.PiUserId, p.ProjectId })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

            var allowed = info.PiUserId == callerId
                || await _review.CouncilMembers.AnyAsync(m => m.UserId == callerId
                    && _review.ProjectAssignments.Any(a => a.CouncilId == m.CouncilId && a.ProjectId == info.ProjectId));
            if (!allowed)
                throw new ForbiddenException("Bạn không có quyền xem đề cương này.");
        }

        return await LoadProposalDetailAsync(proposalId);
    }

    // Nạp chi tiết (KHÔNG kiểm quyền) — dùng nội bộ sau khi đã tạo/sửa (caller đã là PI hợp lệ).
    private async Task<ProposalDto> LoadProposalDetailAsync(Guid proposalId)
    {
        var proposal = await QueryWithProject()
            .Include(p => p.Project).ThenInclude(pr => pr.Members)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        var budgetItems = await _proposals.BudgetItems
            .Include(i => i.Category)
            .Where(i => i.ProposalId == proposalId)
            .OrderBy(i => i.Sequence)
            .ToListAsync();

        return MapDetail(proposal, budgetItems);
    }

    public async Task<ProposalDto> CreateProposalAsync(CreateProposalRequest request, Guid piUserId)
    {
        if (string.IsNullOrWhiteSpace(request.TitleVI))
            throw new ArgumentException("Phải nhập tên đề tài.");
        if (request.DurationMonths <= 0)
            throw new ArgumentException("Thời gian thực hiện phải lớn hơn 0 tháng.");

        if (!int.TryParse(request.TrackId, out int trackId))
            throw new ArgumentException("Lĩnh vực nghiên cứu không hợp lệ.");

        _ = await _cycles.Tracks.FirstOrDefaultAsync(t => t.Id == trackId)
            ?? throw new KeyNotFoundException("Không tìm thấy lĩnh vực nghiên cứu.");

        // PI chọn đợt: nếu có CycleId → dùng đúng đợt đó (phải OPEN); null → fallback đợt OPEN mới nhất.
        var openCycle = request.CycleId.HasValue
            ? await _cycles.Query().FirstOrDefaultAsync(c => c.Id == request.CycleId.Value && c.Status == CycleStatus.Open)
              ?? throw new InvalidOperationException($"Đợt nộp #{request.CycleId} không tồn tại hoặc không ở trạng thái mở.")
            : await _cycles.Query()
                .Where(c => c.Status == CycleStatus.Open)
                .OrderByDescending(c => c.CycleYear)
                .FirstOrDefaultAsync()
              ?? throw new InvalidOperationException("Hiện không có đợt nào đang mở nhận đề cương.");

        // Loại đề tài LẤY TỪ ĐỢT (rule #7: 1 đợt = đúng 1 loại) — KHÔNG lấy từ input PI để tránh lệch dữ liệu.
        var researchType = await _masterData.ResearchTypes
            .FirstOrDefaultAsync(r => r.Id == openCycle.ResearchTypeId)
            ?? throw new KeyNotFoundException("Đợt này chưa gán loại đề tài.");

        var piUser = await _users.GetByIdAsync(piUserId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var cycleTrack = await EnsureCycleTrackAsync(openCycle.Id, trackId);
        var orderId = await ResolveOrderIdAsync(openCycle, request.OrderId, piUser.UnitId ?? 1, piUserId);

        // 1) PROJECT — thực thể gốc
        var project = new Project
        {
            CycleTrackId = cycleTrack.Id,
            OrderId = orderId,
            PiUserId = piUserId,
            HostingUnitId = piUser.UnitId ?? 1,
            ResearchTypeId = researchType.Id,
            TitleVi = request.TitleVI,
            TitleEn = request.TitleEN,
            Status = ProjectStatus.Proposed,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(request.DurationMonths))
        };
        _proposals.AddProject(project);

        // 2) Proposal v1 — tài liệu đề cương
        var proposal = new Proposal
        {
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = request.TitleVI,
            TitleEn = request.TitleEN,
            DurationMonths = request.DurationMonths,
            PlannedStartDate = project.PlannedStartDate,
            PlannedEndDate = project.PlannedEndDate,
            AbstractVi = request.Objectives,
            AbstractEn = request.AbstractEN,
            ResearchObjectives = request.Objectives,
            Methodology = request.Methodology,
            ExpectedOutput = request.ExpectedOutput,
            LiteratureReview = request.Urgency,
            NoveltyOriginality = request.Novelty,
            ApplicationPotential = request.ApplicationPotential,
            TransferPotential = request.TransferPotential,
            FacilitiesEquipment = request.Facilities,
            FundingMethod = request.FundingMethod,
            Status = ProposalStatus.Draft
        };

        await _proposals.AddAsync(proposal);
        await _proposals.SaveChangesAsync();

        // Create budget skeleton
        var budget = new ProposalBudget { ProposalId = proposal.Id };
        _masterData.Add(budget);
        await _masterData.SaveChangesAsync();

        // Add team members (thuộc PROJECT)
        AddProjectMembers(project.Id, request.Members);
        if (request.Members.Count > 0)
            await _proposals.SaveChangesAsync();

        // Persist budget items + tổng kinh phí
        await SyncBudgetItemsAsync(proposal.Id, request.BudgetItems, request.TotalBudget);

        return await LoadProposalDetailAsync(proposal.Id);
    }

    // Tìm/tạo cặp (cycle, track) — Review 2 điểm b: 1 đợt chứa nhiều track.
    private async Task<CycleTrack> EnsureCycleTrackAsync(int cycleId, int trackId)
    {
        var existing = await _cycles.CycleTracks
            .FirstOrDefaultAsync(ct => ct.CycleId == cycleId && ct.TrackId == trackId);
        if (existing != null) return existing;

        var cycleTrack = new CycleTrack { CycleId = cycleId, TrackId = trackId };
        await _cycles.AddCycleTrackAsync(cycleTrack);
        await _cycles.SaveChangesAsync();
        return cycleTrack;
    }

    // Review 2 điểm d: 100% project thuộc 1 order. Đề tài tự do → order "Nghiên cứu cơ bản"
    // mặc định của đợt (tạo nếu chưa có).
    private async Task<int> ResolveOrderIdAsync(ResearchCycle cycle, int? requestedOrderId, int fallbackUnitId, Guid createdBy)
    {
        if (requestedOrderId.HasValue)
        {
            var order = await _cycles.Orders.FirstOrDefaultAsync(o => o.Id == requestedOrderId.Value)
                ?? throw new KeyNotFoundException("Không tìm thấy đơn đặt hàng nghiên cứu.");
            return order.Id;
        }

        var defaultOrder = await _cycles.Orders
            .FirstOrDefaultAsync(o => o.CycleId == cycle.Id && o.IsDefault);
        if (defaultOrder != null) return defaultOrder.Id;

        var newDefault = new ResearchOrder
        {
            CycleId = cycle.Id,
            OrderingUnitId = fallbackUnitId,
            ResearchArea = "Nghiên cứu tự do (đề xuất của PI)",
            ProblemDescription = "Order mặc định của đợt — gom các đề tài PI tự đề xuất (không có đơn vị đặt hàng cụ thể).",
            IsDefault = true,
            Status = "OPEN",
            CreatedBy = createdBy
        };
        await _cycles.AddOrderAsync(newDefault);
        await _cycles.SaveChangesAsync();
        return newDefault.Id;
    }

    // Thêm danh sách thành viên (lưu cả email + đơn vị) — thuộc PROJECT.
    private void AddProjectMembers(Guid projectId, List<CreateMemberRequest> members)
    {
        int seq = 1;
        foreach (var m in members)
        {
            _proposals.AddProjectMember(new ProjectMember
            {
                ProjectId = projectId,
                FullName = m.FullName,
                Email = m.Email,
                UnitName = m.Department,
                AcademicTitle = m.AcademicTitle,
                MemberRoleCode = m.MemberRoleCode,
                WorkContent = m.Role,
                WorkMonths = m.WorkMonths,
                IsPi = false,
                IsSecretary = m.IsSecretary,
                Sequence = seq++
            });
        }
    }

    // Thay toàn bộ budget items: ánh xạ tên hạng mục (FE nhập) -> BudgetExpenseCategory,
    // fallback "OTHER" nếu không khớp; cập nhật lại tổng kinh phí trên ProposalBudget.
    private async Task SyncBudgetItemsAsync(
        Guid proposalId, List<CreateBudgetItemRequest> items, decimal? totalBudgetFallback = null)
    {
        var existing = await _proposals.BudgetItems.Where(i => i.ProposalId == proposalId).ToListAsync();
        if (existing.Count > 0)
            _proposals.RemoveBudgetItemsRange(existing);

        var categories = await _masterData.BudgetExpenseCategories.ToListAsync();
        // Hạng mục "Văn phòng phẩm, chi khác" của Điều 15 là nơi đổ mọi khoản không khớp tên; bộ 12
        // hạng mục cũ dùng mã "OTHER" nên vẫn nhận để đọc dữ liệu cũ.
        var fallback = categories.FirstOrDefault(c => c.Code == "OFFICE_OTHER")
                    ?? categories.FirstOrDefault(c => c.Code == "OTHER")
                    ?? categories.FirstOrDefault();

        int seq = 1;
        decimal total = 0m;
        var byCategory = new Dictionary<int, decimal>();
        foreach (var it in items)
        {
            if (it.Amount <= 0 && string.IsNullOrWhiteSpace(it.Category))
                continue;
            var cat = categories.FirstOrDefault(c => string.Equals(c.Name, it.Category, StringComparison.OrdinalIgnoreCase))
                   ?? categories.FirstOrDefault(c => string.Equals(c.Code, it.Category, StringComparison.OrdinalIgnoreCase))
                   ?? fallback;
            if (cat == null)
                continue;

            _proposals.AddBudgetItem(new ProposalBudgetItem
            {
                ProposalId = proposalId,
                CategoryId = cat.Id,
                Amount = it.Amount,
                Note = it.Note,
                Sequence = seq++
            });
            total += it.Amount;
            byCategory[cat.Id] = byCategory.GetValueOrDefault(cat.Id) + it.Amount;
        }

        // Chưa tách hạng mục thì lấy tổng chủ nhiệm gõ ở wizard; có hạng mục thì tổng LUÔN là tổng
        // hạng mục, để hai con số không bao giờ đá nhau.
        if (items.Count == 0 && totalBudgetFallback is >= 0m)
            total = totalBudgetFallback.Value;

        var budget = await _proposals.Budgets.FirstOrDefaultAsync(b => b.ProposalId == proposalId);
        if (budget != null)
        {
            budget.TotalAmount = total;
            ApplyDieu15Columns(budget, byCategory, categories);
        }

        await _proposals.SaveChangesAsync();

        // QĐ543 Điều 14 — kiểm SAU khi lưu vì trần tra theo loại đề tài của project, cần bản ghi đã
        // gắn đủ quan hệ. Vượt trần thì ném 400, giao dịch của controller cuốn lại.
        await _budgetPolicy.AssertWithinCapAsync(proposalId, total);
        // QĐ543 Điều 15 — tỷ lệ từng hạng mục trên tổng.
        await _budgetPolicy.AssertCategoryLimitsAsync(byCategory, total);
    }

    /// <summary>
    /// Đổ tiền từng hạng mục vào 6 cột tổng hợp của <c>proposal_budgets</c> — đúng 6 hạng mục của
    /// QĐ543 Điều 15. Trước đây 6 cột này tồn tại nhưng <b>không đường code nào ghi vào</b>, nên
    /// mọi bản dự toán đều hiện 0 ở phần tổng hợp.
    /// </summary>
    private static void ApplyDieu15Columns(
        ProposalBudget budget,
        IReadOnlyDictionary<int, decimal> byCategory,
        IReadOnlyCollection<Domain.Entities.MasterData.BudgetExpenseCategory> categories)
    {
        decimal Sum(string code)
        {
            var cat = categories.FirstOrDefault(c => c.Code == code);
            return cat != null ? byCategory.GetValueOrDefault(cat.Id) : 0m;
        }

        budget.LaborAmount = Sum("LABOR");
        budget.EquipmentAmount = Sum("EQUIPMENT");
        budget.ExternalServiceAmount = Sum("OUTSOURCED");
        budget.ConferenceAmount = Sum("CONFERENCE");
        budget.OfficeSuppliesAmount = Sum("OFFICE_OTHER");
        budget.IncidentalIpAmount = Sum("INCIDENTAL_IP");
    }

    public async Task<ProposalDto> UpdateProposalAsync(Guid proposalId, CreateProposalRequest request, Guid userId)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project).ThenInclude(pr => pr.CycleTrack).ThenInclude(ct => ct.Cycle)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        var project = proposal.Project;

        if (project.PiUserId != userId)
            throw new ForbiddenException("Chỉ chủ nhiệm đề tài mới sửa được đề cương này.");

        // DRAFT: sửa tại chỗ. REVISION_REQUIRED: tạo BẢN MỚI (versioning — Review 2 điểm a).
        if (proposal.Status == ProposalStatus.RevisionRequired && proposal.IsCurrent)
            return await CreateRevisionAsync(proposal, request, userId);

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException($"Đề cương đang ở trạng thái {StatusText.Vi(proposal.Status)} — chỉ sửa được bản nháp. Hãy rút lại trước khi sửa.");

        // Hết hạn đợt → khoá, không cho sửa nháp nữa (đồng bộ với chặn nộp quá hạn).
        var todayEdit = DateOnly.FromDateTime(_clock.UtcNow);
        var cycle = project.CycleTrack.Cycle;
        if (cycle != null && todayEdit > cycle.SubmissionDeadline)
            throw new InvalidOperationException(
                $"Đã quá hạn nộp của đợt (hạn {cycle.SubmissionDeadline:dd/MM/yyyy}). Không thể sửa đề cương.");

        if (string.IsNullOrWhiteSpace(request.TitleVI))
            throw new ArgumentException("Phải nhập tên đề tài.");
        if (request.DurationMonths <= 0)
            throw new ArgumentException("Thời gian thực hiện phải lớn hơn 0 tháng.");
        if (!int.TryParse(request.TrackId, out int trackId))
            throw new ArgumentException("Lĩnh vực nghiên cứu không hợp lệ.");

        _ = await _cycles.Tracks.FirstOrDefaultAsync(t => t.Id == trackId)
            ?? throw new KeyNotFoundException("Không tìm thấy lĩnh vực nghiên cứu.");

        // Loại đề tài theo ĐỢT của project (cố định, không đổi khi sửa) — bỏ qua input PI.
        var cycleTypeId = project.CycleTrack.Cycle?.ResearchTypeId ?? project.ResearchTypeId;
        var researchType = await _masterData.ResearchTypes
            .FirstOrDefaultAsync(r => r.Id == cycleTypeId)
            ?? throw new KeyNotFoundException("Đợt này chưa gán loại đề tài.");

        // Đổi track → đổi cycle_track trên PROJECT (giữ nguyên cycle).
        if (project.CycleTrack.TrackId != trackId)
        {
            var newCycleTrack = await EnsureCycleTrackAsync(project.CycleTrack.CycleId, trackId);
            project.CycleTrackId = newCycleTrack.Id;
        }
        project.ResearchTypeId = researchType.Id;
        project.TitleVi = request.TitleVI;
        project.TitleEn = request.TitleEN;
        project.PlannedEndDate = project.PlannedStartDate.AddMonths(request.DurationMonths);
        project.UpdatedAt = DateTime.UtcNow;

        proposal.TitleVi = request.TitleVI;
        proposal.TitleEn = request.TitleEN;
        proposal.DurationMonths = request.DurationMonths;
        proposal.PlannedEndDate = proposal.PlannedStartDate.AddMonths(request.DurationMonths);
        proposal.AbstractVi = request.Objectives;
        proposal.AbstractEn = request.AbstractEN;
        proposal.ResearchObjectives = request.Objectives;
        proposal.Methodology = request.Methodology;
        proposal.ExpectedOutput = request.ExpectedOutput;
        proposal.LiteratureReview = request.Urgency;
        proposal.NoveltyOriginality = request.Novelty;
        proposal.ApplicationPotential = request.ApplicationPotential;
        proposal.TransferPotential = request.TransferPotential;
        proposal.FacilitiesEquipment = request.Facilities;
        proposal.FundingMethod = request.FundingMethod;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _proposals.SaveChangesAsync();

        // Chỉ thay team members khi request có gửi danh sách (≠ rỗng) — để workspace lưu
        // riêng "thông tin chung" mà KHÔNG xoá members/labor đã nhập.
        if (request.Members.Count > 0)
            await ReplaceProjectMembersAsync(project.Id, request.Members);

        // Chỉ thay budget items khi request có gửi (≠ rỗng) — để không xoá cột nguồn vốn đã nhập riêng.
        if (request.BudgetItems.Count > 0 || request.TotalBudget.HasValue)
            await SyncBudgetItemsAsync(proposal.Id, request.BudgetItems, request.TotalBudget);

        return await LoadProposalDetailAsync(proposalId);
    }

    // Versioning (Review 2 điểm a): REVISION_REQUIRED → bản cũ giữ lịch sử, tạo v(n+1) làm bản hiện hành.
    private async Task<ProposalDto> CreateRevisionAsync(Proposal oldVersion, CreateProposalRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.TitleVI))
            throw new ArgumentException("Phải nhập tên đề tài.");
        if (request.DurationMonths <= 0)
            throw new ArgumentException("Thời gian thực hiện phải lớn hơn 0 tháng.");

        var project = oldVersion.Project;
        var maxVersion = await _proposals.Query().IgnoreQueryFilters()
            .Where(p => p.ProjectId == project.Id)
            .MaxAsync(p => p.VersionNo);

        oldVersion.IsCurrent = false;
        oldVersion.UpdatedAt = DateTime.UtcNow;

        var revision = new Proposal
        {
            ProjectId = project.Id,
            VersionNo = maxVersion + 1,
            IsCurrent = true,
            TitleVi = request.TitleVI,
            TitleEn = request.TitleEN,
            DurationMonths = request.DurationMonths,
            PlannedStartDate = oldVersion.PlannedStartDate,
            PlannedEndDate = oldVersion.PlannedStartDate.AddMonths(request.DurationMonths),
            AbstractVi = request.Objectives,
            AbstractEn = request.AbstractEN,
            ResearchObjectives = request.Objectives,
            Methodology = request.Methodology,
            ExpectedOutput = request.ExpectedOutput,
            LiteratureReview = request.Urgency,
            NoveltyOriginality = request.Novelty,
            ApplicationPotential = request.ApplicationPotential,
            TransferPotential = request.TransferPotential,
            FacilitiesEquipment = request.Facilities,
            FundingMethod = request.FundingMethod ?? oldVersion.FundingMethod,
            Status = ProposalStatus.Draft
        };
        await _proposals.AddAsync(revision);

        project.TitleVi = request.TitleVI;
        project.TitleEn = request.TitleEN;
        project.UpdatedAt = DateTime.UtcNow;
        await _proposals.SaveChangesAsync();

        var budget = new ProposalBudget { ProposalId = revision.Id };
        _masterData.Add(budget);
        await _masterData.SaveChangesAsync();

        if (request.Members.Count > 0)
            await ReplaceProjectMembersAsync(project.Id, request.Members);
        await SyncBudgetItemsAsync(revision.Id, request.BudgetItems, request.TotalBudget);

        return await LoadProposalDetailAsync(revision.Id);
    }

    private async Task ReplaceProjectMembersAsync(Guid projectId, List<CreateMemberRequest> members)
    {
        // Phải xoá labor details phụ thuộc trước (FK proposal_budget_labor_details -> project_members).
        var existingMembers = await _proposals.ProjectMembers
            .Where(m => m.ProjectId == projectId)
            .ToListAsync();
        if (existingMembers.Count > 0)
        {
            var memberIds = existingMembers.Select(m => m.Id).ToList();
            var laborDetails = await _proposals.LaborDetails
                .Where(l => memberIds.Contains(l.ProjectMemberId))
                .ToListAsync();
            if (laborDetails.Count > 0)
                _proposals.RemoveLaborDetailsRange(laborDetails);
            _proposals.RemoveProjectMembersRange(existingMembers);
            await _proposals.SaveChangesAsync();
        }
        AddProjectMembers(projectId, members);
        await _proposals.SaveChangesAsync();
    }

    public async Task<ProposalDto> SubmitProposalAsync(Guid proposalId, Guid userId, bool confirmCvUpToDate = false)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project).ThenInclude(pr => pr.CycleTrack).ThenInclude(ct => ct.Cycle)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        var project = proposal.Project;

        if (project.PiUserId != userId)
            throw new ForbiddenException("Chỉ chủ nhiệm đề tài mới nộp được đề cương này.");

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException($"Đề cương đang ở trạng thái {StatusText.Vi(proposal.Status)} — chỉ nộp được bản nháp.");

        // Chặn nộp quá hạn (dùng đồng hồ hệ thống — công cụ tua thời gian test được).
        // Bản revision (v2+) không bị chặn deadline nộp lần đầu — deadline sửa nằm ở RevisionDeadline.
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var cycle = project.CycleTrack.Cycle;
        if (proposal.VersionNo == 1 && cycle != null && today > cycle.SubmissionDeadline)
            throw new InvalidOperationException(
                $"Đã quá hạn nộp của đợt (hạn {cycle.SubmissionDeadline:dd/MM/yyyy}). Không thể nộp.");

        // QĐ543 Điều 14 — cửa chốt. Trần có thể bị siết SAU khi PI lưu nháp (Phòng QLKH sửa master
        // data), nên không thể tin vào lần kiểm lúc nhập.
        var draftTotal = await _proposals.Budgets
            .Where(b => b.ProposalId == proposalId)
            .Select(b => (decimal?)b.TotalAmount)
            .FirstOrDefaultAsync() ?? 0m;
        await _budgetPolicy.AssertWithinCapAsync(proposalId, draftTotal);

        // Nhắc cập nhật CV trước khi nộp (rule tuần 6): CV thiếu/cũ > 6 tháng → bắt PI xác nhận.
        if (!confirmCvUpToDate)
        {
            var profile = await _users.AcademicProfiles.FirstOrDefaultAsync(a => a.UserId == userId);
            var staleBefore = _clock.UtcNow.AddMonths(-6);
            if (profile == null || profile.UpdatedAt < staleBefore)
                throw new InvalidOperationException(
                    "Lý lịch khoa học (CV) chưa được cập nhật gần đây. Vui lòng cập nhật CV hoặc xác nhận CV vẫn đúng trước khi nộp.");
        }

        proposal.Status = ProposalStatus.Submitted;
        proposal.SubmittedAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;
        project.Status = ProjectStatus.UnderReview;
        project.UpdatedAt = DateTime.UtcNow;
        await _proposals.SaveChangesAsync();

        // Báo Phòng QLKH có đề cương mới cần xử lý. Trước đây nộp xong hệ thống im lặng — chuyên
        // viên phải tự nhớ vào màn danh sách rà xem có gì mới, quá hạn mở vòng chấm cũng không ai
        // nhắc. Đây là mắt xích đầu tiên của quy trình nên im lặng ở đây là kẹt cả dây.
        var piName = await _users.Query()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? "Chủ nhiệm";
        var isRevision = proposal.VersionNo > 1;
        await _notifier.NotifyRoleAsync(
            "Staff",
            isRevision ? "PROPOSAL_RESUBMITTED" : "PROPOSAL_SUBMITTED",
            isRevision ? "Đề cương nộp lại sau chỉnh sửa" : "Có đề cương mới được nộp",
            isRevision
                ? $"{piName} đã nộp lại đề cương \"{proposal.TitleVi}\" (bản {proposal.VersionNo}) sau khi chỉnh sửa."
                : $"{piName} vừa nộp đề cương \"{proposal.TitleVi}\". Vui lòng xếp vào vòng xét duyệt.",
            actionUrl: "/proposal-reviews",
            entityType: "Proposal",
            entityId: proposal.Id.ToString());

        // Sinh sẵn tóm tắt AI cho người chấm (thầy góp ý 05/08, nhắc lại 14/08). CHỈ XẾP HÀNG —
        // gọi Gemini mất 30–60 giây, nhét vào đây là bắt PI ngồi nhìn màn hình quay tròn một phút
        // cho một việc họ không cần. Việc sinh chạy nền, xong lúc nào người chấm mở ra là có.
        _summaryQueue.Enqueue(proposal.Id, userId);

        // Rule #1: nộp lại bản REVISION (v2+) → mở lại hội đồng đã chốt "cần chỉnh sửa" để chấm lại
        // (giữ điểm cũ). No-op nếu không có biên bản REVISION nào.
        if (proposal.VersionNo > 1)
            await _reviewRounds.ReopenAfterResubmitAsync(project.Id);

        return await LoadProposalDetailAsync(proposalId);
    }

    public async Task<ProposalDto> WithdrawProposalAsync(Guid proposalId, Guid userId)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        if (proposal.Project.PiUserId != userId)
            throw new ForbiddenException("Chỉ chủ nhiệm đề tài mới rút lại được đề cương này.");

        if (proposal.Status != ProposalStatus.Submitted)
            throw new InvalidOperationException($"Đề cương đang ở trạng thái {StatusText.Vi(proposal.Status)} — chỉ rút lại được đề cương đã nộp.");

        proposal.Status = ProposalStatus.Draft;
        proposal.SubmittedAt = null;
        proposal.UpdatedAt = DateTime.UtcNow;
        proposal.Project.Status = ProjectStatus.Proposed;
        proposal.Project.UpdatedAt = DateTime.UtcNow;
        await _proposals.SaveChangesAsync();

        return await LoadProposalDetailAsync(proposalId);
    }

    private static ProposalSummaryDto MapSummary(Proposal p) => new()
    {
        Id = p.Id,
        TitleVI = p.TitleVi,
        TitleEN = p.TitleEn,
        ResearchType = p.Project?.ResearchType?.Name ?? "—",
        Status = p.Status,
        CycleName = p.Project?.CycleTrack?.Cycle?.SemesterCode
                    ?? p.Project?.CycleTrack?.Cycle?.CycleYear.ToString(),
        TrackName = p.Project?.CycleTrack?.Track?.Name ?? "—",
        PrincipalInvestigatorName = p.Project?.PiUser?.FullName ?? "—",
        TotalBudget = p.Budget?.TotalAmount ?? 0m,
        DurationMonths = p.DurationMonths,
        CreatedAt = p.CreatedAt,
        SubmittedAt = p.SubmittedAt
    };

    private static ProposalDto MapDetail(Proposal p, List<ProposalBudgetItem> budgetItems) => new()
    {
        Id = p.Id,
        ProjectId = p.ProjectId,
        ProjectStatus = p.Project?.Status,
        VersionNo = p.VersionNo,
        TitleVI = p.TitleVi,
        TitleEN = p.TitleEn,
        ResearchType = p.Project?.ResearchType?.Name ?? "—",
        Status = p.Status,
        TrackId = p.Project?.CycleTrack?.TrackId.ToString() ?? "",
        ResearchTypeId = p.Project?.ResearchTypeId ?? 0,
        TrackName = p.Project?.CycleTrack?.Track?.Name ?? "—",
        CycleId = p.Project?.CycleTrack?.CycleId.ToString() ?? "",
        CycleName = p.Project?.CycleTrack?.Cycle?.SemesterCode ?? p.Project?.CycleTrack?.Cycle?.CycleYear.ToString() ?? "—",
        PrincipalInvestigatorName = p.Project?.PiUser?.FullName ?? "—",
        TotalBudget = p.Budget?.TotalAmount ?? 0m,
        DurationMonths = p.DurationMonths,
        Objectives = p.ResearchObjectives,
        Methodology = p.Methodology,
        ExpectedOutput = p.ExpectedOutput,
        RejectionReason = p.RejectionReason,
        AbstractEN = p.AbstractEn,
        Urgency = p.LiteratureReview,
        Novelty = p.NoveltyOriginality,
        ApplicationPotential = p.ApplicationPotential,
        TransferPotential = p.TransferPotential,
        Facilities = p.FacilitiesEquipment,
        FundingMethod = p.FundingMethod,
        CreatedAt = p.CreatedAt,
        SubmittedAt = p.SubmittedAt,
        Members = (p.Project?.Members ?? Enumerable.Empty<ProjectMember>()).OrderBy(m => m.Sequence).Select(m => new ProposalMemberDto
        {
            Id = m.Id,
            FullName = m.FullName,
            Email = m.Email,
            Department = m.UnitName,
            Role = !string.IsNullOrWhiteSpace(m.WorkContent) ? m.WorkContent : (m.MemberRoleCode ?? (m.IsPi ? "PI" : "Member")),
            WorkMonths = m.WorkMonths,
            AcademicTitle = m.AcademicTitle,
            MemberRoleCode = m.MemberRoleCode,
            IsSecretary = m.IsSecretary
        }).ToList(),
        BudgetItems = budgetItems.Select(i => new ProposalBudgetItemDto
        {
            Id = i.Id,
            Category = i.Category?.Name ?? "—",
            CategoryCode = i.Category?.Code,
            Amount = i.Amount,
            Note = i.Note
        }).ToList(),
        Documents = new List<ProposalDocumentDto>()
    };
}
