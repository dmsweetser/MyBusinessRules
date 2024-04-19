using BusinessRules.Domain.Fields;
namespace BusinessRules.Domain.Rules
{
	public class NullBizRule : BizRule
    {
        public NullBizRule() : base("", NullBizField.GetInstance())
        {
            
        }
    }
}
