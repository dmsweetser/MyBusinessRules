using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class FieldOperand : BaseComponent, IAmAnOperand
    {
        public FieldOperand() : base()
        {
            Arguments = new List<Argument>
                {
                    new Argument("targetFieldId", "", false),
                    new Argument("collectionType", "", false)
                };
            DefinitionId = Guid.Parse("1c4a8ca8-2f5e-42a4-ac50-0aad70881e47");
        }

        public List<string> GetValues(BizField parentField, List<int> scopedIndices)
        {
            var results = new List<BizField>();
            parentField.GetChildFieldMultiplesById(Arguments[0].Value, results);
            return results.Select(x => x.Value).ToList();
        }

        public bool SetValue(BizField parentField, string newValue, int index = 0)
        {
            var results = new List<BizField>();
            parentField.GetChildFieldMultiplesById(Arguments[0].Value, results);
            if (Arguments[0].IsADateField
                && !string.IsNullOrWhiteSpace(Arguments[0].ExpectedDateFormat))
            {
                newValue = DateTime.Parse(newValue).ToString(Arguments[0].ExpectedDateFormat);
            }
            results[index].SetValue(newValue);
            return true;
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            var associatedField = parentField.GetChildFieldById(Guid.Parse(Arguments[0].Value));

            //We use this as an opportunity to populate attributes of the component from the provided field
            Arguments[0].AllowedValueRegex = associatedField.AllowedValueRegex;
            Arguments[0].FriendlyValidationMessageForRegex = associatedField.FriendlyValidationMessageForRegex;
            Arguments[0].AllowedValues = associatedField.AllowedValues;
            Arguments[0].IsADateField = associatedField.IsADateField;
            Arguments[0].ExpectedDateFormat = associatedField.ExpectedDateFormat;
            Arguments[0].IsACollection = associatedField.IsACollection;

            var flattenedFields = parentField.FlattenFieldsWithDescription();

            if (flattenedFields.ContainsKey(associatedField.Id))
            {
                var fullName = flattenedFields[associatedField.Id];

                if (Arguments[0].IsACollection && Arguments[1].Value == BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString())
                {
                    return "any " + fullName;
                }
                else if (Arguments[0].IsACollection && Arguments[1].Value == BizFieldCollectionTypeEnum.CorrespondingRecordInCollection.ToString())
                {
                    return "the associated " + fullName;
                }
                else if (Arguments[0].IsACollection && Arguments[1].Value == BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString())
                {
                    return "every " + fullName;
                } else
                {
                    return fullName;
                }
            }
            else
            {
                return $"[ERROR!!! FIELD NOT FOUND]";
            }
        }
    }
}
