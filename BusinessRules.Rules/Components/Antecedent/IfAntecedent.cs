using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class IfAntecedent : BaseComponent, IAmAnAntecedent
    {
        public IfAntecedent() : base()
        {
            DefinitionId = Guid.Parse("1bca7fb6-bf23-4dac-b787-ea2788689f32");
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return "If";
        }
    }
}
