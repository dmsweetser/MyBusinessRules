using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Data.Tables;
using BusinessRules.Tests;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BusinessRules.UI.Models.Tests
{
    [TestClass()]
    public class SystemViewModelTests
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
        public async Task SystemViewModel_IfModelIsInstantiated_CompanyExists()
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
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);


            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            var newModel = new SystemViewModel(
                service,
                _config,
                company,
                newUser,
                await service.GetFieldsAndRulesForCompany(company.Id),
                await service.GetApiKeysForCompany(company.Id),
                "",
                ""
                );

            Assert.IsTrue(newModel.Applications[0].Company.Name == company.Name);
        }


        [TestMethod()]
        public async Task SystemViewModel_IfModelIsInstantiated_BusinessUserDtoExists()
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
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            var fieldsAndRules = await service.GetFieldsAndRulesForCompany(company.Id);

            var newModel = new SystemViewModel(
                service,
                _config,
                company,
                newUser,
                fieldsAndRules,
                await service.GetApiKeysForCompany(company.Id),
                "",
                ""
                );

            Assert.IsTrue(newModel.Applications[0].BusinessUserDTO.Rules.Count == fieldsAndRules.FirstOrDefault().Value.Count);
        }

        [TestMethod()]
        public async Task SystemViewModel_IfModelIsInstantiated_EndUserDtoExists()
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
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            field1.AddChildField(field2);
            var field3 = new BizField("Field 3");
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);


            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            var fieldsAndRules = await service.GetFieldsAndRulesForCompany(company.Id);

            var newModel = new SystemViewModel(
                service,
                _config,
                company,
                newUser,
                fieldsAndRules,
                await service.GetApiKeysForCompany(company.Id),
                "",
                ""
                );

            Assert.IsTrue(newModel.Applications[0].EndUserDTO.CurrentField.ChildFields.Count == fieldsAndRules.FirstOrDefault().Key.ChildFields.Count);
        }


        [TestMethod()]
        public async Task SystemViewModel_IfModelIsInstantiated_DeveloperDtoExists()
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
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = Guid.NewGuid().ToString();

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            field1.AddChildField(field2);
            var field3 = new BizField("Field 3");
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            await service.SaveApiKey(apiKey);

            var fieldsAndRules = await service.GetFieldsAndRulesForCompany(company.Id);

            var newModel = new SystemViewModel(service,
                _config,
                company,
                newUser,
                fieldsAndRules,
                await service.GetApiKeysForCompany(company.Id),
                "",
                ""
                );

            Assert.IsTrue(newModel.Applications[0].DeveloperDTO.CurrentField.ChildFields.Count == fieldsAndRules.FirstOrDefault().Key.ChildFields.Count);
        }

        [TestMethod()]
        public async Task SystemViewModel_IfFieldsAndRulesIsEmpty_ArgumentExceptionIsThrown()
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

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            field1.AddChildField(field2);
            var field3 = new BizField("Field 3");
            field1.AddChildField(field3);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "1" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "4" }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var exceptionEncountered = false;

            try
            {
                var newModel = new SystemViewModel(
                    service,
                settings.Value,
                company,
                newUser,
                new Dictionary<BizField, List<BizRule>>(),
                new List<BizApiKey>(),
                "",
                ""
                );
            }
            catch (ArgumentException)
            {
                exceptionEncountered = true;
            }

            Assert.IsTrue(exceptionEncountered);
        }
    }
}