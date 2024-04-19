using BusinessRules.Rules.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Domain.Rules.Component;
using Newtonsoft.Json.Linq;
using BusinessRules.Domain.Helpers;

namespace BusinessRules.Rules.Extensions.Tests
{
    [TestClass()]
    public class Rule_ExtensionsTests
    {
        [TestMethod()]
        public void Execute_IfYouBuildASequence_ItWillExecuteWithoutError()
        {
            var field1 = new BizField("Field 1", "4d26ecbd-539a-454b-991b-7ddadc610f5b");
            var field2 = new BizField("Field 2", "b6d90071-3912-4e9f-9b3c-c57429d8fa85");
            var field3 = new BizField("Field 3", "2e48bc33-c58c-4c28-b70c-ab5d4fac413c");
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
            newRule.Add(new EmptyOperand());

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false));
        }
        [TestMethod()]
        public void Add_IfAddingWrongComponentToSequence_AnArgumentExceptionIsThrown()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            var nextComponent = newRule.Then().FirstOrDefault();
            newRule.Add(newRule.Then().FirstOrDefault());
            Assert.ThrowsException<ArgumentException>(() => newRule.Add(nextComponent));
        }
        [TestMethod()]
        public void Add_IfAddingWrongComponentDirectlyToSequence_AnArgumentExceptionIsThrown()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            Assert.ThrowsException<ArgumentException>(() => newRule.Add(new IfAntecedent()));
        }
        [TestMethod()]
        public void VerifyMatchingType_IfFoundComponentCannotMatchToType_TypeIsRejected()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.RuleSequence.Add(new IfAntecedent());
            Assert.IsFalse(newRule.VerifyMatchingType(typeof(string)));
        }
        [TestMethod()]
        public void VerifyMatchingType_IfFoundComponentsCountIsZero_TypeIsRejected()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.RuleSequence.Add(new StaticOperand().WithArgumentValues(new string[] { "test" }));
            Assert.IsFalse(newRule.VerifyMatchingType(typeof(string)));
        }
        [TestMethod()]
        public void Instantiate_IfComponentIsInstantiatedAndTypeMatches_ItInstantiatesWithoutError()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            var test = newRule.Then().FirstOrDefault();
            var instantiatedComponent = newRule.Instantiate(test);
            Assert.IsTrue(instantiatedComponent is IfAntecedent);
        }
        [TestMethod()]
        public void Instantiate_IfComponentIsInstantiatedAndTypeIsNotAppropriate_AnArgumentExceptionIsThrown()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            var test = newRule.Then().FirstOrDefault();
            Assert.ThrowsException<ArgumentException>(() =>
                newRule.Instantiate(typeof(StaticOperand), new string[] { Guid.NewGuid().ToString(), "5" }));
        }
        [TestMethod()]
        public void GetNextComponents_IfNextComponentsIncludeChildField_FieldOperandWithMatchingIdIsPresent()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            var topLevelField = new BizField("parent");
            var childField = new BizField("child");
            topLevelField.AddChildField(childField);
            var foundComponents = newRule.GetNextComponents(topLevelField, true);
            Assert.IsTrue(foundComponents.Any(x =>
                x.Value.Arguments.Any(y => y.Value == childField.Id.ToString())
                && x.Value.DefinitionId == new FieldOperand().DefinitionId));
        }
        [TestMethod()]
        public void GetNextComponents_IfNextComponentsIncludeAttributes_AttributeOperandWithMatchingIdIsPresent()
        {
            var topLevelField = new BizField("parent");
            var childField = new BizField("child");
            var childAttribute = new BizField("childAttribute");
            childField.AddChildField(childAttribute);
            topLevelField.AddChildField(childField);
            var newRule = new BizRule("test", topLevelField);
            newRule.Add(newRule.Then().FirstOrDefault());
            var foundComponents = newRule.GetNextComponents(topLevelField, true);
            Assert.IsTrue(foundComponents.Any(x =>
                x.Value.Arguments.Any(y => y.Value == childAttribute.Id.ToString())
                && x.Value.DefinitionId == new FieldOperand().DefinitionId));
        }

        [TestMethod()]
        public void GetNextComponents_IfNotTestMode_TestComponentsAreNotReturned()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            var topLevelField = new BizField("parent");
            var childField = new BizField("child");
            var childAttribute = new BizField("childAttribute");
            childField.AddChildField(childAttribute);
            topLevelField.AddChildField(childField);
            var foundComponentsTestMode = newRule.GetNextComponents(topLevelField, true);
            var foundComponentsNotTestMode = newRule.GetNextComponents(topLevelField, false);


            Assert.IsTrue(foundComponentsTestMode.Any(x => typeof(IAmATestComponent).IsAssignableFrom(x.Value.ToComponent().GetType()))
                && foundComponentsNotTestMode.All(x => !typeof(IAmATestComponent).IsAssignableFrom(x.Value.ToComponent().GetType())));
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_TheChangeIsOnTheSecondChild()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_TheChangeIsOnTheFirstChild()
        {

            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "");
        }


        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_TheChangeIsOnBothChildren()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field2");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_SeparateParallelChildrenAreUpdatedAppropriately()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Hide Me")[1].Value == "true");
        }


        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_ParallelChildrenInSameRuleAreNotUpdatedWhenAppropriate()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == ""
                            && mergedBizField.GetChildFieldsByName("Hide Me")[0].Value == "");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_SeparateParallelChildrenAreUpdatedIdentically()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field2");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildrenWithDates_SeparateParallelChildrenAreUpdatedIdentically()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""01/01/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": ""01/01/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            convertedBizField.GetChildFieldsByName("State Value").ForEach(x => x.IsADateField = true);

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/01/2001" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field2");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildrenWithDates_ParallelChildrenInSameRuleAreNotUpdatedWhenAppropriate()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""01/01/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            convertedBizField.GetChildFieldsByName("State Value").ForEach(x => x.IsADateField = true);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/01/2001" }));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == ""
                            && mergedBizField.GetChildFieldsByName("Hide Me")[0].Value == "");
        }


        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildrenWithDatesGreaterThanAndLessThan_SeparateParallelChildrenAreUpdatedIdentically()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""01/02/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": ""01/02/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            convertedBizField.GetChildFieldsByName("State Value").ForEach(x => x.IsADateField = true);

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new GreaterThanComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/01/2001" }));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new LessThanComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/03/2001" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field2");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildrenWithDatesGreaterThanAndLessThan_ParallelChildrenInSameRuleAreNotUpdatedWhenAppropriate()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""01/02/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            convertedBizField.GetChildFieldsByName("State Value").ForEach(x => x.IsADateField = true);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new GreaterThanComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/01/2001" }));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new LessThanComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/03/2001" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == ""
                            && mergedBizField.GetChildFieldsByName("Hide Me")[0].Value == "");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildrenWithNotEqualDates_SeparateParallelChildrenAreUpdatedIdentically()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""01/01/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": ""01/01/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            convertedBizField.GetChildFieldsByName("State Value").ForEach(x => x.IsADateField = true);

            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new NotEqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/02/2001" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field2");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildrenWithUnequalDates_ParallelChildrenInSameRuleAreNotUpdatedWhenAppropriate()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""01/02/2001"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            convertedBizField.GetChildFieldsByName("State Value").ForEach(x => x.IsADateField = true);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new NotEqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "01/01/2001" }));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == ""
                            && mergedBizField.GetChildFieldsByName("Hide Me")[0].Value == "");
        }
















        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_SeparateParallelChildrenEvaluatedInParallelAreUpdatedAppropriately()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": ""Nonsense"",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": ""12345"",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Street").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Street").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Hide Me")[1].Value == "true");
        }


        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_ParallelChildrenEvaluatedInParallelInSameRuleAreNotUpdatedWhenAppropriate()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": ""Nonsense"",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": ""12345"",
                ""City"": """",
                ""State"": {
                    ""State Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": ""12345"",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("street").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Street").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new AndConjunction());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == ""
                            && mergedBizField.GetChildFieldsByName("Hide Me")[0].Value == "");
        }

        [TestMethod()]
        public void Execute_IfRuleIsExecutedForFieldWithDuplicateChildren_SeparateParallelChildrenEvaluatedInParallelAreUpdatedIdentically()
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": ""Test"",
        ""Last Name"":  ""LastName"",
        ""Address"": [
            {
                ""Street"": ""Nonsense"",
                ""City"": ""Field1"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            },
            {
                ""Street"": ""Nonsense"",
                ""City"": ""Field2"",
                ""State"": {
                    ""State Value"": ""Nonsense"",
                    ""Error Message"": """"
                },
                ""Zip"": {
                    ""Zip Value"": """",
                    ""Hide Me"": """"
                },
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);
            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());
            var mergedBizField = BizField_Helpers.MergeJTokenToBizField(parsedObject, convertedBizField);

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("State Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Street").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Error Message").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("City").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            var zipRule = new BizRule("Zip Is Hidden Rule", convertedBizField);
            zipRule.Add(new IfAntecedent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Zip Value").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsComparator());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "12345" }));
            zipRule.Add(new ThenConsequent());
            zipRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("Hide Me").Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            zipRule.Add(new EqualsAssignment());
            zipRule.Add(new StaticOperand().WithArgumentValues(new string[] { "true" }));
            zipRule.IsActivated = false;
            zipRule.IsActivatedTestOnly = true;

            stateRule.Execute(mergedBizField, true);
            zipRule.Execute(mergedBizField, true);

            Assert.IsTrue(mergedBizField.GetChildFieldsByName("Error Message")[0].Value == "Field1"
                            && mergedBizField.GetChildFieldsByName("Error Message")[1].Value == "Field2");
        }

        [TestMethod()]
        public void GetNextComponents_IfDynamicComparatorIsPresent_DynamicComparatorIsReturned()
        {
            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot1""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var script =
@"
    function myFunction(x, y) {
        return x.includes(y);
    }
";

            var newDynamicComponent = new DynamicComparator("includes", script);

            convertedField.DynamicComponents.Add(newDynamicComponent);

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));

            var nextComponents = newRule.GetNextComponents(convertedField, false);
            Assert.IsTrue(nextComponents.Any(x => x.Value.DefinitionId == newDynamicComponent.DefinitionId));
        }

        [TestMethod()]
        public void GetNextComponents_IfDynamicOperandIsPresent_DynamicOperandIsReturned()
        {
            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot1""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var script =
@"
    function myFunction(currentField) {
        return ""boop"";
    }
";

            var newDynamicComponent = new DynamicOperand("da booper", script);

            convertedField.DynamicComponents.Add(newDynamicComponent);

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());

            var nextComponents = newRule.GetNextComponents(convertedField, false);
            Assert.IsTrue(nextComponents.Any(x => x.Value.DefinitionId == newDynamicComponent.DefinitionId));
        }

        [TestMethod()]
        public void GetNextComponents_IfNextFieldIsCollection_NextComponentsIncludesAnyAndEvery()
        {
            var newField = new BizField("test");
            newField.IsACollection = true;

            var newRule = new BizRule("test", newField);
            newRule.Add(new IfAntecedent());

            var nextComponents = newRule.GetNextComponents(newField, false);
            Assert.IsTrue(
                nextComponents.Any(x => x.Key.Contains("any "))
                && nextComponents.Any(x => x.Key.Contains("every ")));
        }

        [TestMethod()]
        public void GetNextComponents_IfNextFieldIsCollectionAndAnyWasAlreadySelected_NextComponentsIncludesAssociated()
        {
            var newField = new BizField("test");
            newField.IsACollection = true;

            var newRule = new BizRule("test", newField);
            newRule.Add(new IfAntecedent());

            newRule.Add(newRule.GetNextComponents(newField, false).First(x => x.Key.Contains("any ")).Value.ToComponent());

            newRule.Add(new EqualsComparator());

            var nextComponents = newRule.GetNextComponents(newField, false);

            Assert.IsTrue(
                nextComponents.Any(x => x.Key.Contains("associated")));
        }
    }
}