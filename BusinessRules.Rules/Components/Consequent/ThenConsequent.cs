using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class ThenConsequent : BaseComponent, IAmAConsequent
    {
        public ThenConsequent() : base()
        {
            DefinitionId = Guid.Parse("78a98833-8ec6-4436-afce-0913d1d9a863");
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"then";
        }
    }
}
