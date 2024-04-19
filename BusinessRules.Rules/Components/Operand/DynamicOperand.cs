using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Helpers;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Extensions;

namespace BusinessRules.Rules.Components
{
    public class DynamicOperand : DynamicComponent, IAmAnOperand
    {
        public static string FunctionUrl { get; set; }
        public DynamicOperand(string description, string body): base()
        {
            DefinitionId = Guid.Parse("0162be2e-edcc-4a6f-8279-680114c43e60");
            Description = description;
            Body = body;
        }

        /// <summary>
        /// DO NOT USE - Used by the Rule Component factory
        /// </summary>
        public DynamicOperand() : base()
        {
            DefinitionId = Guid.Parse("0162be2e-edcc-4a6f-8279-680114c43e60");
        }

        public List<string> GetValues(BizField parentField, List<int> scopedIndices)
        {
            var results = new List<string>();
            var convertedField = BizField_Helpers.ConvertBizFieldToJToken(parentField);
            for (int i = 0; i < scopedIndices.Count; i++)
            {
                var executionResult = this.ExecuteJavascriptOperand(
                    Body, convertedField.ToString(), scopedIndices[i], FunctionUrl).Result;
                results.Add(executionResult);
            }
            return results;
        }

        public bool SetValue(BizField parentField, string newValue, int index = 0)
        {
            //A dynamic operator does not assign a value
            return true;
        }
    }
}