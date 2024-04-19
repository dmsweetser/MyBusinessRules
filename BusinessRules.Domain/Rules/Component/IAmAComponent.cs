using BusinessRules.Domain.Fields;
namespace BusinessRules.Domain.Rules.Component
{
    public interface IAmAComponent
    {
        public Guid Id { get; }
        public Guid DefinitionId { get; set; }
        public List<Argument> Arguments { get; set; }
        public bool Execute(BizField parentField, BizRule currentRule);
        public string GetFormattedDescription(BizField parentField, bool isActivated);
    }
}
