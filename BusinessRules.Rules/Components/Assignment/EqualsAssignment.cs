using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Extensions;

namespace BusinessRules.Rules.Components
{
    public class EqualsAssignment : BaseComponent, IAmAnAssignment
    {
        public EqualsAssignment() : base()
        {
            DefinitionId = Guid.Parse("70caa70c-022b-41dc-b5a9-571586d77e7f");
        }
        public override bool Execute(BizField parentField, BizRule currentRule)
        {
            this.GetNextAndPriorComponents(currentRule, out IAmAnOperand nextComponent, out IAmAnOperand priorComponent);

            var currentValues = nextComponent.GetValues(parentField, currentRule.ScopedIndices);

            var wasSuccessful = false;

            //This is the case for parallel children
            if (currentValues.Count > 1)
            {
                foreach (var index in currentRule.ScopedIndices)
                {
                    priorComponent.SetValue(parentField, currentValues[index], index);
                    wasSuccessful = true;
                }
                return wasSuccessful;
            }
            //This is the case for a static operand
            else
            {
                foreach (var index in currentRule.ScopedIndices)
                {
                    priorComponent.SetValue(parentField, currentValues[0], index);
                    wasSuccessful = true;
                }
                return wasSuccessful;
            }
        }
        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"will be assigned";
        }
    }
}
