using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Components.Tests
{
    [TestClass()]
    public class EqualsAssignmentTests
    {
        [TestMethod()]
        public void EqualsAssignment_IfRuleShouldAssignStaticOperand_WhenExecutedTheValueIsAssigned()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EqualsComparator());
            newRule.Add(new EmptyOperand());
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            newRule.Execute(field1, false);

            Assert.IsTrue(newRule.RuleSequence[5].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void EqualsAssignment_IfOrphanedComponentIsExecuted_AnArgumentExceptionIsThrown()
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
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => new EqualsAssignment().Execute(field1, newRule));
        }
        [TestMethod()]
        public void EqualsAssignment_IfWrongComponentIsAddedNext_AnArgumentExceptionIsThrown()
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
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.RuleSequence.Add(new IfAntecedent());

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }
        [TestMethod()]
        public void EqualsAssignment_IfWrongComponentIsAddedPreviously_AnArgumentExceptionIsThrown()
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
            newRule.RuleSequence.Add(new IfAntecedent()); 
            newRule.RuleSequence.Add(new EqualsAssignment());
            newRule.RuleSequence.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }
    }
}