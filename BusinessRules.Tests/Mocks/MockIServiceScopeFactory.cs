using Azure.Data.Tables;
using BusinessRules.Domain.Services;
using BusinessRules.ServiceLayer;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BusinessRules.Tests.Mocks
{
    public class MockIServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            return new MockIServiceScope();
        }
    }

    public class MockIServiceScope : IServiceScope
    {
        public IServiceProvider ServiceProvider => new MockIServiceProvider();

        public void Dispose()
        {
            
        }
    }

    public class MockIServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            var config = TestHelpers.InitConfiguration();
            var tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(), config);

            if (serviceType.IsAssignableFrom(typeof(IBusinessRulesService)))
            {
                return new BusinessRulesService(tableServiceClient, backgroundStorageRepository, config);
            }
            
            return null;
        }
    }
}
