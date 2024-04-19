using BusinessRules.Domain.Rules.Component;
namespace BusinessRules.Domain.DTO
{
    public class BizComponentDTO
    {
        public Guid Id { get; set; }
        public Guid DefinitionId { get; set; }
        public string Description { get; set; }
        public List<Argument> Arguments { get; set; }

        /// <summary>
        /// DO NOT USE - required by model binder
        /// </summary>
        public BizComponentDTO()
        {

        }
    }
}
