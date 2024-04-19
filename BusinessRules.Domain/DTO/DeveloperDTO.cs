using BusinessRules.Domain.Fields;
using System.Diagnostics.CodeAnalysis;

namespace BusinessRules.Domain.DTO
{
    public class DeveloperDTO
    {
        public BizField CurrentField { get; set; }
        public List<Guid> RuleIds { get; set; }
        public string BaseEndpointUrl { get; set; }

        /// <summary>
        /// DO NOT USE - required by model binder
        /// </summary>
        [ExcludeFromCodeCoverage]
        public DeveloperDTO()
        {
                
        }

        public DeveloperDTO(BizField currentField, List<Guid> ruleIds, string baseEndpointUrl)
        {
            CurrentField = currentField;
            RuleIds = ruleIds;
            BaseEndpointUrl = baseEndpointUrl;
        }
    }
}
