using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
namespace BusinessRules.Tests.Fields
{
    [TestClass()]
    public class SystemField_ExtensionsTests
    {
        [TestMethod()]
        public void GetValue_IfYouSetAValue_ItMatchesWhenYouGetTheValue()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            field1.AddChildField(field2);
            field2.SetValue("garg");
            var result = field2.GetValue();
            Assert.IsTrue(result == "garg");
        }
    }
}