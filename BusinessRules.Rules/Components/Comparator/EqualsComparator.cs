using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Rules.Components
{
    public class EqualsComparator : BaseComponent, IAmAComparator
    {
        public EqualsComparator() : base()
        {
            DefinitionId = Guid.Parse("23066084-e15a-4309-92b9-e9f74141f884");
        }

        public override bool Execute(BizField parentField, BizRule currentRule)
        {
            this.GetNextAndPriorComponents(currentRule, out IAmAnOperand nextComponent, out IAmAnOperand priorComponent);

            var priorValues = priorComponent.GetValues(parentField, currentRule.ScopedIndices);
            var nextValues = nextComponent.GetValues(parentField, currentRule.ScopedIndices);

            var wasSuccessful = false;

            var successfulIndices = new List<int>();

            for (int i = 0; i < priorValues.Count; i++)
            {
                //Positive cases
                //This is the case for a fixed value
                if (nextValues.Count == 1)
                {
                    var evaluationResult =
                        priorComponent.Arguments.Count > 0
                        && priorComponent.Arguments[0].IsADateField
                        && DateTime.TryParse(priorValues[i], out var firstArgument)
                        && DateTime.TryParse(nextValues[0], out var secondArgument)
                        && firstArgument == secondArgument
                        ? true :
                        (priorComponent.Arguments.Count == 0
                        || priorComponent.Arguments.Count > 0
                        && !priorComponent.Arguments[0].IsADateField)
                        && priorValues[i] == nextValues[0];

                    if (evaluationResult)
                    {
                        successfulIndices.Add(i);
                    }
                }
                //This is the case for parallel children
                else if (nextValues.Count == priorValues.Count)
                {
                    var evaluationResult =
                        priorComponent.Arguments.Count > 0
                        && priorComponent.Arguments[0].IsADateField
                        && DateTime.TryParse(priorValues[i], out var firstArgument)
                        && DateTime.TryParse(nextValues[i], out var secondArgument)
                        && firstArgument == secondArgument
                        ? true :
                        (priorComponent.Arguments.Count == 0
                        || priorComponent.Arguments.Count > 0
                        && !priorComponent.Arguments[0].IsADateField)
                        && priorValues[i] == nextValues[i];

                    if (evaluationResult)
                    {
                        successfulIndices.Add(i);
                    }
                }
            }

            return this.EvaluateSuccess(currentRule, priorComponent, priorValues, ref wasSuccessful, successfulIndices);
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"is equal to";
        }
    }
}
