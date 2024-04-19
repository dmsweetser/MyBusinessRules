using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class Field_ExtensionsTests
    {
        [TestMethod()]
        public void GetFieldById_IfFieldExists_FieldIsReturned()
        {
            var newField = new BizField("Test 1", "7db344e5-6933-423c-b86b-c00abc5e9b4c");
            var childField = new BizField("Child 1", "f5e107bb-875f-4e18-bdd0-63245d16cc45");
            newField.AddChildField(childField);
            Assert.IsNotNull(newField.GetChildFieldByName("f5e107bb-875f-4e18-bdd0-63245d16cc45"));
        }

        [TestMethod()]
        public void GetFieldById_IfFieldDoesNotExist_NullIsReturned()
        {
            var newField = new BizField("Test 1", "7db344e5-6933-423c-b86b-c00abc5e9b4c");
            var childField = new BizField("Child 1", "f5e107bb-875f-4e18-bdd0-63245d16cc45");
            newField.AddChildField(childField);
            Assert.IsTrue(newField.GetChildFieldByName("fa0a398a-ffc3-45d0-bcda-15171fb394b5") is NullBizField);
        }

        [TestMethod()]
        public void GetFieldById_IfRequestedFieldIsChildOfChild_FieldIsReturned()
        {
            var newField = new BizField("Test 1", "7db344e5-6933-423c-b86b-c00abc5e9b4c");
            var childField = new BizField("Child 1", "f5e107bb-875f-4e18-bdd0-63245d16cc45");
            var childField2 = new BizField("Child 2", "a4a94629-1ab9-4f87-bf8f-774fbcab4450");
            var childField3 = new BizField("Child 3", "8e7bf685-ca02-4bea-9986-eab5184a83ef");
            newField.AddChildField(childField);
            childField.AddChildField(childField2);
            childField2.AddChildField(childField3);
            Assert.IsNotNull(newField.GetChildFieldByName("8e7bf685-ca02-4bea-9986-eab5184a83ef"));
        }

        [TestMethod()]
        public void GetChildFieldById_IfRequestingByGuid_TheRequestIsSuccessful()
        {
            var newField = new BizField("Test 1", "7db344e5-6933-423c-b86b-c00abc5e9b4c");
            var childField = new BizField("Child 1", "f5e107bb-875f-4e18-bdd0-63245d16cc45");
            var childField2 = new BizField("Child 2", "a4a94629-1ab9-4f87-bf8f-774fbcab4450");
            var childField3 = new BizField("Child 3", "8e7bf685-ca02-4bea-9986-eab5184a83ef");
            newField.AddChildField(childField);
            childField.AddChildField(childField2);
            childField2.AddChildField(childField3);
            Assert.IsNotNull(newField.GetChildFieldById(childField2.Id));
        }

        [TestMethod()]
        public void RemoveChildField_IfAChildFieldExists_ItIsRemovedSuccessfully()
        {
            var newField = new BizField("Test 1", "7db344e5-6933-423c-b86b-c00abc5e9b4c");
            var childField = new BizField("Child 1", "f5e107bb-875f-4e18-bdd0-63245d16cc45");
            var childField2 = new BizField("Child 2", "a4a94629-1ab9-4f87-bf8f-774fbcab4450");
            var childField3 = new BizField("Child 3", "8e7bf685-ca02-4bea-9986-eab5184a83ef");
            childField.AddChildField(childField2);
            childField2.AddChildField(childField3);

            newField.AddChildField(childField);

            newField.RemoveChildField(newField.GetChildFieldById(childField.Id));
            Assert.IsTrue(newField.GetChildFieldById(childField2.Id) is NullBizField);
        }

        [TestMethod()]
        public void RemoveChildField_IfChildFieldIsNull_ThrowsArgumentException()
        {
            var newField = new BizField("Test");
            newField.AddChildField(new BizField("child"));
            Assert.ThrowsException<ArgumentException>(() => newField.RemoveChildField(null));
        }

        [TestMethod()]
        public void GetComponentById_IfComponentIsNotFound_ThrowsArgumentException()
        {
            var newField = new BizField("Test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            Assert.ThrowsException<ArgumentException>(() => newRule.GetComponentById(Guid.NewGuid()));
        }

        [TestMethod()]
        public void GetComponentById_IfComponentExists_ItIsReturned()
        {
            var newField = new BizField("Test field");
            var newRule = new BizRule("test", newField);
            var newComponent = newRule.Then().FirstOrDefault();
            newRule.Add(newComponent);
            Assert.IsTrue(newRule.GetComponentById(newRule.RuleSequence[0].Id) != null);
        }

        [TestMethod()]
        public void RemoveChildField_IfChildOfChildIsRemoved_ItIsRemovedSuccessfully()
        {
            var newField = new BizField("Test 1", "7db344e5-6933-423c-b86b-c00abc5e9b4c");
            var childField = new BizField("Child 1", "f5e107bb-875f-4e18-bdd0-63245d16cc45");
            var childField2 = new BizField("Child 2", "a4a94629-1ab9-4f87-bf8f-774fbcab4450");
            var childField3 = new BizField("Child 3", "8e7bf685-ca02-4bea-9986-eab5184a83ef");
            childField.AddChildField(childField2);
            childField2.AddChildField(childField3);

            newField.AddChildField(childField);

            newField.RemoveChildField(newField.GetChildFieldById(childField3.Id));
            Assert.IsTrue(newField.GetChildFieldById(childField3.Id) is NullBizField);
        }
    }
}