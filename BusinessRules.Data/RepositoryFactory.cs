using Azure.Data.Tables;
using BusinessRules.Data.Implementation;
using BusinessRules.Data.Implementation.AzureBlobStorage;
using BusinessRules.Data.Implementation.JsonStorage;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Data;
using BusinessRules.Domain.Services;

namespace BusinessRules.Data
{
    public static class RepositoryFactory
    {
        public const string JsonStorageMode = "json";
        public const string AzureBlobStorageMode = "azure_blob";
        public static IRepository GetRepository(
            TableServiceClient tableServiceClient,
            IBackgroundStorageRepository backgroundStorageRepository,
            AppSettings appSettings,
            bool useCache = true)
        {
            if (useCache && appSettings.StorageMode == JsonStorageMode)
            {
                return new CachedStorageRepository(new JsonStorageRepository(appSettings.JsonStorageBasePath), backgroundStorageRepository);
            }
            else if (appSettings.StorageMode == JsonStorageMode)
            {
                return new JsonStorageRepository(appSettings.JsonStorageBasePath);
            }
            else if (useCache && appSettings.StorageMode == AzureBlobStorageMode)
            {
                return new CachedStorageRepository(new AzureBlobStorageRepository(appSettings.AzureStorageConnectionString, appSettings.AzureBlobStorageContainerName), backgroundStorageRepository);
            }
            else if (appSettings.StorageMode == AzureBlobStorageMode)
            {
                return new AzureBlobStorageRepository(appSettings.AzureStorageConnectionString, appSettings.AzureBlobStorageContainerName);
            }
            else
            {
                throw new ArgumentException("Invalid storage mode provided");
            }
        }
    }
}
