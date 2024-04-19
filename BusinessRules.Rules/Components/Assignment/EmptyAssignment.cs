using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class EmptyAssignment : BaseComponent, IAmAnAssignment, IAmATestComponent
    {
        public EmptyAssignment() : base()
        {
            DefinitionId = Guid.Parse("c4dc5f9d-e668-46ef-a46a-cc2b9e087482");
        }
        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return "will be assigned nothing";
        }
    }
}
