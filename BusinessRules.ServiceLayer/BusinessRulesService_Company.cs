using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;

namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public async Task<BizCompany> GetCompanyForUser(string userId)
        {
            var foundUserToCompanyReference = await Repository.GetUserToCompanyReference(userId);
            if (foundUserToCompanyReference is NullBizUserToCompany || foundUserToCompanyReference.CompanyId == Guid.Empty)
            {
                return new NullBizCompany();
            }

            return await Repository.GetCompany(foundUserToCompanyReference.CompanyId.ToString());
        }

        public async Task<BizCompany> GetCompany(Guid companyId)
        {
            return await Repository.GetCompany(companyId.ToString());
        }

        public async Task SaveCompany(BizCompany newCompany)
        {
            await Repository.SaveCompany(newCompany);
            foreach (var user in newCompany.Users)
            {
                var foundUserToCompany = await Repository.GetUserToCompanyReference(user.EmailAddress);
                if (foundUserToCompany is not NullBizUserToCompany
                    && foundUserToCompany.CompanyId.ToString() != newCompany.Id.ToString()
                    && foundUserToCompany.CompanyId != Guid.Empty)
                {
                    throw new ArgumentException("User already assigned to another company");
                }
                else
                {
                    await Repository.SaveUserToCompanyReference(new BizUserToCompany(user.EmailAddress, newCompany.Id));
                }
            }
        }

        public async Task<BizCompany> GetCompanyForApiKey(Guid apiKey)
        {
            var foundKey = await Repository.GetApiKey(apiKey.ToString());
            if (foundKey is NullBizApiKey)
            {
                throw new ArgumentException($"Api key does not exist with ID {apiKey}");
            }
            return await Repository.GetCompany(foundKey.CompanyId.ToString());
        }

        public async Task<Dictionary<BizField, List<BizRule>>> GetFieldsAndRulesForCompany(Guid companyId)
        {
            var fieldsAndRules = new Dictionary<BizField, List<BizRule>>();
            var foundFields = await GetTopLevelFieldsForCompany(companyId);

            if (foundFields.Count == 0)
            {
                var newField = new BizField("New Field");
                await SaveTopLevelField(companyId, newField);
                fieldsAndRules.Add(newField, new List<BizRule>());
            }

            foreach (var field in foundFields)
            {
                fieldsAndRules.Add(field, await GetRules(companyId, field.Id));
            }

            return fieldsAndRules;
        }

        public async Task DeleteCompany(BizCompany currentCompany)
        {
            foreach (var user in currentCompany.Users)
            {
                var foundUserToCompanyReference = await Repository.GetUserToCompanyReference(user.EmailAddress);

                if (foundUserToCompanyReference.CompanyId.ToString() == currentCompany.Id.ToString())
                {
                    await Repository.DeleteUserToCompanyReference(user.EmailAddress);
                }
            }

            await Repository.DeleteCompany(currentCompany.Id.ToString());
        }

        public async Task ApplyCreditCode(BizCompany currentCompany, Guid codeId)
        {
            var foundCreditCode = await Repository.GetCreditCode(codeId.ToString());

            if (foundCreditCode.RedeemedDate != DateTime.MinValue) return;

            currentCompany.CreditsAvailable += foundCreditCode.CreditValue;
            foundCreditCode.RedeemedByCompanyId = currentCompany.Id;
            foundCreditCode.RedeemedDate = DateTime.Now;

            await Repository.SaveCompany(currentCompany);
            await Repository.SaveCreditCode(foundCreditCode);
        }
    }
}
