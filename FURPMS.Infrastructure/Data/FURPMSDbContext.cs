using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.Logs;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Data;

public class FURPMSDbContext : DbContext
{
    public FURPMSDbContext(DbContextOptions<FURPMSDbContext> options) : base(options) { }

    // Domain 1 — Master Data
    public DbSet<ResearchType> ResearchTypes => Set<ResearchType>();
    public DbSet<ResearchTrack> ResearchTracks => Set<ResearchTrack>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<AmendmentCategory> AmendmentCategories => Set<AmendmentCategory>();
    public DbSet<LlmConfig> LlmConfigs => Set<LlmConfig>();
    public DbSet<PersonnelRoleType> PersonnelRoleTypes => Set<PersonnelRoleType>();
    public DbSet<SystemFinancialConfig> SystemFinancialConfigs => Set<SystemFinancialConfig>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<BudgetExpenseCategory> BudgetExpenseCategories => Set<BudgetExpenseCategory>();

    // Domain 2 — Financial & Rubric
    public DbSet<BudgetAllocationRule> BudgetAllocationRules => Set<BudgetAllocationRule>();
    public DbSet<DisbursementTemplate> DisbursementTemplates => Set<DisbursementTemplate>();
    public DbSet<CouncilRemunerationRate> CouncilRemunerationRates => Set<CouncilRemunerationRate>();
    public DbSet<RubricTemplate> RubricTemplates => Set<RubricTemplate>();
    public DbSet<RubricCriterion> RubricCriteria => Set<RubricCriterion>();
    public DbSet<RubricTemplateScope> RubricTemplateScopes => Set<RubricTemplateScope>();

    // Domain 3 — Users
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<AcademicProfile> AcademicProfiles => Set<AcademicProfile>();
    public DbSet<AcademicWork> AcademicWorks => Set<AcademicWork>();

    // Domain 4 — Research Cycle & Orders
    public DbSet<ResearchCycle> ResearchCycles => Set<ResearchCycle>();
    public DbSet<CycleTrack> CycleTracks => Set<CycleTrack>();
    public DbSet<ResearchOrder> ResearchOrders => Set<ResearchOrder>();
    public DbSet<DeadlineExtension> DeadlineExtensions => Set<DeadlineExtension>();

    // Domain 5 — Projects (gốc) & Proposal versions
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectDeliverable> ProjectDeliverables => Set<ProjectDeliverable>();
    public DbSet<ProposalChangeRequest> ProposalChangeRequests => Set<ProposalChangeRequest>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalBudget> ProposalBudgets => Set<ProposalBudget>();
    public DbSet<ProposalBudgetLaborDetail> ProposalBudgetLaborDetails => Set<ProposalBudgetLaborDetail>();
    public DbSet<ProposalBudgetItem> ProposalBudgetItems => Set<ProposalBudgetItem>();
    public DbSet<ProposalResearchContent> ProposalResearchContents => Set<ProposalResearchContent>();
    public DbSet<ProposalActivity> ProposalActivities => Set<ProposalActivity>();

    // Domain 6 — Review
    public DbSet<ReviewRound> ReviewRounds => Set<ReviewRound>();
    public DbSet<ProjectRound> ProjectRounds => Set<ProjectRound>();
    public DbSet<ReviewCouncil> ReviewCouncils => Set<ReviewCouncil>();
    public DbSet<CouncilProjectAssignment> CouncilProjectAssignments => Set<CouncilProjectAssignment>();
    public DbSet<CouncilMember> CouncilMembers => Set<CouncilMember>();
    public DbSet<CouncilMeeting> CouncilMeetings => Set<CouncilMeeting>();
    public DbSet<MeetingAttendance> MeetingAttendances => Set<MeetingAttendance>();
    public DbSet<ProposalReviewScore> ProposalReviewScores => Set<ProposalReviewScore>();
    public DbSet<ReviewScoreDetail> ReviewScoreDetails => Set<ReviewScoreDetail>();
    public DbSet<CouncilDecision> CouncilDecisions => Set<CouncilDecision>();
    public DbSet<CouncilQaEntry> CouncilQaEntries => Set<CouncilQaEntry>();
    public DbSet<CouncilMemberOpinion> CouncilMemberOpinions => Set<CouncilMemberOpinion>();
    public DbSet<ReviewerFeedback> ReviewerFeedbacks => Set<ReviewerFeedback>();
    public DbSet<AcceptanceEvaluation> AcceptanceEvaluations => Set<AcceptanceEvaluation>();

