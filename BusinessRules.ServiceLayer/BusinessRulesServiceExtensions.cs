using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;

namespace BusinessRules.ServiceLayer
{
    public static class BusinessRulesServiceExtensions
    {

        public static async Task<BizCompany> ValidateCompany(this BusinessRulesService service, Guid companyId)
        {
            var foundCompany = await service.Repository.GetCompany(companyId.ToString());
            if (foundCompany is NullBizCompany)
            {
                throw new ArgumentException($"Company was not found with ID {companyId}");
            }

            return foundCompany;
        }

        public static async Task<BizField> ValidateField(this BusinessRulesService service, Guid companyId, Guid fieldId)
        {
            var foundField = await service.Repository.GetTopLevelField(companyId.ToString(), fieldId.ToString());
            if (foundField is NullBizField)
            {
                throw new ArgumentException($"Top level field was not found with ID {fieldId}");
            }
            return foundField;
        }
    }
}
