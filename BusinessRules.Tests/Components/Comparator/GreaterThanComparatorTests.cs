using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Extensions;
using Newtonsoft.Json.Linq;
using BusinessRules.Domain.Helpers;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class GreaterThanComparatorTests
    {
        [TestMethod()]
        public void Execute_IfOperandIsGreaterThanOtherOperand_FieldIsUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
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
        public void Execute_IfOperandIsLessThanOtherOperand_FieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "3" }));
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
        public void Execute_IfOperandIsEqualToOtherOperand_FieldIsNotUpdated()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
            newRule.Add(new GreaterThanComparator());
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
        public void Execute_IfOperandIsGreaterThanOtherOperand_ParallelFieldIsUpdated()
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
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
        public void Execute_IfOperandIsLessThanOtherOperand_ParallelFieldIsNotUpdated()
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
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
        public void Execute_IfOperandIsGreaterThanOtherStaticOperand_ParallelFieldIsUpdated()
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
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
        public void Execute_IfOperandIsLessThanOtherStaticOperand_ParallelFieldIsNotUpdated()
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
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
        public void Execute_IfOperandIsGreaterThanOneStaticOperandButLessThanAnotherStaticOperand_ParallelFieldIsNotUpdated()
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
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "0" }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));
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
        public void Execute_IfOperandIsGreaterThanOneOperandButLessThanAnotherOperand_ParallelFieldIsNotUpdated()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 1
            },
            {
                ""Field 2 Value"": 1
            },
        ],
        ""Field 3"": [
            {
                ""Field 3 Value"": 2
            },
            {
                ""Field 3 Value"": 2
            },
        ],
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { 
                convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new GreaterThanComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { 
                convertedBizField.GetChildFieldByName("Field 3 Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }
    }
}