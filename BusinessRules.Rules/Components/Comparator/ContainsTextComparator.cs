using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Extensions;
namespace BusinessRules.Rules.Components
{
    public class ContainsTextComparator : BaseComponent, IAmAComparator
    {
        public ContainsTextComparator() : base()
        {
            DefinitionId = Guid.Parse("b5c21c53-4df0-42f9-a913-c4f1250c81af");
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
                    bool evaluationResult = priorValues[i].ToUpper().Contains(nextValues[0].ToUpper());
                    if (evaluationResult)
                    {
                        successfulIndices.Add(i);
                    }
                }
                //This is the case for parallel children
                else if (nextValues.Count == priorValues.Count)
                {
                    bool evaluationResult = priorValues[i].ToUpper().Contains(nextValues[i].ToUpper());
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
            return $"contains all the text from";
        }
    }
}
