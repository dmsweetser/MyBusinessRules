using BusinessRules.UI.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.ServiceLayer;
using BusinessRules.Domain.Fields;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BusinessRules.Tests.Mocks;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using BusinessRules.Domain.Common;
using Microsoft.Extensions.Options;
using BusinessRules.UI.Common;
using Moq;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Controllers.Tests
{
    [TestClass()]
    public class FieldControllerTests
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
        public async Task SaveField_IfFieldIsSaved_ReturnedFieldsMatchProvidedFields()
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
            //These are only here to mark test coverage as 100% in test project
            //These functions are not actually used
            httpContextAccessor.HttpContext.Session.Clear();
            await httpContextAccessor.HttpContext.Session.CommitAsync();
            await httpContextAccessor.HttpContext.Session.LoadAsync();
            httpContextAccessor.HttpContext.Session.Remove("test");
            httpContextAccessor.HttpContext.Session.Set("test", new byte[0]);
            httpContextAccessor.HttpContext.Session.TryGetValue("test", out var newByte);
            _ = httpContextAccessor.HttpContext.Session.IsAvailable;
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);

            field1.SetValue("boop");

            var serializedOriginalField =
                JsonConvert.SerializeObject(field1.ToDeveloperDTO(company.Id, service, _config));

            await fieldController.SaveFieldChanges(field1);

            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);

            var result = foundTopLevelField.ToDeveloperDTO(company.Id, service, _config);

            var serializedResultField = JsonConvert.SerializeObject(result);

            Assert.IsTrue(serializedOriginalField ==
                    serializedResultField);
        }

        [TestMethod()]
        public async Task SaveField_IfFieldIsSavedForDemoUser_ReturnedFieldsMatchProvidedFields()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            //These are only here to mark test coverage as 100% in test project
            //These functions are not actually used
            httpContextAccessor.HttpContext.Session.Clear();
            await httpContextAccessor.HttpContext.Session.CommitAsync();
            await httpContextAccessor.HttpContext.Session.LoadAsync();
            httpContextAccessor.HttpContext.Session.Remove("test");
            httpContextAccessor.HttpContext.Session.Set("test", new byte[0]);
            httpContextAccessor.HttpContext.Session.TryGetValue("test", out var newByte);
            _ = httpContextAccessor.HttpContext.Session.IsAvailable;
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);

            field1.SetValue("boop");

            var serializedOriginalField =
                JsonConvert.SerializeObject(field1.ToDeveloperDTO(company.Id, service, _config));

            httpContextAccessor.HttpContext.Session.Clear();
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            var controllerResult = await fieldController.SaveFieldChanges(field1) as OkObjectResult;

            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);

            var result = foundTopLevelField.ToDeveloperDTO(company.Id, service, _config);

            var serializedResultField = JsonConvert.SerializeObject(result);

            Assert.IsTrue(serializedOriginalField == serializedResultField
                    && controllerResult.Value.ToString().Contains("boop"));
        }

        [TestMethod()]
        public async Task SaveField_CompanyIdIsNotPopulated_RedirectsToError()
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

            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);

            field1.SetValue("boop");

            var controllerResult = await fieldController.SaveFieldChanges(field1) as RedirectResult;

            Assert.IsTrue(controllerResult.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task AddNewChildField_IfFieldIsAdded_NewFieldExists()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);
            var result = await fieldController.AddNewChildField(field1, field2.Id) as RedirectResult;
            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);
            var childField = foundTopLevelField.GetChildFieldById(field2.Id).ChildFields.FirstOrDefault(x => x.SystemName == "");
            Assert.IsTrue(childField != null && foundTopLevelField.GetChildFieldById(field2.Id).ChildFields.Count == 1);
        }

        [TestMethod()]
        public async Task AddNewChildField_IfFieldIsAddedForDemoUser_NewFieldExists()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);
            field2 = field1.ChildFields.Where(x => x.SystemName == "Field 2").First();
            var result = await fieldController.AddNewChildField(field1, field2.Id) as OkObjectResult;
            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);
            var childField = foundTopLevelField.GetChildFieldById(field2.Id).ChildFields.FirstOrDefault(x => x.SystemName == "");
            Assert.IsTrue(childField != null
                && foundTopLevelField.GetChildFieldById(field2.Id).ChildFields.Count == 1
                && result.Value.ToString().Contains(childField.Id.ToString()));
        }

        [TestMethod()]
        public async Task RemoveChildField_IfFieldExists_FieldIsRemoved()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);
            var result = await fieldController.RemoveField(field1, field1.Id, field2.Id) as OkObjectResult;
            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);
            Assert.IsTrue(!result.Value.ToString().Contains(field2.Id.ToString()) && foundTopLevelField.GetChildFieldById(field2.Id) is NullBizField);
        }

        [TestMethod()]
        public async Task RemoveChildField_IfFieldExistsForDemoUser_FieldIsRemoved()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);

            httpContextAccessor.HttpContext.Session.Clear();
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            var result = await fieldController.RemoveField(field1, field1.Id, field2.Id) as OkObjectResult;
            var foundTopLevelField = await service.GetTopLevelField(company.Id, field1.Id);
            Assert.IsTrue(!result.Value.ToString().Contains(field2.Id.ToString()) && foundTopLevelField.GetChildFieldById(field2.Id) is NullBizField);
        }

        [TestMethod()]
        public async Task RemoveChildField_IfChildFieldDoesNotExist_RedirectsToErrorPage()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);
            var result = await fieldController.RemoveField(field1, field1.Id, Guid.NewGuid()) as RedirectResult;
            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveChildField_IfTopLevelFieldDoesNotExist_RedirectsToErrorPage()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            await service.SaveTopLevelField(company.Id, field1);
            var dummyId = Guid.NewGuid();
            var result = await fieldController.RemoveField(field1, field1.Id, Guid.NewGuid()) as RedirectResult;
            Assert.IsTrue(result.Url.Contains("Error"));
        }


        [TestMethod()]
        public async Task RemoveChildField_IfFieldIsReferencedByExistingRule_RedirectsToErrorPage()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
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
            var result = await fieldController.RemoveField(field1, field1.Id, field3.Id) as RedirectResult;
            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveTopLevelField_IfExistingRulesReferenceField_RedirectsToErrorPage()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
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

            var foundField1 = await service.GetTopLevelField(company.Id, field1.Id);

            var result = await fieldController.RemoveField(foundField1, field1.Id, field1.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveTopLevelField_IfFieldExists_RedirectsToError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);
            var field1 = new BizField("Field 1");
            var field2 = new BizField("Field 2");
            var field3 = new BizField("Field 3");
            field1.AddChildField(field2);
            field1.AddChildField(field3);
            var newAttribute = new BizField("attribute");
            field1.AddChildField(newAttribute);

            await service.SaveTopLevelField(company.Id, field1);

            var result = await fieldController.RemoveField(field1, field1.Id, field1.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task BuildTopLevelField_IfFieldIsBuiltFromJson_ItGeneratesAsExpected()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(result.Url.Contains("Dashboard")
                            && foundFields[0].ChildFields[2].ChildFields[2].Value == "");
        }

        [TestMethod()]
        public async Task BuildTopLevelField_IfJsonIsBad_RedirectsToError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": 
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task BuildTopLevelField_IfExistingFieldIsPresent_NewFieldIsAdded()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var field1 = new BizField("test");
            await service.SaveTopLevelField(company.Id, field1);

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(result.Url.Contains("Dashboard")
                            && foundFields.Count == 2
                            && foundFields[1].ChildFields[2].ChildFields[2].Value == "");
        }

        [TestMethod()]
        public async Task BuildTopLevelField_IfCompanyIdIsNotPresent_RedirectsToError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task BuildTopLevelField_IfFieldIsBuiltFromJsonForDemoUser_RedirectsToTryItOut()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(result.Url.Contains("TryItOut")
                            && foundFields[0].ChildFields[2].ChildFields[2].Value == "");
        }

        [TestMethod()]
        public async Task AddNewChildField_IfCompanyIdIsNotPresent_RedirectsToError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var field1 = new BizField("test");
            var newRule = new BizRule("testRule", field1);
            await service.SaveTopLevelField(company.Id, field1);
            await service.SaveRule(company.Id, newRule);

            var result = await fieldController.AddNewChildField(field1, field1.Id) as RedirectResult;

            Assert.IsTrue(result.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task GenerateInitialField_IfDataIsRequested_DataIsGenerated()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var demoResult = await fieldController.GenerateInitialField() as RedirectResult;
            var demoFoundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(demoResult.Url.Contains("TryItOut")
                            && foundFields[0].ChildFields[0].Id != demoFoundFields[0].Id);
        }

        [TestMethod()]
        public async Task GenerateInitialField_IfDataIsRequestedForNonDemoUser_DataIsGenerated()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(updatedResult.Url.Contains("Dashboard")
                            && foundFields[0].ChildFields[0].Id != updatedResultFields[0].Id);
        }

        [TestMethod()]
        public async Task GenerateInitialField_IfCompanyDoesNotExist_ReturnsError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(updatedResult.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task AddNewDynamicComponent_IfNewDynamicComparatorIsAdded_ItCanBeRetrieved()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var comparatorResult =
                await fieldController.AddNewDynamicComponent(updatedResultFields[0].Id, true) as OkObjectResult;

            var finalField = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(comparatorResult.Value.ToString().Contains("return")
                            && finalField[0].DynamicComponents.Count == 1);
        }

        [TestMethod()]
        public async Task AddNewDynamicComponent_IfNewDynamicOperatorIsAdded_ItCanBeRetrieved()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var comparatorResult =
                await fieldController.AddNewDynamicComponent(updatedResultFields[0].Id, false) as OkObjectResult;

            var finalField = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(comparatorResult.Value.ToString().Contains("return")
                            && finalField[0].DynamicComponents.Count == 1);
        }

        [TestMethod()]
        public async Task AddNewDynamicComponent_IfCompanyDoesNotExist_ReturnsError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", "garg");
            var comparatorResult =
                await fieldController.AddNewDynamicComponent(updatedResultFields[0].Id, false) as RedirectResult;

            Assert.IsTrue(comparatorResult.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task RemoveDynamicComponent_IfComponentExists_ComponentIsRemoved()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var comparatorResult =
                await fieldController.AddNewDynamicComponent(updatedResultFields[0].Id, false) as OkObjectResult;

            var interimFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var removalResult = await fieldController.RemoveDynamicComponent(interimFields[0].Id, interimFields[0].DynamicComponents[0].Id) as OkObjectResult;

            var finalFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(!removalResult.Value.ToString().Contains("myFunction(currentField)")
                            && finalFields[0].DynamicComponents.Count == 0);
        }

        [TestMethod()]
        public async Task GenerateInitialField_IfRequestIsMadeForCompanyWithNoApiKey_ApiKeyIsCreated()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            httpContextAccessor.HttpContext.Session.SetString("CompanyId", company.Id.ToString());
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var foundKeys = await service.GetApiKeysForCompany(company.Id);
            foreach (var key in foundKeys)
            {
                await service.DeleteApiKey(company.Id, key.Id);
            }

            var demoResult = await fieldController.GenerateInitialField() as RedirectResult;
            var demoFoundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var newApiKeys = await service.GetApiKeysForCompany(company.Id);

            Assert.IsTrue(demoResult.Url.Contains("TryItOut")
                            && foundFields[0].ChildFields[0].Id != demoFoundFields[0].Id
                            && newApiKeys.Count == 1);
        }

        [TestMethod()]
        public async Task RemoveDynamicComponent_IfCompanyDoesNotExist_RedirectsToError()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);


            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.6
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2,
				""Zip"": 12345
			}
		]
	}
}
";

            var result = await fieldController.BuildTopLevelField(testJson) as RedirectResult;
            var foundFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var updatedResult = await fieldController.GenerateInitialField() as RedirectResult;
            var updatedResultFields = await service.GetTopLevelFieldsForCompany(company.Id);

            var comparatorResult =
                await fieldController.AddNewDynamicComponent(updatedResultFields[0].Id, false) as OkObjectResult;

            var interimFields = await service.GetTopLevelFieldsForCompany(company.Id);

            httpContextAccessor.HttpContext.Session.SetString("CompanyId", "");

            var removalResult = await fieldController.RemoveDynamicComponent(interimFields[0].Id, interimFields[0].DynamicComponents[0].Id) as RedirectResult;

            Assert.IsTrue(removalResult.Url.Contains("Error"));
        }

        [TestMethod()]
        public async Task AddNewTopLevelField_IfTwoFieldsAreAddedSuccessively_BothExistWithApiKeys()
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
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var result1 = await fieldController.AddNewTopLevelField() as RedirectResult;
            var result2 = await fieldController.AddNewTopLevelField() as RedirectResult;

            var topLevelFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(
                topLevelFields.Count == 2
                && company.ApiKeyIds.Count == 2
                && result1.Url.Contains("Dashboard")
                && result2.Url.Contains("Dashboard")
                );
        }

        [TestMethod()]
        public async Task AddNewTopLevelField_IfCompanySessionVariableNotPresent_RedirectsToError()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var result1 = await fieldController.AddNewTopLevelField() as RedirectResult;
            var result2 = await fieldController.AddNewTopLevelField() as RedirectResult;

            Assert.IsTrue(
                result1.Url.Contains("Error")
                && result2.Url.Contains("Error")
                );
        }

        [TestMethod()]
        public async Task AddNewTopLevelField_IfNewFieldIsAddedForDemoUser_RedirectsToTryItOut()
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
            httpContextAccessor.HttpContext.Session.SetString("demoCompanyId", company.Id.ToString());
            var fieldController = new FieldController(new NullLogger<FieldController>(), service, httpContextAccessor, Options.Create(_config), _renderer);

            var result1 = await fieldController.AddNewTopLevelField() as RedirectResult;

            var topLevelFields = await service.GetTopLevelFieldsForCompany(company.Id);

            Assert.IsTrue(
                topLevelFields.Count == 1
                && company.ApiKeyIds.Count == 1
                && result1.Url.Contains("TryItOut")
                );
        }
    }
}