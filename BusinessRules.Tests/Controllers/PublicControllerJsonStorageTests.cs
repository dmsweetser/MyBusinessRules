#if DEBUG
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Http;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.Domain.Fields;
using BusinessRules.Rules.Components;
using BusinessRules.Domain.Rules;
using Microsoft.AspNetCore.Mvc;
using BusinessRules.Rules.Extensions;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.Tests;
using BusinessRules.Domain.Common;
using Microsoft.Extensions.Options;
using BusinessRules.UI.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using BusinessRules.Domain.Services;
using Newtonsoft.Json.Linq;
using System.Text;
using BusinessRules.Domain.Helpers;
using BusinessRules.Domain.Rules.Component;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class PublicControllerJsonStorageTests
    {
        private TableServiceClient _tableServiceClient;
        private AppSettings _config;

        private IBackgroundStorageRepository _backgroundStorageRepository;

        [TestInitialize]
        public void Initialize()
        {
            var config = TestHelpers.InitConfiguration("json");
            _config = config;
            DynamicComparator.FunctionUrl = _config.DynamicComparatorFunctionUrl;
            DynamicOperand.FunctionUrl = _config.DynamicOperandFunctionUrl;
            _tableServiceClient = new TableServiceClient(config.AzureStorageConnectionString);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            _backgroundStorageRepository = new BackgroundStorageRepository(
                serviceScopeFactory,
                _tableServiceClient,
                new NullLogger<BackgroundStorageRepository>(),
                _config);
        }

        [TestMethod()]
        public async Task ExecuteRules_IfRuleIsAvailable_RuleIsExecuted()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);
            settings.Value.IsTestMode = false;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("wonky"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfNoCreditsAreAvailable_UnauthorizedResultIsReturned()
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

            company.BillingId = await service.GenerateNewStripeCustomerForCompany(_config.StripeBillingApiKey, company.Id, newUser.EmailAddress);

            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var clonedConfig = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(_config));

            clonedConfig.IsTestMode = true;
            clonedConfig.CreditGracePeriodAmount = 0;
            clonedConfig.NewCompanyFreeCreditAmount = 1;

            var settings = Options.Create(clonedConfig);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);

            _ =
                await publicController.ExecuteRules(
                    apiKey.Id, testField) as ContentResult;

            await Task.Delay(2000);

            _ =
                await publicController.ExecuteRules(
                    apiKey.Id, testField) as ContentResult;

            await Task.Delay(2000);

            var result =
                await publicController.ExecuteRules(
                    apiKey.Id, testField) as UnauthorizedObjectResult;

            Assert.IsTrue(result.Value.ToString() == "No credits are currently available for this company");
        }

        [TestMethod()]
        public async Task ExecuteRules_IfDomainIsAMismatch_UnauthorizedResultIsReturned()
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
            httpContextAccessor.HttpContext.Request.Host = new HostString("whatsup.net");

            var company = new BizCompany(Guid.NewGuid().ToString());
            company.CreditsAvailable = 1;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = await service.GenerateNewStripeCustomerForCompany(_config.StripeBillingApiKey, company.Id, newUser.EmailAddress);

            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var clonedConfig = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(_config));

            clonedConfig.IsTestMode = true;
            clonedConfig.CreditGracePeriodAmount = 0;
            clonedConfig.NewCompanyFreeCreditAmount = 1;

            var settings = Options.Create(clonedConfig);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newChild = new BizField("test child")
            {
                Value = "woot"
            };
            field3.AddChildField(newChild);
            var newRule = new BizRule("test", field1);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { newChild.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field2.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "5" }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, field1.Id);
            apiKey.AllowedDomains = "wazzup.org";
            await service.SaveApiKey(apiKey);

            field1.SetValue("boop");

            var result =
                await publicController.ExecuteRules(apiKey.Id, JsonConvert.SerializeObject(field1)) as UnauthorizedObjectResult;

            Assert.IsTrue(result.Value.ToString() == "Request is not authorized for domain " + httpContextAccessor.HttpContext.Request.Host.Value);
        }

        [TestMethod()]
        public async Task ExecuteRules_IfRequestBodyContainsJSON_RuleISExecutedSuccessfully()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);
            settings.Value.IsTestMode = false;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";

            httpContextAccessor.HttpContext.Request.Body = new MemoryStream(Encoding.Default.GetBytes(testField));

            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteRules(apiKey.Id, "") as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("wonky"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfRuleIsAvailableWithDynamicComponent_RuleIsExecuted()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);
            settings.Value.IsTestMode = false;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot1""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var script =
