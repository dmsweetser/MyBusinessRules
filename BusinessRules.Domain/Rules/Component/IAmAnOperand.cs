using BusinessRules.Domain.Fields;
namespace BusinessRules.Domain.Rules.Component
{
    public interface IAmAnOperand: IAmAComponent
    {
        public List<string> GetValues(BizField parentField, List<int> scopedIndices);
        public bool SetValue(BizField parentField, string newValue, int index);
    }
}
