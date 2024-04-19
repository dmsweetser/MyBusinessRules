namespace BusinessRules.Domain.UserInterface
{
    public class AutocompleteEntry
    {
        public string label { get; set; }
        public string value { get; set; }
        public AutocompleteEntry(string label, string value)
        {
            this.label = label;
            this.value = value;
        }
    }
}
