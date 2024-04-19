using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Extensions;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class LessThanOrEqualToComparatorTests
    {
        [TestMethod()]
        public void Execute_IfOperandIsLessThanOtherOperand_FieldIsUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfOperandIsGreaterThanOtherOperand_FieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "3" }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfOperandIsEqualToOtherOperand_FieldIsUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfOperandIsLessThanOtherOperand_ParallelFieldIsUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("1");
            field3.SetValue("2");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var ruleResult = newRule.Execute(field1, false);

            Assert.IsTrue(ruleResult
                && newRule.RuleSequence[5].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfOperandIsGreaterThanOtherOperand_ParallelFieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("2");
            field3.SetValue("1");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfOperandIsEqualToOtherOperand_ParallelFieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("2");
            field3.SetValue("2");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "5");
        }


        [TestMethod()]
        public void Execute_IfOperandIsLessThanOtherStaticOperand_ParallelFieldIsUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("1");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var ruleResult = newRule.Execute(field1, false);

            Assert.IsTrue(ruleResult
                && newRule.RuleSequence[5].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfOperandIsGreaterThanOtherStaticOperand_ParallelFieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("2");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfOperandIsEqualToOtherStaticOperand_ParallelFieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("2");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false)
                && newRule.RuleSequence[5].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfValueIsGreaterThanStaticOperandAndAlreadyPresentInScopedIndices_OperandIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("2");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(field1, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfValueIsGreaterThanOtherFieldAndAlreadyPresentInScopedIndices_OperandIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field2.SetValue("2");
            field3.SetValue("1");

            field1.AddChildField(field2);
            field1.AddChildField(field2);

            field1.AddChildField(field3);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new LessThanOrEqualToComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(field1, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }
    }
}