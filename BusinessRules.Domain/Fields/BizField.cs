using BusinessRules.Domain.Common;
using BusinessRules.Domain.Rules.Component;
using System.Collections.Concurrent;

namespace BusinessRules.Domain.Fields
{
    public class BizField: IHaveAnId
    {
        public static ConcurrentDictionary<Guid, Dictionary<Guid, string>> FlattenedFieldsCache { get; set; } = new();

        public Guid Id { get; set; }
        public Guid ParentFieldId { get; set; }
        public Guid TopLevelFieldId { get; set; }
        public string AllowedValueRegex { get; set; }
        public string FriendlyValidationMessageForRegex { get; set; }
        public string AllowedValues { get; set; }
        public string SystemName { get; set; }
        public string Value { get; set; }
        public string FriendlyName { get; set; }
        public bool DisplayForBusinessUser { get; set; }
        public bool IsADateField { get; set; }
        public string ExpectedDateFormat { get; set; }
        public bool IsACollection { get; set; }
        public bool IsDeprecated { get; set; }
        public List<BizField> ChildFields { get; set; }
        public List<Guid> RuleIds { get; set; }
        public List<DynamicComponent> DynamicComponents { get; set; }

        public BizField()
        {
            ChildFields = new();
            RuleIds = new();
            DynamicComponents = new();
            AllowedValueRegex = "";
            FriendlyValidationMessageForRegex = "";
            AllowedValues = "";
            Value = "";
        }

        public BizField(string name, string providedId = "")
        {
            if (string.IsNullOrWhiteSpace(providedId)) providedId = Guid.NewGuid().ToString();
            Id = Guid.Parse(providedId);
            ParentFieldId = Guid.Empty;
            TopLevelFieldId = Id;
            SystemName = name;
            FriendlyName = name;
            ChildFields = new();
            RuleIds = new();
            DynamicComponents = new();
            AllowedValueRegex = "";
            FriendlyValidationMessageForRegex = "";
            AllowedValues = "";
            DisplayForBusinessUser = true;
            Value = "";
            IsADateField = false;
            ExpectedDateFormat = "";
            IsACollection = false;
        }

        public override string ToString()
        {
            return SystemName;
        }
    }
}