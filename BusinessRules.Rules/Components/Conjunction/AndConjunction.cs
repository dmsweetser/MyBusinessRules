using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class AndConjunction : BaseComponent, IAmAConjunction
    {
        public AndConjunction() : base()
        {
            DefinitionId = Guid.Parse("97407344-66e4-4f81-8bad-eb22339c277a");
        }
        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"and";
        }
    }
}
