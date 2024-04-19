using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Extensions;
using Newtonsoft.Json.Linq;
using BusinessRules.Domain.Helpers;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class ContainsTextComparatorTests
    {
        [TestMethod()]
        public void ContainsTextComparator_IfTheFirstComponentContainsTheSecond_RuleIsAllowedToExecute()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "booger" }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "boOg" }));
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
        public void ContainsTextComparator_IfFirstComponentDoesNotContainTheSecond_RuleIsNotAllowedToExecute()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "booger" }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
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
        public void ContainsTextComparator_IfOrphanedComponentIsExecuted_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "booger" }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "boog" }));
            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;
            Assert.ThrowsException<ArgumentException>(() => new ContainsTextComparator().Execute(field1, newRule));
        }

        [TestMethod()]
        public void ContainsTextComparator_IfWrongComponentIsAddedNext_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "booger" }));
            newRule.Add(new ContainsTextComparator());
            newRule.RuleSequence.Add(new IfAntecedent());

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void ContainsTextComparator_IfWrongComponentIsAddedPreviously_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());

            newRule.RuleSequence.Add(new IfAntecedent());
            newRule.RuleSequence.Add(new ContainsTextComparator());
            newRule.RuleSequence.Add(new StaticOperand().WithArgumentValues(new string[] { "booger" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void ContainsTextComparator_IfSecondComponentContainsFirstButNotThird_RuleIsNotAllowedToExecute()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Value"": ""boog"",
        ""Field 2"": ""booger"",
        ""Field 3"": ""woot""
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { 
                convertedBizField.GetChildFieldByName("Field 2").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 2").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new ContainsTextComparator());
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
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 4
            },
            {
                ""Field 2 Value"": 0,
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
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
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
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsMetForAnyChild_RuleIsExecuted()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": ""booger"",
                ""Other parallel child"": ""boog""
            },
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": ""woot"",
                ""Other parallel child"": ""Wo""
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
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { 
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivatedTestOnly = false;
            newRule.IsActivated = true;

            Assert.IsTrue(newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsNotMetForAnyChild_RuleIsNotExecuted()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 1,
                ""Parallel child"": ""booger"",
                ""Other parallel child"": ""boog""
            },
            {
                ""Field 2 Value"": 1,
                ""Parallel child"": ""woot"",
                ""Other parallel child"": ""Wo""
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
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivatedTestOnly = false;
            newRule.IsActivated = true;

            Assert.IsTrue(!newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }

        [TestMethod()]
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsMetForAllChildren_RuleIsExecuted()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": ""booger"",
                ""Other parallel child"": ""boog""
            },
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": ""woot"",
                ""Other parallel child"": ""Wo""
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
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivatedTestOnly = false;
            newRule.IsActivated = true;

            Assert.IsTrue(newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "5");
        }

        [TestMethod()]
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsNotMetForAllChildren_RuleIsNotExecuted()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": ""booger"",
                ""Other parallel child"": ""boog""
            },
            {
                ""Field 2 Value"": 1,
                ""Parallel child"": ""woot"",
                ""Other parallel child"": ""Wo""
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
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new ContainsTextComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Other parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivatedTestOnly = false;
            newRule.IsActivated = true;

            Assert.IsTrue(!newRule.Execute(mergedBizField, false)
                && newRule.RuleSequence[9].Arguments[0].Value == "4");
        }
    }
}