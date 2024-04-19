using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Rules;
using BusinessRules.Rules.Extensions;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.Tests;
using BusinessRules.Tests.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BusinessRules.UI.Controllers;
using BusinessRules.Rules.Components;
using BusinessRules.UI.Common;
using Moq;
using Newtonsoft.Json;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Stripe;

namespace BusinessRules.ServiceLayer.Tests
{
    [TestClass()]
    public class BusinessRulesServiceTests
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
        public async Task SaveRulesForTopLevelField_IfRulesAreSaved_ThenRulesCanBeRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            await service.SaveRule(company.Id, newRule);
            var foundRules = await service.GetRules(company.Id, newField.Id);
            Assert.IsTrue(foundRules.Count > 0);
        }

        [TestMethod()]
        public void CreateNewRule_IfFieldIsNotTopLevel_ThenArgumentExceptionIsThrown()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var foundChild = newField.GetChildFieldById(newChild.Id);
                var newRule = new BizRule("test", foundChild);
            });
        }

        [TestMethod()]
        public async Task SaveTopLevelField_IfFieldIsSaved_FieldCanBeRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            Assert.IsTrue(await service.GetTopLevelField(company.Id, newField.Id) != null);
        }

        [TestMethod()]
        public async Task TryGetTopLevelField_IfTopLevelFieldExists_ReturnsTrue()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            var foundField = await service.GetTopLevelField(company.Id, newField.Id);
            Assert.IsTrue(foundField != null);
        }

        [TestMethod()]
        public async Task TryGetTopLevelField_IfTopLevelFieldDoesNotExist_ArgumentExceptionIsThrown()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            await service.SaveCompany(company);

            var encounteredException = false;
            try
            {
                await service.GetTopLevelField(company.Id, newField.Id);
            }
            catch (ArgumentException)
            {
                encounteredException = true;
            }

            Assert.IsTrue(encounteredException);
        }

        [TestMethod()]
        public async Task GetUser_IfUserExists_UserIsRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            await service.SaveCompany(company);
            Assert.IsNotNull(await service.GetUser(company.Id, newUser.EmailAddress));
        }


        [TestMethod()]
        public async Task GenerateSessionForStripeCustomer_IfCustomerIdExists_SessionIsCreated()
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

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

            _ = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            var result = await service.GenerateSessionForStripeCustomer(settings.Value.StripeBillingApiKey, foundCompany.BillingId, "https://localhost");

            Assert.IsTrue(!string.IsNullOrWhiteSpace(result));
        }

        [TestMethod()]
        public async Task SaveRulesForTopLevelField_IfRulesAreSavedWithoutCache_ThenRulesCanBeRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            await service.SaveRule(company.Id, newRule);
            var foundRules = await service.GetRules(company.Id, newField.Id);
            Assert.IsTrue(foundRules.Count > 0);
        }

        [TestMethod()]
        public void CreateNewRule_IfFieldIsNotTopLevelWithoutCache_ThenArgumentExceptionIsThrown()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var foundChild = newField.GetChildFieldById(newChild.Id);
                var newRule = new BizRule("test", foundChild);
            });
        }

        [TestMethod()]
        public async Task SaveTopLevelField_IfFieldIsSavedWithoutCache_FieldCanBeRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            Assert.IsTrue(await service.GetTopLevelField(company.Id, newField.Id) != null);
        }

        [TestMethod()]
        public async Task TryGetTopLevelField_IfTopLevelFieldExistsWithoutCache_ReturnsTrue()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            var foundField = await service.GetTopLevelField(company.Id, newField.Id);
            Assert.IsTrue(foundField != null);
        }

        [TestMethod()]
        public async Task TryGetTopLevelField_IfTopLevelFieldDoesNotExistWithoutCache_ArgumentExceptionIsThrown()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            await service.SaveCompany(company);

            var encounteredException = false;
            try
            {
                await service.GetTopLevelField(company.Id, newField.Id);
            }
            catch (ArgumentException)
            {
                encounteredException = true;
            }

            Assert.IsTrue(encounteredException);
        }

        [TestMethod()]
        public async Task GetUser_IfUserExistsWithoutCache_UserIsRetrieved()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            await service.SaveCompany(company);
            var result = await service.GetUser(company.Id, newUser.EmailAddress);
            Assert.IsNotNull(result);
        }


        [TestMethod()]
        public async Task GenerateSessionForStripeCustomer_IfCustomerIdExistsWithoutCache_SessionIsCreated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new MockHttpSession()
                }
            };

            httpContextAccessor.HttpContext.Request.Scheme = "https";
            httpContextAccessor.HttpContext.Request.Host = new HostString("mybusinessrules-qa.azurewebsites.net");

            var settings = Options.Create(TestHelpers.InitConfiguration());

            var homeController =
                new HomeController(new NullLogger<HomeController>(), service, httpContextAccessor, settings, _renderer);

            TestHelpers.PopulateMockClaimsPrincipalForController(homeController, out var userEmail);

            _ = await homeController.Dashboard() as ViewResult;

            var foundCompany = await service.GetCompanyForUser(userEmail);

            var result = await service.GenerateSessionForStripeCustomer(settings.Value.StripeBillingApiKey, foundCompany.BillingId, "https://localhost");

            Assert.IsTrue(!string.IsNullOrWhiteSpace(result));
        }

        [TestMethod()]
        public async Task GetCompanyForApiKey_IfApiKeyDoesNotExist_ThrowsArgumentException()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);

            var exceptionEncountered = false;
            try
            {
                await service.GetCompanyForApiKey(Guid.NewGuid());
            }
            catch (ArgumentException)
            {
                exceptionEncountered = true;
            }
            Assert.IsTrue(exceptionEncountered);
        }

        [TestMethod()]
        public async Task GetFieldsAndRulesForCompany_IfNoFieldsArePresent_NewFieldIsAdded()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            await service.SaveCompany(company);

            var foundFields = await service.GetFieldsAndRulesForCompany(company.Id);

            Assert.IsTrue(foundFields.Count == 1);
        }

        [TestMethod()]
        public async Task AddNewChildField_IfChildFieldIsAdded_ChildFieldIsPresent()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);

            await service.AddNewChildField(company.Id, newField.Id, newField.Id);

            var foundField = await service.GetTopLevelField(company.Id, newField.Id);

            Assert.IsTrue(foundField.ChildFields.Count == 1);
        }

        [TestMethod()]
        public async Task GetApiKeysForCompany_IfApiKeyIdsAreNull_NewListIsCreated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            company.ApiKeyIds = null;
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            await service.SaveRule(company.Id, newRule);

            _ = await service.GetApiKeysForCompany(company.Id);

            var updatedCompany = await service.GetCompany(company.Id);

            Assert.IsTrue(updatedCompany.ApiKeyIds is not null && updatedCompany.ApiKeyIds.Count == 0);
        }

        [TestMethod()]
        public async Task SaveCompany_IfUserIsAlreadyAssignedToAnotherCompany_ThrowsArgumentException()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);
            await service.SaveRule(company.Id, newRule);

            var otherCompany = new BizCompany(Guid.NewGuid().ToString());
            otherCompany.Users.Add(newUser);

            var exceptionEncountered = false;

            try
            {
                await service.SaveCompany(otherCompany);
            }
            catch (ArgumentException)
            {
                exceptionEncountered = true;
            }

            Assert.IsTrue(exceptionEncountered);
        }


        [TestMethod()]
        public async Task DeleteTopLevelField_IfFieldExists_FieldIsDeleted()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newAttribute = new BizField("attrib");
            newField.AddChildField(newAttribute);
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);

            await service.DeleteTopLevelField(company.Id, newField.Id);

            var exceptionEncountered = false;

            try
            {
                var foundField = await service.GetTopLevelField(company.Id, newField.Id);
            }
            catch (ArgumentException)
            {
                exceptionEncountered = true;
            }

            Assert.IsTrue(exceptionEncountered);
        }

        [TestMethod()]
        public async Task DeleteTopLevelField_IfRuleReferencesTopLevelField_ExceptionOccurs()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newAttribute = new BizField("attrib");
            newField.AddChildField(newAttribute);
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);

            var newRule = new BizRule("test", newField);
            newRule.Add(newRule.Then().FirstOrDefault());
            newRule.Add(new FieldOperand().WithArgumentValues(new string[] { newField.Id.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() }));
            await service.SaveRule(company.Id, newRule);

            var exceptionEncountered = false;

            try
            {
                await service.DeleteTopLevelField(company.Id, newField.Id);
            }
            catch (Exception)
            {
                exceptionEncountered = true;
            }

            Assert.IsTrue(exceptionEncountered);
        }

        [TestMethod()]
        public async Task DeleteTopLevelField_IfApiKeyExists_ApiKeyIsDisassociated()
        {
            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);
            var company = new BizCompany(Guid.NewGuid().ToString());
            var newUser = new BizUser(Guid.NewGuid().ToString(), UserRole.Administrator);
            company.Users.Add(newUser);
            var newField = new BizField("test");
            var newChild = new BizField("child");
            newField.AddChildField(newChild);
            var newAttribute = new BizField("attrib");
            newField.AddChildField(newAttribute);
            await service.SaveCompany(company);
            await service.SaveTopLevelField(company.Id, newField);

            var newApiKey = new BizApiKey(company.Id, newField.Id);
            await service.SaveApiKey(newApiKey);

            await service.DeleteTopLevelField(company.Id, newField.Id);

            var exceptionEncountered = false;

            try
            {
                var foundField = await service.GetTopLevelField(company.Id, newField.Id);
            }
            catch (ArgumentException)
            {
                exceptionEncountered = true;
            }

            var foundApiKeys = await service.GetApiKeysForCompany(company.Id);

            Assert.IsTrue(exceptionEncountered && foundApiKeys.All(x => x.TopLevelFieldId == Guid.Empty));
        }

        [TestMethod]
        public async Task UpdateBillingInfoForCompany_PaidInvoice_SuccessfulUpdate()
        {
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);

            // Arrange
            var currentCompany = new BizCompany
            {
                BillingId = "customer_id",
                LastBilledDate = new DateTime(2023, 1, 1),
                CreditsAvailable = 0
            };

            var paidInvoice = new Invoice
            {
                StatusTransitions = new InvoiceStatusTransitions
                {
                    PaidAt = currentCompany.LastBilledDate.AddHours(1)
                },
                Lines = new StripeList<InvoiceLineItem>
                {
                    Data = new List<InvoiceLineItem>
                {
                    new InvoiceLineItem
                    {
                        Quantity = 2,
                        Description = "(2 Credits)"
                    }
                }
                }
            };

            // Act
            var result = await service.UpdateBillingInfoForCompany(currentCompany, new List<Invoice> { paidInvoice });

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(4, currentCompany.CreditsAvailable); // Assuming initial CreditsAvailable is 0
            Assert.AreEqual(paidInvoice.StatusTransitions.PaidAt, currentCompany.LastBilledDate);
        }

        [TestMethod]
        public async Task UpdateBillingInfoForCompany_NoPaidInvoicePastLastBillingDate_CreditsAreNotApplied()
        {
            if (FeatureFlags.OfflineMode)
            {
                Assert.IsTrue(true, "Test does not apply in offline mode");
                return;
            }

            var service = new BusinessRulesService(_tableServiceClient, _backgroundStorageRepository, _config, false);

            // Arrange
            var currentCompany = new BizCompany
            {
                BillingId = "customer_id",
                LastBilledDate = new DateTime(2023, 1, 1),
                CreditsAvailable = 0
            };

            var unpaidInvoice = new Invoice
            {
                StatusTransitions = new InvoiceStatusTransitions
                {
                    PaidAt = currentCompany.LastBilledDate.AddHours(-1)
                },
                Lines = new StripeList<InvoiceLineItem>
                {
                    Data = new List<InvoiceLineItem>
                    {
                        new InvoiceLineItem
                        {
                            Quantity = 500,
                            Description = "(500 Credits)"
                        }
                    }
                }
            };

            // Act
            var result = await service.UpdateBillingInfoForCompany(currentCompany, new List<Invoice> { unpaidInvoice });

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, currentCompany.CreditsAvailable);
        }
    }
}