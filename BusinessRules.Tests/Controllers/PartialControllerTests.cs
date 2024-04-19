using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using BusinessRules.UI.Common;
using Moq;
using Newtonsoft.Json;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules;
using BusinessRules.Rules.Extensions;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using BusinessRules.Domain.Common;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class PartialControllerTests
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
        public async Task GetBusinessUserComponent_IfComponentIsRequested_ComponentIsRetrieved()
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

            var partialController =
                new PartialController(new NullLogger<PartialController>(), service, httpContextAccessor, settings, _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newChild = new BizField("child");
            field1.AddChildField(newChild);
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { newChild.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await partialController.GetBusinessUserComponent(field1.Id) as OkObjectResult;

            Assert.IsTrue(result.Value.ToString().Contains(newRule.Id.ToString()));
        }


        [TestMethod()]
        public async Task GetBusinessUserComponent_IfComponentIsRequestedForDemoUser_ComponentIsRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection);
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

            var partialController =
                new PartialController(new NullLogger<PartialController>(), service, httpContextAccessor, settings, _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newChild = new BizField("child");
            field1.AddChildField(newChild);
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { newChild.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await partialController.GetBusinessUserComponent(field1.Id) as OkObjectResult;

            Assert.IsTrue(result.Value.ToString().Contains(newRule.Id.ToString()));
        }


        [TestMethod()]
        public async Task GetEndUserComponent_IfComponentIsRequested_ComponentIsRetrieved()
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

            var partialController =
                new PartialController(new NullLogger<PartialController>(), service, httpContextAccessor, settings, _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newChild = new BizField("child");
            field1.AddChildField(newChild);
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {newChild.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            var result = await partialController.GetEndUserComponent(field1.Id) as OkObjectResult;

            Assert.IsTrue(result.Value.ToString().Contains(field1.Id.ToString()));
        }


        [TestMethod()]
        public async Task GetEndUserComponent_IfComponentIsRequestedForDemoUser_ComponentIsRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection);
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

            var partialController =
                new PartialController(new NullLogger<PartialController>(), service, httpContextAccessor, settings, _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newChild = new BizField("child");
            field1.AddChildField(newChild);
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field3.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { newChild.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            var result = await partialController.GetEndUserComponent(field1.Id) as OkObjectResult;

            Assert.IsTrue(result.Value.ToString().Contains(field1.Id.ToString()));
        }
    }
}