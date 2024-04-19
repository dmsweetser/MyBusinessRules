using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class EmptyComparator : BaseComponent, IAmAComparator, IAmATestComponent
    {
        public EmptyComparator() : base()
        {
            DefinitionId = Guid.Parse("46b9f9c8-cad8-44ab-aa54-a385e92452c8");
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"is not compared to";
        }
    }
}
