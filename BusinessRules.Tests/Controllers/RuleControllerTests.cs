using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.ServiceLayer;
using BusinessRules.Domain.Fields;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BusinessRules.Tests.Mocks;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules;
using BusinessRules.Rules.Extensions;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.Tests;
using Microsoft.Extensions.Options;
using BusinessRules.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;
using BusinessRules.Domain.DTO;
using BusinessRules.UI.Common;
using Moq;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class RuleControllerTests
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
        public async Task SaveRules_IfRulesAreSaved_RetreivedRulesMatch()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
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
            var foundRules = await service.GetRules(company.Id, field1.Id);
            var convertedRules = foundRules.Select(x => x.ToRuleDTO(field1, true)).ToList();

            for (int i = 0; i < convertedRules.Count; i++)
            {
                _ =
                await ruleController.SaveRules(
                    new BusinessUserDTO(field1, convertedRules, _config.BaseEndpointUrl), field1.Id, newRule.Id) as RedirectResult;
            }
            
            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);
            var latestFoundRules = await service.GetRules(company.Id, field1.Id);
            var latestConvertedRules = latestFoundRules.Select(x => x.ToRuleDTO(foundTopLevelField, true)).ToList();

            //These will differ every time the rule is cast to a DTO, so they are omitted from this test
            convertedRules.ForEach(x => x.NextComponents = new());
            latestConvertedRules.ForEach(x => x.NextComponents = new());


            Assert.AreEqual(JsonConvert.SerializeObject(convertedRules),
                    JsonConvert.SerializeObject(latestConvertedRules));
        }

        [TestMethod()]
        public async Task SaveRules_IfFieldDoesNotExist_RequestFails()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
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
            var dummyId = Guid.NewGuid();

            var result =
                await ruleController.SaveRules(
                    new BusinessUserDTO(field1, new List<BizRuleDTO> { newRule.ToRuleDTO(field1, false)}, _config.BaseEndpointUrl), dummyId, newRule.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task AddNewRule_IfTopLevelFieldDoesNotExist_RedirectsToError()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var field1 = new BizField("Field 1");
            await service.SaveTopLevelField(company.Id, field1);
            var foundRules = await service.GetRules(company.Id, field1.Id);
            var convertedRules = foundRules.Select(x => x.ToRuleDTO(field1, true)).ToList();
            var dummyId = Guid.NewGuid();
            var result = await ruleController.AddNewRule(dummyId) as RedirectResult;
            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveRule_IfTopLevelFieldDoesNotExist_RequestFails()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
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

            var result =
                await ruleController.SaveRules(new BusinessUserDTO(field1, new List<BizRuleDTO>() { newRule.ToRuleDTO(field1, true) }, _config.BaseEndpointUrl), field1.Id, newRule.Id) as RedirectResult;

            var madeUpFieldId = Guid.NewGuid();

            var removeResult = await ruleController.RemoveRule(madeUpFieldId, newRule.Id) as RedirectResult;
            Assert.IsTrue(removeResult.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveRule_IfRuleDoesNotExist_RequestFails()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
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

            var result =
                await ruleController.SaveRules(new BusinessUserDTO(field1, new List<BizRuleDTO>() { newRule.ToRuleDTO(field1, true) }, _config.BaseEndpointUrl), field1.Id, newRule.Id) as RedirectResult;

            var badId = Guid.NewGuid();
            var removeResult = await ruleController.RemoveRule(field1.Id, badId) as RedirectResult;
            Assert.IsTrue(removeResult.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task GetNextComponents_IfComponentsAreRequested_ComponentsReturned()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule); ;
            var foundRules = await service.GetRules(company.Id, field1.Id);
            var convertedRules = foundRules.Select(x => x.ToRuleDTO(field1, true)).ToList();
            await ruleController.AddNewRule(field1.Id);
            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);

            var foundNewRules = await service.GetRules(company.Id, field1.Id);
            var addedRule = foundNewRules.Where(x => x.Id != newRule.Id).FirstOrDefault();
            var foundNewRule = await service.GetRuleById(company.Id, foundTopLevelField.Id, addedRule.Id);
            var foundNewRuleSequence = foundNewRule.RuleSequence;
            Assert.IsTrue(foundNewRuleSequence.Any(x => x.DefinitionId == new IfAntecedent().DefinitionId));
        }

        [TestMethod()]
        public async Task RemoveLatestComponent_IfLatestComponentIsRemoved_LatestComponentIsNoLongerPresent()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field1.Id.ToString() , BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);
            var stashedNewRule = JsonConvert.SerializeObject(newRule);
            await ruleController.RemoveLatestComponent(field1.Id, newRule.Id, 0);
            var unstashedNewRule = JsonConvert.DeserializeObject<BizRule>(stashedNewRule);
            var foundRule = await service.GetRuleById(company.Id, field1.Id, newRule.Id);
            Assert.IsTrue(unstashedNewRule.RuleSequence.Count == 2 && foundRule.RuleSequence.Count == 1);
        }

        [TestMethod()]
        public async Task RemoveLatestComponent_IfBadFieldIdIsProvided_ArgumentExceptionIsThrown()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field1.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);
            var result = await ruleController.RemoveLatestComponent(Guid.NewGuid(), newRule.Id, 0) as RedirectResult;
            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveLatestComponent_IfBadRuleIdIsProvided_ArgumentExceptionIsThrown()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { field1.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);
            var result = await ruleController.RemoveLatestComponent(field1.Id, Guid.NewGuid(), 0) as RedirectResult;
            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveLatestComponent_IfOnlyIfAntecedentIsPresent_WhenRemovedItIsReadded()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };
            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);
            var stashedNewRule = JsonConvert.SerializeObject(newRule);
            await ruleController.RemoveLatestComponent(field1.Id, newRule.Id, 0);
            var unstashedNewRule = JsonConvert.DeserializeObject<BizRule>(stashedNewRule);
            var foundRule = await service.GetRuleById(company.Id, field1.Id, newRule.Id);
            Assert.IsTrue(unstashedNewRule.RuleSequence.Count == 1 && foundRule.RuleSequence.Count == 1);
        }

        [TestMethod()]
        public async Task RemoveRule_IfRuleIsRemoved_ItIsNoLongerPresent()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            await Task.Delay(2000);

            await ruleController.RemoveRule(field1.Id, newRule.Id);

            var result = await service.GetRuleById(company.Id, field1.Id, newRule.Id);

            Assert.IsTrue(result is NullBizRule);
        }

        [TestMethod()]
        public async Task AddNewComponent_IfNewComponentIsAdded_NewComponentIsPresent()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var foundRule = await service.GetRuleById(company.Id, field1.Id, newRule.Id);
            var foundRuleSequenceCount = foundRule.RuleSequence.Count;

            await ruleController.AddNewComponent(field1.Id, newRule.Id, foundRule.GetNextComponents(field1, true).Keys.First(), 0);

            var result = await service.GetRuleById(company.Id, field1.Id, newRule.Id);

            Assert.IsTrue(result.RuleSequence.Count == foundRuleSequenceCount + 1);
        }

        [TestMethod()]
        public async Task AddNewComponent_IfNoCompanyIsSpecified_RedirectsToError()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var foundRule = await service.GetRuleById(company.Id, field1.Id, newRule.Id);

            var result = await ruleController.AddNewComponent(field1.Id, newRule.Id, foundRule.GetNextComponents(field1, true).Keys.First(), 0) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveRule_IfRuleIsRemoved_ItIsNoLongerPresentInPage()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("woozle", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            var newRule2 = new BizRule("butter", field1);
            newRule2.Add(newRule2.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);
            await service.SaveRule(company.Id, newRule2);

            var result = await ruleController.RemoveRule(field1.Id, newRule.Id) as OkObjectResult;

            Assert.IsTrue(!result.ToString().Contains("woozle"));
        }

        [TestMethod()]
        public async Task RemoveRule_IfLastRuleIsRemoved_RedirectsToDashboard()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("woozle", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await ruleController.RemoveRule(field1.Id, newRule.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Dashboard"));
        }

        [TestMethod()]
        public async Task RemoveRule_IfLastRuleIsRemovedForDemoUser_RedirectsToTryItOut()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("woozle", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await ruleController.RemoveRule(field1.Id, newRule.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("TryItOut"));
        }

        [TestMethod()]
        public async Task AddNewComponent_IfNewComponentIsDynamicComponent_ItIsAddedSuccessfully()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var ruleController = new RuleController(new NullLogger<RuleController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());

            var field1 = new BizField("Field 1");
            var newRule = new BizRule("test", field1);
            newRule.Add(newRule.Then().FirstOrDefault());

            var script =
@"
    return 'boop';
";

            var newDynamicComponent = new DynamicOperand("the booper", script);

            field1.DynamicComponents.Add(newDynamicComponent);

            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var foundRule = await service.GetRuleById(company.Id, field1.Id, newRule.Id);
            var foundRuleSequenceCount = foundRule.RuleSequence.Count;

            await ruleController.AddNewComponent(field1.Id, newRule.Id,
                foundRule.GetNextComponents(field1, true)
                .Where(x => x.Value.DefinitionId == new DynamicOperand().DefinitionId)
                .ToList().FirstOrDefault().Key, 0);

            var result = await service.GetRuleById(company.Id, field1.Id, newRule.Id);

            Assert.IsTrue(result.RuleSequence.Count == foundRuleSequenceCount + 1
                            && result.RuleSequence[1].DefinitionId.ToString() == new DynamicOperand().DefinitionId.ToString());
        }
    }
}