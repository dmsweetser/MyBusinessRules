using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Components.Tests
{
    [TestClass()]
    public class SystemFieldOperandTests
    {
        [TestMethod()]
        public void SystemFieldOperand_IfAFieldIsProvided_TheComponentInstantiatesWithoutError()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field1.AddChildField(field2);
            field1.AddChildField(field3);

            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EqualsComparator());
            newRule.Add(new EmptyOperand());
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            Assert.IsTrue(newRule.Execute(field1, false)
                && field1.GetChildFieldById(field3.Id).Value == "5");
        }

        [TestMethod()]
        public void SystemFieldOperand_IfAFieldIsProvided_GetValueReturnsTheFieldValue()
        {
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");

            field3.SetValue("woot");

            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            
            var newOperand = new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
            Assert.IsTrue(newOperand.GetValues(field1, newRule.ScopedIndices).FirstOrDefault() == "woot");
        }
    }
}