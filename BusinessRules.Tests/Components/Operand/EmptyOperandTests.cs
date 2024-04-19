using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Fields;
namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class EmptyOperandTests
    {
        [TestMethod()]
        public void EmptyOperand_IfGetValueIsCalled_ValueIsBlank()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test rule", newField);
            Assert.IsTrue(new EmptyOperand().GetValues(newField, newRule.ScopedIndices).FirstOrDefault() == "");
        }

        [TestMethod()]
        public void EmptyOperand_IfSetValueIsCalled_ReturnsTrueButValueIsUnchanged()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test rule", newField);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            var newOperand = new EmptyOperand();
            Assert.IsTrue(newOperand.SetValue(newField, "test") == true
                && newOperand.GetValues(newField, newRule.ScopedIndices).FirstOrDefault() == "");
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequestedForActivatedRule_DescriptionIsGivenWithoutError()
        {
            var testField = new BizField("test");
            var test = new EmptyOperand();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(test.GetFormattedDescription(testField, true)));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequestedForNonActivatedRule_DescriptionIsGivenWithoutError()
        {
            var testField = new BizField("test");
            var test = new EmptyOperand();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(test.GetFormattedDescription(testField, false)));
        }
    }
}