@"
    return x.includes(y);
";

            var newDynamicComponent = new DynamicComparator("includes", script);

            convertedField.DynamicComponents.Add(newDynamicComponent);

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(convertedField.DynamicComponents[0] as BaseComponent);
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("wonky"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfRuleIsAvailableWithDynamicOperator_RuleIsExecuted()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);
            settings.Value.IsTestMode = false;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var script =
@"
    return 'boop';
";

            var newDynamicComponent = new DynamicOperand("the booper", script);

            convertedField.DynamicComponents.Add(newDynamicComponent);

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsAssignment());
            newRule.Add(convertedField.DynamicComponents[0] as BaseComponent);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("boop"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfRuleIsAvailableWithDynamicOperatorIncludingFetch_RuleIsExecuted()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);
            settings.Value.IsTestMode = false;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var script =
@"
fetch('https://www.google.com')
        .then(function (response) {
            if (response.ok) {
                return response.text();
            } else {
                throw new Error('Request failed: ' + response.status);
            }
        })
        .then(function (text) {
            return text;
        })
        .catch(function (error) {
            console.error(error);
        });
";

            var newDynamicComponent = new DynamicOperand("fetch the goog", script);

            convertedField.DynamicComponents.Add(newDynamicComponent);

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsAssignment());
            newRule.Add(convertedField.DynamicComponents[0] as BaseComponent);

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("google"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfFieldDoesNotExist_FieldIsCreated()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField1 =
@"
{
    ""wonky"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField1);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());


            await service.SaveTopLevelField(company.Id, convertedField);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);

            var foundTopLevelField = await service.GetTopLevelField(company.Id, convertedField.Id);
            foundTopLevelField.ChildFields.Clear();

            await service.SaveTopLevelField(company.Id, foundTopLevelField);

            var testField2 =
@"
{
    ""bongo"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";

            var result =
                await publicController.ExecuteRules(apiKey.Id, testField2) as ContentResult;

            var allTopLevelFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var generatedTopLevelField = allTopLevelFields.First();

            Assert.IsTrue(result.Content.ToString().Contains("bongo")
                            && generatedTopLevelField is not null
                            && generatedTopLevelField.ChildFields.Count == 2);
        }

        [TestMethod()]
        public async Task ExecuteRules_IfFieldDoesNotExist_FieldIsCreatedAndApiKeysAreUpdated()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField1 =
@"
{
    ""wonky"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField1);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());


            await service.SaveTopLevelField(company.Id, convertedField);

            var foundTopLevelField = await service.GetTopLevelField(company.Id, convertedField.Id);
            foundTopLevelField.ChildFields.Clear();

            await service.SaveTopLevelField(company.Id, foundTopLevelField);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            var apiKey2 = new BizApiKey(company.Id, convertedField.Id);
            var apiKey3 = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);
            await service.SaveApiKey(apiKey2);
            await service.SaveApiKey(apiKey3);

            var testField2 =
@"
{
    ""bongo"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";

            var currentCompany = await service.GetCompany(company.Id);

            var result =
                await publicController.ExecuteRules(apiKey.Id, testField2) as ContentResult;

            var generatedTopLevelField = (await service.GetTopLevelFieldsForCompany(company.Id)).First();

            var apiKeys = await service.GetApiKeysForCompany(company.Id);

            Assert.IsTrue(result.Content.ToString().Contains("bongo")
                            && generatedTopLevelField is not null
                            && generatedTopLevelField.ChildFields.Count == 2
                            && apiKeys.Any(x =>
                            x.TopLevelFieldId == generatedTopLevelField.Id
                            || x.TopLevelFieldId == Guid.Empty));
        }

        [TestMethod()]
        public async Task ExecuteTestRules_IfRuleIsAvailable_RuleIsExecuted()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));

            newRule.IsActivated = false;
            newRule.IsActivatedTestOnly = true;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteTestRules(apiKey.Id, testField) as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("wonky"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfFieldDoesNotExistAndIsAnArray_FieldIsCreated()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField1 =
@"
{
    ""wonky"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField1);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());


            await service.SaveTopLevelField(company.Id, convertedField);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);

            var foundTopLevelField = await service.GetTopLevelField(company.Id, convertedField.Id);
            foundTopLevelField.ChildFields.Clear();

            await service.SaveTopLevelField(company.Id, foundTopLevelField);

            var testField2 =
@"
{
    ""wonky"" : 
        [
    {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""doot""
            }
        },
    {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
    }
    ]
}
";

            var result =
                await publicController.ExecuteRules(apiKey.Id, testField2) as ContentResult;

            var generatedTopLevelField = (await service.GetTopLevelFieldsForCompany(company.Id)).First();

            Assert.IsTrue(result.Content.ToString().Contains("doot")
                            && generatedTopLevelField is not null
                            && generatedTopLevelField.ChildFields.Count == 2);
        }

        [TestMethod()]
        public async Task Register_IfEmailIsNotAlreadyAllocated_NewCompanyIsCreated()
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

            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var email = Guid.NewGuid().ToString() + "@mybizrules.com";


            var result =
                await publicController.Register(email) as ContentResult;

            var foundApiKey = await service.GetApiKey(JsonConvert.DeserializeObject<Guid>(result.Content));
            var foundCompany = await service.GetCompanyForApiKey(foundApiKey.Id);

            Assert.IsTrue(foundApiKey is not null
                            && foundCompany.Users[0].EmailAddress == email);
        }

        [TestMethod()]
        public async Task Register_IfEmailIsAlreadyAllocated_BadRequestIsReturned()
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

            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var email = Guid.NewGuid().ToString() + "@mybizrules.com";


            var firstResult =
                await publicController.Register(email) as ContentResult;

            var foundApiKey = await service.GetApiKey(JsonConvert.DeserializeObject<Guid>(firstResult.Content));
            var foundCompany = await service.GetCompanyForApiKey(foundApiKey.Id);

            var secondResult =
                await publicController.Register(email) as BadRequestObjectResult;

            Assert.IsTrue(foundApiKey is not null
                            && foundCompany.Users[0].EmailAddress == email
                            && secondResult is not null);
        }

        [TestMethod()]
        public async Task Register_IfEmailAddressIsInRequestBody_NewCompanyIsCreated()
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

            var email = Guid.NewGuid().ToString() + "@mybizrules.com";

            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(email));
            MemoryStream stream = new MemoryStream(byteArray);

            httpContextAccessor.HttpContext.Request.Body = stream;

            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var result =
                await publicController.Register() as ContentResult;

            var foundApiKey = await service.GetApiKey(JsonConvert.DeserializeObject<Guid>(result.Content));
            var foundCompany = await service.GetCompanyForApiKey(foundApiKey.Id);

            Assert.IsTrue(foundApiKey is not null
                            && foundCompany.Users[0].EmailAddress == email);
        }

        [TestMethod()]
        public async Task ExecuteRules_IfTwoRulesAreAvailable_RulesAreExecutedInGroupNameThenNameOrder()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField =
