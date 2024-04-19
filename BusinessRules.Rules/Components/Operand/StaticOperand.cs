using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class StaticOperand : BaseComponent, IAmAnOperand
    {
        public StaticOperand() : base()
        {
            Arguments = new List<Argument>
            {
                new Argument("fixedValue", "", true)
            };
            DefinitionId = Guid.Parse("a443a4bf-3d26-44c3-8a82-0d3b88c5df72");
        }

        public List<string> GetValues(BizField parentField, List<int> scopedIndices)
        {
            return new List<string> { Arguments[0].Value };
        }

        public bool SetValue(BizField parentField, string newValue, int index = 0)
        {
            Arguments[0].Value = newValue;
            return true;
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            if (!string.IsNullOrWhiteSpace(Arguments[0].Value) && (!Arguments[0].Editable || isActivated))
            {
                return $"the constant value \"{Arguments[0].Value}\"";
            }

            return $"the constant value";
        }
    }
}
