using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Http;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.Rules.Components;
using BusinessRules.Domain.Rules;
using Microsoft.AspNetCore.Mvc;
using BusinessRules.Rules.Extensions;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using Microsoft.Extensions.Options;
using BusinessRules.UI.Controllers;
using System.Diagnostics;
using BusinessRules.UI.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using BusinessRules.Domain.Common;
using Newtonsoft.Json;
using BusinessRules.Domain.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using BusinessRules.Domain.Helpers;
using BusinessRules.Domain.Fields;
using BusinessRules.Rules;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace BusinessRules.Tests.BENCHMARKS
{
    [TestClass()]
    public class BENCHMARKS
    {
        public const int RuleExecutionBenchmark = 100;

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
                new NullLogger<BackgroundStorageRepository>(),
                _config,
                FeatureFlags.OfflineMode);
        }

        [TestMethod()]
        public async Task PublicController_ExecuteRules_Benchmark()
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

            var clonedConfig = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(_config));

            clonedConfig.IsTestMode = false;
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


            var executionTime = new Stopwatch();
            executionTime.Start();
            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            executionTime.Stop();
            Console.WriteLine(executionTime.ElapsedMilliseconds);

            await Task.Delay(10000);

            var foundCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(
                result.Content.ToString().Contains("wonky")
                && executionTime.ElapsedMilliseconds <= RuleExecutionBenchmark
                && (foundCompany.CreditsUsed == 1 || FeatureFlags.OfflineMode));
        }

        [TestMethod()]
        public async Task PublicController_ExecuteRulesWithLargeObject_Benchmark()
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

            var clonedConfig = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(_config));

            clonedConfig.IsTestMode = false;
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

            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"{{\"Field 1\":{{");
            for (int i = 0; i < 10000; i++)
            {
                stringBuilder.Append($"\"test\": \"woot\",");
            }
            stringBuilder.Append($"\"{Guid.NewGuid()}\": {{");
            for (int i = 0; i < 60; i++)
            {
                stringBuilder.Append($"\"{Guid.NewGuid()}\": {{");
            }
            for (int i = 0; i < 1000; i++)
            {
                stringBuilder.Append($"\"{Guid.NewGuid()}\": \"\",");
            }
            stringBuilder.Append($"\"{Guid.NewGuid()}\": \"\"");
            for (int i = 0; i < 60; i++)
            {
                stringBuilder.Append($"}}");
            }
            stringBuilder.Append($"}}");
            stringBuilder.Append($"}}");
            stringBuilder.Append($"}}");

            var parsedObject = JObject.Parse(stringBuilder.ToString());
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            var newRule = new BizRule("test", convertedField);
            newRule.Add(new IfAntecedent());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] {
                convertedField.ChildFields[0].Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()}));
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


            var executionTime = new Stopwatch();
            executionTime.Start();
            var result =
                await publicController.ExecuteRules(apiKey.Id, stringBuilder.ToString()) as ContentResult;
            executionTime.Stop();
            Console.WriteLine(executionTime.ElapsedMilliseconds);

            await Task.Delay(10000);

            var foundCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(
                result.Content.ToString().Contains("wonky")
                && executionTime.ElapsedMilliseconds <= RuleExecutionBenchmark * 5
                && (foundCompany.CreditsUsed == 1 || FeatureFlags.OfflineMode));
        }

        [TestMethod()]
        public async Task PublicController_ExecuteRulesInBulk_Benchmark()
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
            company.CreditsAvailable = 2000000;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = "";

            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var clonedConfig = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(_config));

            clonedConfig.IsTestMode = false;
            clonedConfig.CreditGracePeriodAmount = 0;
            clonedConfig.NewCompanyFreeCreditAmount = 2000000;

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
            },
        ""Field 4"": """",
        ""Field 5"": """",
        ""Field 6"": """",
        ""Field 7"": """",
        ""Field 8"": """",
        ""Field 9"": """",
        ""Field 10"": """",
        ""Field 11"": """",
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            await service.SaveTopLevelField(company.Id, convertedField);

            for (int i = 0; i < 1000; i++)
            {
                var newRule = new BizRule(Guid.NewGuid().ToString("N"), convertedField);
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

                await service.SaveRule(company.Id, newRule);
            }

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);

            var executionTime = new Stopwatch();
            executionTime.Start();
            var result =
                await publicController.ExecuteRules(apiKey.Id, testField) as ContentResult;
            executionTime.Stop();
            Console.WriteLine(executionTime.ElapsedMilliseconds);

            await Task.Delay(2000);

            var foundCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(
                result.Content.ToString().Contains("wonky")
                && executionTime.ElapsedMilliseconds <= RuleExecutionBenchmark * 40);
        }

        [TestMethod()]
        public async Task PublicController_ExecuteRulesInBulkAndParallel_Benchmark()
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

            var resultTimings = new List<long>();
            var testSets = new ConcurrentDictionary<Guid, string>();

            var company = new BizCompany(Guid.NewGuid().ToString());
            company.CreditsAvailable = 2000000;

            var newUser = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Administrator);
            company.Users.Add(newUser);

            company.BillingId = "";

            await service.SaveCompany(company);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var clonedConfig = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(_config));

            clonedConfig.IsTestMode = true;
            clonedConfig.CreditGracePeriodAmount = 0;
            clonedConfig.NewCompanyFreeCreditAmount = 2000000;

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
            },
        ""Field 4"": """",
        ""Field 5"": """",
        ""Field 6"": """",
        ""Field 7"": """",
        ""Field 8"": """",
        ""Field 9"": """",
        ""Field 10"": """",
        ""Field 11"": """",
        }
}
";
            var parsedObject = JObject.Parse(testField);
            var convertedField = BizField_Helpers.ConvertJTokenToBizField(parsedObject, Guid.NewGuid());

            await service.SaveTopLevelField(company.Id, convertedField);

            for (int i = 0; i < 1000; i++)
            {
                var newRule = new BizRule(Guid.NewGuid().ToString("N"), convertedField);
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

                await service.SaveRule(company.Id, newRule);
            }

            var apiKey = new BizApiKey(company.Id, convertedField.Id);
            await service.SaveApiKey(apiKey);

            for (int i = 0; i < 100; i++)
            {
                testSets.AddOrUpdate(apiKey.Id, testField, (key, oldValue) => testField);
            }

            //Execute the rules
            Parallel.ForEach(testSets, (testSet) =>
            {
                Task.Run(async () =>
                {
                    var executionTime = new Stopwatch();
                    executionTime.Start();
                    var result =
                        await publicController.ExecuteRules(testSet.Key, testSet.Value) as ContentResult;
                    executionTime.Stop();
                    resultTimings.Add(executionTime.ElapsedMilliseconds);
                }).Wait();
            });

            var averageTime = resultTimings.Average();
            Console.WriteLine(averageTime);

            Assert.IsTrue(averageTime <= RuleExecutionBenchmark * 5);
        }

        [TestMethod()]
        public async Task RuleController_AddNewComponentWithLocalStorage_Benchmark()
        {
            _config.StorageMode = "json";
            _config.JsonStorageBasePath = Path.GetTempPath();
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var renderer = new Mock<IRazorPartialToStringRenderer>();
            renderer.Setup(x => x.RenderToString(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
              .Returns((string viewName, object model, ViewDataDictionary viewDictionary) =>
              {
                  return Task.Run(() => JsonConvert.SerializeObject(model));
              });

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), renderer.Object);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            for (int i = 0; i < 10; i++)
            {
                var newChild = new BizField("child" + i);
                for (int j = 0; j < 100; j++)
                {
                    var newSubChild = new BizField("subchild" + j);
                    newChild.AddChildField(newSubChild);
                }
                field1.AddChildField(newChild);
            }

            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var foundRule = await service.GetRuleById(company.Id, field1.Id, newRule.Id);
            var foundRuleSequenceCount = foundRule.RuleSequence.Count;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var renderedView = await ruleController.AddNewComponent(field1.Id, newRule.Id, foundRule.GetNextComponents(field1, true).Keys.First(), 0);
            stopwatch.Stop();
            var result = await service.GetRuleById(company.Id, field1.Id, newRule.Id);
            Console.WriteLine("Total time: " + stopwatch.ElapsedMilliseconds);
            Assert.IsTrue(
                result.RuleSequence.Count == foundRuleSequenceCount + 1
                && stopwatch.ElapsedMilliseconds < 1000);
        }
    }
}