using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Reminders;

// ── FakeClock ────────────────────────────────────────────────────────────────

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
    public int OffsetDays { get; set; }
}

// ── NullEmailService ─────────────────────────────────────────────────────────

public class NullEmailService : IEmailService
{
    public List<(string email, string emailType)> Sent { get; } = new();

    public Task SendAsync(string recipientEmail, string subject, string body,
        string emailType, Guid? recipientUserId = null, string? actionUrl = null)
    {
        Sent.Add((recipientEmail, emailType));
        return Task.CompletedTask;
    }
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class DeadlineReminderScannerTests
{
    private static User MakePi() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"pi-{Guid.NewGuid():N}"[..20] + "@test.com",
        FullName = "PI User",
        Status = "ACTIVE"
    };

    // ── Test 1: T-30 scan inserts one notification + one email ───────────────

    [Fact]
    public async Task Scan_T30_InsertsNotificationAndEmail()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var today = new DateOnly(2026, 6, 11);
        var dueDate = today.AddDays(30);
        var contractId = Guid.NewGuid();

        var rt = new ResearchType { Code = $"R-{Guid.NewGuid():N}"[..8], Name = "RT", IsActive = true };
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(), PiUserId = pi.Id,
            CycleTrackId = 1, OrderId = 1, ResearchTypeId = rt.Id, HostingUnitId = 1,
            TitleVi = "T", Status = "IN_PROGRESS",
            PlannedStartDate = today, PlannedEndDate = today.AddMonths(12)
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var contract = new Contract
        {
            Id = contractId, ProjectId = project.Id,
            ContractNumber = "CTR-T30", TotalAmount = 100_000m,
            StartDate = today, EndDate = dueDate.AddDays(60), OriginalEndDate = dueDate.AddDays(60),
            MaxExtensionMonths = 6, Status = "ACTIVE", CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contractId,
            CategoryId = 1, ProductName = "Deliverable T30",
            AcceptanceStatus = "PENDING", IsCompleted = false, DueDate = dueDate
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = today.ToDateTime(TimeOnly.MinValue) };
        var emailSvc = new NullEmailService();
        var scanner = TestServices.DeadlineScanner(db, clock, emailSvc);

        await scanner.ScanAsync();

