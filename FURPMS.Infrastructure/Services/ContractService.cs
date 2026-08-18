using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Sau Review 2 (điểm e): contract thuộc PROJECT — 1 project có thể ký NHIỀU hợp đồng
// (từng phần/giai đoạn). API vẫn nhận proposalId (FE giữ nguyên) → resolve project.
public class ContractService : IContractService
{
    private readonly IContractRepository _contracts;
    private readonly IProposalRepository _proposals;
    private readonly IClock _clock;
    private readonly ISystemSettingService _settings;

    private readonly INotifier _notifier;
    private readonly IDocumentRepository _documents;

    public ContractService(IContractRepository contracts, IProposalRepository proposals,
        IClock clock,
        ISystemSettingService settings,
        INotifier notifier,
        IDocumentRepository documents)
    {
        _documents = documents;
        _contracts = contracts;
        _proposals = proposals;
        _clock = clock;
        _settings = settings;
        _notifier = notifier;
    }

    private IQueryable<Contract> QueryWithProject() => _contracts.Query()
        .Include(c => c.Project)
            .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
        .Include(c => c.Project)
            .ThenInclude(p => p.PiUser);

    public async Task<IEnumerable<ContractListResponse>> GetListAsync(Guid? piUserId = null)
    {
        var query = QueryWithProject().AsQueryable();

        if (piUserId.HasValue)
            query = query.Where(c => c.Project.PiUserId == piUserId.Value);

        var contracts = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return contracts.Select(MapList);
    }

    public async Task<ContractDetailResponse> GetByIdAsync(Guid contractId)
    {
        var c = await QueryWithProject()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        return MapDetail(c);
    }

