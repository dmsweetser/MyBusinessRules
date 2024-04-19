using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Helpers;
using BusinessRules.Domain.Rules;
using Newtonsoft.Json.Linq;
using BusinessRules.Rules.Extensions;
using BusinessRules.Tests;
using BusinessRules.Domain.Common;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class DynamicComparatorTests
    {
        const string equalsScript =
        @"
    return x === y;
"
        ;

        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration();
            DynamicComparator.FunctionUrl = config.DynamicComparatorFunctionUrl;
            DynamicOperand.FunctionUrl = config.DynamicOperandFunctionUrl;
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequested_DescriptionIsReturned()
        {
            var script =
@"
    return x.includes(y);
";

            var test = new DynamicComparator("asdf", script);

            Assert.IsTrue(test.GetFormattedDescription(new BizField("bbb"), true) == "asdf");
        }



        [TestMethod()]
        public void DynamicComparator_IfTwoComponentsAreEqual_RuleIsAllowedToExecute()
        {
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);

            field1.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
        public void DynamicComparator_IfTwoComponentsAreNotEqual_RuleIsNotAllowedToExecute()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);

            field1.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
        public void DynamicComparator_IfOrphanedComponentIsExecuted_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;
            Assert.ThrowsException<ArgumentException>(() => new EqualsComparator().Execute(field1, newRule));
        }

        [TestMethod()]
        public void DynamicComparator_IfWrongComponentIsAddedNext_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);

            field1.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.RuleSequence.Add(new IfAntecedent());

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void DynamicComparator_IfWrongComponentIsAddedPreviously_AnArgumentExceptionIsThrown()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());

            newRule.RuleSequence.Add(new IfAntecedent());
            newRule.RuleSequence.Add(new EqualsComparator());
            newRule.RuleSequence.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.ThrowsException<ArgumentException>(() => newRule.Execute(field1, false));
        }

        [TestMethod()]
        public void DynamicComparator_IfTwoComponentsAreEqualButSubsequentComponentsAreNotEqual_RuleIsNotAllowedToExecute()
        {
            var newField =
@"
{
    ""Field 1"": {
        ""Value"": 0,
        ""Field 2"": 0,
        ""Field 3"": 1
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var dynamicComparator = new DynamicComparator(Guid.NewGuid().ToString(), equalsScript);
            convertedBizField.DynamicComponents.Add(dynamicComparator);

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 2").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 2").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsNotMetForAnyChild_RuleIsNotExecuted()
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

            convertedBizField.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsMetForAnyChildren_RuleIsExecuted()
        {
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            },
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            convertedBizField.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
        public void Execute_IfDuplicateParentsArePresentAndChildConditionIsNotMetForAnyChildren_RuleIsNotExecuted()
        {
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 4,
                ""Other parallel child"": 3
            },
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 4,
                ""Other parallel child"": 3
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            convertedBizField.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            },
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            convertedBizField.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var newField =
@"
{
    ""Field 1"": {
        ""Field 1 Value"": 0,
        ""Field 2"": [
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 3,
                ""Other parallel child"": 3
            },
            {
                ""Field 2 Value"": 0,
                ""Parallel child"": 4,
                ""Other parallel child"": 3
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            convertedBizField.DynamicComponents.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var newRule = new BizRule("test", convertedBizField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
               convertedBizField.GetChildFieldByName("Field 2 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Field 1 Value").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new AndConjunction());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Parallel child").Id.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() }));
            newRule.Add(new DynamicComparator(Guid.NewGuid().ToString(), equalsScript));
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