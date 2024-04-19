using BusinessRules.Data;
using Azure.Data.Tables;
using BusinessRules.Tests;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BusinessRules.Data.Tests
{
    [TestClass()]
    public class RepositoryFactoryTests
    {
        [TestMethod()]
        public void GetRepository_IfAnAzureRepositoryIsRequested_TheReturnedRepositoryIsAnIRepository()
        {
            var settings = TestHelpers.InitConfiguration("azure");

            var _mockTableServiceClient = new Mock<TableServiceClient>();
            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var _backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _mockTableServiceClient.Object,
                new NullLogger<BackgroundStorageRepository>(),
                settings);

            var repository = RepositoryFactory.GetRepository(_mockTableServiceClient.Object, _backgroundStorageRepository, settings, false);
            Assert.IsTrue(repository is not null);
        }

        [TestMethod()]
        public void GetRepository_IfAJsonRepositoryIsRequested_TheReturnedRepositoryIsAnIRepository()
        {
            var settings = TestHelpers.InitConfiguration("json");

            var _mockTableServiceClient = new Mock<TableServiceClient>();
            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var _backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _mockTableServiceClient.Object,
                new NullLogger<BackgroundStorageRepository>(),
                settings);

            var repository = RepositoryFactory.GetRepository(_mockTableServiceClient.Object, _backgroundStorageRepository, settings, false);
            Assert.IsTrue(repository is not null);
        }

        [TestMethod()]
        public void GetRepository_IfNullIsProvided_ThrowsException()
        {
            var settings = TestHelpers.InitConfiguration("json");
            var _mockTableServiceClient = new Mock<TableServiceClient>();
            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var _backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _mockTableServiceClient.Object,
                new NullLogger<BackgroundStorageRepository>(),
                settings);

            var exceptionFound = false;
            settings.StorageMode = null;
            try
            {
                var repository = 
                    RepositoryFactory.GetRepository(_mockTableServiceClient.Object, _backgroundStorageRepository, settings, false);
            }
            catch (Exception)
            {
                exceptionFound = true;
            }
            
            Assert.IsTrue(exceptionFound);
        }
    }
}