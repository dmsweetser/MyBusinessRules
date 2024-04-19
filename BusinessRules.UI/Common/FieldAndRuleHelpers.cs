using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Helpers;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
using Newtonsoft.Json.Linq;

namespace BusinessRules.UI.Common
{
    public static class FieldAndRuleHelpers
    {

        public static async Task<(BizField TopLevelField, List<BizRule> Rules)> GenerateTopLevelFieldAndRules(
            IBusinessRulesService service,
            Guid companyId)
        {
            var newField =
@"
{ 
    ""Customer"" : {
        ""First Name"": """",
        ""Last Name"":  """",
        ""Address"": [
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": """",
                ""AddressType"": """",
            },
            {
                ""Street"": """",
                ""City"": """",
                ""State"": {
                    ""Value"": """",
                    ""Error Message"": """"
                },
                ""Zip"": """",
                ""AddressType"": """",
            }
        ]
    }
}
";

            var parsedObject = JObject.Parse(newField);

            var convertedBizField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            convertedBizField.GetChildFieldByName("Zip").AllowedValueRegex = "^[0-9]{5}$";
            convertedBizField.GetChildFieldByName("Zip").FriendlyValidationMessageForRegex = "Value must be a 5 digit number";
            convertedBizField.GetChildFieldByName("AddressType").AllowedValues = "Business:Business|Home:Home";

            var causeEffectRule = new BizRule("Cause and Effect Rule", convertedBizField);
            causeEffectRule.Add(new IfAntecedent());
            causeEffectRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedBizField.GetChildFieldByName("First Name").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            causeEffectRule.Add(new EqualsComparator());
            causeEffectRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Cause" }));
            causeEffectRule.Add(new ThenConsequent());
            causeEffectRule.Add(new FieldOperand().WithArgumentValues(new string[] { 
                convertedBizField.GetChildFieldByName("Last Name").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            causeEffectRule.Add(new EqualsAssignment());
            causeEffectRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Effect" }));
            causeEffectRule.IsActivated = false;
            causeEffectRule.IsActivatedTestOnly = true;

            var stateRule = new BizRule("State Is Nonsense Rule", convertedBizField);
            stateRule.Add(new IfAntecedent());
            stateRule.Add(new FieldOperand().WithArgumentValues(new string[] { 
                convertedBizField.GetChildFieldByName("Value").Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsComparator());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Nonsense" }));
            stateRule.Add(new ThenConsequent());
            stateRule.Add(new FieldOperand().WithArgumentValues(
                new string[] {
                    convertedBizField.GetChildFieldByName("Error Message").Id.ToString()
                , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            stateRule.Add(new EqualsAssignment());
            stateRule.Add(new StaticOperand().WithArgumentValues(new string[] { "Sorry! We don't do business in made-up places" }));
            stateRule.IsActivated = false;
            stateRule.IsActivatedTestOnly = true;

            await service.SaveTopLevelField(companyId, convertedBizField);

            await service.SaveRule(companyId, causeEffectRule);
            await service.SaveRule(companyId, stateRule);

            return new(convertedBizField, new List<BizRule> { causeEffectRule, stateRule });
        }
    }
}
