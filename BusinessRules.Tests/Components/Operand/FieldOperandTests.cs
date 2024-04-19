using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class FieldOperandTests
    {
        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequestedForActivatedRule_DescriptionIsGivenWithoutError()
        {
            var testField = new BizField("test");
            var newRule = new BizRule("testRule", testField);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var test = new FieldOperand().WithArgumentValues(new string[] { testField.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
            Assert.IsTrue(!string.IsNullOrWhiteSpace(test.GetFormattedDescription(testField, true)));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequestedForNonActivatedRule_DescriptionIsGivenWithoutError()
        {
            var testField = new BizField("test");
            var newRule = new BizRule("testRule", testField);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var test = new FieldOperand().WithArgumentValues(new string[] { testField.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
            Assert.IsTrue(!string.IsNullOrWhiteSpace(test.GetFormattedDescription(testField, false)));
        }

        [TestMethod()]
        public void SetValue_IfFieldIsDateFieldAndFormatIsProvided_ValueIsSetUsingFormat()
        {
            var testField = new BizField("test");
            testField.IsADateField = true;
            testField.ExpectedDateFormat = "yyyy";
            var newRule = new BizRule("testRule", testField);
            var test = new FieldOperand().WithArgumentValues(new string[] { testField.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
            newRule.Add(new IfAntecedent());
            newRule.Add(test);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var castRule = newRule.ToRuleDTO(testField, false);
            var returnedRule = castRule.ToRule(testField);
            (returnedRule.RuleSequence[1] as FieldOperand).SetValue(testField, DateTime.Now.ToString());
            Assert.IsTrue(testField.GetValue() == DateTime.Now.Year.ToString());
        }

        [TestMethod()]
        public void GetFormattedDescription_IfFieldIsNotAssociatedWithRule_ErrorIsReturned()
        {
            var testField = new BizField("test");
            var otherField = new BizField("booger");
            var newRule = new BizRule("testRule", testField);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var test = new FieldOperand().WithArgumentValues(new string[] { testField.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
            Assert.IsTrue(
                !string.IsNullOrWhiteSpace(test.GetFormattedDescription(otherField, true))
                && test.GetFormattedDescription(otherField, true).Contains("ERROR"));
        }
    }
}