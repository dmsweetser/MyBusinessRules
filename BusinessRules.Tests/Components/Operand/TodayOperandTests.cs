using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class TodayOperandTests
    {
        [TestMethod()]
        public void TodayOperand_IfOperandIsCreated_ItExists()
        {
            var test = new TodayOperand();

            Assert.IsNotNull(test);
        }

        [TestMethod()]
        public void GetValues_IfValueIsRequested_ItIsToday()
        {
            var testField = new BizField("test");
            var testRule = new BizRule("testRule", testField);
            
            var testOperand = new TodayOperand();

            var result = testOperand.GetValues(testField, new List<int>())[0];
            Assert.IsTrue(DateTime.TryParse(result, out var parsedDate) && parsedDate.Date == DateTime.Now.Date);
        }

        [TestMethod()]
        public void SetValue_IfValueIsProvided_ItIsIgnored()
        {
            var testField = new BizField("test");
            var testRule = new BizRule("testRule", testField);

            var testOperand = new TodayOperand();

            var result = testOperand.SetValue(testField, "booger");

            var getValue = testOperand.GetValues(testField, new List<int>())[0];
            
            Assert.IsTrue(result && DateTime.TryParse(getValue, out _));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequested_TheResultIsToday()
        {
            var testField = new BizField("test");
            var testRule = new BizRule("testRule", testField);

            var testOperand = new TodayOperand();

            Assert.IsTrue(testOperand.GetFormattedDescription(testField, true) == "today's date");
        }
    }
}