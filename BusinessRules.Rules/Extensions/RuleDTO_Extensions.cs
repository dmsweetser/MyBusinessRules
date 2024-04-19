using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Rules
{
    public static class RuleDTO_Extensions
    {
        public static BizRuleDTO ToRuleDTO(this BizRule rule, BizField topLevelField, bool isTestMode)
        {
            //Populate the current component with the input restrictions from the other side of the comparator
            for (int i = 0; i < rule.RuleSequence.Count; i++)
            {
                if (i < 2) continue;
                if (rule.RuleSequence[i] is not DynamicComponent
                    && rule.RuleSequence[i - 2] is IAmAnOperand
                    && rule.RuleSequence[i - 2] is not DynamicOperand
                    && ((rule.RuleSequence[i - 1] is IAmAComparator
                            && rule.RuleSequence[i - 1] is not DynamicComparator)
                        || rule.RuleSequence[i - 1] is IAmAnAssignment)
                    && rule.RuleSequence[i].Arguments.Count > 0
                    && rule.RuleSequence[i - 2].Arguments.Count > 0)
                {
                    rule.RuleSequence[i].Arguments[0].AllowedValueRegex = rule.RuleSequence[i - 2].Arguments[0].AllowedValueRegex;
                    rule.RuleSequence[i].Arguments[0].IsADateField = rule.RuleSequence[i - 2].Arguments[0].IsADateField;
                    rule.RuleSequence[i].Arguments[0].FriendlyValidationMessageForRegex = rule.RuleSequence[i - 2].Arguments[0].FriendlyValidationMessageForRegex;
                    rule.RuleSequence[i].Arguments[0].AllowedValues = rule.RuleSequence[i - 2].Arguments[0].AllowedValues;
                    rule.RuleSequence[i].Arguments[0].IsACollection = rule.RuleSequence[i - 2].Arguments[0].IsACollection;
                }
            }

            return new BizRuleDTO()
            {
                Id = rule.Id,
                Name = rule.Name,
                GroupName = rule.GroupName ?? "",
                RuleSequence = rule.RuleSequence.Select(x => x.ToComponentDTO(topLevelField, rule.IsActivated)).ToList(),
                IsActivated = rule.IsActivated,
                IsTestMode = rule.IsActivatedTestOnly,
                StartUsingOn = rule.StartUsingOn,
                StopUsingOn = rule.StopUsingOn,
                NextComponents = rule.GetNextComponents(topLevelField, isTestMode).OrderBy(x => x.Key).ToList()
            };
        }

        public static BizRule ToRule(this BizRuleDTO ruleDTO, BizField topLevelField)
        {
            var convertedRule = new BizRule(ruleDTO.Name, topLevelField)
            {
                Id = ruleDTO.Id,
                GroupName = ruleDTO.GroupName ?? "",
                RuleSequence = ruleDTO.RuleSequence.Select(x => x.ToComponent()).ToList(),
                IsActivated = ruleDTO.IsActivated,
                IsActivatedTestOnly = ruleDTO.IsTestMode,
                StartUsingOn = ruleDTO.StartUsingOn,
                StopUsingOn = ruleDTO.StopUsingOn
            };

            foreach (var component in convertedRule.RuleSequence)
            {
                if (component is DynamicComparator dynamicComparator)
                {
                    var foundComponent = 
                        topLevelField.DynamicComponents.FirstOrDefault(x => x.DefinitionId == dynamicComparator.DefinitionId)
                        as DynamicComparator;
                    dynamicComparator.Body = foundComponent.Body;
                    dynamicComparator.Description = foundComponent.Description;
                } else if (component is DynamicOperand dynamicOperand)
                {
                    var foundComponent =
                        topLevelField.DynamicComponents.FirstOrDefault(x => x.DefinitionId == dynamicOperand.DefinitionId)
                        as DynamicOperand;
                    dynamicOperand.Body = foundComponent.Body;
                    dynamicOperand.Description = foundComponent.Description;
                }
            }
            
            return convertedRule;
        }
    }
}