@"
{
    ""Field 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var newRule = new BizRule("aaaa", convertedField);
            newRule.GroupName = "zzzz";
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            newRule.Add(new EqualsComparator());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule.Add(new ThenConsequent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule.Add(new EqualsAssignment());
            newRule.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));

            var otherRule = new BizRule("zzzz", convertedField);
            otherRule.GroupName = "aaaa";
            otherRule.Add(new IfAntecedent());
            otherRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            otherRule.Add(new EqualsComparator());
            otherRule.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            otherRule.Add(new ThenConsequent());
            otherRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[1].ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
            otherRule.Add(new EqualsAssignment());
            otherRule.Add(new StaticOperand().WithArgumentValues(new string[] { "zoop" }));

            newRule.IsActivated = true;
            newRule.IsActivatedTestOnly = false;

            await service.SaveTopLevelField(company.Id, convertedField);
            await service.SaveRule(company.Id, newRule);
            await service.SaveRule(company.Id, otherRule);

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);


            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            Assert.IsTrue(result.Content.ToString().Contains("zoop")
                            && !result.Content.ToString().Contains("wonky"));
        }

        [TestMethod()]
        public async Task ExecuteRules_IfTwoFieldsAreAvailable_RelatedRulesAreExecutedForEach()
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

            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var settings = Options.Create(_config);
            settings.Value.IsTestMode = false;

            IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
            var backgroundServiceWorker = new BackgroundServiceWorker(serviceScopeFactory, new NullLogger<BackgroundServiceWorker>());

            var publicController = new PublicController(service, httpContextAccessor, settings, backgroundServiceWorker)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContextAccessor.HttpContext
                }
            };

            var testField1 =
