using BusinessRules.UI.Common;
using Azure.Data.Tables;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using BusinessRules.Domain.Helpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Common.Tests
{
    [TestClass()]
    public class ControllerHelpersTests
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
        public void ParseClaimsToGetEmail_IfEmailNotPresent_ReturnsNull()
        {
            IList<Claim> claimCollection =
                new List<Claim>();

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);

            Assert.IsNull(ControllerHelpers.ParseClaimsToGetEmail(principal));
        }

        [TestMethod()]
        public async Task SelectCompany_IfRendererIsNotNull_StringIsReturned()
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

            var result = await ControllerHelpers.SelectCompany(
                companyController, service, httpContextAccessor.HttpContext, settings.Value, userEmail, company.Id.ToString(), true, _renderer) as ObjectResult;

            Assert.IsTrue(result.Value.ToString().Contains(company.Name));

        }

        [TestMethod]
        public void ConvertJTokenToBizField_IfJsonIsConverted_OutputIsAsExpected()
        {
            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 1,
				""Zip"": 12345
			}
		]
	}
}
";

            JObject jObject = JObject.Parse(testJson);

            var result = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());

            // Expected BizField object
            BizField expected = new BizField("Customer")
            {
                Value = "",
                ChildFields = new List<BizField>()
                {
                    new BizField("Name")
                    {
                        Value = "",
                    },
                    new BizField("Age")
                    {
                        Value = "",
                    },
                    new BizField("Address")
                    {
                        Value = "",
                        IsACollection = true,
                        ChildFields = new List<BizField>()
                        {
                            new BizField("Street")
                            {
                                IsACollection = true,
                                Value = ""
                            },
                            new BizField("Type")
                            {
                                IsACollection = true,
                                Value = ""
                            },
                            new BizField("Zip")
                            {
                                IsACollection = true,
                                Value = ""
                            }
                        }
                    }
                }
            };

            TestHelpers.ScrubId(expected);
            TestHelpers.ScrubId(result);

            // Assert
            Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(result));
        }

        [TestMethod]
        public void MergeJObjectToBizField_IfJsonIsMerged_OutputIsAsExpected()
        {
            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1
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

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);

            // Expected BizField object
            BizField expected = new BizField("Customer")
            {
                Value = "",
                ChildFields = new List<BizField>()
                {
                    new BizField("Name")
                    {
                        Value = "smith",
                    },
                    new BizField("Age")
                    {
                        Value = "55",
                    },
                    new BizField("Address")
                    {
                        IsACollection = true,
                        Value = "",
                        ChildFields = new List<BizField>()
                        {
                            new BizField("Street")
                            {
                                IsACollection = true,
                                Value = "100 Main St"
                            },
                            new BizField("Type")
                            {
                                IsACollection = true,
                                Value = "1"
                            },
                            new BizField("Zip")
                            {
                                IsACollection = true,
                                Value = ""
                            }
                        }
                    },
                    new BizField("Address")
                    {
                        IsACollection = true,
                        Value = "",
                        ChildFields = new List<BizField>()
                        {
                            new BizField("Street")
                            {
                                IsACollection = true,
                                Value = "202 Main St"
                            },
                            new BizField("Type")
                            {
                                IsACollection = true,
                                Value = "2"
                            },
                            new BizField("Zip")
                            {
                                IsACollection = true,
                                Value = "12345"
                            }
                        }
                    }
                }
            };

            TestHelpers.ScrubId(expected);
            TestHelpers.ScrubId(mergedField);

            var actual = mergedField;

            // Assert
            Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actual));
            Assert.IsTrue(
                actual.ChildFields.Count == 4
                && actual.ChildFields[3].ChildFields[2].Value == "12345");
        }

        [TestMethod]
        public void ConvertBizFieldToJObject_IfBizFieldIsConverted_ItMatchesTheInput()
        {
            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1
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

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);
            var backToJson = BizField_Helpers.ConvertBizFieldToJToken(mergedField);

            var expectedJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1,
                ""Zip"": """"
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

            dynamic expectedDynamic = JObject.Parse(expectedJson);
            dynamic actualDynamic = JObject.Parse(JsonConvert.SerializeObject(backToJson));

            // Assert using the dynamic objects
            Assert.AreEqual(expectedDynamic.ToString(), actualDynamic.ToString());
        }

        [TestMethod]
        public void ConvertBizFieldToJObject_IfBizFieldWithDecimalIsConverted_ItMatchesTheInput()
        {
            var testJson =
@"
{
    ""test"": 
    {
	    ""Customer.Name"": {
		    ""ChildNodes"": null,
		    ""Children"": null,
		    ""Key"": ""Customer.Name"",
		    ""SubKey"": {
			    ""Buffer"": ""Customer.Name"",
			    ""Offset"": 9,
			    ""Length"": 4,
			    ""Value"": ""Name"",
			    ""HasValue"": true
		    },
		    ""IsContainerNode"": false,
		    ""RawValue"": ""John Doe"",
		    ""AttemptedValue"": ""John Doe"",
		    ""Errors"": [],
		    ""ValidationState"": 2
	    },
	    ""Customer.Email"": {
		    ""ChildNodes"": null,
		    ""Children"": null,
		    ""Key"": ""Customer.Email"",
		    ""SubKey"": {
			    ""Buffer"": ""Customer.Email"",
			    ""Offset"": 9,
			    ""Length"": 5,
			    ""Value"": ""Email"",
			    ""HasValue"": true
		    },
		    ""IsContainerNode"": false,
		    ""RawValue"": ""johndoe@example.com"",
		    ""AttemptedValue"": ""johndoe@example.com"",
		    ""Errors"": [],
		    ""ValidationState"": 2
	    },
	    ""Customer.Phone"": {
		    ""ChildNodes"": null,
		    ""Children"": null,
		    ""Key"": ""Customer.Phone"",
		    ""SubKey"": {
			    ""Buffer"": ""Customer.Phone"",
			    ""Offset"": 9,
			    ""Length"": 5,
			    ""Value"": ""Phone"",
			    ""HasValue"": true
		    },
		    ""IsContainerNode"": false,
		    ""RawValue"": ""+1234567890"",
		    ""AttemptedValue"": ""+1234567890"",
		    ""Errors"": [],
		    ""ValidationState"": 2
	    },
	    ""Customer.Address"": {
		    ""ChildNodes"": null,
		    ""Children"": null,
		    ""Key"": ""Customer.Address"",
		    ""SubKey"": {
			    ""Buffer"": ""Customer.Address"",
			    ""Offset"": 9,
			    ""Length"": 7,
			    ""Value"": ""Address"",
			    ""HasValue"": true
		    },
		    ""IsContainerNode"": false,
		    ""RawValue"": ""100 main st"",
		    ""AttemptedValue"": ""100 main st"",
		    ""Errors"": [],
		    ""ValidationState"": 2
	    },
	    ""Customer.DateOfBirth"": {
		    ""ChildNodes"": null,
		    ""Children"": null,
		    ""Key"": ""Customer.DateOfBirth"",
		    ""SubKey"": {
			    ""Buffer"": ""Customer.DateOfBirth"",
			    ""Offset"": 9,
			    ""Length"": 11,
			    ""Value"": ""DateOfBirth"",
			    ""HasValue"": true
		    },
		    ""IsContainerNode"": false,
		    ""RawValue"": ""1990-01-01T00:00"",
		    ""AttemptedValue"": ""1990-01-01T00:00"",
		    ""Errors"": [],
		    ""ValidationState"": 2
	    }
    }
}
";

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);
            var backToJson = BizField_Helpers.ConvertBizFieldToJToken(mergedField);

            var expectedJson =
@"
{
    ""test"": {
        ""Customer.Name"": {
		""ChildNodes"": """",
		""Children"": """",
		""Key"": ""Customer.Name"",
		""SubKey"": {
			""Buffer"": ""Customer.Name"",
			""Offset"": 9,
			""Length"": 4,
			""Value"": ""Name"",
			""HasValue"": true
		},
		""IsContainerNode"": false,
		""RawValue"": ""John Doe"",
		""AttemptedValue"": ""John Doe"",
		""ValidationState"": 2
	},
	""Customer.Email"": {
		""ChildNodes"": """",
		""Children"": """",
		""Key"": ""Customer.Email"",
		""SubKey"": {
			""Buffer"": ""Customer.Email"",
			""Offset"": 9,
			""Length"": 5,
			""Value"": ""Email"",
			""HasValue"": true
		},
		""IsContainerNode"": false,
		""RawValue"": ""johndoe@example.com"",
		""AttemptedValue"": ""johndoe@example.com"",
		""ValidationState"": 2
	},
	""Customer.Phone"": {
		""ChildNodes"": """",
		""Children"": """",
		""Key"": ""Customer.Phone"",
		""SubKey"": {
			""Buffer"": ""Customer.Phone"",
			""Offset"": 9,
			""Length"": 5,
			""Value"": ""Phone"",
			""HasValue"": true
		},
		""IsContainerNode"": false,
		""RawValue"": ""+1234567890"",
		""AttemptedValue"": ""+1234567890"",
		""ValidationState"": 2
	},
	""Customer.Address"": {
		""ChildNodes"": """",
		""Children"": """",
		""Key"": ""Customer.Address"",
		""SubKey"": {
			""Buffer"": ""Customer.Address"",
			""Offset"": 9,
			""Length"": 7,
			""Value"": ""Address"",
			""HasValue"": true
		},
		""IsContainerNode"": false,
		""RawValue"": ""100 main st"",
		""AttemptedValue"": ""100 main st"",
		""ValidationState"": 2
	},
	""Customer.DateOfBirth"": {
		""ChildNodes"": """",
		""Children"": """",
		""Key"": ""Customer.DateOfBirth"",
		""SubKey"": {
			""Buffer"": ""Customer.DateOfBirth"",
			""Offset"": 9,
			""Length"": 11,
			""Value"": ""DateOfBirth"",
			""HasValue"": true
		},
		""IsContainerNode"": false,
		""RawValue"": ""1990-01-01T00:00"",
		""AttemptedValue"": ""1990-01-01T00:00"",
		""ValidationState"": 2
	}   
    }
}
";

            dynamic expectedDynamic = JObject.Parse(expectedJson);
            dynamic actualDynamic = JObject.Parse(JsonConvert.SerializeObject(backToJson));

            // Assert using the dynamic objects
            Assert.AreEqual(expectedDynamic.ToString(), actualDynamic.ToString());
        }

        [TestMethod]
        public void MergeJObjectToBizField_IfJsonIsMergedWithTopLevelArray_OutputIsAsExpected()
        {
            var testJson =
@"
{
	""Address"": [
	{
		""Street"": ""100 Main St"",
		""Type"": 1
	},
	{
		""Street"": ""202 Main St"",
		""Type"": 2,
		""Zip"": 12345
	}
]
}
";

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);

            // Expected BizField object
            BizField expected = new BizField("Address")
            {
                Value = "",
                IsACollection = true,
                ChildFields = new List<BizField>()
                {
                    new BizField("Address")
                    {
                        Value = "",
                        IsACollection = true,
                        ChildFields = new List<BizField>()
                        {
                            new BizField("Street")
                            {
                                IsACollection = true,
                                Value = "100 Main St"
                            },
                            new BizField("Type")
                            {
                                IsACollection = true,
                                Value = "1"
                            },
                            new BizField("Zip")
                            {
                                IsACollection = true,
                                Value = ""
                            }
                        }
                    },
                    new BizField("Address")
                    {
                        Value = "",
                        IsACollection = true,
                        ChildFields = new List<BizField>()
                        {
                            new BizField("Street")
                            {
                                IsACollection = true,
                                Value = "202 Main St"
                            },
                            new BizField("Type")
                            {
                                IsACollection = true,
                                Value = "2"
                            },
                            new BizField("Zip")
                            {
                                IsACollection = true,
                                Value = "12345"
                            }
                        }
                    }
                }
            };

            TestHelpers.ScrubId(expected);
            TestHelpers.ScrubId(mergedField);

            // Assert
            Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(mergedField));
            Assert.IsTrue(
                mergedField.ChildFields.Count == 2
                && mergedField.ChildFields[1].ChildFields[2].Value == "12345");
        }

        [TestMethod]
        public void MergeJObjectToBizField_IfFieldIsMergedWithNoValues_ItMatchesConvertedField()
        {
            var testJson =
@"
{
	""Customer"": {
		""Name"": """",
		""Age"": """",
		""Address"": [
			{
				""Street"": """",
				""Type"": """"
			}
		]
	}
}
";

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);
            // Assert
            Assert.AreEqual(JsonConvert.SerializeObject(initialField), JsonConvert.SerializeObject(mergedField));
        }

        [TestMethod]
        public void ConvertBizFieldToJObject_IfBizFieldIsConvertedWithDouble_ItMatchesTheInput()
        {
            var testJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.4
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2.3,
				""Zip"": 12345
			}
		]
	}
}
";

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);
            var backToJson = BizField_Helpers.ConvertBizFieldToJToken(mergedField);

            var expectedJson =
@"
{
	""Customer"": {
		""Name"": ""smith"",
		""Age"": 55,
		""Address"": [
			{
				""Street"": ""100 Main St"",
				""Type"": 1.4,
                ""Zip"": """"
			},
			{
				""Street"": ""202 Main St"",
				""Type"": 2.3,
				""Zip"": 12345
			}
		]
	}
}
";

            dynamic expectedDynamic = JObject.Parse(expectedJson);
            dynamic actualDynamic = JObject.Parse(JsonConvert.SerializeObject(backToJson));

            // Assert using the dynamic objects
            Assert.AreEqual(expectedDynamic.ToString(), actualDynamic.ToString());
        }

        [TestMethod()]
        public void MergeJObjectToBizField_IfObjectIsSingleProperty_ItMergesSuccessfully()
        {
            var testJson =
@"
{
	""Customer"": ""woot""
}
";

            JObject jObject = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jObject, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jObject, initialField);
            var backToJson = BizField_Helpers.ConvertBizFieldToJToken(mergedField);

            var expectedJson =
