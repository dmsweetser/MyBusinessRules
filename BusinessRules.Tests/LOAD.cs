using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.UI.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;

namespace BusinessRules.Tests.LOAD
{
    [TestClass()]
    public class LOAD
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
                _config,
                FeatureFlags.OfflineMode);
        }

        [TestMethod()]
        public async Task JsonStorageRepository_RepeatedUpdatesToCompany_SaveWithoutError()
        {
            _config.StorageMode = "json";
            _config.JsonStorageBasePath = Path.GetTempPath();
            Console.WriteLine("Storage path: " + _config.JsonStorageBasePath);
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var company = new BizCompany(Guid.NewGuid().ToString());
            await service.SaveCompany(company);

            for (int i = 0; i < 100; i++)
            {
                var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
                company.Users.Add(newUser);
                await service.SaveCompany(company);
            }

            await Task.Delay(10000);

            Assert.IsTrue(true);
        }
    }
}