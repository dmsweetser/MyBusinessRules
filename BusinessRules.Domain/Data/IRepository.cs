using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
namespace BusinessRules.Domain.Data
{
    public interface IRepository
    {
        Task DeleteApiKey(string apiKeyId);
        Task DeleteCompany(string companyId);
        Task DeleteTopLevelField(string companyId, string currentFieldId); 
        Task DeleteRule(string fieldId, string currentRuleId);
        Task DeleteUserToCompanyReference(string userId);

        Task<BizApiKey> GetApiKey(string apiKeyId);
        Task<BizCompany> GetCompany(string companyId);
        Task<BizField> GetTopLevelField(string companyId, string fieldId);
        Task<BizRule> GetRule(string fieldId, string ruleId);
        Task<BizUser> GetUser(string companyId, string userId);
        Task<BizUserToCompany> GetUserToCompanyReference(string userId);
        Task<BizCreditCode> GetCreditCode(string codeId);

        Task SaveApiKey(BizApiKey currentApiKey);
        Task SaveCompany(BizCompany currentCompany);
        Task SaveTopLevelField(string companyId, BizField currentField);
        Task SaveRule(string fieldId, BizRule currentRule);
        Task SaveUserToCompanyReference(BizUserToCompany currentUserToCompany);
        Task SaveCreditCode(BizCreditCode creditCode);
    }
}
