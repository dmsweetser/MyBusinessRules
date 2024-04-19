namespace BusinessRules.Domain.Rules.Component
{
    public class Argument
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Editable { get; set; }
        public string AllowedValueRegex { get; set; }
        public string FriendlyValidationMessageForRegex { get; set; }
        public string AllowedValues { get; set; }
        public bool IsADateField { get; set; }
        public string ExpectedDateFormat { get; set; }
        public bool IsACollection { get; set; }

        public Argument()
        {
        }

        public Argument(string name, string value, bool editable)
        {
            Name = name;
            Value = value;
            Editable = editable;
        }
    }
}
