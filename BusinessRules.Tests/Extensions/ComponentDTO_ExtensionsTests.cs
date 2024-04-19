using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Rules.Components;
using BusinessRules.Domain.Fields;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Rules.Tests
{
    [TestClass()]
    public class ComponentDTO_ExtensionsTests
    {
        [TestMethod()]
        public void ToComponent_IfDtoArgumentsAreNull_ComponentArgumentsAreAnEmptyArray()
        {
            var test = new StaticOperand().WithArgumentValues(new string[] { "test" });
            var dto = test.ToComponentDTO(new BizField("newField"), true);
            dto.Arguments = null;
            Assert.IsTrue(dto.ToComponent().Arguments.Count == 0);
        }

        [TestMethod()]
        public void ToComponentDTO_IfArgumentsAreNull_DtoArgumentsAreAnEmptyArray()
        {
            var test = new IfAntecedent();
            test.Arguments = null;
            Assert.IsTrue(test.ToComponentDTO(new BizField("newField"), true).Arguments.Count == 0);
        }
    }
}