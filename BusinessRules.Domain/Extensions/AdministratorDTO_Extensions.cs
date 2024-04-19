using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;

namespace BusinessRules.Domain.Fields
{
    public static class AdministratorDTO_Extensions
    {
        public static async Task<AdministratorDTO> ToAdministratorDTO(
            this BizCompany company,
            IBusinessRulesService service,
            string baseEndpointUrl,
            string billingUrl,
            string purchaseUrl)
        {
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);
            var foundApiKeys = await service.GetApiKeysForCompany(company.Id);
            return new AdministratorDTO()
            {
                Name = company.Name,
                Users = company.Users,
                ApiKeys = foundApiKeys,  
                RemainingCredits = company.CreditsAvailable - company.CreditsUsed,
                BaseEndpointUrl = baseEndpointUrl,
                BillingUrl = billingUrl,
                PurchaseUrl = purchaseUrl,
                LastBilledDate = company.LastBilledDate,
                FieldIds = company.FieldIds,
                ApiKeyIds = company.ApiKeyIds,
                Fields = foundFields
            };
        }

        public static void SyncValues(this BizCompany company, AdministratorDTO currentCompanyDto)
        {
            company.Name = currentCompanyDto.Name;
            company.FieldIds = currentCompanyDto.FieldIds;
            company.ApiKeyIds = currentCompanyDto.ApiKeyIds;
            foreach (var user in company.Users)
            {
                user.Role = currentCompanyDto.Users.First(x => x.Id == user.Id).Role;
                user.EmailAddress = currentCompanyDto.Users.First(x => x.Id == user.Id).EmailAddress;
            }
        }
    }
}
