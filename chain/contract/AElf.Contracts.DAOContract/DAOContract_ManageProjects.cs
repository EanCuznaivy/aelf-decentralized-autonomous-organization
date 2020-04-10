using System.Linq;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.DAOContract
{
    // ReSharper disable InconsistentNaming
    public partial class DAOContract
    {
        public override Empty AddInvestmentProject(ProjectInfo input)
        {
            AssertApprovedByDecentralizedAutonomousOrganization(input);
            var projectId = input.GetProjectId();
            input.VirtualAddress = Context.ConvertVirtualAddressToContractAddress(projectId);
            State.Projects[projectId] = input;
            return new Empty();
        }

        public override Empty UpdateInvestmentProject(ProjectInfo input)
        {
            AssertApprovedByDecentralizedAutonomousOrganization(input);
            var projectId = input.GetProjectId();
            var currentProject = State.Projects[projectId];
            currentProject.Status = input.Status;

            if (input.CurrentBudgetPlanIndex > 0)
            {
                currentProject.CurrentBudgetPlanIndex = input.CurrentBudgetPlanIndex;
            }

            if (input.Status == ProjectStatus.Approved && currentProject.ProfitSchemeId == null)
            {
                foreach (var budgetPlan in input.BudgetPlans)
                {
                    State.BudgetPlans[projectId][budgetPlan.Index] = budgetPlan;
                }
                var profitSchemeId = CreateProfitScheme(input);
                currentProject.ProfitSchemeId = profitSchemeId;
            }

            if (input.Status == ProjectStatus.Taken)
            {
                if (input.CurrentBudgetPlanIndex > 0)
                {
                    PayBudget(input);
                }

                if (input.CurrentBudgetPlanIndex.Add(1) == input.BudgetPlans.Count)
                {
                    State.PreviewProposalIds.Remove(projectId);
                }
            }

            if (input.Status == ProjectStatus.Delivered)
            {
                // TODO: Maintain beneficiaries for sender.
                // Once a project is DELIVERED, beneficiaries will be investors.
                // If symbols are diff for every budget plan, may need to calculate weight.
            }

            State.Projects[projectId] = currentProject;
            return new Empty();
        }

        public override Empty AddRewardProject(ProjectInfo input)
        {
            AssertApprovedByDecentralizedAutonomousOrganization(input);
            var projectId = input.GetProjectId();
            input.VirtualAddress = Context.ConvertVirtualAddressToContractAddress(projectId);
            State.Projects[projectId] = input;
            return new Empty();
        }

        public override Empty UpdateRewardProject(ProjectInfo input)
        {
            AssertApprovedByDecentralizedAutonomousOrganization(input);
            var projectId = input.GetProjectId();
            var currentProject = State.Projects[projectId];
            currentProject.Status = input.Status;
            
            if (input.CurrentBudgetPlanIndex > 0)
            {
                currentProject.CurrentBudgetPlanIndex = input.CurrentBudgetPlanIndex;
            }

            if (input.Status == ProjectStatus.Approved)
            {
                if (currentProject.BudgetPlans.Any())
                {
                    // Take budget plans.
                    foreach (var inputBudgetPlan in input.BudgetPlans.Where(p => p.ReceiverAddress != null))
                    {
                        var budgetPlan = currentProject.BudgetPlans.Single(p => p.Index == inputBudgetPlan.Index);
                        budgetPlan.ReceiverAddress = inputBudgetPlan.ReceiverAddress;
                    }
                }
                else
                {
                    // Initial budget plans.
                    currentProject.BudgetPlans.AddRange(input.BudgetPlans);
                    // Need to use Invest method to collect budgets.
                }
            }

            if (input.Status == ProjectStatus.Taken)
            {
                if (input.CurrentBudgetPlanIndex > 0)
                {
                    PayBudget(input);
                }
                
                if (input.CurrentBudgetPlanIndex.Add(1) == input.BudgetPlans.Count)
                {
                    State.PreviewProposalIds.Remove(projectId);
                }
            }
            
            State.Projects[projectId] = currentProject;
            return new Empty();
        }
    }
}