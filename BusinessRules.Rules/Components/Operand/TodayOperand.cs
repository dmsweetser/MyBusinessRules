using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Rules.Components
{
    public class TodayOperand : BaseComponent, IAmAnOperand
    {
        public TodayOperand() : base()
        {
            DefinitionId = Guid.Parse("9b10c6b9-1f5a-4292-8af5-0159693cf774");
        }

        public List<string> GetValues(BizField parentField, List<int> scopedIndices)
        {
            return new List<string> { DateTime.Now.ToString() };
        }

        public bool SetValue(BizField parentField, string newValue, int index = 0)
        {
            return true;
        }

        public override string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return $"today's date";
        }
    }
}
