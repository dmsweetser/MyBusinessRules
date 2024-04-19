using BusinessRules.Domain.Fields;
namespace BusinessRules.Domain.Rules.Component
{
    public class BaseComponent : IAmAComponent
    {
        public Guid Id { get; set; }
        public Guid DefinitionId { get; set; }
        public List<Argument> Arguments { get; set; }
        
        public BaseComponent()
        {
            Id = Guid.NewGuid();
            Arguments = new();
        }

        public virtual bool Execute(BizField parentField, BizRule currentRule)
        {
            return true;
        }

        public virtual string GetFormattedDescription(BizField parentField, bool isActivated)
        {
            return "";
        }
    }
}
