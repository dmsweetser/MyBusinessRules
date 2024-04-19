using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;
using BusinessRules.Rules;
namespace BusinessRules.UI.Models
{
    public class AppViewModel
    {
        private readonly Guid _companyId;
        private readonly IBusinessRulesService _service;

        public AdministratorDTO Company { get; set; }
        public BizField TopLevelField { get; set; }
        public BizApiKey CurrentApiKey { get; set; }
        public List<BizRule> Rules { get; set; }
        public EndUserDTO EndUserDTO =>
            TopLevelField.ToEndUserDTO(CurrentApiKey.Id, _settings);
        public DeveloperDTO DeveloperDTO =>
            TopLevelField.ToDeveloperDTO(_companyId, _service, _settings);
        public BusinessUserDTO BusinessUserDTO =>
            TopLevelField.ToBusinessUserDTO(Rules, _baseEndpointUrl, _settings.IsTestMode);

        private readonly string _baseEndpointUrl;
        private readonly AppSettings _settings;

        public AppViewModel(
            Guid companyId,
            IBusinessRulesService service,
            AppSettings settings,
            AdministratorDTO company,
            BizField providedField,
            List<BizRule> providedRules,
            BizApiKey currentApiKey)
        {
            _companyId = companyId;
            _service = service;
            TopLevelField = providedField;
            Rules = providedRules;
            CurrentApiKey = currentApiKey;
            Company = company;
            _baseEndpointUrl = settings.BaseEndpointUrl;
            _settings = settings;
        }
    }
}