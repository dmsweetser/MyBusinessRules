using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using BusinessRules.Tests.Mocks;
using BusinessRules.Domain.Organization;
using Azure.Data.Tables;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests;
using BusinessRules.Domain.Services;
using BusinessRules.Domain.Common;

namespace BusinessRules.UI.Common.Tests
{
    [TestClass()]
    public class BackgroundServiceWorkerTests
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
        public void Execute_IfIServiceScopeFactoryConstructorIsCalled_WorkerIsInstantiated()
        {
            IServiceScopeFactory serviceScopeFactory = null;
            var worker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());
            worker.Execute((x) => new Task(() => { _ = 1; }));
            Assert.IsNotNull(worker);
        }

        [TestMethod()]
        public async Task Execute_IfIServiceScopeFactoryConstructorIsCalledWithWorkingFactory_ServiceExecutesSuccessfully()
        {
            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var worker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var company = new BizCompany(Guid.NewGuid().ToString());
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);

            worker.Execute(async (x) =>
            {
                await x.SaveCompany(company);
            });
            await Task.Delay(2000);

            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            Assert.IsTrue(await service.GetCompany(company.Id) is not null);
        }
    }
}