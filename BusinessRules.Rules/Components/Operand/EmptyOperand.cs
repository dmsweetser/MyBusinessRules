using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class EmptyOperand : BaseComponent, IAmAnOperand, IAmATestComponent
    {
        public EmptyOperand() : base()
        {
            DefinitionId = Guid.Parse("57d2e556-3a0a-496b-b1bb-62fbce057566");
        }

        public List<string> GetValues(BizField parentField, List<int> scopedIndices)
        {
            return new List<string> { "" };
        }

        public bool SetValue(BizField parentField, string newValue, int index = 0)
        {
            return true;
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"nothing";
        }
    }
}