        var notifications = db.Notifications.ToList();
        Assert.Single(notifications);
        Assert.Equal("DEADLINE_REMINDER_T30", notifications[0].NotificationType);
        Assert.Equal(pi.Id, notifications[0].UserId);
        Assert.Equal("HIGH", notifications[0].Priority);
        Assert.Single(emailSvc.Sent);
        Assert.Equal("DEADLINE_REMINDER_T30", emailSvc.Sent[0].emailType);
    }

    // Nhắc hạn BÁO CÁO TIẾN ĐỘ (thầy 29/07: "gần đến deadline gửi thông báo, vd trước 3 ngày").
    // Trước đây scanner chỉ quét sản phẩm nên PI không được nhắc gì về báo cáo.
    [Theory]
    [InlineData(3, "REPORT_REMINDER_T3", "HIGH")]
    [InlineData(-1, "REPORT_OVERDUE", "URGENT")]
    public async Task Scan_ProgressReport_NotifiesPi(int daysUntilDue, string expectedType, string expectedPriority)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var today = new DateOnly(2026, 6, 11);
        var rt = new ResearchType { Code = $"R-{Guid.NewGuid():N}"[..8], Name = "RT", IsActive = true };
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(), PiUserId = pi.Id,
            CycleTrackId = 1, OrderId = 1, ResearchTypeId = rt.Id, HostingUnitId = 1,
            TitleVi = "T", Status = "IN_PROGRESS",
            PlannedStartDate = today, PlannedEndDate = today.AddMonths(12)
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id,
            ContractNumber = "CTR-RPT", TotalAmount = 100_000m,
            StartDate = today, EndDate = today.AddMonths(12), OriginalEndDate = today.AddMonths(12),
            MaxExtensionMonths = 6, Status = "ACTIVE", CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        db.ProgressReports.Add(new FURPMS.Domain.Entities.Progress.ProgressReport
        {
            ContractId = contract.Id,
            ReportRound = 1,
            RoundName = "Giữa kỳ",
            ReportingPeriodStart = today.AddMonths(-3),
            ReportingPeriodEnd = today,
            CompletedContent = "",
            DueDate = today.AddDays(daysUntilDue),
            Status = "DRAFT"          // chưa nộp
        });
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = today.ToDateTime(TimeOnly.MinValue) };
        var emailSvc = new NullEmailService();
        var scanner = TestServices.DeadlineScanner(db, clock, emailSvc);

        await scanner.ScanAsync();

        var n = Assert.Single(db.Notifications.ToList());
        Assert.Equal(expectedType, n.NotificationType);
        Assert.Equal(expectedPriority, n.Priority);
        Assert.Equal(pi.Id, n.UserId);
        Assert.Contains("Giữa kỳ", n.Title);      // dùng tên đợt Staff đặt, không phải "Kỳ 1"
    }

    // ── Test 2: Running scan twice does NOT create duplicates ─────────────────

    [Fact]
    public async Task Scan_RunTwice_NoDuplicateNotification()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var today = new DateOnly(2026, 6, 11);
        var dueDate = today.AddDays(7);

        var rt = new ResearchType { Code = $"R-{Guid.NewGuid():N}"[..8], Name = "RT", IsActive = true };
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(), PiUserId = pi.Id,
            CycleTrackId = 1, OrderId = 1, ResearchTypeId = rt.Id, HostingUnitId = 1,
            TitleVi = "T", Status = "IN_PROGRESS",
            PlannedStartDate = today, PlannedEndDate = today.AddMonths(12)
        };
        db.Projects.Add(project);
        var contract = new Contract
        {
            ProjectId = project.Id, ContractNumber = "CTR-DUP",
            TotalAmount = 100_000m, StartDate = today, EndDate = dueDate.AddDays(30),
            OriginalEndDate = dueDate.AddDays(30), MaxExtensionMonths = 6,
            Status = "ACTIVE", CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contract.Id, CategoryId = 1,
            ProductName = "Dup Deliverable", AcceptanceStatus = "PENDING",
            IsCompleted = false, DueDate = dueDate
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = today.ToDateTime(TimeOnly.MinValue) };
        var emailSvc = new NullEmailService();
        var scanner = TestServices.DeadlineScanner(db, clock, emailSvc);

        // Chạy lần 1 rồi lần 2 — điều cần kiểm là lần 2 KHÔNG sinh thêm gì, chứ không phải
        // "toàn hệ thống chỉ có đúng 1 thông báo". Từ 25/08 scanner còn quét cả báo cáo nghiệm thu
        // (hạn suy từ ngày kết thúc hợp đồng), nên một lượt quét hợp lệ có thể ra nhiều thông báo
        // khác loại — khoá cứng số 1 là khoá nhầm thứ.
        await scanner.ScanAsync();
        var afterFirst = db.Notifications.Count();
        var emailsAfterFirst = emailSvc.Sent.Count;

        await scanner.ScanAsync();

        Assert.Equal(afterFirst, db.Notifications.Count());
        Assert.Equal(emailsAfterFirst, emailSvc.Sent.Count);

        // Và đúng cái sản phẩm này chỉ được nhắc MỘT lần.
        var deliverableId = deliverable.Id.ToString();
        Assert.Single(db.Notifications.Where(n => n.RelatedEntityId == deliverableId));
    }

    // ── Test 3: Overdue deliverable → URGENT priority + amendments action URL ──

    [Fact]
    public async Task Scan_Overdue_SetsUrgentPriorityAndAmendmentsUrl()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var today = new DateOnly(2026, 6, 11);
        var dueDate = today.AddDays(-5);  // 5 days overdue

        var rt = new ResearchType { Code = $"R-{Guid.NewGuid():N}"[..8], Name = "RT", IsActive = true };
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(), PiUserId = pi.Id,
            CycleTrackId = 1, OrderId = 1, ResearchTypeId = rt.Id, HostingUnitId = 1,
            TitleVi = "T", Status = "IN_PROGRESS",
            PlannedStartDate = dueDate, PlannedEndDate = today.AddMonths(12)
        };
        db.Projects.Add(project);
        var contract = new Contract
        {
            ProjectId = project.Id, ContractNumber = "CTR-OVD",
            TotalAmount = 100_000m, StartDate = dueDate, EndDate = today.AddDays(30),
            OriginalEndDate = today.AddDays(30), MaxExtensionMonths = 6,
            Status = "ACTIVE", CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contract.Id, CategoryId = 1,
            ProductName = "Overdue Product", AcceptanceStatus = "PENDING",
            IsCompleted = false, DueDate = dueDate
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = today.ToDateTime(TimeOnly.MinValue) };
        var emailSvc = new NullEmailService();
        var scanner = TestServices.DeadlineScanner(db, clock, emailSvc);

        await scanner.ScanAsync();

        var notification = db.Notifications.Single();
        Assert.Equal("DEADLINE_OVERDUE", notification.NotificationType);
        Assert.Equal("URGENT", notification.Priority);
        // actionUrl phải là route thật của FE (/my-amendments) — trước đây là "/api/contracts/.../
        // amendments", một đường API mà router FE không khớp được, bấm vào rơi thẳng vào trang 404.
        Assert.Equal("/my-amendments", notification.ActionUrl);
    }

    // ── Test 4: No notification for deliverable with no due date ─────────────

    [Fact]
    public async Task Scan_NoDueDate_NoNotification()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var today = new DateOnly(2026, 6, 11);
        var rt = new ResearchType { Code = $"R-{Guid.NewGuid():N}"[..8], Name = "RT", IsActive = true };
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(), PiUserId = pi.Id,
            CycleTrackId = 1, OrderId = 1, ResearchTypeId = rt.Id, HostingUnitId = 1,
            TitleVi = "T", Status = "IN_PROGRESS",
            PlannedStartDate = today, PlannedEndDate = today.AddMonths(12)
        };
        db.Projects.Add(project);
        var contract = new Contract
        {
            ProjectId = project.Id, ContractNumber = "CTR-NODUE",
            TotalAmount = 100_000m, StartDate = today, EndDate = today.AddDays(365),
            OriginalEndDate = today.AddDays(365), MaxExtensionMonths = 6,
            Status = "ACTIVE", CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contract.Id, CategoryId = 1,
            ProductName = "No Due Date Product", AcceptanceStatus = "PENDING",
            IsCompleted = false, DueDate = null   // no due date
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = today.ToDateTime(TimeOnly.MinValue) };
        var emailSvc = new NullEmailService();
        var scanner = TestServices.DeadlineScanner(db, clock, emailSvc);

        await scanner.ScanAsync();

        Assert.Empty(db.Notifications);
    }
}
