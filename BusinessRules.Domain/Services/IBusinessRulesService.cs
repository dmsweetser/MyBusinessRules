using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using Stripe;

namespace BusinessRules.Domain.Services
{
    public interface IBusinessRulesService
    {
        public InvoiceService StripeInvoiceService { get; set; }

        Task<BizField> GetTopLevelField(Guid companyId, Guid topLevelFieldId);
        Task<List<BizField>> GetTopLevelFieldsForCompany(Guid companyId);
        Task SaveTopLevelField(Guid companyId, BizField currentField);
        Task DeleteTopLevelField(Guid companyId, Guid fieldId);


        Task DeleteChildField(Guid companyId, Guid topLevelFieldId, Guid parentFieldId, Guid childFieldId);
        Task<BizField> AddNewChildField(Guid companyId, Guid topLevelFieldId, Guid parentFieldId);


        Task<BizCompany> GetCompanyForUser(string userId);
        Task<BizCompany> GetCompany(Guid companyId);
        Task SaveCompany(BizCompany newCompany);
        Task<BizCompany> GetCompanyForApiKey(Guid apiKey);
        Task<Dictionary<BizField, List<BizRule>>> GetFieldsAndRulesForCompany(Guid companyId);
        Task DeleteCompany(BizCompany currentCompany);
        Task ApplyCreditCode(BizCompany currentCompany, Guid codeId);


        Task IncrementBillingStats(Guid companyId, string stripeApiKey);


        Task<BizUser> GetUser(Guid companyId, string userId);
        Task<List<BizUser>> GetUsersForCompany(Guid companyId);
        Task DeleteUser(BizCompany currentCompany, string userId);


        Task<List<BizRule>> GetRules(Guid companyId, Guid fieldId);
        Task<BizRule> GetRuleById(Guid companyId, Guid fieldId, Guid ruleId);
        Task SaveRule(Guid companyId, BizRule currentRule);
        Task DeleteRule(Guid companyId, Guid fieldId, BizRule currentRule);


        Task<BizApiKey> GetApiKey(Guid apiKeyId);
        Task<List<BizApiKey>> GetApiKeysForCompany(Guid companyId);
        Task SaveApiKey(BizApiKey newApiKey);
        Task DeleteApiKey(Guid companyId, Guid apiKeyId);


        Task<string> GenerateNewStripeCustomerForCompany(string stripeApiKey, Guid newCompanyId, string emailAddress);
        Task<string> GenerateSessionForStripeCustomer(string stripeApiKey, string customerId, string returnUrl);
        Task<string> GeneratePurchaseUrlForStripeCustomer(string stripeApiKey, string customerId, string returnUrl, string priceId);
        Task<bool> UpdateBillingInfoForCompany(BizCompany currentCompany, List<Invoice> invoices);
        List<Invoice> GetInvoicesForCompany(BizCompany currentCompany, string stripeApiKey);
    }
}