    // Domain 7 — Contracts
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractPhase> ContractPhases => Set<ContractPhase>();
    public DbSet<ContractDisbursement> ContractDisbursements => Set<ContractDisbursement>();
    public DbSet<ContractSettlement> ContractSettlements => Set<ContractSettlement>();

    // Domain 8 — Progress
    public DbSet<ProgressReport> ProgressReports => Set<ProgressReport>();
    public DbSet<ProgressReportItem> ProgressReportItems => Set<ProgressReportItem>();
    public DbSet<AmendmentRequest> AmendmentRequests => Set<AmendmentRequest>();
    public DbSet<FinalReport> FinalReports => Set<FinalReport>();

    // Domain 9 — AI / Polymorphic
    public DbSet<LlmOutput> LlmOutputs => Set<LlmOutput>();
    public DbSet<SemanticSearchVector> SemanticSearchVectors => Set<SemanticSearchVector>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Domain 10 — Logs
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Global decimal precision: decimal(18,2) for all decimal properties ──
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // ── Global query filters (soft delete) ──
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Proposal>().HasQueryFilter(p => !p.IsDeleted && !p.Project.IsDeleted);

        // ── UserRole composite PK ──
        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ur => ur.AssignedByUser)
                .WithMany()
                .HasForeignKey(ur => ur.AssignedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── User self-reference (deleted_by) + unit FK ──
        modelBuilder.Entity<User>(b =>
        {
            b.HasOne(u => u.DeletedByUser)
                .WithMany()
                .HasForeignKey(u => u.DeletedBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(u => u.Unit)
                .WithMany(ou => ou.Members)
                .HasForeignKey(u => u.UnitId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(u => u.AcademicProfile)
                .WithOne(ap => ap.User)
                .HasForeignKey<AcademicProfile>(ap => ap.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Property(u => u.Email).HasMaxLength(255);
            b.HasIndex(u => u.Email).IsUnique();
        });

        // ── AcademicWork — công trình/đề tài trong lý lịch khoa học (BM02) ──
        modelBuilder.Entity<AcademicWork>(b =>
        {
            b.Property(w => w.WorkType).HasMaxLength(20).IsRequired();
            b.Property(w => w.Category).HasMaxLength(30).IsRequired();
            b.Property(w => w.Title).HasMaxLength(500).IsRequired();
            b.Property(w => w.Venue).HasMaxLength(500);
            b.Property(w => w.Authors).HasMaxLength(1000);
            b.Property(w => w.Role).HasMaxLength(30);
            b.Property(w => w.Identifier).HasMaxLength(200);
            b.Property(w => w.Volume).HasMaxLength(50);
            b.Property(w => w.Pages).HasMaxLength(50);
            b.Property(w => w.Status).HasMaxLength(20);
            b.Property(w => w.Url).HasMaxLength(1000);
            b.Property(w => w.Note).HasMaxLength(1000);
            b.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Lý lịch luôn đọc theo người + gom nhóm theo loại, nên đánh chỉ mục đúng thứ tự đó.
            b.HasIndex(w => new { w.UserId, w.WorkType });
        });

        // ── OrganizationalUnit self-reference ──
        modelBuilder.Entity<OrganizationalUnit>(b =>
        {
            b.HasOne(ou => ou.Parent)
                .WithMany(ou => ou.Children)
                .HasForeignKey(ou => ou.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(ou => ou.HeadUser)
                .WithMany()
                .HasForeignKey(ou => ou.HeadUserId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(ou => ou.Code).IsUnique();
        });

        // ── ResearchType unique code ──
        modelBuilder.Entity<ResearchType>(b =>
        {
            b.HasIndex(rt => rt.Code).IsUnique();
        });

        // ── ResearchTrack unique code ──
        modelBuilder.Entity<ResearchTrack>(b =>
        {
            b.HasIndex(rt => rt.Code).IsUnique();
        });

        // ── LlmConfig unique config_code ──
        modelBuilder.Entity<LlmConfig>(b =>
        {
            b.HasIndex(lc => lc.ConfigCode).IsUnique();
        });

        // ── PersonnelRoleType unique code ──
        modelBuilder.Entity<PersonnelRoleType>(b =>
        {
            b.HasIndex(p => p.Code).IsUnique();
        });

        // ── SystemFinancialConfig unique code ──
        modelBuilder.Entity<SystemFinancialConfig>(b =>
        {
            b.HasIndex(s => s.Code).IsUnique();
        });

        // ── SystemSetting unique key ──
        modelBuilder.Entity<SystemSetting>(b =>
        {
            b.HasIndex(s => s.Key).IsUnique();
            b.Property(s => s.Key).HasMaxLength(100);
            b.Property(s => s.Value).HasMaxLength(1000);
            b.Property(s => s.RecommendedValue).HasMaxLength(1000);
        });

        // ── BudgetExpenseCategory unique code ──
        modelBuilder.Entity<BudgetExpenseCategory>(b =>
        {
            b.HasIndex(c => c.Code).IsUnique();
        });

        // ── ResearchCycle FK to User (no cascade from users) ──
        modelBuilder.Entity<ResearchCycle>(b =>
        {
            b.HasOne(rc => rc.CreatedByUser)
                .WithMany()
                .HasForeignKey(rc => rc.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── DeadlineExtension: log gia hạn (rule tuần 10) ──
        modelBuilder.Entity<DeadlineExtension>(b =>
        {
            b.HasIndex(e => new { e.TargetType, e.TargetId });
            b.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── CycleTrack: 1 đợt chứa nhiều track (Review 2 điểm b) ──
        modelBuilder.Entity<CycleTrack>(b =>
        {
            b.HasIndex(ct => new { ct.CycleId, ct.TrackId }).IsUnique();
            b.HasOne(ct => ct.Cycle)
                .WithMany()
                .HasForeignKey(ct => ct.CycleId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(ct => ct.Track)
                .WithMany()
                .HasForeignKey(ct => ct.TrackId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ResearchOrder: circular ref with Project ──
        modelBuilder.Entity<ResearchOrder>(b =>
        {
            b.HasOne(ro => ro.MatchedProject)
                .WithMany()
                .HasForeignKey(ro => ro.MatchedProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(ro => ro.CreatedByUser)
                .WithMany()
                .HasForeignKey(ro => ro.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Project (gốc — Review 2 điểm a) ──
        modelBuilder.Entity<Project>(b =>
        {
            b.HasIndex(p => p.ProjectCode).IsUnique().HasFilter("[project_code] IS NOT NULL");
            b.HasOne(p => p.CycleTrack)
                .WithMany()
                .HasForeignKey(p => p.CycleTrackId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(p => p.PiUser)
                .WithMany()
                .HasForeignKey(p => p.PiUserId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(p => p.HostingUnit)
                .WithMany()
                .HasForeignKey(p => p.HostingUnitId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(p => p.ResearchType)
                .WithMany()
                .HasForeignKey(p => p.ResearchTypeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProjectMember ──
        modelBuilder.Entity<ProjectMember>(b =>
        {
            b.HasOne(m => m.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProjectDeliverable (gộp expected_product + product_deliverable) ──
        modelBuilder.Entity<ProjectDeliverable>(b =>
        {
            b.HasOne(d => d.Project)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.Contract)
                .WithMany()
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(d => d.ContractPhase)
                .WithMany()
                .HasForeignKey(d => d.ContractPhaseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProposalChangeRequest: yêu cầu thay đổi đề tài (PI → Staff duyệt) ──
        modelBuilder.Entity<ProposalChangeRequest>(b =>
        {
            b.HasIndex(cr => cr.ProjectId);
            b.HasOne(cr => cr.Project)
                .WithMany()
                .HasForeignKey(cr => cr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(cr => cr.RequestedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.RequestedBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cr => cr.ReviewedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.ReviewedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Proposal = tài liệu có version thuộc Project ──
        modelBuilder.Entity<Proposal>(b =>
        {
            b.HasIndex(p => new { p.ProjectId, p.VersionNo }).IsUnique();
            b.HasOne(p => p.Project)
                .WithMany(pr => pr.Proposals)
                .HasForeignKey(p => p.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(p => p.ApprovedByUser)
                .WithMany()
                .HasForeignKey(p => p.ApprovedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProposalBudget: CK_budget_sum removed per v1.4 spec ──
        modelBuilder.Entity<ProposalBudget>(b =>
        {
            b.HasIndex(pb => pb.ProposalId).IsUnique();
        });

        // ── ProposalBudgetLaborDetail computed column ──
        modelBuilder.Entity<ProposalBudgetLaborDetail>(b =>
        {
            b.Property(x => x.TotalAmount)
                .HasComputedColumnSql("[total_research_hours] * [hourly_rate]", stored: true);
            b.HasOne(x => x.Proposal)
                .WithMany()
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(x => x.ProjectMember)
                .WithMany(m => m.BudgetLaborDetails)
                .HasForeignKey(x => x.ProjectMemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProposalBudgetItem FKs ──
        modelBuilder.Entity<ProposalBudgetItem>(b =>
        {
            b.HasOne(x => x.Proposal)
                .WithMany()
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProposalActivity: Proposal FK no cascade (already via Content) ──
        modelBuilder.Entity<ProposalActivity>(b =>
        {
            b.HasOne(pa => pa.Proposal)
                .WithMany()
                .HasForeignKey(pa => pa.ProposalId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ReviewRound (Phase B): thuộc cycle_track; unique (cycle_track, số vòng, dimension) ──
        modelBuilder.Entity<ReviewRound>(b =>
        {
            b.HasIndex(r => new { r.CycleTrackId, r.RoundNumber, r.Dimension }).IsUnique();
            b.HasOne(r => r.PrerequisiteRound)
                .WithMany()
                .HasForeignKey(r => r.PrerequisiteRoundId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(r => r.CycleTrack)
                .WithMany()
                .HasForeignKey(r => r.CycleTrackId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(r => r.RubricTemplate)
                .WithMany()
                .HasForeignKey(r => r.RubricTemplateId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── RubricTemplateScope: bộ tiêu chí ↔ (đợt + lĩnh vực) ──
        modelBuilder.Entity<RubricTemplateScope>(b =>
        {
            // 1 bộ không gắn trùng cùng (đợt, lĩnh vực) 2 lần. Ràng buộc "mỗi (đợt+lĩnh vực+loại
            // vòng) chỉ 1 bộ" cần biết TemplateType nên kiểm ở service, không đặt được ở DB index.
            b.HasIndex(s => new { s.TemplateId, s.CycleId, s.TrackId }).IsUnique();
            b.HasOne(s => s.Template)
                .WithMany(t => t.Scopes)
                .HasForeignKey(s => s.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);   // xoá bộ → xoá phạm vi của nó
        });

        // ── ProjectRound (Phase B): Project–Round nhiều-nhiều ──
        modelBuilder.Entity<ProjectRound>(b =>
        {
            b.HasIndex(pr => new { pr.ProjectId, pr.RoundId }).IsUnique();
            b.HasOne(pr => pr.Project)
                .WithMany()
                .HasForeignKey(pr => pr.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(pr => pr.Round)
                .WithMany(r => r.ProjectRounds)
                .HasForeignKey(pr => pr.RoundId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ReviewCouncil ──
        modelBuilder.Entity<ReviewCouncil>(b =>
        {
            b.HasOne(rc => rc.CreatedByUser)
                .WithMany()
                .HasForeignKey(rc => rc.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(rc => rc.Round)
                .WithMany(r => r.Councils)
                .HasForeignKey(rc => rc.RoundId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── CouncilProjectAssignment (Phase B): council chấm nhóm project ──
        modelBuilder.Entity<CouncilProjectAssignment>(b =>
        {
            b.HasIndex(a => new { a.CouncilId, a.ProjectId }).IsUnique();
            b.HasOne(a => a.Council)
                .WithMany(c => c.ProjectAssignments)
                .HasForeignKey(a => a.CouncilId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(a => a.Project)
                .WithMany()
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── CouncilDecision: unique (council, project) — Chủ tịch chốt từng đề tài ──
        modelBuilder.Entity<CouncilDecision>(b =>
        {
            b.HasIndex(cd => new { cd.CouncilId, cd.ProjectId }).IsUnique();
            b.HasOne(cd => cd.Council)
                .WithMany(c => c.Decisions)
                .HasForeignKey(cd => cd.CouncilId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cd => cd.Project)
                .WithMany()
                .HasForeignKey(cd => cd.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cd => cd.ChairUser)
                .WithMany()
                .HasForeignKey(cd => cd.ChairUserId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cd => cd.SecretaryUser)
                .WithMany()
                .HasForeignKey(cd => cd.SecretaryUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── CouncilQaEntry: hỏi–đáp thuộc biên bản, xoá theo biên bản ──
        modelBuilder.Entity<CouncilQaEntry>(b =>
        {
            b.HasOne(q => q.Decision)
                .WithMany(cd => cd.QaEntries)
                .HasForeignKey(q => q.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CouncilMemberOpinion: ý kiến TV thuộc biên bản, xoá theo biên bản ──
        modelBuilder.Entity<CouncilMemberOpinion>(b =>
        {
            b.HasOne(o => o.Decision)
                .WithMany(cd => cd.MemberOpinions)
                .HasForeignKey(o => o.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MeetingAttendance unique pair ──
        modelBuilder.Entity<MeetingAttendance>(b =>
        {
            b.HasIndex(ma => new { ma.MeetingId, ma.MemberId }).IsUnique();
        });

        // ── ProposalReviewScore unique (council, project, evaluator) — Phase B ──
        modelBuilder.Entity<ProposalReviewScore>(b =>
        {
            b.HasIndex(prs => new { prs.CouncilId, prs.ProjectId, prs.EvaluatorMemberId }).IsUnique();
            b.HasOne(prs => prs.Project)
                .WithMany()
                .HasForeignKey(prs => prs.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(prs => prs.Council)
                .WithMany()
                .HasForeignKey(prs => prs.CouncilId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(prs => prs.EvaluatorMember)
                .WithMany()
                .HasForeignKey(prs => prs.EvaluatorMemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ReviewerFeedback unique (council, project, reviewer) — Phase B ──
        modelBuilder.Entity<ReviewerFeedback>(b =>
        {
            b.HasIndex(rf => new { rf.CouncilId, rf.ProjectId, rf.ReviewerMemberId }).IsUnique();
            b.HasOne(rf => rf.Project)
                .WithMany()
                .HasForeignKey(rf => rf.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(rf => rf.Council)
                .WithMany()
                .HasForeignKey(rf => rf.CouncilId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(rf => rf.ReviewerMember)
                .WithMany()
                .HasForeignKey(rf => rf.ReviewerMemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── AcceptanceEvaluation unique (council, project, evaluator) — Phase B ──
        modelBuilder.Entity<AcceptanceEvaluation>(b =>
        {
            b.HasIndex(ae => new { ae.CouncilId, ae.ProjectId, ae.EvaluatorMemberId }).IsUnique();
            b.HasOne(ae => ae.Project)
                .WithMany()
                .HasForeignKey(ae => ae.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(ae => ae.Council)
                .WithMany()
                .HasForeignKey(ae => ae.CouncilId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(ae => ae.EvaluatorMember)
                .WithMany()
                .HasForeignKey(ae => ae.EvaluatorMemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Contract: 1 project → n contract (Review 2 điểm e — bỏ unique cũ) ──
        modelBuilder.Entity<Contract>(b =>
        {
            b.HasIndex(c => c.ContractNumber).IsUnique();
            b.HasIndex(c => c.ProjectId);
            b.HasOne(c => c.Project)
                .WithMany(p => p.Contracts)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.TerminatedByUser)
                .WithMany()
                .HasForeignKey(c => c.TerminatedBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ContractPhase: unique (contract_id, phase_no) ──
        modelBuilder.Entity<ContractPhase>(b =>
        {
            b.HasIndex(cp => new { cp.ContractId, cp.PhaseNo }).IsUnique();
            b.HasOne(cp => cp.Contract)
                .WithMany(c => c.Phases)
                .HasForeignKey(cp => cp.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ContractDisbursement unique (contract_id, round_number) ──
        modelBuilder.Entity<ContractDisbursement>(b =>
        {
            b.HasIndex(cd => new { cd.ContractId, cd.RoundNumber }).IsUnique();
            b.HasOne(cd => cd.ConditionMetByUser)
                .WithMany()
                .HasForeignKey(cd => cd.ConditionMetBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cd => cd.ProcessedByUser)
                .WithMany()
                .HasForeignKey(cd => cd.ProcessedBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cd => cd.Deliverable)
                .WithMany()
                .HasForeignKey(cd => cd.DeliverableId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(cd => cd.Phase)
                .WithMany()
                .HasForeignKey(cd => cd.PhaseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ContractSettlement unique on contract_id ──
        modelBuilder.Entity<ContractSettlement>(b =>
        {
            b.HasIndex(cs => cs.ContractId).IsUnique();
            b.HasOne(cs => cs.SideASignee)
                .WithMany()
                .HasForeignKey(cs => cs.SideASigneeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProgressReport unique (contract_id, report_round) ──
        modelBuilder.Entity<ProgressReport>(b =>
        {
            b.HasIndex(pr => new { pr.ContractId, pr.ReportRound }).IsUnique();
            b.HasOne(pr => pr.EvaluatedByUser)
                .WithMany()
                .HasForeignKey(pr => pr.EvaluatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── FinalReport: 1 bản nghiệm thu cuối / PROJECT (Review 2 điểm a) ──
        modelBuilder.Entity<FinalReport>(b =>
        {
            b.HasIndex(fr => fr.ProjectId).IsUnique();
            b.HasOne(fr => fr.Project)
                .WithOne(p => p.FinalReport)
                .HasForeignKey<FinalReport>(fr => fr.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── AmendmentRequest FKs no cascade ──
        modelBuilder.Entity<AmendmentRequest>(b =>
        {
            b.HasOne(ar => ar.RequestedByUser)
                .WithMany()
                .HasForeignKey(ar => ar.RequestedBy)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(ar => ar.ReviewedByUser)
                .WithMany()
                .HasForeignKey(ar => ar.ReviewedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Notification CHECK priority ──
        modelBuilder.Entity<Notification>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint("CK_notifications_priority",
                "[priority] IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')"));
        });

        // ── Indexes for polymorphic tables ──
        modelBuilder.Entity<LlmOutput>(b =>
        {
            b.HasIndex(lo => new { lo.EntityType, lo.EntityId, lo.IsActive })
                .HasDatabaseName("IX_llm_outputs_entity");
            b.HasOne(lo => lo.ReviewedByUser)
                .WithMany()
                .HasForeignKey(lo => lo.ReviewedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SemanticSearchVector>(b =>
        {
            b.HasIndex(sv => new { sv.EntityType, sv.EntityId }).IsUnique();
        });

        modelBuilder.Entity<Document>(b =>
        {
            b.HasIndex(d => new { d.EntityType, d.EntityId })
                .HasDatabaseName("IX_documents_entity");

            // Nối navigation với đúng cột UploadedBy. Thiếu dòng này EF tự sinh thêm cột
            // shadow `uploaded_by_user_id` — insert luôn vi phạm khoá ngoại (SQL 547),
            // khiến upload tài liệu không bao giờ chạy được trên SQL Server thật.
            b.HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Notification>(b =>
        {
            b.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                .HasDatabaseName("IX_notifications_user_unread");
        });

        // Global guard: prevent any accidental cascade cycles/multiple paths
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
