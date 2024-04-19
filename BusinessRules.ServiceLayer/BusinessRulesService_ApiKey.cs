using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;

namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public async Task<BizApiKey> GetApiKey(Guid apiKeyId)
        {
            return await Repository.GetApiKey(apiKeyId.ToString());
        }

        public async Task<List<BizApiKey>> GetApiKeysForCompany(Guid companyId)
        {
            var foundCompany = await GetCompany(companyId);

            var foundApiKeys = new List<BizApiKey>();

            if (foundCompany.ApiKeyIds is null)
            {
                foundCompany.ApiKeyIds = new();
                await SaveCompany(foundCompany);
            }

            foreach (var apiKeyId in foundCompany.ApiKeyIds)
            {
                foundApiKeys.Add(await GetApiKey(apiKeyId));
            }

            return foundApiKeys;
        }

        public async Task SaveApiKey(BizApiKey newApiKey)
        {
            var foundCompany = await GetCompany(newApiKey.CompanyId);

            if (!foundCompany.ApiKeyIds.Any(x => x == newApiKey.Id))
            {
                foundCompany.ApiKeyIds.Add(newApiKey.Id);
            }

            await Repository.SaveCompany(foundCompany);

            await Repository.SaveApiKey(newApiKey);
        }

        public async Task DeleteApiKey(Guid companyId, Guid apiKeyId)
        {
            var foundCompany = await GetCompany(companyId);

            if (foundCompany.ApiKeyIds.Any(x => x == apiKeyId))
            {
                foundCompany.ApiKeyIds.Remove(apiKeyId);
            }

            await Repository.SaveCompany(foundCompany);

            await Repository.DeleteApiKey(apiKeyId.ToString());
        }

        public async Task IncrementBillingStats(Guid companyId, string stripeApiKey)
        {
            var foundCompany = await Repository.GetCompany(companyId.ToString());
            foundCompany.CreditsUsed += 1;

            if (foundCompany.BillingId != "")
            {
                await UpdateBillingInfoForCompany(foundCompany, GetInvoicesForCompany(foundCompany, stripeApiKey));
            }

            await Repository.SaveCompany(foundCompany);
        }
    }
}
