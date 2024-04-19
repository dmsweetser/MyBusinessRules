using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;

namespace BusinessRules.UI.Models
{
    public class SystemViewModel
    {
        public AdministratorDTO CurrentCompany { get; set; }
        public BizUser CurrentUser { get; set; }
        public List<AppViewModel> Applications { get; set; }
        public SystemViewModel(
            IBusinessRulesService service,
            AppSettings settings,
            BizCompany currentCompany,
            BizUser currentUser,
            Dictionary<BizField, List<BizRule>> fieldsAndRules,
            List<BizApiKey> apiKeys,
            string billingUrl,
            string purchaseUrl)
        {
            if (fieldsAndRules.Count == 0)
            {
                throw new ArgumentException("At least one field and one rule must be present");
            }

            if (apiKeys.Count == 0)
            {
                var newApiKey = new BizApiKey(currentCompany.Id, fieldsAndRules.Keys.FirstOrDefault().Id);
                service.SaveApiKey(newApiKey).Wait();
                apiKeys.Add(newApiKey);
            }

            CurrentCompany = currentCompany.ToAdministratorDTO(service, settings.BaseEndpointUrl, billingUrl, purchaseUrl).Result;

            CurrentUser = currentUser;
            Applications = new List<AppViewModel>();
            foreach (var entry in fieldsAndRules)
            {
                var apiKey = apiKeys.FirstOrDefault(x => x.TopLevelFieldId == entry.Key.Id);

                if (apiKey is null)
                {
                    apiKey = new BizApiKey(currentCompany.Id, entry.Key.Id);
                    service.SaveApiKey(apiKey).Wait();
                }

                Applications.Add(
                    new AppViewModel(
                        currentCompany.Id,
                        service,
                        settings,
                        CurrentCompany,
                        entry.Key,
                        entry.Value,
                        apiKey));
            }
        }
    }
}