using BusinessRules.Domain.Common;
using BusinessRules.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class EndUserDTO_ExtensionsTests
    {
        private AppSettings _config;

        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration();
            _config = config;
        }

        [TestMethod()]
        public void ToEndUserDTO_IfAFieldIsProvided_TheEndUserDtoPropertiesMatch()
        {
            var parentField = new BizField("parent");
            var newAttribute = new BizField("attribute");
            var childField = new BizField("child");
            parentField.AddChildField(newAttribute);
            parentField.AddChildField(childField);
            newAttribute.SetValue("test");
            childField.SetValue("woot");
            parentField.SetValue("arrghh");
            parentField.SetValue("hhgrra");
            var endUserDTO = parentField.ToEndUserDTO(Guid.NewGuid(), _config);
            Assert.IsTrue(endUserDTO.CurrentField.SystemName == parentField.SystemName
                & endUserDTO.CurrentField.ParentFieldId == parentField.ParentFieldId
                & endUserDTO.CurrentField.SystemName == "parent"
                & endUserDTO.CurrentField.Value == "hhgrra"
                & endUserDTO.CurrentField.ChildFields.Count == parentField.ChildFields.Count
                );
        }
    }
}