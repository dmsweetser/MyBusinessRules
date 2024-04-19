using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules;
namespace BusinessRules.Tests
{
    [TestClass()]
    public class RuleComponentFactoryTests
    {
        [TestMethod()]
        public void GetComponents_WhenYouGetComponents_ComponentsAreReturned()
        {
            var result = RuleComponentFactory.GetComponents<IAmAnOperand>();
            Assert.IsTrue(result.Count > 0);
        }
    }
}