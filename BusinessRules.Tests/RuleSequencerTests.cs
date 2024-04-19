using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Tests
{
    [TestClass()]
    public class RuleSequencerTests
    {
        [TestMethod()]
        public void Then_IfYouProvideAnEmptyRule_ItReturnsAnAntecedent()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            var result = newRule.Then();
            Assert.IsTrue(result.First().IsAssignableTo(typeof(IAmAnAntecedent)));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAnAntecedent_ItReturnsAnOperand()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            var result = newRule.Then();
            Assert.IsTrue(result.First().IsAssignableTo(typeof(IAmAnOperand)));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAnOperand_ItReturnsAComparator()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            var result = newRule.Then();
            Assert.IsTrue(result.First().IsAssignableTo(typeof(IAmAComparator)));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAComparator_ItReturnsAnOperand()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            var result = newRule.Then();
            Assert.IsTrue(result.First().IsAssignableTo(typeof(IAmAnOperand)));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAnOperandAndPriorComponentIsAComparator_ItReturnsEitherAConjunctionOrAConsequent()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            var result = newRule.Then();
            Assert.IsTrue(result.All(x => x.IsAssignableTo(typeof(IAmAConjunction))
                || x.IsAssignableTo(typeof(IAmAConsequent))));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAConjunction_ItReturnsAnOperand()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault(x => x.IsAssignableTo(typeof(IAmAConjunction))));
            var result = newRule.Then();
            Assert.IsTrue(result.All(x => x.IsAssignableTo(typeof(IAmAConjunction))
                | x.IsAssignableTo(typeof(IAmAnOperand))));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAConsequent_ItReturnsAnOperand()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault(x => x.IsAssignableTo(typeof(IAmAConsequent))));
            var result = newRule.Then();
            Assert.IsTrue(result.All(x => x.IsAssignableTo(typeof(IAmAnOperand))));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAnOperandAndPriorComponentIsAConsequent_ItReturnsAnAssignment()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault(x => x.IsAssignableTo(typeof(IAmAConsequent))));
            newRule.Add(newRule.Then().FirstOrDefault());
            var result = newRule.Then();
            Assert.IsTrue(result.All(x => x.IsAssignableTo(typeof(IAmAnAssignment))));
        }
        [TestMethod()]
        public void Then_IfLatestComponentIsAnAssignment_ItReturnsAnOperand()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault(x => x.IsAssignableTo(typeof(IAmAConsequent))));
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(newRule.Then().FirstOrDefault());
            var result = newRule.Then();
            Assert.IsTrue(result.All(x => x.IsAssignableTo(typeof(IAmAnOperand))));
        }
        [TestMethod()]
        public void Then_IfARuleHasAThousandSteps_ItReturnsAValidComponent()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            for (int i = 0; i < 1000; i++)
            {
                var availableComponents = newRule.Then();
                var selectorIndex = new Random().Next(0, availableComponents.Count - 1);
                var componentToAdd =
                    availableComponents
                        .FirstOrDefault(x => x.IsAssignableFrom(typeof(StaticOperand))) ??
                    availableComponents
                        .FirstOrDefault(x => !x.IsAssignableFrom(typeof(StaticOperand)));
                newRule.Add(componentToAdd
                    , componentToAdd.IsAssignableFrom(typeof(StaticOperand)) ? new string[] { "5" } : null);
            }
            var result = newRule.Then();
            Assert.IsTrue(result.Count > 0);
        }
        [TestMethod]
        public void Then_IfLatestComponentIsOperandAndPriorComponentIsAssignment_ConjunctionIsReturned()
        {
            var newField = new BizField("test field");
            var newRule = new BizRule("test", newField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyComparator());
            newRule.Add(new EmptyOperand());
            newRule.Add(new ThenConsequent());
            newRule.Add(new EmptyOperand());
            newRule.Add(new EmptyAssignment());
            newRule.Add(new EmptyOperand());
            var nextComponents = newRule.Then();
            Assert.IsTrue(nextComponents.All(x => typeof(IAmAConjunction).IsAssignableFrom(x)));
        }
    }
}