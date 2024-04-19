using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules
{
    public static class RuleSequencer
    {
        public static List<Type> Then(this BizRule currentRule)
        {
            //Example: ___ > if
            if (currentRule.RuleSequence.Count == 0)
            {
                return RuleComponentFactory.GetComponents<IAmAnAntecedent>();
            }
            var latestComponent = currentRule.RuleSequence.Last();
            var latestComponentMinus1 = currentRule.RuleSequence.Count >= 2 ? currentRule.RuleSequence.SkipLast(1).Last() : null;
            var latestComponentMinus2 = currentRule.RuleSequence.Count >= 3 ? currentRule.RuleSequence.SkipLast(2).Last() : null;
            //Example: if > if field1
            if (latestComponent is IAmAnAntecedent)
            {
                return RuleComponentFactory.GetComponents<IAmAnOperand>();
            }
            //Example: if field1 is greater than > if field1 is greater than field2
            if (latestComponent is IAmAComparator)
            {
                return RuleComponentFactory.GetComponents<IAmAnOperand>();
            }
            //Example: if ... and > if ... and field3
            if (latestComponent is IAmAConjunction)
            {
                return RuleComponentFactory.GetComponents<IAmAnOperand>();
            }
            //Example: if ... then > if ... then field1
            if (latestComponent is IAmAConsequent)
            {
                return RuleComponentFactory.GetComponents<IAmAnOperand>();
            }
            //Example: if ... then field1 equals > if ... then field1 equals 5
            if (latestComponent is IAmAnAssignment)
            {
                return RuleComponentFactory.GetComponents<IAmAnOperand>();
            }
            //Example: if ... then field1 > if ... then field1 equals
            if (latestComponent is IAmAnOperand
                && latestComponentMinus1 is not null
                && latestComponentMinus1 is not IAmAnAssignment
                && currentRule.RuleSequence.Any(x => x is IAmAConsequent))
            {
                return RuleComponentFactory.GetComponents<IAmAnAssignment>();
            }
            //Example: if ... then field1 equals field2 > if ... then field1 equals field2 and
            if (latestComponent is IAmAnOperand
                && latestComponentMinus1 is not null
                && latestComponentMinus1 is IAmAnAssignment
                && currentRule.RuleSequence.Any(x => x is IAmAConsequent)
                )
            {
                return RuleComponentFactory.GetComponents<IAmAConjunction>();
            }
            //Example: if field1 > if field1 is greater than
            if (latestComponent is IAmAnOperand
                && latestComponentMinus1 is not null
                && latestComponentMinus1 is not IAmAComparator
                && !currentRule.RuleSequence.Any(x => x is IAmAConsequent))
            {
                return RuleComponentFactory.GetComponents<IAmAComparator>();
            }
            //Example: if field1 is greater than field2 > if field1 is greater than field2 and
            //Example: if field1 is greater than field2 > if field1 is greater than field2 then
            if (latestComponent is IAmAnOperand
                && latestComponentMinus1 is not null
                && latestComponentMinus1 is IAmAComparator
                && !currentRule.RuleSequence.Any(x => x is IAmAConsequent))
            {
                var componentDictionaries = new List<Type>();
                componentDictionaries.AddRange(RuleComponentFactory.GetComponents<IAmAConjunction>());
                componentDictionaries.AddRange(RuleComponentFactory.GetComponents<IAmAConsequent>());
                return componentDictionaries;
            }
            return new();
        }
    }
}
