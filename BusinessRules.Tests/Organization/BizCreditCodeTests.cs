using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessRules.Domain.Organization.Tests
{
    [TestClass()]
    public class BizCreditCodeTests
    {
        [TestMethod()]
        //Don't judge me
        public void BizCreditCode_IfInstantiated_ItExists()
        {
            var test = new BizCreditCode();
            Assert.IsNotNull(test);
        }
    }
}