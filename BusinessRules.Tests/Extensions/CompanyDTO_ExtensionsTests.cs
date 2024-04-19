using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Organization;
using Newtonsoft.Json;
using Azure.Data.Tables;
using BusinessRules.Tests;
using BusinessRules.ServiceLayer;
using BusinessRules.Domain.Common;

namespace BusinessRules.Domain.Fields.Tests
{
    [TestClass()]
    public class CompanyDTO_ExtensionsTests
    {
        private TableServiceClient _tableServiceClient;
        private AppSettings _config;

        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration();
            _config = config;
            _tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);
        }

        [TestMethod()]
        public async Task ToCompany_IfCompanyDtoIsConvertedToCompany_CompanyRoundtripsAccurately()
        {
            var service = new BusinessRulesService(_tableServiceClient, null, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var companyDto = await company.ToAdministratorDTO(service, "base", "", "");

            var serializedCompany = JsonConvert.SerializeObject(company);

            company.SyncValues(companyDto);

            var updatedCompany = JsonConvert.SerializeObject(company);

            Assert.IsTrue(serializedCompany == updatedCompany);
        }

        [TestMethod()]
        public async Task SyncValues_IfUsersArePresentInDTO_UsersAreUpdatedForCompany()
        {
            var service = new BusinessRulesService(_tableServiceClient, null, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var companyDto = await company.ToAdministratorDTO(service, "base", "", "");

            companyDto.Users.Add(new BizUser("test", UserRole.Administrator));

            company.SyncValues(companyDto);

            Assert.IsTrue(company.Users.FirstOrDefault().EmailAddress == "test"
                            && company.Users.FirstOrDefault().Role == UserRole.Administrator);
        }
    }
}