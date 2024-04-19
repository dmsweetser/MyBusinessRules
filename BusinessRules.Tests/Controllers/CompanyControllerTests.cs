using BusinessRules.UI.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.Tests;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using BusinessRules.Domain.DTO;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class CompanyControllerTests
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
        public async Task AddNewApiKey_IfEndpointIsHit_NewApiKeyIsAdded()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());


            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.AddNewApiKey() as OkObjectResult;

            var foundApiKeys = await service.GetApiKeysForCompany(company.Id);

            Assert.IsTrue(foundApiKeys.Count == 1 && result.Value.ToString().Contains(foundApiKeys.FirstOrDefault().Id.ToString()));
        }

        [TestMethod()]
        public async Task DeleteApiKey_IfApiKeyExists_ApiKeyIsRemoved()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            _ = await companyController.AddNewApiKey();

            var foundApiKeys = await service.GetApiKeysForCompany(company.Id);

            var result = await companyController.RemoveApiKey(foundApiKeys[0].Id) as OkObjectResult;

            var remainingApiKeys = await service.GetApiKeysForCompany(company.Id);

            Assert.IsTrue(remainingApiKeys.Count == 0 && !result.Value.ToString().Contains(foundApiKeys.FirstOrDefault().Id.ToString()));
        }

        [TestMethod()]
        public async Task DeleteApiKey_IfApiKeyDoesNotExist_RedirectToUnauthorizedIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var newApiKey = new BizApiKey(company.Id, field1.Id);
            var result = await companyController.RemoveApiKey(newApiKey.Id) as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task DeleteApiKey_IfCompanyDoesNotExist_RedirectToUnauthorizedOccurs()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var newApiKey = new BizApiKey(company.Id, field1.Id);

            await service.DeleteCompany(company);

            var result = await companyController.RemoveApiKey(newApiKey.Id) as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task DeleteApiKey_IfCompanyIdIsInvalid_RedirectToUnauthorizedIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", "abcd");

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var newApiKey = new BizApiKey(Guid.NewGuid(), Guid.NewGuid());

            var result = await companyController.RemoveApiKey(newApiKey.Id) as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task AddNewUser_IfRequestIsMade_NewUserIsAdded()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.AddNewUser() as OkObjectResult;

            var companyUsers = await service.GetUsersForCompany(company.Id);

            Assert.IsTrue(companyUsers.Count == 2 && result.Value.ToString().Contains(companyUsers.LastOrDefault().EmailAddress));
        }


        [TestMethod()]
        public async Task AddNewUser_IfCompanyIdIsInvalid_RedirectToUnauthorizedisReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", "asdf");

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var result = await companyController.AddNewUser() as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }


        [TestMethod()]
        public async Task RemoveUser_IfRequestIsMade_UserIsRemoved()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var addResult = await companyController.AddNewUser() as OkObjectResult;

            var companyUsers = await service.GetUsersForCompany(company.Id);

            var removeResult = await companyController.RemoveUser(companyUsers.FirstOrDefault().EmailAddress) as OkObjectResult;

            var refreshedUsers = await service.GetUsersForCompany(company.Id);

            Assert.IsTrue(refreshedUsers.Count == 1
                && addResult.Value.ToString().Contains(companyUsers.FirstOrDefault().EmailAddress)
                && !removeResult.Value.ToString().Contains(companyUsers.FirstOrDefault().EmailAddress)
                );
        }


        [TestMethod()]
        public async Task RemoveUser_IfAllUsersAreRemoved_NoUsersAreLeft()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            _ = await companyController.AddNewUser();

            var companyUsers = await service.GetUsersForCompany(company.Id);

            await companyController.RemoveUser(companyUsers.FirstOrDefault().EmailAddress);

            var refreshedUsers = await service.GetUsersForCompany(company.Id);

            var result = await companyController.RemoveUser(refreshedUsers.FirstOrDefault().EmailAddress) as OkObjectResult;

            var finalListOfUsers = await service.GetUsersForCompany(company.Id);

            Assert.IsTrue(finalListOfUsers.Count == 0 && !result.Value.ToString().Contains(companyUsers.FirstOrDefault().EmailAddress));
        }


        [TestMethod()]
        public async Task RemoveUser_IfCompanyIdIsInvalid_RedirectToUnauthorizedIsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", "asdf");

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var result = await companyController.RemoveUser("abcd") as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }


        [TestMethod()]
        public async Task SaveChanges_IfCompanyIdIsNotSpecified_RedirectsToUnauthorized()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var currentCompany = await service.GetCompany(company.Id);

            company.Name = "Woot";

            var result =
                await companyController.SaveChanges(await company.ToAdministratorDTO(service, "", "", "")) as UnauthorizedResult;


            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task RemoveCompany_IfCompanyIsRemoved_CompanyNoLongerExists()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            _ = await companyController.RemoveCompany();

            var updatedCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(updatedCompany is NullBizCompany);
        }

        [TestMethod()]
        public async Task RemoveCompany_IfEmailDoesNotExist_RedirectsToError()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", Guid.NewGuid().ToString());

            var result = await companyController.RemoveCompany() as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task AddNewApiKey_IfCompanyIdIsNotSpecified_RedirectsToUnauthorized()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await companyController.AddNewApiKey() as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task SaveChanges_IfChangeIsMade_ChangeIsSaved()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var currentCompany = await service.GetCompany(company.Id);

            currentCompany.Name = "Woot";

            var result =
                await companyController.SaveChanges(await currentCompany.ToAdministratorDTO(service, "", "", "")) as OkObjectResult;

            var updatedCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(result.Value.ToString().Contains("Woot") && updatedCompany.Name == "Woot");
        }

        [TestMethod()]
        public async Task RemoveApiKey_IfNoApiKeysArePresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            company.ApiKeyIds = new List<Guid>();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var newApiKey = new BizApiKey(Guid.NewGuid(), Guid.NewGuid());

            var result = await companyController.RemoveApiKey(newApiKey.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task SaveChanges_IfApiKeyIsUpdated_ChangeIsSaved()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var currentCompany = await service.GetCompany(company.Id);

            var apiKey = new BizApiKey(currentCompany.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            currentCompany.Name = "Woot";

            currentCompany = await service.GetCompany(company.Id);

            var dto = await currentCompany.ToAdministratorDTO(service, "", "", "");
            dto.ApiKeys[0].AllowedDomains = "booger";

            var result = await companyController.SaveChanges(dto) as OkObjectResult;

            var updatedApiKey = await service.GetApiKey(apiKey.Id);

            Assert.IsTrue(result.Value.ToString().Contains("booger") && updatedApiKey.AllowedDomains == "booger");
        }

        [TestMethod()]
        public async Task ApplyCreditCode_IfCodeExists_CodeCanBeApplied()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var newCredit = new BizCreditCode(Guid.NewGuid(), 1, Guid.NewGuid().ToString());

            await service.Repository.SaveCreditCode(newCredit);

            var result = await companyController.ApplyCreditCode(newCredit.Id) as OkObjectResult;

            _ = await service.Repository.GetCreditCode(newCredit.Id.ToString());
            var updatedCreditCode = await service.Repository.GetCreditCode(newCredit.Id.ToString());

            Assert.IsTrue(
                updatedCreditCode.RedeemedByCompanyId == company.Id
                && updatedCreditCode.RedeemedDate > DateTime.MinValue);
        }

        [TestMethod()]
        public async Task ApplyCreditCode_IfCodeDoesNotExist_ReturnsError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var newCredit = new BizCreditCode(Guid.NewGuid(), 1, Guid.NewGuid().ToString());

            await service.Repository.SaveCreditCode(newCredit);

            var result = await companyController.ApplyCreditCode(Guid.NewGuid()) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task ApplyCreditCode_IfCodeWasAlreadyUsed_ReturnsError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var newCredit = new BizCreditCode(Guid.NewGuid(), 1000000, Guid.NewGuid().ToString());

            await service.Repository.SaveCreditCode(newCredit);

            _ = await companyController.ApplyCreditCode(newCredit.Id) as RedirectResult;
            var result = await companyController.ApplyCreditCode(newCredit.Id) as OkResult;

            var foundCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(result is not null && foundCompany.CreditsAvailable < 2000000);
        }

        [TestMethod()]
        public async Task SaveChanges_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.SaveChanges(new AdministratorDTO()) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task AddNewApiKey_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.AddNewApiKey() as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveApiKey_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.RemoveApiKey(Guid.NewGuid()) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task AddNewUser_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.AddNewUser() as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveUser_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.RemoveUser("woot") as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveCompany_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.RemoveCompany() as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task ApplyCreditCode_IfCompanyIdIsNotPresent_RedirectsToError()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.ApplyCreditCode(Guid.NewGuid()) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task ApplyCreditCode_IfEmailAddressIsNotProvided_ReturnsUnauthorized()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );
            companyController.IsMockOnly = true;

            var result = await companyController.ApplyCreditCode(Guid.NewGuid()) as UnauthorizedResult;

            Assert.IsTrue(result is not null);
        }

        [TestMethod()]
        public async Task DownloadKeyFile_IfKeyFileIsRequested_ItIsProvidedWithoutError()
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

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();
            company.LastBilledDate = DateTime.Now.AddDays(-29);

            await service.SaveCompany(company);

            await Task.Delay(10000);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);

            await Task.Delay(10000);

            await service.SaveRule(company.Id, newRule);

            await Task.Delay(10000);

            var result = await companyController.DownloadKeyFile() as FileContentResult;

            Assert.IsTrue(result.FileDownloadName == "key.bin" && result.FileContents.Length > 0);

            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public async Task DownloadKeyFile_IfKeyFileIsRequestedForUnauthorized_ReturnsOkInstead()
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

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            field3.AddChildField(new BizField("gargonaut"));
            field2.SetValue("newValue");
            var newRule = new BizRule("wakawaka", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await companyController.DownloadKeyFile() as OkResult;

            Assert.IsTrue(result is not null);

            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public async Task DownloadKeyFile_IfEmailIsBlank_ReturnsOkInstead()
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

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();
            company.LastBilledDate = DateTime.Now.AddDays(-29);

            await service.SaveCompany(company);

            await Task.Delay(10000);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForControllerWithBlankEmail(companyController);
            
            var result = await companyController.DownloadKeyFile() as OkResult;

            Assert.IsTrue(result is not null);

            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public async Task DownloadKeyFile_IfCompanyIsNotFound_ReturnsOkInstead()
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

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var companyId = Guid.NewGuid().ToString();

            await Task.Delay(10000);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", companyId);

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            var result = await companyController.DownloadKeyFile() as OkResult;

            Assert.IsTrue(result is not null);

            FeatureFlags.OfflineMode = false;
        }

        [TestMethod()]
        public async Task DownloadKeyFile_IfOfflineModeIsNotEnabled_ReturnsOkInstead()
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

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();
            company.LastBilledDate = DateTime.Now.AddDays(-29);

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var settings = Options.Create(TestHelpers.InitConfiguration());
            var companyController = new CompanyController(
                new NullLogger<CompanyController>(),
                service,
                httpContextAccessor,
                settings,
                _renderer
                );

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            var result = await companyController.DownloadKeyFile() as OkResult;

            Assert.IsTrue(result is not null);

            FeatureFlags.OfflineMode = false;
        }
    }
}