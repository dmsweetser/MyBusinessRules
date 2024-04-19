using Newtonsoft.Json;

namespace BusinessRules.TestMVC.Models
{
    public class Component
    {
        // Properties
        public string Name { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }
        public bool IsHidden { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsReadOnly { get; set; }
        public string AllowedValues { get; set; }

        // Constructor
        public Component(string name, string value, bool isHidden, string errorMessage, bool isReadOnly, List<string> allowedValues)
        {
            Name = name;
            Value = value;
            IsHidden = isHidden;
            ErrorMessage = errorMessage;
            IsReadOnly = isReadOnly;
            AllowedValues = JsonConvert.SerializeObject(allowedValues);
        }

        public Component()
        {
        }
    }
}
