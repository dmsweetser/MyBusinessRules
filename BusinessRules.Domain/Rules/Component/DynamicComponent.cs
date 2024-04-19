using BusinessRules.Domain.Fields;

namespace BusinessRules.Domain.Rules.Component
{
    public class DynamicComponent : BaseComponent, IAmAComponent
    {
        public string Description { get; set; }
        public string Body { get; set; }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return Description;
        }
    }
}
