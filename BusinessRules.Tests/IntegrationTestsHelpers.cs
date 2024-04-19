using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Tests
{
    public class IntegrationTestsHelpers
    {
        internal static BizField BuildPolicyField()
        {
            var parentPolicyField = new BizField("Policy", "e36fd356-633b-4f2b-a999-4e921fa05759");
            var newField1 = new BizField("NewField1", "f81a9503-bd0a-4441-ad1a-76933aa3e616");
            parentPolicyField.AddChildField(newField1);
            return parentPolicyField;
        }

        internal static BizRule BuildRule(BizField policyField)
        {
            var newRule = new BizRule("test", policyField);
            newRule.Add(new IfAntecedent());
            var staticOperand = new StaticOperand();
            staticOperand.Arguments[0].Value = "1";
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] {"1"}));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            return newRule;
        }

        internal static void UseTheSystem(BizField policyField, BizRule currentRule, out bool evalResult)
        {
            policyField.ChildFields[0].SetValue("test1");
            currentRule.Execute(policyField, false);
            evalResult = currentRule.RuleSequence[5].Arguments[0].Value == "5";
        }
    }
}
