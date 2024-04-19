using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;

namespace BusinessRules.Rules
{
    public static class BusinessUserDTO_Extensions
    {
        public static BusinessUserDTO ToBusinessUserDTO(
            this BizField field,
            List<BizRule> rules,
            string baseEndpointUrl,
            bool isTestMode)
        {
            if (field.ParentFieldId != Guid.Empty)
            {
                throw new ArgumentException("Provided field is not top-level field");
            }

            return new BusinessUserDTO(
                field, 
                rules.Select(x => x.ToRuleDTO(field, isTestMode)).OrderBy(x => x.Name).ToList(), 
                baseEndpointUrl);
        }
    }
}
