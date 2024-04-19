using BusinessRules.Domain.Fields;
using System.Diagnostics.CodeAnalysis;

namespace BusinessRules.Domain.DTO
{
    public class BusinessUserDTO
    {
        public BizField CurrentField { get; set; }
        public List<BizRuleDTO> Rules { get; set; }
        public string BaseEndpointUrl { get; set; }

        /// <summary>
        /// DO NOT USE - required by model binder
        /// </summary>
        [ExcludeFromCodeCoverage]
        public BusinessUserDTO()
        {
                
        }

        public BusinessUserDTO(BizField currentField, List<BizRuleDTO> rules, string baseEndpointUrl)
        {
            CurrentField = currentField;
            Rules = rules;
            BaseEndpointUrl = baseEndpointUrl;
        }
    }
}
