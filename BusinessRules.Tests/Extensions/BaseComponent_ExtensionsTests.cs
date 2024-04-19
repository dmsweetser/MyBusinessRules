using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Rules.Components;
using BusinessRules.Tests;
using BusinessRules.Domain.Common;

namespace BusinessRules.Rules.Extensions.Tests
{
    [TestClass()]
    public class BaseComponent_ExtensionsTests
    {
        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration();
            DynamicComparator.FunctionUrl = config.DynamicComparatorFunctionUrl;
            DynamicOperand.FunctionUrl = config.DynamicOperandFunctionUrl;
        }

        [TestMethod()]
        public void WithArgumentValues_IfInsufficientArgumentsAreProvided_ArgumentExceptionIsThrown()
        {
            var test = new FieldOperand();
            Assert.ThrowsException<ArgumentException>(() => test.WithArgumentValues(new string[] { }));
        }

        [TestMethod()]
        public async Task ExecuteJavascriptComparator_IfJavascriptIsMalFormed_ReturnsNull()
        {
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var script =
@"
    return x.includes(
";
            var test = new DynamicComparator("test", script);

            var config = TestHelpers.InitConfiguration();
            DynamicComparator.FunctionUrl = config.DynamicComparatorFunctionUrl;

            var result = await test.ExecuteJavascriptComparator(script, "1", "1", 0, DynamicComparator.FunctionUrl);

            Assert.IsTrue(result == "False");
        }

        [TestMethod()]
        public async Task ExecuteJavascriptOperand_IfJavascriptIsMalformed_ThrowsException()
        {
            var script =
@"
    return x.includes(
";
            var test = new DynamicComparator("test", script);

            var config = TestHelpers.InitConfiguration();
            DynamicOperand.FunctionUrl = config.DynamicOperandFunctionUrl;

            var exceptionFound = false;

            try
            {
                var result = await test.ExecuteJavascriptOperand(script, "{\"Test\":\"1\"}", 0, DynamicOperand.FunctionUrl);
            }
            catch (Exception)
            {
                exceptionFound = true;
            }
            
            Assert.IsTrue(exceptionFound);
        }
    }
}