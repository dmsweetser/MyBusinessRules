using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.Tests;
using BusinessRules.Domain.Services;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BusinessRules.Domain.Common;

namespace BusinessRules.ServiceLayer.Tests
{
    [TestClass()]
	public class BusinessRulesServiceExtensionsTests
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
                new NullLogger<BackgroundStorageRepository>(),
				_config);
        }

        [TestMethod()]
		public async Task ValidateField_IfFieldDoesNotExist_ArgumentExceptionIsThrown()
		{
			var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
			var company = new BizCompany(Guid.NewGuid().ToString());
			var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
			company.Users.Add(newUser);
			await service.SaveCompany(company);

			var encounteredException = false;
			try
			{
				await service.ValidateField(company.Id, Guid.NewGuid());
			}
			catch (ArgumentException)
			{
				encounteredException = true;
			}

			Assert.IsTrue(encounteredException);
		}

		[TestMethod()]
		public async Task ValidateCompany_IfCompanyIsNotFound_ArgumentExceptionIsThrown()
		{
			var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
			var company = new BizCompany(Guid.NewGuid().ToString());
			var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
			company.Users.Add(newUser);

			await service.SaveCompany(company);

            await Task.Delay(5000);

            await service.DeleteCompany(company);

			await Task.Delay(5000);

			var encounteredException = false;
			try
			{
				await service.ValidateCompany(company.Id);
			}
			catch (ArgumentException)
			{
				encounteredException = true;
			}

			Assert.IsTrue(encounteredException);
		}
    }
}