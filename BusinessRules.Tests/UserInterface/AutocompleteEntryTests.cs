using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessRules.Domain.UserInterface.Tests
{
    [TestClass()]
    public class AutocompleteEntryTests
    {
        [TestMethod()]
        public void AutocompleteEntry_IfEntryIsGenerated_PropertiesMatch()
        {
            var test = new AutocompleteEntry("label", "value");
            Assert.IsTrue(test.label == "label" && test.value == "value");
        }
    }
}