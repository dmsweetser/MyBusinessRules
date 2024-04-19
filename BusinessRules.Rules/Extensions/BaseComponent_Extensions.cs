using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Components;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;

namespace BusinessRules.Rules.Extensions
{
    public static class BaseComponent_Extensions
    {
        public static void GetNextAndPriorComponents(this BaseComponent currentComponent,
            BizRule parentRule,
            out IAmAnOperand nextComponentFound,
            out IAmAnOperand priorComponentFound)
        {
            var currentComponentIndex = parentRule.RuleSequence.FindIndex(x => x.Id == currentComponent.Id);
            if (currentComponentIndex < 0)
            {
                throw new ArgumentException("Current component not found in rule sequence");
            }
            if (parentRule.RuleSequence[currentComponentIndex + 1] is not IAmAnOperand nextComponent)
            {
                throw new ArgumentException("Operand was expected for next component but not found");
            }
            if (parentRule.RuleSequence[currentComponentIndex - 1] is not IAmAnOperand priorComponent)
            {
                throw new ArgumentException("Operand was expected for prior component but not found");
            }
            nextComponentFound = nextComponent;
            priorComponentFound = priorComponent;
        }

        public static T WithArgumentValues<T>(this T currentComponent, string[] argumentValues)
            where T : BaseComponent
        {
            if (argumentValues.Length != currentComponent.Arguments.Count)
            {
                throw new ArgumentException("Argument counts do not match");
            }
            for (int i = 0; i < currentComponent.Arguments.Count; i++)
            {
                currentComponent.Arguments[i].Value = argumentValues[i];
            }
            return currentComponent;
        }

        public static BigInteger GetStringAsNumber(this BaseComponent currentComponent, string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            BigInteger result = new BigInteger(bytes);
            return result;
        }

        public static async Task<string> ExecuteJavascriptComparator(
           this BaseComponent currentComponent,
           string script,
           string x,
           string y,
           int scopedIndex,
           string functionUrl)
        {
            // Create the request data as a JSON string
            var requestData =
                JsonConvert.SerializeObject(
                    new { functionCode = script.ReplaceLineEndings(""), x = JsonConvert.SerializeObject(x), y = JsonConvert.SerializeObject(y), scopedIndex });
            var requestContent = new StringContent(requestData, Encoding.UTF8, "application/json");

            // Send the request to the function URL with the function key as a query parameter
            var response = await _httpClient.PostAsync(functionUrl, requestContent);

            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            else
            {
                return false.ToString();
            }
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ExecuteJavascriptOperand(
            this BaseComponent currentComponent,
            string script,
            string currentField,
            int scopedIndex,
            string functionUrl)
        {
            // Create the request data as a JSON string
            var requestData = 
                JsonConvert.SerializeObject(
                    new { functionCode = script.ReplaceLineEndings(""), currentField = JsonConvert.SerializeObject(currentField), scopedIndex });
            var requestContent = new StringContent(requestData, Encoding.UTF8, "application/json");

            // Send the request to the function URL with the function key as a query parameter
            var response = await _httpClient.PostAsync(functionUrl, requestContent);

            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            else
            {
                // Handle the error
                throw new Exception($"Function call failed: {response.ReasonPhrase}");
            }
        }

        public static bool EvaluateSuccess(this BaseComponent currentComponent, BizRule currentRule, IAmAnOperand priorComponent, List<string> priorValues, ref bool wasSuccessful, List<int> successfulIndices)
        {
            if (priorComponent is FieldOperand
                            && priorComponent.Arguments[1].Value == BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()
                            && successfulIndices.Count > 0)
            {
                currentRule.ScopedIndices.AddRange(successfulIndices);
                wasSuccessful = true;
            }
            else if (priorComponent is FieldOperand
                && priorComponent.Arguments[1].Value == BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString()
                && successfulIndices.Count == priorValues.Count)
            {
                currentRule.ScopedIndices.AddRange(successfulIndices);
                wasSuccessful = true;
            }
            else if (priorComponent is FieldOperand
                && priorComponent.Arguments[1].Value == BizFieldCollectionTypeEnum.NotACollection.ToString()
                && successfulIndices.Count > 0)
            {
                currentRule.ScopedIndices.AddRange(successfulIndices);
                wasSuccessful = true;
            } else if (priorComponent is not FieldOperand && successfulIndices.Count > 0)
            {
                currentRule.ScopedIndices.AddRange(successfulIndices);
                wasSuccessful = true;
            }

            return wasSuccessful;
        }
    }
}
