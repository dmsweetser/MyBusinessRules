using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
namespace BusinessRules.Rules.Tests
{
    [TestClass()]
    public class BusinessUserDTO_ExtensionsTests
    {
        [TestMethod()]
        public void ToBusinessUserDTO_IfProvidedFieldIsNotTopLevelField_ArgumentExceptionIsThrown()
        {
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            Assert.ThrowsException<ArgumentException>(() => newField.GetChildFieldById(newChild.Id).ToBusinessUserDTO(new List<BizRule>(), "", true));
        }
    }
}