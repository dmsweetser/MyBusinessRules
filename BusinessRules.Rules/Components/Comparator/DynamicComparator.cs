using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Extensions;

namespace BusinessRules.Rules.Components
{
    public class DynamicComparator : DynamicComponent, IAmAComparator
    {
        public static string FunctionUrl { get; set; }
        public DynamicComparator(string description, string body) : base()
        {
            Description = description;
            Body = body;
            DefinitionId = Guid.Parse("6db5f32d-4884-460f-aa3f-b3d4c1f5e927");
        }

        /// <summary>
        /// DO NOT USE - Used by the Rule Component factory
        /// </summary>
        public DynamicComparator() : base()
        {
            DefinitionId = Guid.Parse("6db5f32d-4884-460f-aa3f-b3d4c1f5e927");
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
                    if (bool.TryParse(
                            this.ExecuteJavascriptComparator(
                                Body, priorValues[i], nextValues[0], i, FunctionUrl).Result, out var evaluationResult)
                        && evaluationResult)
                    {
                        successfulIndices.Add(i);
                    };
                }
                //This is the case for parallel children
                else if (nextValues.Count == priorValues.Count)
                {
                    if (bool.TryParse(
                            this.ExecuteJavascriptComparator(
                                Body, priorValues[i], nextValues[i], i, FunctionUrl).Result, out var evaluationResult)
                        && evaluationResult)
                    {
                        successfulIndices.Add(i);
                    };
                }
            }

            return this.EvaluateSuccess(currentRule, priorComponent, priorValues, ref wasSuccessful, successfulIndices);

        }
    }
}