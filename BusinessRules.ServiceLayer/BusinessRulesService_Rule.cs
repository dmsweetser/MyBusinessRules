using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;

namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public async Task SaveRule(Guid companyId, BizRule currentRule)
        {
            var foundField = await this.ValidateField(companyId, currentRule.ParentFieldId);

            if (!foundField.RuleIds.Any(x => x == currentRule.Id))
            {
                foundField.RuleIds.Add(currentRule.Id);
            }            

            await Repository.SaveTopLevelField(companyId.ToString(), foundField);

            await Repository.SaveRule(currentRule.ParentFieldId.ToString(), currentRule);
        }

        public async Task<List<BizRule>> GetRules(Guid companyId, Guid fieldId)
        {
            var foundField = await this.ValidateField(companyId, fieldId);

            var rulesToReturn = new List<BizRule>();

            foreach (var ruleId in foundField.RuleIds)
            {
                rulesToReturn.Add(await GetRuleById(companyId, foundField.Id, ruleId));
            }

            return rulesToReturn;
        }

        public async Task<BizRule> GetRuleById(Guid companyId, Guid fieldId, Guid ruleId)
        {
            return await Repository.GetRule(fieldId.ToString(), ruleId.ToString());
        }

        public async Task DeleteRule(Guid companyId, Guid fieldId, BizRule currentRule)
        {
            var foundField = await this.ValidateField(companyId, fieldId);

            if (foundField.RuleIds.Contains(currentRule.Id))
            {
                foundField.RuleIds.Remove(currentRule.Id);
            }

            await Repository.SaveTopLevelField(companyId.ToString(), foundField);

            await Repository.DeleteRule(fieldId.ToString(), currentRule.Id.ToString());
            return;
        }
    }
}