@"
{
	""Customer"": ""woot""
}
";

            dynamic expectedDynamic = JObject.Parse(expectedJson);
            dynamic actualDynamic = JObject.Parse(JsonConvert.SerializeObject(backToJson));

            // Assert using the dynamic objects
            Assert.AreEqual(expectedDynamic.ToString(), actualDynamic.ToString());
        }

        [TestMethod()]
        public void MergeJObjectToBizField_IfProvidedObjectIsAnArrayOfObjectsWithinOneParent_ItMergesSuccessfully()
        {
            var testJson =
@"
{
    ""test"":
        [
            {
                ""id"":1,
                ""firstName"":""Susan"",
                ""lastName"":""Jordon"",
                ""email"":""susan@example.com"",
                ""salary"":95000,
                ""date"":""2019-04-11""
            },
            {
                ""id"":2,
                ""firstName"":""Adrienne"",
                ""lastName"":""Doak"",
                ""email"":""adrienne@example.com"",
                ""salary"":80000,
                ""date"":""2019-04-17""
            },
            {
                ""id"":3,
                ""firstName"":""Rolf"",
                ""lastName"":""Hegdal"",
                ""email"":""rolf@example.com"",
                ""salary"":79000,
                ""date"":""2019-05-01""
            },
            {
                ""id"":4,
                ""firstName"":""Kent"",
                ""lastName"":""Rosner"",
                ""email"":""kent@example.com"",
                ""salary"":56000,
                ""date"":""2019-05-03""
            }
        ]
}
";

            JToken jToken = JObject.Parse(testJson);

            var initialField = BizField_Helpers.ConvertJTokenToBizField(jToken, Guid.NewGuid());
            var mergedField = BizField_Helpers.MergeJTokenToBizField(jToken, initialField);
            var backToJson = BizField_Helpers.ConvertBizFieldToJToken(mergedField);

            dynamic expectedDynamic = JObject.Parse(testJson);
            dynamic actualDynamic = JObject.Parse(JsonConvert.SerializeObject(backToJson));

            // Assert using the dynamic objects
            Assert.AreEqual(expectedDynamic.ToString(), actualDynamic.ToString());
        }

        [TestMethod()]
        public async Task SelectCompany_IfApiKeyHasBadFieldId_FieldIdIsCorrected()
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

            var newApiKey = new BizApiKey(company.Id, Guid.NewGuid());

            await service.SaveApiKey(newApiKey);

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

            TestHelpers.PopulateMockClaimsPrincipalForController(companyController, out var userEmail);

            _ = await ControllerHelpers.SelectCompany(
                companyController, service, httpContextAccessor.HttpContext, settings.Value, userEmail, company.Id.ToString(), true);

            var apiKeyResult = await service.GetApiKeysForCompany(company.Id);
            var topLevelField = (await service.GetTopLevelFieldsForCompany(company.Id))[0];

            Assert.IsTrue(apiKeyResult.All(x => x.TopLevelFieldId == topLevelField.Id || x.TopLevelFieldId == Guid.Empty));
        }

        [TestMethod()]
        public void GenerateNewCompany_IfDefaultIdIsProvided_ItIsUsed()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);

            var testId = Guid.NewGuid().ToString();

            var newCompany =
                ControllerHelpers.GenerateNewCompany(
                    service,
                    _config,
                    new BizUser(Guid.NewGuid().ToString("N"), UserRole.Administrator),
                    true,
                    testId
                    );
        }
    }
}