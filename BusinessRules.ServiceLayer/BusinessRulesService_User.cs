using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;

namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public async Task<BizUser> GetUser(Guid companyId, string userId)
        {
            return await Repository.GetUser(companyId.ToString(), userId);
        }

        public async Task<List<BizUser>> GetUsersForCompany(Guid companyId)
        {
            var foundCompany = await Repository.GetCompany(companyId.ToString());
            return foundCompany.Users;
        }

        public async Task DeleteUser(BizCompany currentCompany, string userId)
        {
            var foundUserToCompanyReference = await Repository.GetUserToCompanyReference(userId);

            if (foundUserToCompanyReference.CompanyId.ToString() == currentCompany.Id.ToString())
            {
                await Repository.DeleteUserToCompanyReference(userId);
            }

            currentCompany.Users = currentCompany.Users.Where(x => x.EmailAddress != userId).ToList();

            await Repository.SaveCompany(currentCompany);
        }
    }
}
