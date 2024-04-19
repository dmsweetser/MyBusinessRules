using Azure.Data.Tables;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using BusinessRules.UI.Controllers;
using BusinessRules.UI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using BusinessRules.Rules.Extensions;
using BusinessRules.Domain.Helpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Common;

namespace BusinessRules.Tests.INTEGRATION
{
    [TestClass()]
    public class INTEGRATION
    {
        [TestMethod()]
        public void Integration_Full_User_Experience()
        {
            var builtField = IntegrationTestsHelpers.BuildPolicyField();
            Assert.IsTrue(builtField.ChildFields.Count > 0, "BuildPolicyField");
            var builtRules = IntegrationTestsHelpers.BuildRule(builtField);
            Assert.IsTrue(builtRules is not null, "BuildRules");
            IntegrationTestsHelpers.UseTheSystem(builtField, builtRules, out var evalResult);
            Assert.IsTrue(evalResult, "UseTheSystem");
        }

        [TestMethod()]
        public void Integration_Full_User_Experience_OfflineMode()
        {
            FeatureFlags.OfflineMode = true;
            var builtField = IntegrationTestsHelpers.BuildPolicyField();
            Assert.IsTrue(builtField.ChildFields.Count > 0, "BuildPolicyField");
            var builtRules = IntegrationTestsHelpers.BuildRule(builtField);
            Assert.IsTrue(builtRules is not null, "BuildRules");
            IntegrationTestsHelpers.UseTheSystem(builtField, builtRules, out var evalResult);
            Assert.IsTrue(evalResult, "UseTheSystem");
            FeatureFlags.OfflineMode = false;
        }

        [TestMethod]
        public async Task Integration_IfTryItOutIsRequestedAndThenTheUserSignsUp_TestRulesWillNotExecute()
        {
            var config = TestHelpers.InitConfiguration();
            config.NewCompanyFreeCreditAmount = 1000;
            config.IsTestMode = false;
            var _tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            var renderer = new Mock<IRazorPartialToStringRenderer>();
            renderer.Setup(x => x.RenderToString(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
              .Returns((string viewName, object model, ViewDataDictionary viewDictionary) =>
              {
                  return Task.Run(() => JsonConvert.SerializeObject(model));
              });

            var _renderer = renderer.Object;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(), config);

            var service = new BusinessRulesService(_tableServiceClient, backgroundStorageRepository, config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(config);
            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            _ = homeController.Index();

            var tryItOutResult = await homeController.TryItOut() as ViewResult;

            var currentApp = (tryItOutResult.Model as SystemViewModel).Applications[0];

            var newRule = new BizRule("test", currentApp.TopLevelField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                currentApp.TopLevelField.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "2" }));

            newRule.IsActivated = false;
            newRule.IsActivatedTestOnly = true;

            var foundCompany = await service.GetCompanyForApiKey(currentApp.Company.ApiKeyIds[0]);

            await service.SaveRule(foundCompany.Id, newRule);

            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var jObject = BizField_Helpers.ConvertBizFieldToJToken(currentApp.TopLevelField);

            var demoExecuteResult = await publicController.ExecuteRules(currentApp.Company.ApiKeyIds[0], jObject.ToString()) as ContentResult;

            var accountController = new AccountController(new NullLogger<AccountController>(), service, httpContextAccessor, settings);
            await accountController.SignUp(Guid.NewGuid().ToString() + "@mybizrules.com");

            await homeController.Dashboard();

            var signUpExecuteResult = await publicController.ExecuteRules(currentApp.Company.ApiKeyIds[0], jObject.ToString()) as ContentResult;


            Assert.IsTrue(tryItOutResult.ViewName.Contains("Dashboard")
                & (tryItOutResult.Model as SystemViewModel) != null
                && demoExecuteResult.Content.Contains("2")
                && !signUpExecuteResult.Content.Contains("2"));
        }
    }
}