@"
{
    ""TEST FIELD 1"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";
            var parsedObject1 = JObject.Parse(testField1);
            var convertedField1 = BizField_Helpers.ConvertJTokenToBizField(parsedObject1, Guid.NewGuid());
            var newRule1 = new BizRule("aaaa", convertedField1);
            newRule1.Add(new IfAntecedent());
            newRule1.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField1.ChildFields[1].ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule1.Add(new EqualsComparator());
            newRule1.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule1.Add(new ThenConsequent());
            newRule1.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField1.ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule1.Add(new EqualsAssignment());
            newRule1.Add(new StaticOperand().WithArgumentValues(new string[] { "wonky" }));
            newRule1.IsActivated = true;
            newRule1.IsActivatedTestOnly = false;
            await service.SaveTopLevelField(company.Id, convertedField1);
            await service.SaveRule(company.Id, newRule1);
            var apiKey1 = new BizApiKey(company.Id, convertedField1.Id);
            await service.SaveApiKey(apiKey1);

            var testField2 =
@"
{
    ""TEST FIELD 2"": {
        ""Field 2"": """",
        ""Field 3"":  {
            ""test child"": ""woot""
            }
        }
}
";

            var parsedObject2 = JObject.Parse(testField2);
            var convertedField2 = BizField_Helpers.ConvertJTokenToBizField(parsedObject2, Guid.NewGuid());
            var newRule2 = new BizRule("aaaa", convertedField2);
            newRule2.Add(new IfAntecedent());
            newRule2.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField2.ChildFields[1].ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule2.Add(new EqualsComparator());
            newRule2.Add(new StaticOperand().WithArgumentValues(new string[] { "woot" }));
            newRule2.Add(new ThenConsequent());
            newRule2.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField2.ChildFields[0].Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            newRule2.Add(new EqualsAssignment());
            newRule2.Add(new StaticOperand().WithArgumentValues(new string[] { "doot" }));
            newRule2.IsActivated = true;
            newRule2.IsActivatedTestOnly = false;
            await service.SaveTopLevelField(company.Id, convertedField2);
            await service.SaveRule(company.Id, newRule2);
            var apiKey2 = new BizApiKey(company.Id, convertedField2.Id);
            await service.SaveApiKey(apiKey2);

            var result1 =
                await publicController.ExecuteRules(apiKey1.Id, testField1) as ContentResult;
            var result2 =
                await publicController.ExecuteRules(apiKey2.Id, testField2) as ContentResult;

            Assert.IsTrue(result1.Content.ToString().Contains("wonky")
                            && result2.Content.ToString().Contains("doot"));
        }
    }
}
#endif