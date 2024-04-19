using BusinessRules.UI.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.Tests.Mocks;
using BusinessRules.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BusinessRules.Domain.Common;

namespace BusinessRules.UI.Common.Tests
{
    [TestClass()]
    public class BackgroundStorageRepositoryTests
    {
        [TestMethod()]
        public void Execute_IfExceptionOccursInBackgroundJob_ExceptionIsSwallowed()
        {
            var config = TestHelpers.InitConfiguration();
            var tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(), config);

            var exceptionEncountered = false;

            try
            {
                backgroundStorageRepository.Execute((repo) => throw new Exception());
            }
            catch (Exception)
            {
                exceptionEncountered = true;
            }

            Assert.IsFalse(exceptionEncountered);
        }

        [TestMethod()]
        public void BackgroundStorageRepository_IfIOptionsIsProvided_RepositoryIsInitialized()
        {
            var config = TestHelpers.InitConfiguration();
            var tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            var test = Options.Create(config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(),
                test
                );

            Assert.IsNotNull(backgroundStorageRepository);
        }
    }
}