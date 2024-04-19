using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusinessRules.Tests.Mocks;
using BusinessRules.Domain.Rules;
using Azure.Data.Tables;
using BusinessRules.Domain.Organization;
using BusinessRules.UI.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using BusinessRules.Domain.Common;
using Newtonsoft.Json;
using BusinessRules.Domain.Services;
using BusinessRules.Domain.Fields;

namespace BusinessRules.Tests.RegressionTests
{
    [TestClass()]
    public class RegressionTests
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
                new NullLogger<BackgroundStorageRepository>(),
				_config,
                true);
        }

        [TestMethod()]
        public void Data_IfExistingSerializedDataExists_ItIsDeserializedWithoutError()
        {
            var serializedCompany =
@"{
	""Id"": ""acaa4dc3-520a-49ee-a584-e8e6ada1769c"",
	""Name"": ""fe9e02cf-eab8-4dff-8947-2d3eab2f959d"",
	""Users"": [
		{
			""Id"": ""485d890a-77af-4330-9385-ac4eda0ca213"",
			""EmailAddress"": ""063fdf22-b629-44e5-9024-1232b24a24a1@mybizrules.com"",
			""Role"": 0
		}
	],
	""FieldIds"": [
		""c7afa5f0-4f33-43c7-973c-4158fe2fef2f""
	],
	""ApiKeyIds"": [
		""74b7f06c-e171-4767-83e9-341ab9c2504a""
	],
	""BillingId"": ""cus_Od4eG1FmhEWQVG"",
	""LastBilledDate"": ""0001-01-01T00:00:00"",
	""CreditsAvailable"": 1,
	""CreditsUsed"": 0
}";

            var deserializedCompany = JsonConvert.DeserializeObject<BizCompany>(serializedCompany);

            var serializedField =
@"
{
	""Id"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
	""ParentFieldId"": ""00000000-0000-0000-0000-000000000000"",
	""WrapperFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
	""AllowedValueRegex"": """",
	""FriendlyValidationMessageForRegex"": """",
	""AllowedValues"": """",
	""SystemName"": ""4325e3e5-a790-4339-a92b-80e9505e53ac"",
	""Value"": """",
	""FriendlyName"": ""4325e3e5-a790-4339-a92b-80e9505e53ac"",
	""DisplayForBusinessUser"": true,
	""IsADateField"": false,
	""ChildFields"": [
		{
			""Id"": ""99f6f4ee-b623-46ae-9e8c-5e83557b54c9"",
			""ParentFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
			""WrapperFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
			""AllowedValueRegex"": """",
			""FriendlyValidationMessageForRegex"": """",
			""AllowedValues"": """",
			""SystemName"": ""Field 1"",
			""Value"": """",
			""FriendlyName"": ""Field 1"",
			""DisplayForBusinessUser"": true,
			""IsADateField"": false,
			""ChildFields"": [
				{
					""Id"": ""db5cc4b3-5401-483a-a6bd-270f87e9a90f"",
					""ParentFieldId"": ""99f6f4ee-b623-46ae-9e8c-5e83557b54c9"",
					""WrapperFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
					""AllowedValueRegex"": """",
					""FriendlyValidationMessageForRegex"": """",
					""AllowedValues"": """",
					""SystemName"": ""Field 2"",
					""Value"": """",
					""FriendlyName"": ""Field 2"",
					""DisplayForBusinessUser"": true,
					""IsADateField"": false,
					""ChildFields"": [],
					""RuleIds"": [],
					""DynamicComponents"": []
				},
				{
					""Id"": ""fbf5613d-0da5-4ac5-86ee-a9b809623e71"",
					""ParentFieldId"": ""99f6f4ee-b623-46ae-9e8c-5e83557b54c9"",
					""WrapperFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
					""AllowedValueRegex"": """",
					""FriendlyValidationMessageForRegex"": """",
					""AllowedValues"": """",
					""SystemName"": ""Field 3"",
					""Value"": """",
					""FriendlyName"": ""Field 3"",
					""DisplayForBusinessUser"": true,
					""IsADateField"": false,
					""ChildFields"": [
						{
							""Id"": ""d0b0c507-4861-4286-b464-1dadfc34d357"",
							""ParentFieldId"": ""fbf5613d-0da5-4ac5-86ee-a9b809623e71"",
							""WrapperFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
							""AllowedValueRegex"": """",
							""FriendlyValidationMessageForRegex"": """",
							""AllowedValues"": """",
							""SystemName"": ""test child"",
							""Value"": """",
							""FriendlyName"": ""test child"",
							""DisplayForBusinessUser"": true,
							""IsADateField"": false,
							""ChildFields"": [],
							""RuleIds"": [],
							""DynamicComponents"": []
						}
					],
					""RuleIds"": [],
					""DynamicComponents"": []
				}
			],
			""RuleIds"": [],
			""DynamicComponents"": []
		}
	],
	""RuleIds"": [
		""017ad497-21bb-4ecd-8d51-422cf4a42bd1""
	],
	""DynamicComponents"": []
}";

            var deserializedField = JsonConvert.DeserializeObject<BizField>(serializedField);


            var serializedRule =
