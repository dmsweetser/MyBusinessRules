using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;

namespace BusinessRules.Rules.Components.Tests
{
    [TestClass()]
    public class DynamicOperandTests
    {
        [TestMethod()]
        public void SetValue_IfValueIsSet_ReturnsTrue()
        {
            var script =
@"
    return ""boop"";
";

            var test = new DynamicOperand("test", script);

            Assert.IsTrue(test.SetValue(new BizField("test"), "boop"));
        }

        [TestMethod()]
        public void GetFormattedDescription_IfDescriptionIsRequested_ItIsReceived()
        {
            var script =
@"
return ""boop"";
";

            var test = new DynamicOperand("garg", script);

            Assert.IsTrue(test.GetFormattedDescription(new BizField("Asdf"), true) == "garg");
        }
    }
}