using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.Domain.Services;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using BusinessRules.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Common.Tests
{
    [TestClass()]
    public class CustomAuthenticationMiddlewareTests
    {
        private TableServiceClient _tableServiceClient;
        private AppSettings _config;

        private IRazorPartialToStringRenderer _renderer;
        private IBackgroundStorageRepository _backgroundStorageRepository;

        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration();
            _config = config;
            _tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            var renderer = new Mock<IRazorPartialToStringRenderer>();
            renderer.Setup(x => x.RenderToString(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
              .Returns((string viewName, object model, ViewDataDictionary viewDictionary) =>
              {
                  return Task.Run(() => JsonConvert.SerializeObject(model));
              });

            _renderer = renderer.Object;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            _backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(), _config);
        }


        [TestMethod()]
        public async Task InvokeAsync_IfBypassEmailIsPresent_ClaimIsPopulated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var test = new CustomAuthenticationMiddleware(new RequestDelegate(x => Task.Run(() => x)));

            httpContextAccessor.HttpContext.Session.SetString("bypassEmail", "boop");

            await test.InvokeAsync(httpContextAccessor.HttpContext);

            Assert.IsTrue(httpContextAccessor.HttpContext.User.Claims.Count() == 1);
        }

        [TestMethod()]
        public async Task InvokeAsync_IfBypassEmailIsNotPresent_ClaimsAreNotPopulated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var test = new CustomAuthenticationMiddleware(new RequestDelegate(x => Task.Run(() => x)));

            await test.InvokeAsync(httpContextAccessor.HttpContext);

            Assert.IsTrue(httpContextAccessor.HttpContext.User.Claims.Count() == 0);
        }
    }
}