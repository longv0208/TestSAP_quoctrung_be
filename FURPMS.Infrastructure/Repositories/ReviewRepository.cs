using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Review;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class ReviewRepository : Repository<ReviewCouncil>, IReviewRepository
{
    public ReviewRepository(FURPMSDbContext db) : base(db) { }

    public IQueryable<CouncilMember> CouncilMembers => _db.CouncilMembers;
    public IQueryable<ReviewRound> ReviewRounds => _db.ReviewRounds;
    public IQueryable<ProjectRound> ProjectRounds => _db.ProjectRounds;
    public IQueryable<CouncilProjectAssignment> ProjectAssignments => _db.CouncilProjectAssignments;
    public IQueryable<CouncilDecision> Decisions => _db.CouncilDecisions;
    public IQueryable<ProposalReviewScore> ReviewScores => _db.ProposalReviewScores;
    public IQueryable<ReviewScoreDetail> ReviewScoreDetails => _db.ReviewScoreDetails;
    public IQueryable<ReviewerFeedback> ReviewerFeedbacks => _db.ReviewerFeedbacks;
    public IQueryable<CouncilMeeting> Meetings => _db.CouncilMeetings;
    public IQueryable<MeetingAttendance> MeetingAttendances => _db.MeetingAttendances;
    public IQueryable<AcceptanceEvaluation> AcceptanceEvaluations => _db.AcceptanceEvaluations;

    public async Task<ReviewRound?> GetRoundByIdAsync(Guid id) => await _db.ReviewRounds.FindAsync(id);
    public async Task AddRoundAsync(ReviewRound round) => await _db.ReviewRounds.AddAsync(round);
    public void RemoveRound(ReviewRound round) => _db.ReviewRounds.Remove(round);
    public async Task AddProjectRoundAsync(ProjectRound projectRound) => await _db.ProjectRounds.AddAsync(projectRound);
    public void RemoveProjectRoundsRange(IEnumerable<ProjectRound> projectRounds) => _db.ProjectRounds.RemoveRange(projectRounds);
    public async Task AddProjectAssignmentAsync(CouncilProjectAssignment assignment) => await _db.CouncilProjectAssignments.AddAsync(assignment);
    public async Task AddMemberAsync(CouncilMember member) => await _db.CouncilMembers.AddAsync(member);
    public async Task AddScoreAsync(ProposalReviewScore score) => await _db.ProposalReviewScores.AddAsync(score);
    public void AddScoreDetailsRange(IEnumerable<ReviewScoreDetail> details) => _db.ReviewScoreDetails.AddRange(details);
    public void RemoveScoreDetailsRange(IEnumerable<ReviewScoreDetail> details) => _db.ReviewScoreDetails.RemoveRange(details);
    public async Task AddDecisionAsync(CouncilDecision decision) => await _db.CouncilDecisions.AddAsync(decision);
    public async Task AddMeetingAsync(CouncilMeeting meeting) => await _db.CouncilMeetings.AddAsync(meeting);
    public async Task AddAcceptanceEvaluationAsync(AcceptanceEvaluation eval) => await _db.AcceptanceEvaluations.AddAsync(eval);
    public async Task AddReviewerFeedbackAsync(ReviewerFeedback feedback) => await _db.ReviewerFeedbacks.AddAsync(feedback);
    public void RemoveMember(CouncilMember member) => _db.CouncilMembers.Remove(member);
}
