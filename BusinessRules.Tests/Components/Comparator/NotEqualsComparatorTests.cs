using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Extensions;
using Newtonsoft.Json.Linq;
using BusinessRules.Domain.Helpers;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class NotEqualsComparatorTests
    {
        [TestMethod()]
        public void NotEqualsComparator_IfTwoComponentsAreNotEqual_RuleIsAllowedToExecute()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new NotEqualsComparator());
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
        public void NotEqualsComparator_IfTwoComponentsAreEqual_RuleIsNotAllowedToExecute()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new NotEqualsComparator());
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
        public void NotEqualsComparator_IfOrphanedComponentIsExecuted_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => new NotEqualsComparator().Execute(field1, newRule));
        }

        [TestMethod()]
        public void NotEqualsComparator_IfWrongComponentIsAddedNext_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            newRule.Add(new NotEqualsComparator());
            newRule.RuleSequence.Add(new IfAntecedent());

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void NotEqualsComparator_IfWrongComponentIsAddedPreviously_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());

            newRule.RuleSequence.Add(new IfAntecedent());
            newRule.RuleSequence.Add(new NotEqualsComparator());
            newRule.RuleSequence.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void NotEqualsComparator_IfTwoComponentsAreNotEqualButSubsequentComponentsAreEqual_RuleIsNotAllowedToExecute()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Value"": 0,
        ""Field 2"": 1,
        ""Field 3"": 1
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 2").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 2").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 3").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var executeOutcome = newRule.Execute(mergedBizField, false);

            Assert.IsTrue(!executeOutcome
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsNotMet_RuleIsNotExecuted()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 1,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            },
            {
                ""Field 2 Value"": 1,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(!newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsMet_RuleIsExecuted()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 1,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 4
            },
            {
                ""Field 2 Value"": 1,
                ""Parallel child"": 3,
                ""Other parallel child"": 4
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new NotEqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivatedTestOnly = false;
            newRule.IsActivated = true;

            Assert.IsTrue(newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "5");
        }
    }
}