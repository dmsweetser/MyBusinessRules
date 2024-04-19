using BusinessRules.UI.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.ServiceLayer;
using Microsoft.AspNetCore.Http;
using BusinessRules.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using BusinessRules.UI.Models;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Azure.Data.Tables;
using BusinessRules.Tests;
using System.Security.Claims;
using BusinessRules.UI.Common;
using Moq;
using Newtonsoft.Json;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Organization;
using System.ComponentModel;
using BusinessRules.Licensing;
using LicenseManager = BusinessRules.Licensing.LicenseManager;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class HomeControllerTests
    {
        private TableServiceClient _tableServiceClient;
        private IRazorPartialToStringRenderer _renderer;
        private AppSettings _config;
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
        public async Task Index_IfIndexIsRequestedInOfflineMode_DashboardIsReturned()
        {
            FeatureFlags.OfflineMode = true;
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());

            File.WriteAllBytes(
                Path.Combine(settings.Value.JsonStorageBasePath, "key.bin"), 
                new LicenseManager().EncryptCompanyId(Guid.NewGuid()));

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = await homeController.Index() as ViewResult;
            Assert.IsTrue(result.ViewName.Contains("Dashboard"));
            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public async Task Index_IfIndexIsRequestedInOfflineModeWithoutKeyFile_UnauthorizedIsReturned()
        {
            FeatureFlags.OfflineMode = true;
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = await homeController.Index() as UnauthorizedResult;
            Assert.IsTrue(result is not null);
            FeatureFlags.OfflineMode = false;
        }

        public async Task Index_IfIndexIsRequestedNotInOfflineMode_IndexIsReturned()
        {
            FeatureFlags.OfflineMode = false;
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = await homeController.Index() as ViewResult;
            Assert.IsTrue(result.ViewName.Contains("Index"));
            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public void Privacy_IfPrivacyIsRequested_PrivacyIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = homeController.Privacy() as ViewResult;
            Assert.IsTrue(result.ViewName == "Privacy");
        }

        [TestMethod()]
        public async Task TryItOut_IfTryItOutIsRequestedAndNoFieldExists_TryItOutIsReturnedWithNewField()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            _ = homeController.Index();

            var result = await homeController.TryItOut() as ViewResult;

            Assert.IsTrue(result.ViewName.Contains("Dashboard")
                & (result.Model as SystemViewModel) != null);
        }

        [TestMethod()]
        public async Task Error_IfErrorIsRequested_ErrorIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = await homeController.Error() as ViewResult;
            Assert.IsTrue(result.ViewName == "Error");
        }

        [TestMethod()]
        public async Task Error_IfErrorIsRequestedWithRequestId_ErrorIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            httpContextAccessor.HttpContext.TraceIdentifier = "test";
            var result = await homeController.Error() as ViewResult;
            Assert.IsTrue(result.ViewName == "Error"
                & (result.Model as ErrorViewModel).RequestId == "test"
                & (result.Model as ErrorViewModel).ShowRequestId == true);
        }

        [TestMethod()]
        public async Task Error_IfTraceIdentifierIsSpecified_ItShowsAsRequestId()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            httpContextAccessor.HttpContext.TraceIdentifier = "woot";
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = await homeController.Error() as ViewResult;
            Assert.IsTrue(result.ViewName == "Error"
                & (result.Model as ErrorViewModel).RequestId == "woot"
                & (result.Model as ErrorViewModel).ShowRequestId == true);
        }

        [TestMethod()]
        public async Task Error_IfActivityIsSpecified_ItShowsAsRequestId()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var unitTestActivity = new Activity("UnitTest").Start();
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);
            var result = await homeController.Error() as ViewResult;
            Assert.IsTrue(result.ViewName == "Error"
                & (result.Model as ErrorViewModel).RequestId == unitTestActivity.Id
                & (result.Model as ErrorViewModel).ShowRequestId == true);
            unitTestActivity.Stop();
        }

        [TestMethod()]
        public async Task Index_IfUserIsAuthenticated_SystemIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);


            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession(),
                    User = principal
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var otherEmail);

            var result = await homeController.Index() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            Assert.IsTrue(result.ViewName == "~/Views/Home/Dashboard.cshtml" && foundCompany is not null);
        }

        [TestMethod()]
        public async Task TryItOut_IfTryItOutIsCalledTwice_GetsExistingCompany()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            _ = homeController.Index();

            var firstResult = await homeController.TryItOut() as ViewResult;

            var result = await homeController.TryItOut() as ViewResult;

            Assert.IsTrue(result.ViewName.Contains("Dashboard")
                && (result.Model as SystemViewModel) != null
                && (result.Model as SystemViewModel).Applications[0].TopLevelField.Id ==
                    (firstResult.Model as SystemViewModel).Applications[0].TopLevelField.Id);
        }

        [TestMethod()]
        public async Task System_IfOnlyDemoCompanyExistsForAuthenticatedUser_CompanyIsMigrated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            _ = homeController.Index();

            _ = await homeController.TryItOut() as ViewResult;

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var otherEmail);

            var result = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            Assert.IsTrue(result.ViewName == "~/Views/Home/Dashboard.cshtml" && foundCompany is not null);

        }

        [TestMethod()]
        public async Task System_IfCompanyDoesNotExist_CompanyIsCreated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            _ = homeController.Index();

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var otherEmail);

            var result = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            Assert.IsTrue(result.ViewName == "~/Views/Home/Dashboard.cshtml" && foundCompany is not null);

        }

        [TestMethod()]
        public async Task System_IfUserIsNotAuthenticated_UnauthorizedResultIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);


            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession(),
                    User = principal
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            var result = await homeController.Dashboard() as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task Index_IfOfflineModeWithNoBypassEmailAndNoKey_ReturnsUnauthorized()
        {
            FeatureFlags.OfflineMode = true;

            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);


            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession(),
                    User = principal
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var otherEmail);

            var result = await homeController.Index() as UnauthorizedResult;

            Assert.IsTrue(result is not null);

            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public async Task Index_IfOfflineModeWithNoBypassEmailAndHasKey_ReturnsUnauthorized()
        {
            FeatureFlags.OfflineMode = true;

            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);


            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession(),
                    User = principal
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(TestHelpers.InitConfiguration());
            settings.Value.JsonStorageBasePath = Path.GetTempPath();

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var otherEmail);

            var newCompany = new BizCompany(Guid.NewGuid().ToString("N"));
            await service.SaveCompany(newCompany);
            var licenseManager = new LicenseManager();
            var fileBytes = licenseManager.EncryptCompanyId(newCompany.Id);

            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "key.bin"), fileBytes);

            var result = await homeController.Index() as ViewResult;

            Assert.IsTrue(result.ViewName.Contains("Dashboard"));

            FeatureFlags.OfflineMode = false;
        }
    }
}