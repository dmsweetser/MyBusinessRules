using Azure.Data.Tables;
using Azure.Storage.Blobs.Models;
using BusinessRules.Data.Implementation.AzureBlobStorage;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BusinessRules.Admin
{
    public static class Helpers
    {
        public static async Task ResetMyCompany(BusinessRulesService service)
        {
            var foundCompany = await service.GetCompanyForUser("dmsweetser@gmail.com");
            foundCompany.FieldIds = new();
            foundCompany.ApiKeyIds = new();
            await service.SaveCompany(foundCompany);
            Console.WriteLine("Company reset successfully");
        }

        public static async Task AddCreditCodes(
            BusinessRulesService service,
            string description,
            int creditAmount,
            int quantity)
        {
            var creditCodeIds = new List<string>();
            for (int i = 0; i < quantity; i++)
            {
                var newId = Guid.NewGuid();
                creditCodeIds.Add(newId.ToString());
                var newCreditCode = new BizCreditCode(newId, creditAmount, description);
                await service.Repository.SaveCreditCode(newCreditCode);
            }
            Console.WriteLine(string.Join(',', creditCodeIds));
            Console.WriteLine("Successfully added credit codes");
        }

        public static async Task MigrateCompanyToBlob(BusinessRulesService service)
        {
            var foundCompany = await service.GetCompanyForUser("dmsweetser@gmail.com");
            var foundFields = await service.GetTopLevelFieldsForCompany(foundCompany.Id);
            var foundApiKeys = await service.GetApiKeysForCompany(foundCompany.Id);

            var settings2 = new AppSettings();
            settings2.StorageMode = "azure_blob";
            settings2.AzureStorageConnectionString = "ADD HERE";

            var blobClient = new AzureBlobStorageRepository(settings2.AzureStorageConnectionString, "mybusinessrules-prod");

            await blobClient.SaveCompany(foundCompany);

            foreach (var user in foundCompany.Users)
            {
                await blobClient.SaveUserToCompanyReference(new BizUserToCompany(user.EmailAddress, foundCompany.Id));
            }

            foreach (var field in foundFields)
            {
                await blobClient.SaveTopLevelField(foundCompany.Id.ToString(), field);
            }

            foreach (var apiKey in foundApiKeys)
            {
                await blobClient.SaveApiKey(apiKey);
            }


            Console.WriteLine("Successfully copied company to Blob storage");
        }
    }
}
