using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ChangeRequests;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.ChangeRequests;

// Yêu cầu thay đổi đề tài: chỉ PI gửi được; Staff duyệt 1 lần (approve/reject).
public class ChangeRequestServiceTests
{
    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"u-{Guid.NewGuid():N}"[..18] + "@t.com",
        FullName = "U",
        Status = UserStatus.Active
    };

    private static async Task<(Project project, Proposal proposal)> SeedAsync(FURPMSDbContext db, Guid piId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = piId,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "Đề tài test",
            Status = ProjectStatus.InProgress,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "Đề tài test",
            AbstractVi = "A",
            ResearchObjectives = "O",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ProposalStatus.Approved
        };
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();
        return (project, proposal);
    }

    private static ChangeRequestService MakeService(FURPMSDbContext db) =>
        new(new ProposalRepository(db));

    // ── 1: PI gửi yêu cầu → tạo thành công, status Pending, type ánh xạ tên ──
    [Fact]
    public async Task Create_ByPi_Succeeds_Pending()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        db.Users.Add(pi);
        await db.SaveChangesAsync();
        var (_, proposal) = await SeedAsync(db, pi.Id);
        var svc = MakeService(db);

        var dto = await svc.CreateAsync(proposal.Id, new CreateChangeRequestRequest
        {
            Type = 1, // ExtendTime
            Description = "Xin gia hạn 3 tháng",
            NewValue = "3"
        }, pi.Id);

        Assert.Equal("Pending", dto.Status);
        Assert.Equal("ExtendTime", dto.Type);
        Assert.Equal(proposal.Id, dto.ProposalId);
        Assert.Null(dto.ReviewedAt);
    }

    // ── 2: người khác PI gửi → 401 ──
    [Fact]
    public async Task Create_ByNonPi_Throws_403()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        db.Users.Add(pi);
        await db.SaveChangesAsync();
        var (_, proposal) = await SeedAsync(db, pi.Id);
        var svc = MakeService(db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => svc.CreateAsync(proposal.Id, new CreateChangeRequestRequest { Type = 2, Description = "x" }, Guid.NewGuid()));
    }

    // ── 3: Staff duyệt (approve) → Approved + ReviewedAt set; hiện trong pending trước đó ──
    [Fact]
    public async Task Review_Approve_SetsApproved()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        db.Users.Add(pi);
        await db.SaveChangesAsync();
        var (_, proposal) = await SeedAsync(db, pi.Id);
        var svc = MakeService(db);

        var created = await svc.CreateAsync(proposal.Id, new CreateChangeRequestRequest { Type = 4, Description = "Điều chỉnh kinh phí" }, pi.Id);

        var pending = await svc.GetPendingAsync();
        Assert.Single(pending);

        var reviewed = await svc.ReviewAsync(created.Id, new ReviewChangeRequestRequest { Approved = true, AdminNote = "OK" }, Guid.NewGuid());
        Assert.Equal("Approved", reviewed.Status);
        Assert.NotNull(reviewed.ReviewedAt);
        Assert.Equal("OK", reviewed.AdminNote);

        Assert.Empty(await svc.GetPendingAsync());
    }

    // ── 4: duyệt lần 2 → 409 ──
    [Fact]
    public async Task Review_Twice_Throws_409()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        db.Users.Add(pi);
        await db.SaveChangesAsync();
        var (_, proposal) = await SeedAsync(db, pi.Id);
        var svc = MakeService(db);

        var created = await svc.CreateAsync(proposal.Id, new CreateChangeRequestRequest { Type = 5, Description = "Tạm dừng" }, pi.Id);
        await svc.ReviewAsync(created.Id, new ReviewChangeRequestRequest { Approved = false }, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ReviewAsync(created.Id, new ReviewChangeRequestRequest { Approved = true }, Guid.NewGuid()));
    }
}