@"{
	""Id"": ""017ad497-21bb-4ecd-8d51-422cf4a42bd1"",
	""ParentFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
	""Name"": ""test"",
	""RuleSequence"": [
		{
			""Id"": ""1f27e069-bd57-448c-aac6-72b2c26ab350"",
			""DefinitionId"": ""1bca7fb6-bf23-4dac-b787-ea2788689f32"",
			""Arguments"": []
		},
		{
			""Id"": ""1c6d68ba-8615-46f9-9b68-7715bc0e9879"",
			""DefinitionId"": ""1c4a8ca8-2f5e-42a4-ac50-0aad70881e47"",
			""Arguments"": [
				{
					""Name"": ""targetFieldId"",
					""Value"": ""d0b0c507-4861-4286-b464-1dadfc34d357"",
					""Editable"": false,
					""AllowedValueRegex"": null,
					""FriendlyValidationMessageForRegex"": null,
					""AllowedValueCommaSeparatedList"": null,
					""IsADateField"": false
				}
			]
		},
		{
			""Id"": ""19557828-3487-4e38-86b1-62dcdafb1de4"",
			""DefinitionId"": ""23066084-e15a-4309-92b9-e9f74141f884"",
			""Arguments"": []
		},
		{
			""Id"": ""16671710-0bdd-465f-9e5d-05ad18cd13da"",
			""DefinitionId"": ""a443a4bf-3d26-44c3-8a82-0d3b88c5df72"",
			""Arguments"": [
				{
					""Name"": ""fixedValue"",
					""Value"": ""woot"",
					""Editable"": true,
					""AllowedValueRegex"": null,
					""FriendlyValidationMessageForRegex"": null,
					""AllowedValueCommaSeparatedList"": null,
					""IsADateField"": false
				}
			]
		},
		{
			""Id"": ""cfa139c2-3cad-458f-8dc4-9c9eb96df0de"",
			""DefinitionId"": ""78a98833-8ec6-4436-afce-0913d1d9a863"",
			""Arguments"": []
		},
		{
			""Id"": ""86ed2924-2851-4c95-9f21-6537904af9db"",
			""DefinitionId"": ""1c4a8ca8-2f5e-42a4-ac50-0aad70881e47"",
			""Arguments"": [
				{
					""Name"": ""targetFieldId"",
					""Value"": ""db5cc4b3-5401-483a-a6bd-270f87e9a90f"",
					""Editable"": false,
					""AllowedValueRegex"": null,
					""FriendlyValidationMessageForRegex"": null,
					""AllowedValueCommaSeparatedList"": null,
					""IsADateField"": false
				}
			]
		},
		{
			""Id"": ""ef794e84-0b98-4b91-b7e5-fceca0967714"",
			""DefinitionId"": ""70caa70c-022b-41dc-b5a9-571586d77e7f"",
			""Arguments"": []
		},
		{
			""Id"": ""e9cd223c-d1ed-4dac-909f-bfca704fc4a4"",
			""DefinitionId"": ""a443a4bf-3d26-44c3-8a82-0d3b88c5df72"",
			""Arguments"": [
				{
					""Name"": ""fixedValue"",
					""Value"": ""wonky"",
					""Editable"": true,
					""AllowedValueRegex"": null,
					""FriendlyValidationMessageForRegex"": null,
					""AllowedValueCommaSeparatedList"": null,
					""IsADateField"": false
				}
			]
		}
	],
	""IsActivated"": true,
	""IsActivatedTestOnly"": false,
	""ScopedIndices"": [],
	""StartUsingOn"": ""2023-09-13T00:00:00-04:00"",
	""StopUsingOn"": ""2123-09-13T00:00:00-04:00""
}";

            var deserializedRule = JsonConvert.DeserializeObject<BizRule>(serializedRule);


			var serializedApiKey =
@"{
	""Id"": ""74b7f06c-e171-4767-83e9-341ab9c2504a"",
	""CompanyId"": ""acaa4dc3-520a-49ee-a584-e8e6ada1769c"",
	""TopLevelFieldId"": ""c7afa5f0-4f33-43c7-973c-4158fe2fef2f"",
	""AllowedDomains"": """"
}";

			var deserializedApiKey = JsonConvert.DeserializeObject<BizApiKey>(serializedApiKey);

            Assert.IsTrue(
				deserializedCompany is not null
				&& deserializedField is not null
				&& deserializedRule is not null
				&& deserializedApiKey is not null);
        }
    }
}