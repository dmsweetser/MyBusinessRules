using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class BizFieldTests
    {
        [TestMethod()]
        public void ToString_IfFieldHasSystemName_SystemNameReturnsInToString()
        {
            var test = new BizField("woot");
            Assert.IsTrue(test.ToString() == "woot");
        }
    }
}