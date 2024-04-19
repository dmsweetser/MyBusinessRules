using BusinessRules.Domain.Common;
using BusinessRules.Domain.Data;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Text;
using Azure.Data.Tables;

namespace BusinessRules.Data.Implementation.AzureBlobStorage
{
    public class AzureBlobStorageRepository : IRepository
    {
        private readonly string _connectionString;
        private readonly BlobContainerClient _blobContainerClient;

        public AzureBlobStorageRepository(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _blobContainerClient = new BlobContainerClient(_connectionString, containerName);

            if (!_blobContainerClient.Exists())
            {
                _blobContainerClient.CreateIfNotExists();
            }
        }

        public async Task DeleteApiKey(string apiKeyId)
        {
            await DeleteBlobAsync<BizApiKey>(ApiKeyPath.Invoke(Guid.Parse(apiKeyId)));
        }

        public async Task DeleteCompany(string companyId)
        {
            await DeleteBlobAsync<BizCompany>(CompanyPath.Invoke(Guid.Parse(companyId)));
        }

        public async Task DeleteRule(string fieldId, string currentRuleId)
        {
            await DeleteBlobAsync<BizRule>(RulePath.Invoke(Guid.Parse(currentRuleId)));
        }

        public async Task DeleteTopLevelField(string companyId, string currentFieldId)
        {
            await DeleteBlobAsync<BizField>(FieldPath.Invoke(Guid.Parse(currentFieldId)));
        }

        public async Task DeleteUserToCompanyReference(string userId)
        {
            await DeleteBlobAsync<BizUserToCompany>(UserToCompanyPath.Invoke(userId));
        }

        public async Task<BizApiKey> GetApiKey(string apiKeyId)
        {
            var result = await GetBlobAsync<BizApiKey>(ApiKeyPath.Invoke(Guid.Parse(apiKeyId)));
            if (result is null) return new NullBizApiKey();
            return result;
        }

        public async Task<BizCompany> GetCompany(string companyId)
        {
            var result = await GetBlobAsync<BizCompany>(CompanyPath.Invoke(Guid.Parse(companyId)));
            if (result is null) return new NullBizCompany();
            return result;
        }

        public async Task<BizCreditCode> GetCreditCode(string codeId)
        {
            var result = await GetBlobAsync<BizCreditCode>(CreditCodePath.Invoke(Guid.Parse(codeId)));
            return result;
        }

        public async Task<BizRule> GetRule(string fieldId, string ruleId)
        {
            var result = await GetBlobAsync<BizRule>(RulePath.Invoke(Guid.Parse(ruleId)));
            if (result is null) return new NullBizRule();
            return result;
        }

        public async Task<BizField> GetTopLevelField(string companyId, string fieldId)
        {
            var result = await GetBlobAsync<BizField>(FieldPath.Invoke(Guid.Parse(fieldId)));
            if (result is null) return new NullBizField();
            return result;
        }

        public async Task<BizUser> GetUser(string companyId, string userId)
        {
            var companyBlob = await GetBlobAsync<BizCompany>(CompanyPath.Invoke(Guid.Parse(companyId)));

            if (companyBlob is null) return null;

            return companyBlob.Users.FirstOrDefault(x => x.EmailAddress.ToString() == userId);
        }

        public async Task<BizUserToCompany> GetUserToCompanyReference(string userId)
        {
            var result = await GetBlobAsync<BizUserToCompany>(UserToCompanyPath.Invoke(userId));
            if (result is null) return new NullBizUserToCompany();
            return result;
        }

        public async Task SaveApiKey(BizApiKey currentApiKey)
        {
            await SaveBlobAsync(ApiKeyPath.Invoke(currentApiKey.Id), currentApiKey);
        }

        public async Task SaveCompany(BizCompany currentCompany)
        {
            await SaveBlobAsync(CompanyPath.Invoke(currentCompany.Id), currentCompany);
        }

        public async Task SaveCreditCode(BizCreditCode creditCode)
        {
            await SaveBlobAsync(CreditCodePath.Invoke(creditCode.Id), creditCode);
        }

        public async Task SaveRule(string fieldId, BizRule currentRule)
        {
            await SaveBlobAsync(RulePath.Invoke(currentRule.Id), currentRule);
        }

        public async Task SaveTopLevelField(string companyId, BizField currentField)
        {
            await SaveBlobAsync(FieldPath.Invoke(currentField.Id), currentField);
        }

        public async Task SaveUserToCompanyReference(BizUserToCompany currentUserToCompany)
        {
            await SaveBlobAsync(UserToCompanyPath.Invoke(currentUserToCompany.UserId), currentUserToCompany);
        }

        private async Task<T> GetBlobAsync<T>(string blobName) where T : IHaveAnId
        {
            try
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
                BlobDownloadInfo downloadInfo = await blobClient.DownloadAsync();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await downloadInfo.Content.CopyToAsync(memoryStream);
                    return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(memoryStream.ToArray()));
                }
            }
            catch (Exception)
            {
                //Swallow the exception for now - we will just fall through and return default(T)
            }

            return default(T);
        }

        private async Task DeleteBlobAsync<T>(string blobName) where T : IHaveAnId
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        private async Task SaveBlobAsync<T>(string blobName, T newRecord) where T : IHaveAnId
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            string json = JsonConvert.SerializeObject(newRecord);

            await blobClient.UploadAsync(new MemoryStream(new UTF8Encoding().GetBytes(json)), overwrite: true);
        }

        private Func<Guid, string> ApiKeyPath = (x) => string.Concat("ApiKey_", x.ToString("N"), ".json");
        private Func<Guid, string> CompanyPath = (x) => string.Concat("Company_", x.ToString("N"), ".json");
        private Func<Guid, string> FieldPath = (x) => string.Concat("Field_", x.ToString("N"), ".json");
        private Func<Guid, string> RulePath = (x) => string.Concat("Rule_", x.ToString("N"), ".json");
        private Func<Guid, string> CreditCodePath = (x) => string.Concat("CreditCode_", x.ToString("N"), ".json");
        private Func<string, string> UserToCompanyPath = (x) => string.Concat("UserToCompany_", x.ToString(), ".json");
    }
}