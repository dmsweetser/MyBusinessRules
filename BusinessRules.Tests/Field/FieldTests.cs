using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class FieldTests
    {
        [TestMethod()]
        public void SystemField_IfFieldIsCreated_TheFieldIsSerializableAndDeserializable()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var serializedField = JsonConvert.SerializeObject(field1);
            var deserializedField = JsonConvert.DeserializeObject<BizField>(serializedField);
            Assert.IsTrue(deserializedField.Id.ToString() == field1.Id.ToString());
        }
    }
}