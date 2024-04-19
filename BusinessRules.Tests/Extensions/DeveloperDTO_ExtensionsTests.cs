using Azure.Data.Tables;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class DeveloperDTO_ExtensionsTests
    {
        private TableServiceClient _tableServiceClient;
        private AppSettings _config;
        private IBackgroundStorageRepository _backgroundStorageRepository;

        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration();
            _config = config;
            _tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            _backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(), _config);
        }

        [TestMethod()]
        public async Task ToDeveloperDTO_IfAFieldIsProvided_TheDeveloperDtoPropertiesMatch()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var company = new BizCompany(Guid.NewGuid().ToString());
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var parentField = new BizField("parent");
            var newAttribute = new BizField("attribute");
            var childField = new BizField("child");
            parentField.AddChildField(newAttribute);
            parentField.AddChildField(childField);
            newAttribute.SetValue("test");
            childField.SetValue("woot");

            await service.SaveTopLevelField(company.Id, parentField);

            var developerDTO = parentField.ToDeveloperDTO(company.Id, service, _config);
            Assert.IsTrue(developerDTO.CurrentField.Id == parentField.Id
                & developerDTO.CurrentField.ParentFieldId == parentField.ParentFieldId
                & developerDTO.CurrentField.SystemName == "parent"
                & developerDTO.CurrentField.ChildFields.Count == parentField.ChildFields.Count
                );
        }
    }
}