using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Extensions;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class StaticOperandTests
    {
        [TestMethod()]
        public void StaticOperand_IfTheParameterIsProvided_TheComponentInstantiatesWithoutError()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyComparator());
            newRule.Add(new EmptyOperand());
            newRule.Add(new ThenConsequent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfAttributeIsNotEditable_ValueIsIncludedInDescription()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyComparator());
            newRule.Add(new EmptyOperand());
            newRule.Add(new ThenConsequent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            
            newRule.RuleSequence.Last().Arguments[0].Editable = false;

            var description = newRule.RuleSequence.Last().GetFormattedDescription(field1, false);

            Assert.IsTrue(description.Contains("5"));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfAttributeIsEditable_ValueIsNotIncludedInDescription()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyComparator());
            newRule.Add(new EmptyOperand());
            newRule.Add(new ThenConsequent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.RuleSequence.Last().Arguments[0].Editable = true;

            var description = newRule.RuleSequence.Last().GetFormattedDescription(field1, false);

            Assert.IsTrue(!description.Contains("5"));
        }
    }
}