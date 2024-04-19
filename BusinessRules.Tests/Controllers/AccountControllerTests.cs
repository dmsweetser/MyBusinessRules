using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.Tests;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BusinessRules.UI.Common;
using Moq;
using Newtonsoft.Json;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class AccountControllerTests
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
        public async Task SignUp_IfSignUpIsCalled_NewCompanyIsGenerated()
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

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

            var result = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            Assert.IsTrue(result.ViewName == "~/Views/Home/Dashboard.cshtml" && foundCompany is not null);
        }

        [TestMethod()]
        public async Task LogIn_IfLoginIsCalledForUserWithSingleCompany_SingleCompanySystemViewIsReturnedSuccessfully()
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

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

            _ = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            var result = await homeController.Dashboard() as ViewResult;

            Assert.IsTrue(
                result.ViewName == "~/Views/Home/Dashboard.cshtml"
                && (result.Model as SystemViewModel).CurrentCompany.Name == foundCompany.Name);
        }

        [TestMethod()]
        public async Task LogIn_IfLogInIsCalledWithNoCompanies_NewCompanyIsCreated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession(),
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

            var result = await homeController.Dashboard() as ViewResult;

            Assert.IsTrue(
                result.ViewName == "~/Views/Home/Dashboard.cshtml"
                && (result.Model as SystemViewModel).CurrentCompany.Users.FirstOrDefault().EmailAddress == userEmail);
        }

		[TestMethod()]
		public async Task LogIn_IfCompanyHasNoBillingId_NewBillingIdIsCreated()
		{
			var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
			var httpContextAccessor = new HttpContextAccessor
			{
				HttpContext = new DefaultHttpContext
				{
					Session = new MockHttpSession(),
				}
			};

			httpContextAccessor.HttpContext.Request.Scheme = "https";
			httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

			var settings = Options.Create(TestHelpers.InitConfiguration());

			var homeController =
				new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

			TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

			var result = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            foundCompany.BillingId = null;

            await service.SaveCompany(foundCompany);

            _ = await homeController.Dashboard() as ViewResult;

            var updatedCompany = await service.GetCompanyForUser(userEmail);

			Assert.IsTrue(
				result.ViewName == "~/Views/Home/Dashboard.cshtml"
				&& (result.Model as SystemViewModel).CurrentCompany.Users.FirstOrDefault().EmailAddress == userEmail
                && updatedCompany.BillingId is not null);
		}

		[TestMethod()]
        public async Task SignUp_IfSignUpIsCalledForUserWithExistingCompany_ExistingCompanyIsReturned()
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

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

            _ = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            var result = await homeController.Dashboard() as ViewResult;

            var updatedCompany = await service.GetCompanyForUser(userEmail);

            Assert.IsTrue(result.ViewName == "~/Views/Home/Dashboard.cshtml" && updatedCompany is not null);
        }
    }
}