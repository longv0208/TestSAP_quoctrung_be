using FURPMS.Domain.Entities.Review;

namespace FURPMS.Application.Interfaces.Repositories;

public interface IReviewRepository : IRepository<ReviewCouncil>
{
    IQueryable<CouncilMember> CouncilMembers { get; }
    IQueryable<ReviewRound> ReviewRounds { get; }
    IQueryable<ProjectRound> ProjectRounds { get; }
    IQueryable<CouncilProjectAssignment> ProjectAssignments { get; }
    IQueryable<CouncilDecision> Decisions { get; }
    IQueryable<ProposalReviewScore> ReviewScores { get; }
    IQueryable<ReviewScoreDetail> ReviewScoreDetails { get; }
    IQueryable<ReviewerFeedback> ReviewerFeedbacks { get; }
    IQueryable<CouncilMeeting> Meetings { get; }
    IQueryable<MeetingAttendance> MeetingAttendances { get; }
    IQueryable<AcceptanceEvaluation> AcceptanceEvaluations { get; }

    Task<ReviewRound?> GetRoundByIdAsync(Guid id);
    Task AddRoundAsync(ReviewRound round);
    void RemoveRound(ReviewRound round);
    Task AddProjectRoundAsync(ProjectRound projectRound);
    void RemoveProjectRoundsRange(IEnumerable<ProjectRound> projectRounds);
    Task AddProjectAssignmentAsync(CouncilProjectAssignment assignment);
    void RemoveProjectAssignmentsRange(IEnumerable<CouncilProjectAssignment> assignments);
    Task AddMemberAsync(CouncilMember member);
    Task AddScoreAsync(ProposalReviewScore score);
    void AddScoreDetailsRange(IEnumerable<ReviewScoreDetail> details);
    void RemoveScoreDetailsRange(IEnumerable<ReviewScoreDetail> details);
    Task AddDecisionAsync(CouncilDecision decision);
    void RemoveQaEntriesRange(IEnumerable<CouncilQaEntry> entries);
    void RemoveMemberOpinionsRange(IEnumerable<CouncilMemberOpinion> opinions);
    Task AddMeetingAsync(CouncilMeeting meeting);
    Task AddMeetingAttendanceAsync(MeetingAttendance attendance);
    Task AddAcceptanceEvaluationAsync(AcceptanceEvaluation eval);
    Task AddReviewerFeedbackAsync(ReviewerFeedback feedback);
    void RemoveMember(CouncilMember member);
    // Xóa hội đồng chưa chấm: con đều NoAction nên phải gỡ tay trước khi xóa council.
    void RemoveMembersRange(IEnumerable<CouncilMember> members);
    void RemoveMeetingsRange(IEnumerable<CouncilMeeting> meetings);
    void RemoveAttendancesRange(IEnumerable<MeetingAttendance> attendances);
}