    public async Task<ContractDetailResponse> CreateAsync(CreateContractRequest request, Guid createdBy)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Budget)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        if (proposal.Status != ProposalStatus.Approved)
            throw new InvalidOperationException(
                $"Đề cương đang ở trạng thái {StatusText.Vi(proposal.Status)} — chỉ lập được hợp đồng cho đề cương ĐÃ DUYỆT.");

        var project = proposal.Project;
        var totalAmount = proposal.Budget?.TotalAmount ?? 0m;

        ValidateMaxExtension(request.MaxExtensionMonths, proposal.DurationMonths);

        var sideA = await _settings.GetStringAsync(
            SystemSettingKeys.ContractSideARepresentative,
            SystemSettingKeys.DefaultContractSideARepresentative);

        var contract = new Contract
        {
            ProjectId = project.Id,
            ContractNumber = request.ContractNumber,
            ScopeTitle = request.ScopeTitle,
            TotalAmount = totalAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OriginalEndDate = request.EndDate,
            MaxExtensionMonths = request.MaxExtensionMonths,
            // `??` KHÔNG đủ: form gửi lên chuỗi RỖNG chứ không phải null khi người dùng để trống,
            // mà "" ?? sideA vẫn ra "". Người đại diện cấu hình trong Cài đặt vì thế chưa bao giờ
            // được dùng — Staff phải gõ lại đúng cái tên đó mỗi lần lập hợp đồng.
            SideARepresentative = string.IsNullOrWhiteSpace(request.SideARepresentative)
                ? sideA
                : request.SideARepresentative.Trim(),
            EcontractUrl = request.EcontractUrl,
            Status = ContractStatus.PendingSignature,
            CreatedBy = createdBy
        };
        await _contracts.AddAsync(contract);
        await _contracts.SaveChangesAsync();

        // Gắn các deliverable của project CHƯA thuộc hợp đồng nào vào hợp đồng này
        // (contract ký một tập con sản phẩm — mặc định lấy hết phần chưa ký).
        var unassigned = await _proposals.Deliverables
            .Where(d => d.ProjectId == project.Id && d.ContractId == null)
            .OrderBy(d => d.Sequence)
            .ToListAsync();
        foreach (var d in unassigned)
        {
            d.ContractId = contract.Id;
            d.AcceptanceStatus ??= AcceptanceStatus.Pending;
        }
        await _proposals.SaveChangesAsync();

        return await GetByIdAsync(contract.Id);
    }

    /// <summary>
    /// QĐ543 **Điều 10 khoản 4**: *"Gia hạn tối đa 1/2 tổng thời gian thực hiện của đề tài được
    /// phê duyệt"*. Trần phụ thuộc từng đề tài, KHÔNG phải con số 6 tháng cố định — 6 chỉ đúng khi
    /// đề tài dài 12 tháng (Mẫu 1 giới hạn "không quá 12 tháng" nên 6 là ca hay gặp nhất).
    /// </summary>
    private static void ValidateMaxExtension(int requested, int durationMonths)
    {
        if (requested < 0)
            throw new ArgumentException("Số tháng gia hạn tối đa không được âm.");
        if (durationMonths <= 0) return;   // đề cương chưa ghi thời gian → không có gì để đối chiếu

        var cap = durationMonths / 2;
        if (requested > cap)
            throw new ArgumentException(
                $"Gia hạn tối đa không được quá 1/2 thời gian thực hiện (QĐ543 Điều 10.4). " +
                $"Đề tài {durationMonths} tháng ⇒ tối đa {cap} tháng, đang nhập {requested}.");
    }

    /// <summary>
    /// Sửa hợp đồng. Trước đây không hề có endpoint này: Staff gõ sai số HĐ hay ngày là **kẹt
    /// vĩnh viễn**, chỉ còn cách tạo hợp đồng mới đè lên.
    ///
    /// Chỉ sửa phần "giấy tờ" Staff tự gõ. Không đụng `TotalAmount` (lấy từ dự toán đề cương) và
    /// không đụng `ProjectId`.
    /// </summary>
    public async Task<ContractDetailResponse> UpdateAsync(Guid contractId, UpdateContractRequest request)
    {
        var contract = await QueryWithProject()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        // QĐ543 BM05 Điều 6.1: *"Các sửa đổi, bổ sung phải lập thành văn bản PHỤ LỤC có đầy đủ chữ
        // ký của các bên"* — và phải báo trước 15 ngày làm việc. Sửa đè bản đã ký là làm sai lệch
        // văn bản hai bên đã ký, còn tệ hơn không cho sửa (rule #21).
        if (contract.Status != ContractStatus.PendingSignature)
            throw new InvalidOperationException(
                $"Hợp đồng đã ký (trạng thái {StatusText.Vi(contract.Status)}) — không sửa đè được. " +
                "Theo QĐ543 (BM05 Điều 6.1), mọi thay đổi sau khi ký phải lập thành PHỤ LỤC có chữ ký " +
                "hai bên. Hãy dùng mục \"Điều chỉnh\" để tạo đơn điều chỉnh.");

        if (string.IsNullOrWhiteSpace(request.ContractNumber))
            throw new ArgumentException("Số hợp đồng không được để trống.");
        if (request.EndDate <= request.StartDate)
            throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");
        // Trần gia hạn suy từ thời gian thực hiện của đề cương hiện hành của đề tài.
        ValidateMaxExtension(request.MaxExtensionMonths,
            contract.Project.Proposals.FirstOrDefault()?.DurationMonths ?? 0);

        // Số HĐ là thứ đối chiếu với bản giấy — trùng số thì không tra ra được hợp đồng nào là hợp đồng nào.
        var number = request.ContractNumber.Trim();
        if (await _contracts.Query().AnyAsync(c => c.Id != contractId && c.ContractNumber == number))
            throw new InvalidOperationException($"Số hợp đồng \"{number}\" đã tồn tại.");

        /*
         * OriginalEndDate là mốc để biết "đã gia hạn hay chưa" (FE hiện nhãn "Đã gia hạn — hạn gốc…").
         * Nếu hợp đồng CHƯA từng gia hạn thì Staff sửa hạn ở đây là **sửa cho đúng**, phải dời cả
         * hạn gốc — không thì màn hình sẽ báo "đã gia hạn" trong khi chẳng ai xin gia hạn cả.
         * Đã gia hạn rồi thì giữ nguyên hạn gốc để không xoá dấu vết.
         */
        var neverExtended = contract.EndDate == contract.OriginalEndDate;

        contract.ContractNumber = number;
        contract.ScopeTitle = request.ScopeTitle;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        if (neverExtended) contract.OriginalEndDate = request.EndDate;
        contract.MaxExtensionMonths = request.MaxExtensionMonths;
        // Để trống khi SỬA cũng rơi về người đại diện mặc định, không xoá trắng ô đang có.
        contract.SideARepresentative = string.IsNullOrWhiteSpace(request.SideARepresentative)
            ? await _settings.GetStringAsync(
                SystemSettingKeys.ContractSideARepresentative,
                SystemSettingKeys.DefaultContractSideARepresentative)
            : request.SideARepresentative.Trim();
        contract.EcontractUrl = request.EcontractUrl;
        contract.UpdatedAt = DateTime.UtcNow;

        await _contracts.SaveChangesAsync();
        return await GetByIdAsync(contractId);
    }

    /// <summary>
    /// Xoá hợp đồng nhập nhầm. **Chỉ khi chưa ký** — ký rồi là đã có hiệu lực pháp lý, xoá đi
    /// thì mất luôn dấu vết; muốn dừng thì dùng chấm dứt (`TerminatedAt`), không phải xoá.
    ///
    /// Chặn tiếp nếu PI đã bắt đầu làm việc trên hợp đồng đó (nộp sản phẩm / báo cáo tiến độ /
    /// báo cáo tổng kết / đã có quyết toán hoặc đơn điều chỉnh) — xoá là mất trắng công của người ta.
    /// </summary>
    public async Task DeleteAsync(Guid contractId)
    {
        var contract = await _contracts.Query()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (contract.Status != ContractStatus.PendingSignature)
            throw new InvalidOperationException(
                $"Hợp đồng đã ở trạng thái \"{StatusText.Vi(contract.Status)}\" — chỉ xoá được hợp đồng CHƯA KÝ. " +
                "Hợp đồng đang hiệu lực thì dùng chấm dứt hợp đồng.");

        var blockers = new List<string>();
        if (await _contracts.Deliverables.AnyAsync(d => d.ContractId == contractId && d.SubmittedAt != null))
            blockers.Add("đã có sản phẩm được nộp");
        if (await _contracts.ProgressReports.AnyAsync(r => r.ContractId == contractId && r.SubmittedAt != null))
            blockers.Add("đã có báo cáo tiến độ được nộp");
        // FinalReport gắn theo ĐỀ TÀI (không có cột ContractId) — vẫn phải chặn, vì xoá hợp đồng
        // là kéo theo mất lịch giải ngân/kỳ báo cáo mà bản tổng kết đang tổng hợp lên.
        if (await _contracts.FinalReports.AnyAsync(r => r.ProjectId == contract.ProjectId))
            blockers.Add("đã có báo cáo tổng kết");
        if (await _contracts.Settlements.AnyAsync(s => s.ContractId == contractId))
            blockers.Add("đã có quyết toán");
        if (await _contracts.Amendments.AnyAsync(a => a.ContractId == contractId))
            blockers.Add("đã có đơn đề nghị điều chỉnh");
        if (blockers.Count > 0)
            throw new InvalidOperationException(
                $"Không xoá được hợp đồng: {string.Join(", ", blockers)}.");

        // Sản phẩm KHÔNG xoá theo — nó thuộc về đề tài, chỉ gỡ khỏi hợp đồng để hợp đồng sau gắn lại.
        var deliverables = await _contracts.Deliverables.Where(d => d.ContractId == contractId).ToListAsync();
        foreach (var d in deliverables) d.ContractId = null;

        // Lịch giải ngân + kỳ báo cáo là do hệ thống tự sinh cho hợp đồng này, không có ý nghĩa
        // khi đứng một mình. FK không cascade nên phải xoá tay, không thì SaveChanges nổ.
        var reports = await _contracts.ProgressReports.Where(r => r.ContractId == contractId).ToListAsync();
        var reportIds = reports.Select(r => r.Id).ToList();
        _contracts.RemoveProgressReportItemsRange(
            await _contracts.ProgressReportItems.Where(i => reportIds.Contains(i.ReportId)).ToListAsync());
        _contracts.RemoveProgressReportsRange(reports);
        _contracts.RemoveDisbursementsRange(
            await _contracts.Disbursements.Where(d => d.ContractId == contractId).ToListAsync());

        _contracts.Remove(contract);
        await _contracts.SaveChangesAsync();
    }

    /// <summary>
    /// <b>Ghi nhận đã ký</b> — hệ thống KHÔNG ký thay ai.
    /// <para>
    /// QĐ543 <b>BM05 Điều 7.2–7.3</b>: hợp đồng *"được thực hiện qua phương thức **ký điện tử trên
    /// phần mềm Econtract**"* và *"các bên tự bảo quản và lưu trữ"*. Việc ký diễn ra ở phần mềm
    /// NGOÀI; phần của hệ thống này là sinh đúng biểu mẫu rồi <b>giữ bản đã ký làm bằng chứng</b>
    /// (rule #21).
    /// </para>
    /// <para>
    /// Vì vậy bắt buộc phải có <b>ít nhất một tài liệu đính kèm</b> trước khi ghi nhận. Trước đây
    /// bấm một nút là hợp đồng thành "Đang hiệu lực" dù chưa ai ký gì — hỏi "bằng chứng ký đâu" là
    /// không có gì đưa ra.
    /// </para>
    /// </summary>
    /// <param name="signedOn">
    /// Ngày ký GHI TRÊN GIẤY. Khác ngày bấm nút (có thể ký hôm trước, nhập hôm sau) và mọi mốc hợp
    /// đồng phải tính theo ngày này. Bỏ trống thì lấy ngày hệ thống.
    /// </param>
    public async Task<ContractDetailResponse> SignAsync(Guid contractId, Guid signedBy, DateOnly? signedOn = null)
    {
        var contract = await QueryWithProject()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (contract.Status != ContractStatus.PendingSignature)
            throw new InvalidOperationException(
                $"Hợp đồng đang ở trạng thái {StatusText.Vi(contract.Status)} — chỉ ký được hợp đồng đang chờ ký.");

        var hasSignedCopy = await _documents.Query()
            .AnyAsync(d => d.EntityType == "Contract" && d.EntityId == contractId.ToString() && !d.IsDeleted);
        if (!hasSignedCopy)
            throw new InvalidOperationException(
                "Chưa có bản hợp đồng đã ký. Theo QĐ543 (BM05 Điều 7.2) hợp đồng được ký điện tử ở " +
                "phần mềm ngoài, hệ thống chỉ lưu bản đã ký làm minh chứng. " +
                "Hãy: Xuất hợp đồng (Word) → ký ngoài → tải bản đã ký lên mục \"Hồ sơ hợp đồng đã ký\" rồi ghi nhận lại.");

        var signDate = signedOn ?? DateOnly.FromDateTime(_clock.UtcNow);
        if (signDate > DateOnly.FromDateTime(_clock.UtcNow))
            throw new ArgumentException("Ngày ký không được ở tương lai.");

        contract.Status = ContractStatus.Active;
        contract.SignedAt = new DateTime(signDate.Year, signDate.Month, signDate.Day, 0, 0, 0, DateTimeKind.Utc);
        contract.UpdatedAt = DateTime.UtcNow;

        // Ký hợp đồng → project sang giai đoạn thực hiện.
        contract.Project.Status = ProjectStatus.InProgress;
        contract.Project.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        // Đây là lúc đề tài chính thức được thực hiện và đồng hồ tiến độ bắt đầu chạy — chủ nhiệm
        // phải biết ngay. Trước đây ký xong hệ thống im lặng, chủ nhiệm chỉ phát hiện khi tự vào
        // xem, mà hạn nộp báo cáo kỳ 1 thì đã tính từ ngày này.
        await _notifier.NotifyAsync(
            contract.Project.PiUserId,
            "CONTRACT_SIGNED",
            "Hợp đồng nghiên cứu đã được ký",
            $"Hợp đồng {contract.ContractNumber ?? ""} cho đề tài \"{contract.Project.TitleVi}\" đã được ký. " +
            $"Thời gian thực hiện {contract.StartDate:dd/MM/yyyy} – {contract.EndDate:dd/MM/yyyy}. " +
            "Hãy theo dõi các mốc báo cáo tiến độ trong mục Tiến trình đề tài.",
            actionUrl: "/my-timeline",
            entityType: "Contract",
            entityId: contract.Id.ToString(),
            priority: "HIGH");

        return MapDetail(contract);
    }

    /// <summary>
    /// Chấm dứt bất thường một hợp đồng đang thực hiện. Đây là quyết định có dấu vết, không phải
    /// nút bật/tắt: muốn sửa một quyết định đã chấm dứt phải có quy trình/biên bản khác.
    /// </summary>
    public async Task<ContractDetailResponse> TerminateAsync(Guid contractId, Guid terminatedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Phải nêu lý do chấm dứt hợp đồng.");

        var contract = await QueryWithProject()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (contract.Status == ContractStatus.PendingSignature)
            throw new InvalidOperationException(
                "Hợp đồng chưa ký nên chưa có hiệu lực để chấm dứt. Nếu lập nhầm, hãy xoá hợp đồng.");
        if (contract.Status == ContractStatus.Settled)
            throw new InvalidOperationException("Hợp đồng đã thanh lý — không thể chuyển sang chấm dứt.");
        if (contract.Status == ContractStatus.Terminated || contract.TerminatedAt.HasValue)
            throw new InvalidOperationException("Hợp đồng đã được chấm dứt trước đó.");
        if (contract.Project.Status == ProjectStatus.Completed)
            throw new InvalidOperationException(
                "Đề tài đã nghiệm thu Đạt — hãy hoàn tất quyết toán và thanh lý BM13, không chuyển sang chấm dứt bất thường.");

        contract.Status = ContractStatus.Terminated;
        contract.TerminatedAt = _clock.UtcNow;
        contract.TerminatedBy = terminatedBy;
        contract.TerminatedReason = reason.Trim();
        contract.UpdatedAt = _clock.UtcNow;

        contract.Project.Status = ProjectStatus.Terminated;
        contract.Project.UpdatedAt = _clock.UtcNow;
        await _contracts.SaveChangesAsync();

        await _notifier.NotifyAsync(
            contract.Project.PiUserId,
            "CONTRACT_TERMINATED",
            "Hợp đồng nghiên cứu đã chấm dứt",
            $"Hợp đồng {contract.ContractNumber} của đề tài \"{contract.Project.TitleVi}\" đã được ghi nhận chấm dứt. " +
            $"Lý do: {contract.TerminatedReason}",
            actionUrl: "/my-timeline",
            entityType: "Contract",
            entityId: contract.Id.ToString(),
            priority: "HIGH");

        return MapDetail(contract);
    }

    private static ContractListResponse MapList(Contract c) => new()
    {
        Id = c.Id,
        ContractNumber = c.ContractNumber,
        ProjectId = c.ProjectId,
        ProposalId = c.Project?.Proposals.FirstOrDefault()?.Id ?? Guid.Empty,
        ProposalCode = c.Project?.ProjectCode,
        ProposalTitle = c.Project?.TitleVi,
        PiName = c.Project?.PiUser?.FullName,
        Status = c.Status,
        ProjectStatus = c.Project?.Status,
        TotalAmount = c.TotalAmount,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        OriginalEndDate = c.OriginalEndDate,
        MaxExtensionMonths = c.MaxExtensionMonths,
        SignedAt = c.SignedAt,
        CreatedAt = c.CreatedAt
    };

    private static ContractDetailResponse MapDetail(Contract c) => new()
    {
        Id = c.Id,
        ContractNumber = c.ContractNumber,
        ProjectId = c.ProjectId,
        ProposalId = c.Project?.Proposals.FirstOrDefault()?.Id ?? Guid.Empty,
        ProposalCode = c.Project?.ProjectCode,
        ProposalTitle = c.Project?.TitleVi,
        FundingMethod = c.Project?.Proposals.FirstOrDefault()?.FundingMethod,
        ScopeTitle = c.ScopeTitle,
        Status = c.Status,
        ProjectStatus = c.Project?.Status,
        TotalAmount = c.TotalAmount,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        OriginalEndDate = c.OriginalEndDate,
        MaxExtensionMonths = c.MaxExtensionMonths,
        SideARepresentative = c.SideARepresentative,
        EcontractUrl = c.EcontractUrl,
        SignedAt = c.SignedAt,
        TerminatedAt = c.TerminatedAt,
        TerminatedReason = c.TerminatedReason,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
