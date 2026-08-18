using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;

namespace FURPMS.Application.Interfaces.Repositories;

public interface IProposalRepository : IRepository<Proposal>
{
    IQueryable<Project> Projects { get; }
    IQueryable<ProjectMember> ProjectMembers { get; }
    IQueryable<ProjectDeliverable> Deliverables { get; }
    IQueryable<ProposalChangeRequest> ChangeRequests { get; }
    IQueryable<ProposalBudget> Budgets { get; }
    IQueryable<ProposalBudgetItem> BudgetItems { get; }
    IQueryable<ProposalBudgetLaborDetail> LaborDetails { get; }
    IQueryable<ProposalActivity> Activities { get; }
    IQueryable<ProposalResearchContent> ResearchContents { get; }

    void AddProject(Project project);
    void AddChangeRequest(ProposalChangeRequest changeRequest);
    void AddBudgetItem(ProposalBudgetItem item);
    void RemoveBudgetItemsRange(IEnumerable<ProposalBudgetItem> items);
    void AddProjectMember(ProjectMember member);
    void RemoveProjectMembersRange(IEnumerable<ProjectMember> members);
    void RemoveLaborDetailsRange(IEnumerable<ProposalBudgetLaborDetail> details);
    void AddResearchContent(ProposalResearchContent content);
    void RemoveResearchContent(ProposalResearchContent content);
    void AddActivity(ProposalActivity activity);
    void RemoveActivity(ProposalActivity activity);
    void AddDeliverable(ProjectDeliverable product);
    void RemoveDeliverable(ProjectDeliverable product);
}
