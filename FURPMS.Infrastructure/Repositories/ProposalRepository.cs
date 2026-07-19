using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class ProposalRepository : Repository<Proposal>, IProposalRepository
{
    public ProposalRepository(FURPMSDbContext db) : base(db) { }

    public IQueryable<Project> Projects => _db.Projects;
    public IQueryable<ProposalChangeRequest> ChangeRequests => _db.ProposalChangeRequests;
    public IQueryable<ProjectMember> ProjectMembers => _db.ProjectMembers;
    public IQueryable<ProjectDeliverable> Deliverables => _db.ProjectDeliverables;
    public IQueryable<ProposalBudget> Budgets => _db.ProposalBudgets;
    public IQueryable<ProposalBudgetItem> BudgetItems => _db.ProposalBudgetItems;
    public IQueryable<ProposalBudgetLaborDetail> LaborDetails => _db.ProposalBudgetLaborDetails;
    public IQueryable<ProposalActivity> Activities => _db.ProposalActivities;
    public IQueryable<ProposalResearchContent> ResearchContents => _db.ProposalResearchContents;

    public void AddProject(Project project) => _db.Projects.Add(project);
    public void AddChangeRequest(ProposalChangeRequest changeRequest) => _db.ProposalChangeRequests.Add(changeRequest);
    public void AddBudgetItem(ProposalBudgetItem item) => _db.ProposalBudgetItems.Add(item);
    public void RemoveBudgetItemsRange(IEnumerable<ProposalBudgetItem> items) => _db.ProposalBudgetItems.RemoveRange(items);
    public void AddProjectMember(ProjectMember member) => _db.ProjectMembers.Add(member);
    public void RemoveProjectMembersRange(IEnumerable<ProjectMember> members) => _db.ProjectMembers.RemoveRange(members);
    public void RemoveLaborDetailsRange(IEnumerable<ProposalBudgetLaborDetail> details) => _db.ProposalBudgetLaborDetails.RemoveRange(details);
    public void AddResearchContent(ProposalResearchContent content) => _db.ProposalResearchContents.Add(content);
    public void RemoveResearchContent(ProposalResearchContent content) => _db.ProposalResearchContents.Remove(content);
    public void AddActivity(ProposalActivity activity) => _db.ProposalActivities.Add(activity);
    public void RemoveActivity(ProposalActivity activity) => _db.ProposalActivities.Remove(activity);
    public void AddDeliverable(ProjectDeliverable product) => _db.ProjectDeliverables.Add(product);
    public void RemoveDeliverable(ProjectDeliverable product) => _db.ProjectDeliverables.Remove(product);
}
