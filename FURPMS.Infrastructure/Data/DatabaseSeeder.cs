using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly FURPMSDbContext _db;

    public DatabaseSeeder(FURPMSDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
        await SeedPersonnelRoleTypesAsync();
        await SeedBudgetExpenseCategoriesAsync();
        await SeedAmendmentCategoriesAsync();
        await SeedSystemFinancialConfigsAsync();
        await SeedDemoProposalAsync();
        await EnsureDemoCycleOpenAsync();
        await SeedDemoAccountsAsync();
        await SeedRubricCriteriaAsync();
        await SeedApprovedScenarioAsync();
    }

    // Đảm bảo chu kỳ demo ở trạng thái OPEN để PI có thể tạo/nộp đề xuất khi test.
    private async Task EnsureDemoCycleOpenAsync()
    {
        var cycle = await _db.ResearchCycles
            .FirstOrDefaultAsync(c => c.CycleYear == 2026 && c.SemesterCode == "SU26");
        if (cycle == null) return;

        var changed = false;
        if (cycle.Status != CycleStatus.Open && cycle.Status != CycleStatus.Closed)
        {
            cycle.Status = CycleStatus.Open;
            changed = true;
        }
        // Seed lại cửa sổ nộp về hiện tại nếu hạn đã qua (tránh nhìn "quá khứ").
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (cycle.SubmissionDeadline < today)
        {
            cycle.SubmissionOpenDate = today.AddDays(-7);
            cycle.SubmissionDeadline = today.AddDays(60);
            cycle.ReviewDeadline = today.AddDays(120);
            changed = true;
        }
        if (changed)
        {
            cycle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    // Tài khoản mẫu cho 4 vai trò (idempotent theo email). Mật khẩu in trong README/đăng nhập.
    private async Task SeedDemoAccountsAsync()
    {
        var unit = await GetOrCreateDemoUnitAsync();

        var accounts = new[]
        {
            (Email: "staff.demo@furpms.edu.vn",     Name: "Trần Thị Quản Lý",   Role: "Staff",           Pwd: "Staff@123456"),
            (Email: "reviewer1.demo@furpms.edu.vn", Name: "PGS.TS Lê Phản Biện", Role: "ReviewCommittee", Pwd: "Reviewer@123456"),
            (Email: "reviewer2.demo@furpms.edu.vn", Name: "TS. Phạm Hội Đồng",   Role: "ReviewCommittee", Pwd: "Reviewer@123456"),
            (Email: "reviewer3.demo@furpms.edu.vn", Name: "TS. Vũ Thẩm Định",    Role: "ReviewCommittee", Pwd: "Reviewer@123456"),
            (Email: "pi2.demo@furpms.edu.vn",       Name: "Hoàng Văn Bình",      Role: "Faculty",         Pwd: "Faculty@123456"),
        };

        foreach (var a in accounts)
        {
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == a.Email))
                continue;

            var role = await _db.Roles.FirstAsync(r => r.Name == a.Role);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = a.Email,
                FullName = a.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(a.Pwd, workFactor: 12),
                Status = UserStatus.Active,
                UnitId = unit.Id,
                IsExternal = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }
    }

    // Bộ tiêu chí chấm (rubric) cho vòng xét duyệt — để reviewer chấm điểm được.
    private async Task SeedRubricCriteriaAsync()
    {
        if (await _db.RubricTemplates.AnyAsync(t => t.TemplateType == "REVIEW"))
            return;

        var template = new RubricTemplate
        {
            TemplateType = "REVIEW",
            Name = "Phiếu chấm xét duyệt đề tài",
            MaxTotalScore = 100m,
            IsActive = true
        };
        _db.RubricTemplates.Add(template);
        await _db.SaveChangesAsync();

        _db.RubricCriteria.AddRange(
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Tính cấp thiết, ý nghĩa khoa học và thực tiễn", MaxScore = 20m, Sequence = 1, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Mục tiêu, nội dung và phương pháp nghiên cứu",     MaxScore = 30m, Sequence = 2, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Tính mới, tính sáng tạo",                          MaxScore = 20m, Sequence = 3, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Sản phẩm và khả năng ứng dụng",                    MaxScore = 20m, Sequence = 4, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Tính hợp lý của kinh phí và tiến độ",              MaxScore = 10m, Sequence = 5, IsActive = true }
        );
        await _db.SaveChangesAsync();
    }

    // Kịch bản "đã duyệt + đang thực hiện hợp đồng" (idempotent theo ProjectCode DEMO-2026-002)
    // để demo các màn sau duyệt: hợp đồng (có PHASE), giải ngân, sản phẩm/nghiệm thu.
    private async Task SeedApprovedScenarioAsync()
    {
        const string code = "DEMO-2026-002";
        if (await _db.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == code))
            return;

        var admin = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == "admin@furpms.edu.vn");
        var pi = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "pi.demo@furpms.edu.vn");
        var cycle = await _db.ResearchCycles.FirstOrDefaultAsync(c => c.CycleYear == 2026 && c.SemesterCode == "SU26");
        var track = await _db.ResearchTracks.FirstOrDefaultAsync(t => t.Code == "AI");
        var rt = await _db.ResearchTypes.FirstOrDefaultAsync(t => t.Code == "APPLIED");
        var unit = await _db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Code == "DEMO-UNIT");
        if (pi == null || cycle == null || track == null || rt == null || unit == null)
            return; // SeedDemoProposalAsync chạy trước đã tạo các bản ghi này

        var category = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Code == "ARTICLE");
        if (category == null)
        {
            category = new ProductCategory { Code = "ARTICLE", Name = "Bài báo khoa học", IsActive = true };
            _db.ProductCategories.Add(category);
            await _db.SaveChangesAsync();
        }

        var cycleTrack = await GetOrCreateCycleTrackAsync(cycle.Id, track.Id);
        var defaultOrder = await GetOrCreateDefaultOrderAsync(cycle.Id, unit.Id, admin.Id);

        var now = DateTime.UtcNow;

        // PROJECT (gốc) — đang thực hiện hợp đồng
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = code,
            CycleTrackId = cycleTrack.Id,
            OrderId = defaultOrder.Id,
            PiUserId = pi.Id,
            HostingUnitId = unit.Id,
            ResearchTypeId = rt.Id,
            TitleVi = "Hệ thống khuyến nghị học liệu cá nhân hoá dựa trên học sâu",
            TitleEn = "Personalized Learning Material Recommendation using Deep Learning",
            Status = ProjectStatus.InProgress,
            PlannedStartDate = new DateOnly(2026, 3, 1),
            PlannedEndDate = new DateOnly(2027, 3, 1),
            CreatedAt = now.AddDays(-70),
            UpdatedAt = now
        };
        _db.Projects.Add(project);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = project.TitleVi,
            TitleEn = project.TitleEn,
            AbstractVi = "Đề tài xây dựng hệ thống khuyến nghị học liệu cá nhân hoá cho sinh viên dựa trên mô hình học sâu; đã được hội đồng phê duyệt và ký hợp đồng triển khai.",
            ResearchObjectives = "1. Xây dựng mô hình khuyến nghị cá nhân hoá.\n2. Tích hợp vào hệ thống LMS.\n3. Đánh giá hiệu quả trên dữ liệu thực tế.",
            Methodology = "Học sâu (collaborative filtering + content-based), thử nghiệm A/B trên dữ liệu thực tế.",
            ExpectedOutput = "01 bài báo hội nghị quốc tế, 01 hệ thống tích hợp LMS.",
            DurationMonths = 12,
            PlannedStartDate = new DateOnly(2026, 3, 1),
            PlannedEndDate = new DateOnly(2027, 3, 1),
            FundingMethod = FundingMethod.Partial,
            Status = ProposalStatus.Approved,
            SubmittedAt = now.AddDays(-60),
            ReviewedAt = now.AddDays(-30),
            ApprovedAt = now.AddDays(-30),
            ApprovedBy = admin.Id,
            CreatedAt = now.AddDays(-70),
            UpdatedAt = now
        };
        _db.Proposals.Add(proposal);
        await _db.SaveChangesAsync();

        _db.ProposalBudgets.Add(new ProposalBudget { ProposalId = proposal.Id, TotalAmount = 450_000_000m });
        _db.ProjectMembers.AddRange(
            new ProjectMember { ProjectId = project.Id, FullName = pi.FullName, Email = pi.Email, UnitName = "Khoa CNTT", WorkContent = "Chủ nhiệm", WorkMonths = 8, IsPi = true, Sequence = 1 },
            new ProjectMember { ProjectId = project.Id, FullName = "Đỗ Thị Em", Email = "em.dt@fpt.edu.vn", UnitName = "SE", WorkContent = "Thành viên chính", WorkMonths = 5, Sequence = 2 }
        );
        await _db.SaveChangesAsync();

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = "HD-2026-002",
            ScopeTitle = "Toàn bộ nội dung đề tài (2 giai đoạn)",
            Status = ContractStatus.Active,
            SignedAt = now.AddDays(-25),
            TotalAmount = 450_000_000m,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2027, 3, 1),
            OriginalEndDate = new DateOnly(2027, 3, 1),
            MaxExtensionMonths = 6,
            SideARepresentative = "Nguyễn Kim Ánh",
            CreatedBy = admin.Id,
            CreatedAt = now.AddDays(-26),
            UpdatedAt = now
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        // Giai đoạn thực hiện trong hợp đồng (Review 2 điểm e)
        var phase1 = new ContractPhase { ContractId = contract.Id, PhaseNo = 1, Name = "Giai đoạn 1 — Mô hình & bài báo", StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 9, 1), Amount = 270_000_000m, Status = "IN_PROGRESS" };
        var phase2 = new ContractPhase { ContractId = contract.Id, PhaseNo = 2, Name = "Giai đoạn 2 — Tích hợp LMS & nghiệm thu", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2027, 3, 1), Amount = 180_000_000m, Status = "PLANNED" };
        _db.ContractPhases.AddRange(phase1, phase2);
        await _db.SaveChangesAsync();

        // Sản phẩm của PROJECT — gắn hợp đồng + phase (gộp expected_product + deliverable)
        var deliv1 = new ProjectDeliverable { ProjectId = project.Id, ContractId = contract.Id, ContractPhaseId = phase1.Id, CategoryId = category.Id, ProductName = "Bài báo hội nghị quốc tế", DueDate = new DateOnly(2026, 9, 1), AcceptanceStatus = AcceptanceStatus.Passed, IsCompleted = true, SubmittedAt = now.AddDays(-10), FileUrl = "https://example.com/paper.pdf", QualityAssessment = "Đạt yêu cầu, đã được chấp nhận đăng", Sequence = 1 };
        var deliv2 = new ProjectDeliverable { ProjectId = project.Id, ContractId = contract.Id, ContractPhaseId = phase2.Id, CategoryId = category.Id, ProductName = "Hệ thống tích hợp LMS", DueDate = new DateOnly(2027, 2, 1), AcceptanceStatus = AcceptanceStatus.Pending, IsCompleted = false, Sequence = 2 };
        _db.ProjectDeliverables.AddRange(deliv1, deliv2);
        await _db.SaveChangesAsync();

        _db.ContractDisbursements.AddRange(
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 1, PhaseId = phase1.Id, Percentage = 40m, PlannedAmount = 180_000_000m, ActualAmount = 180_000_000m, ConditionDescription = "Tạm ứng sau khi ký hợp đồng", Status = DisbursementStatus.Disbursed, DisbursedAt = now.AddDays(-24), BankReference = "FT2026030100123" },
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 2, PhaseId = phase1.Id, Percentage = 40m, PlannedAmount = 180_000_000m, ConditionDescription = "Sau khi nghiệm thu sản phẩm giữa kỳ", Status = DisbursementStatus.Pending, DeliverableId = deliv1.Id },
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 3, PhaseId = phase2.Id, Percentage = 20m, PlannedAmount = 90_000_000m,  ConditionDescription = "Sau khi nghiệm thu cuối kỳ", Status = DisbursementStatus.Pending, DeliverableId = deliv2.Id }
        );
        await _db.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roleNames = new[] { "Admin", "Staff", "Faculty", "ReviewCommittee" };
        foreach (var name in roleNames)
        {
            if (!await _db.Roles.AnyAsync(r => r.Name == name))
                _db.Roles.Add(new Role { Name = name, Description = name + " role" });
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedAdminAsync()
    {
        const string adminEmail = "admin@furpms.edu.vn";
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == adminEmail))
            return;

        var adminRole = await _db.Roles.FirstAsync(r => r.Name == "Admin");

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            FullName = "System Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456", workFactor: 12),
            Status = UserStatus.Active,
            IsExternal = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        _db.UserRoles.Add(new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedPersonnelRoleTypesAsync()
    {
        var roles = new[]
        {
            new PersonnelRoleType { Code = "CNNV", Name = "Chủ nhiệm nghiên cứu viên", DefaultCoefficient = 0.79m, IsActive = true },
            new PersonnelRoleType { Code = "TKKH", Name = "Thư ký khoa học",           DefaultCoefficient = 0.49m, IsActive = true },
            new PersonnelRoleType { Code = "TVC",  Name = "Thành viên chính",           DefaultCoefficient = 0.49m, IsActive = true },
            new PersonnelRoleType { Code = "TV",   Name = "Thành viên",                 DefaultCoefficient = 0.25m, IsActive = true },
            new PersonnelRoleType { Code = "KTV",  Name = "Kỹ thuật viên",              DefaultCoefficient = 0.30m, IsActive = true },
        };

        foreach (var r in roles)
        {
            if (!await _db.PersonnelRoleTypes.AnyAsync(x => x.Code == r.Code))
                _db.PersonnelRoleTypes.Add(r);
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedBudgetExpenseCategoriesAsync()
    {
        var categories = new[]
        {
            new BudgetExpenseCategory { Code = "LABOR",            Name = "Công lao động trực tiếp",                      Sequence = 1,  IsActive = true },
            new BudgetExpenseCategory { Code = "MATERIALS",        Name = "Nguyên, nhiên liệu, vật tư, phụ tùng",         Sequence = 2,  IsActive = true },
            new BudgetExpenseCategory { Code = "DOMESTIC_TRAVEL",  Name = "Công tác trong nước",                          Sequence = 3,  IsActive = true },
            new BudgetExpenseCategory { Code = "SURVEY",           Name = "Chi điều tra, khảo sát",                       Sequence = 4,  IsActive = true },
            new BudgetExpenseCategory { Code = "OFFICE_PRINTING",  Name = "Chi văn phòng phẩm, in ấn",                    Sequence = 5,  IsActive = true },
            new BudgetExpenseCategory { Code = "CONFERENCE",       Name = "Chi hội thảo khoa học",                        Sequence = 6,  IsActive = true },
            new BudgetExpenseCategory { Code = "ADVISORY_COUNCIL", Name = "Chi Hội đồng tư vấn",                          Sequence = 7,  IsActive = true },
            new BudgetExpenseCategory { Code = "OUTSOURCED",       Name = "Dịch vụ thuê ngoài phục vụ nghiên cứu",        Sequence = 8,  IsActive = true },
            new BudgetExpenseCategory { Code = "FIXED_ASSETS",     Name = "Sửa chữa, mua sắm tài sản cố định",            Sequence = 9,  IsActive = true },
            new BudgetExpenseCategory { Code = "OVERSEAS_TRAVEL",  Name = "Chi đoàn ra",                                  Sequence = 10, IsActive = true },
            new BudgetExpenseCategory { Code = "OTHER",            Name = "Chi khác",                                     Sequence = 11, IsActive = true },
            new BudgetExpenseCategory { Code = "MANAGEMENT_FEE",   Name = "Chi quản lý phí cơ quan chủ trì",              Sequence = 12, IsActive = true },
        };

        foreach (var c in categories)
        {
            if (!await _db.BudgetExpenseCategories.AnyAsync(x => x.Code == c.Code))
                _db.BudgetExpenseCategories.Add(c);
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedAmendmentCategoriesAsync()
    {
        var cats = new[]
        {
            new AmendmentCategory { Code = "EXTENSION",     Name = "Gia hạn thời gian thực hiện", IsActive = true },
            new AmendmentCategory { Code = "BUDGET_ADJUST", Name = "Điều chỉnh kinh phí",         IsActive = true },
            new AmendmentCategory { Code = "SCOPE_CHANGE",  Name = "Thay đổi nội dung nghiên cứu", IsActive = true },
            new AmendmentCategory { Code = "TEAM_CHANGE",   Name = "Thay đổi thành viên nhóm",    IsActive = true },
            new AmendmentCategory { Code = "OTHER",         Name = "Điều chỉnh khác",              IsActive = true },
        };
        foreach (var c in cats)
        {
            if (!await _db.AmendmentCategories.AnyAsync(x => x.Code == c.Code))
                _db.AmendmentCategories.Add(c);
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedSystemFinancialConfigsAsync()
    {
        if (!await _db.SystemFinancialConfigs.AnyAsync(x => x.Code == "BASE_DAILY_SALARY"))
        {
            _db.SystemFinancialConfigs.Add(new SystemFinancialConfig
            {
                Code = "BASE_DAILY_SALARY",
                Value = 1_490_000m,
                Description = "Mức lương cơ bản ngày (QĐ 543/QĐ-ĐHFPT)",
                EffectiveDate = new DateOnly(2025, 1, 1),
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }
    }

    // ── Helpers (project-centric) ────────────────────────────────────────────

    private async Task<OrganizationalUnit> GetOrCreateDemoUnitAsync()
    {
        var unit = await _db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Code == "DEMO-UNIT");
        if (unit != null) return unit;

        unit = new OrganizationalUnit
        {
            Name = "Khoa Công nghệ Thông tin (Demo)",
            Code = "DEMO-UNIT",
            UnitType = "FACULTY",
            IsActive = true,
            SortOrder = 99
        };
        _db.OrganizationalUnits.Add(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    // Review 2 điểm (b): quan hệ đợt–track.
    private async Task<CycleTrack> GetOrCreateCycleTrackAsync(int cycleId, int trackId)
    {
        var ct = await _db.CycleTracks.FirstOrDefaultAsync(x => x.CycleId == cycleId && x.TrackId == trackId);
        if (ct != null) return ct;

        ct = new CycleTrack { CycleId = cycleId, TrackId = trackId, IsOpen = true };
        _db.CycleTracks.Add(ct);
        await _db.SaveChangesAsync();
        return ct;
    }

    // Review 2 điểm (d): order "Nghiên cứu tự do/cơ bản" mặc định — 100% project thuộc 1 order.
    private async Task<ResearchOrder> GetOrCreateDefaultOrderAsync(int cycleId, int unitId, Guid createdBy)
    {
        var order = await _db.ResearchOrders.FirstOrDefaultAsync(o => o.CycleId == cycleId && o.IsDefault);
        if (order != null) return order;

        order = new ResearchOrder
        {
            CycleId = cycleId,
            OrderingUnitId = unitId,
            ResearchArea = "Nghiên cứu tự do (đề xuất của PI)",
            ProblemDescription = "Order mặc định của đợt — gom các đề tài PI tự đề xuất (không có đơn vị đặt hàng cụ thể).",
            IsDefault = true,
            Status = "OPEN",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.ResearchOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // ── Demo project + proposal v1 (idempotent via ProjectCode "DEMO-2026-001") ──

    private async Task SeedDemoProposalAsync()
    {
        const string demoCode = "DEMO-2026-001";
        if (await _db.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == demoCode))
            return;

        var admin = await _db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Email == "admin@furpms.edu.vn");

        var unit = await GetOrCreateDemoUnitAsync();

        // ResearchType — APPLIED
        var rt = await _db.ResearchTypes.FirstOrDefaultAsync(x => x.Code == "APPLIED");
        if (rt == null)
        {
            rt = new ResearchType
            {
                Code = "APPLIED",
                Name = "Nghiên cứu ứng dụng",
                MaxBudgetCap = 900_000_000m,
                RequireOrderingUnit = false,
                RequirePublication = true,
                IsActive = true
            };
            _db.ResearchTypes.Add(rt);
            await _db.SaveChangesAsync();
        }

        // ResearchType — BASIC
        if (!await _db.ResearchTypes.AnyAsync(x => x.Code == "BASIC"))
        {
            _db.ResearchTypes.Add(new ResearchType
            {
                Code = "BASIC",
                Name = "Nghiên cứu cơ bản",
                MaxBudgetCap = 500_000_000m,
                RequireOrderingUnit = false,
                RequirePublication = false,
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }

        // ResearchTrack
        var track = await _db.ResearchTracks.FirstOrDefaultAsync(x => x.Code == "AI");
        if (track == null)
        {
            track = new ResearchTrack
            {
                Code = "AI",
                Name = "Trí tuệ nhân tạo",
                IsActive = true
            };
            _db.ResearchTracks.Add(track);
            await _db.SaveChangesAsync();
        }

        // ResearchCycle
        var cycle = await _db.ResearchCycles.FirstOrDefaultAsync(x => x.CycleYear == 2026 && x.SemesterCode == "SU26");
        if (cycle == null)
        {
            cycle = new ResearchCycle
            {
                CycleYear = 2026,
                SemesterCode = "SU26",
                ResearchTypeId = rt.Id,
                SubmissionOpenDate = new DateOnly(2026, 1, 1),
                SubmissionDeadline = new DateOnly(2026, 3, 31),
                ReviewDeadline = new DateOnly(2026, 5, 31),
                Status = CycleStatus.Open,
                Description = "Chu kỳ nghiên cứu SU26 (Demo)",
                CreatedBy = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.ResearchCycles.Add(cycle);
            await _db.SaveChangesAsync();
        }

        // Cycle ↔ Track (điểm b) + order mặc định (điểm d)
        var cycleTrack = await GetOrCreateCycleTrackAsync(cycle.Id, track.Id);
        var defaultOrder = await GetOrCreateDefaultOrderAsync(cycle.Id, unit.Id, admin.Id);

        // ResearchOrder (đặt hàng Applied) — demo cho luồng cạnh tranh nhiều PI → 1 winner
        if (!await _db.ResearchOrders.AnyAsync(o => o.CycleId == cycle.Id && !o.IsDefault))
        {
            _db.ResearchOrders.AddRange(
                new ResearchOrder
                {
                    CycleId = cycle.Id,
                    OrderingUnitId = unit.Id,
                    ResearchArea = "Ứng dụng AI trong quản lý đào tạo",
                    ProblemDescription = "Xây dựng hệ thống gợi ý lộ trình học tập cá nhân hóa.",
                    ExpectedProducts = "Phần mềm demo + báo cáo.",
                    Status = "OPEN",
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new ResearchOrder
                {
                    CycleId = cycle.Id,
                    OrderingUnitId = unit.Id,
                    ResearchArea = "Chuyển đổi số quy trình NCKH",
                    ProblemDescription = "Số hóa quy trình xét duyệt đề tài cấp trường.",
                    ExpectedProducts = "Quy trình + tài liệu hướng dẫn.",
                    Status = "OPEN",
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow
                });
            await _db.SaveChangesAsync();
        }

        // PI user
        const string piEmail = "pi.demo@furpms.edu.vn";
        var piUser = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == piEmail);
        if (piUser == null)
        {
            var facultyRole = await _db.Roles.FirstAsync(r => r.Name == "Faculty");
            piUser = new User
            {
                Id = Guid.NewGuid(),
                Email = piEmail,
                FullName = "Nguyễn Văn An",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Faculty@123456", workFactor: 12),
                Status = UserStatus.Active,
                UnitId = unit.Id,
                IsExternal = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(piUser);
            await _db.SaveChangesAsync();

            _db.UserRoles.Add(new UserRole { UserId = piUser.Id, RoleId = facultyRole.Id, AssignedAt = DateTime.UtcNow });
            _db.AcademicProfiles.Add(new AcademicProfile
            {
                UserId = piUser.Id,
                AcademicTitle = "TS",
                DegreeLevel = "Tiến sĩ",
                Specialization = "Khoa học máy tính",
                DateOfBirth = new DateOnly(1980, 5, 15),
                Gender = "Nam",
                Institution = "Trường Đại học FPT",
                InstitutionAddress = "Hòa Lạc, Hà Nội",
                Nationality = "Việt Nam",
                IsEligiblePi = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        // PROJECT (gốc) + Proposal v1
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = demoCode,
            CycleTrackId = cycleTrack.Id,
            OrderId = defaultOrder.Id,
            PiUserId = piUser.Id,
            HostingUnitId = unit.Id,
            ResearchTypeId = rt.Id,
            TitleVi = "Nghiên cứu ứng dụng học máy trong phân tích cảm xúc văn bản tiếng Việt",
            TitleEn = "Machine Learning for Vietnamese Sentiment Analysis",
            Status = ProjectStatus.UnderReview,
            PlannedStartDate = new DateOnly(2026, 7, 1),
            PlannedEndDate = new DateOnly(2028, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = project.TitleVi,
            TitleEn = project.TitleEn,
            AbstractVi = "Đề tài nghiên cứu phát triển hệ thống phân tích cảm xúc văn bản tiếng Việt sử dụng các mô hình học sâu hiện đại. Hệ thống hướng tới độ chính xác cao trên tập dữ liệu thực tế và có khả năng triển khai trong môi trường sản xuất.",
            ResearchObjectives = "1. Xây dựng bộ dữ liệu huấn luyện chất lượng cao cho bài toán phân tích cảm xúc tiếng Việt.\n2. Phát triển mô hình học sâu đạt F1-score >= 0.90 trên tập kiểm thử.\n3. Xây dựng API phục vụ phân tích cảm xúc theo thời gian thực.",
            DurationMonths = 18,
            PlannedStartDate = new DateOnly(2026, 7, 1),
            PlannedEndDate = new DateOnly(2028, 1, 1),
            FundingMethod = FundingMethod.Partial,
            Status = ProposalStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Proposals.Add(proposal);
        await _db.SaveChangesAsync();

        // Team members (thuộc PROJECT)
        var member1 = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = piUser.Id,
            FullName = "Nguyễn Văn An",
            AcademicTitle = "TS",
            UnitName = "Khoa CNTT",
            WorkContent = "Chủ nhiệm đề tài, xây dựng mô hình học máy, viết báo cáo",
            WorkMonths = 18,
            IsPi = true,
            IsSecretary = false,
            MemberRoleCode = "CNNV",
            SalaryCoefficient = 0.79m,
            Sequence = 1
        };
        var member2 = new ProjectMember
        {
            ProjectId = project.Id,
            FullName = "Trần Thị Bình",
            AcademicTitle = "ThS",
            UnitName = "Khoa CNTT",
            WorkContent = "Thư ký khoa học, xử lý dữ liệu, kiểm thử",
            WorkMonths = 12,
            IsPi = false,
            IsSecretary = true,
            MemberRoleCode = "TKKH",
            SalaryCoefficient = 0.49m,
            Sequence = 2
        };
        var member3 = new ProjectMember
        {
            ProjectId = project.Id,
            FullName = "Lê Minh Cường",
            AcademicTitle = "KS",
            UnitName = "Trung tâm AI",
            WorkContent = "Phát triển API, triển khai hệ thống",
            WorkMonths = 9,
            IsPi = false,
            IsSecretary = false,
            MemberRoleCode = "TVC",
            SalaryCoefficient = 0.49m,
            Sequence = 3
        };
        _db.ProjectMembers.AddRange(member1, member2, member3);
        await _db.SaveChangesAsync();

        // Research contents + activities (nội dung tài liệu — vẫn theo proposal version)
        var rc1 = new ProposalResearchContent
        {
            ProposalId = proposal.Id,
            ContentNumber = 1,
            Title = "Xây dựng và tiền xử lý bộ dữ liệu",
            Description = "Thu thập, làm sạch và gán nhãn dữ liệu văn bản tiếng Việt từ mạng xã hội và diễn đàn.",
            Sequence = 1
        };
        var rc2 = new ProposalResearchContent
        {
            ProposalId = proposal.Id,
            ContentNumber = 2,
            Title = "Phát triển mô hình phân tích cảm xúc",
            Description = "Huấn luyện và tinh chỉnh các mô hình transformer (PhoBERT, XLM-R) cho bài toán phân tích cảm xúc.",
            Sequence = 2
        };
        var rc3 = new ProposalResearchContent
        {
            ProposalId = proposal.Id,
            ContentNumber = 3,
            Title = "Triển khai và đánh giá hệ thống",
            Description = "Xây dựng REST API, đánh giá hiệu suất và viết báo cáo tổng kết.",
            Sequence = 3
        };
        _db.ProposalResearchContents.AddRange(rc1, rc2, rc3);
        await _db.SaveChangesAsync();

        _db.ProposalActivities.AddRange(
            new ProposalActivity { ContentId = rc1.Id, ProposalId = proposal.Id, ActivityName = "Thu thập và tiền xử lý dữ liệu", ExpectedResult = "Bộ dữ liệu 50.000 mẫu đã gán nhãn", StartMonth = 1, EndMonth = 4, Sequence = 1, EstimatedCost = 50_000_000 },
            new ProposalActivity { ContentId = rc2.Id, ProposalId = proposal.Id, ActivityName = "Huấn luyện mô hình PhoBERT", ExpectedResult = "Mô hình đạt F1 >= 0.88 trên tập val", StartMonth = 5, EndMonth = 10, Sequence = 2, EstimatedCost = 400_000_000 },
            new ProposalActivity { ContentId = rc2.Id, ProposalId = proposal.Id, ActivityName = "Tối ưu hoá và tinh chỉnh mô hình", ExpectedResult = "Mô hình đạt F1 >= 0.90 trên tập test", StartMonth = 11, EndMonth = 14, Sequence = 3, EstimatedCost = 200_000_000 },
            new ProposalActivity { ContentId = rc3.Id, ProposalId = proposal.Id, ActivityName = "Xây dựng REST API và tích hợp", ExpectedResult = "API hoạt động ổn định, latency < 200ms", StartMonth = 15, EndMonth = 17, Sequence = 4, EstimatedCost = 150_000_000 },
            new ProposalActivity { ContentId = rc3.Id, ProposalId = proposal.Id, ActivityName = "Viết báo cáo tổng kết đề tài", ExpectedResult = "Báo cáo đầy đủ, nộp Hội đồng", StartMonth = 18, EndMonth = 18, Sequence = 5, EstimatedCost = 10_000_000 }
        );
        await _db.SaveChangesAsync();

        // Sản phẩm dự kiến của PROJECT (chưa gắn hợp đồng — contract_id null)
        var cat = await _db.ProductCategories.FirstOrDefaultAsync();
        _db.ProjectDeliverables.AddRange(
            new ProjectDeliverable { ProjectId = project.Id, CategoryId = cat?.Id, ProductName = "Báo cáo tổng kết đề tài và các phụ lục", ScientificRequirements = "Đầy đủ, chính xác, có giá trị khoa học", Sequence = 1 },
            new ProjectDeliverable { ProjectId = project.Id, CategoryId = cat?.Id, ProductName = "02 bài báo khoa học trên tạp chí quốc tế", ScientificRequirements = "Đăng trên tạp chí Q1/Q2 theo SJR", Sequence = 2 },
            new ProjectDeliverable { ProjectId = project.Id, CategoryId = cat?.Id, ProductName = "Bộ dữ liệu và source code phần mềm", ScientificRequirements = "Bộ dữ liệu >= 50.000 mẫu; phần mềm ổn định", Sequence = 3 }
        );
        await _db.SaveChangesAsync();

        // Budget
        var budget = new ProposalBudget
        {
            ProposalId = proposal.Id,
            TotalAmount = 900_000_000m,
            LaborAmount = 0,
            EquipmentAmount = 0,
            ExternalServiceAmount = 0,
            ConferenceAmount = 0,
            OfficeSuppliesAmount = 0,
            IncidentalIpAmount = 0
        };
        _db.ProposalBudgets.Add(budget);
        await _db.SaveChangesAsync();

        // Budget items
        var laborCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "LABOR");
        var officeCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "OFFICE_PRINTING");
        var advisoryCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "ADVISORY_COUNCIL");
        var mgmtCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "MANAGEMENT_FEE");
        _db.ProposalBudgetItems.AddRange(
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = laborCat.Id, Amount = 849_747_000m, SourceKhoan = 849_747_000m, Sequence = 1 },
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = officeCat.Id, Amount = 1_003_000m, SourceNsnn = 1_003_000m, Sequence = 5 },
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = advisoryCat.Id, Amount = 7_250_000m, SourceNsnn = 7_250_000m, Sequence = 7 },
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = mgmtCat.Id, Amount = 42_000_000m, SourceNsnn = 42_000_000m, Sequence = 12 }
        );
        await _db.SaveChangesAsync();

        // Labor details (FK → project_member)
        _db.ProposalBudgetLaborDetails.AddRange(
            new ProposalBudgetLaborDetail { ProposalId = proposal.Id, ProjectMemberId = member1.Id, TotalResearchHours = 255 * 8m, HourlyRate = 0, WorkDays = 255, Coefficient = 0.79m, DailyRate = 0.79m * 1_490_000m, Sequence = 1 },
            new ProposalBudgetLaborDetail { ProposalId = proposal.Id, ProjectMemberId = member2.Id, TotalResearchHours = 215 * 8m, HourlyRate = 0, WorkDays = 215, Coefficient = 0.49m, DailyRate = 0.49m * 1_490_000m, Sequence = 2 },
            new ProposalBudgetLaborDetail { ProposalId = proposal.Id, ProjectMemberId = member3.Id, TotalResearchHours = 141 * 8m, HourlyRate = 0, WorkDays = 141, Coefficient = 0.49m, DailyRate = 0.49m * 1_490_000m, Sequence = 3 }
        );
        await _db.SaveChangesAsync();
    }
}
