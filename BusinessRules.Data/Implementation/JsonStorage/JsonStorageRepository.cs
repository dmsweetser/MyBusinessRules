using BusinessRules.Domain.Common;
using BusinessRules.Domain.Data;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace BusinessRules.Data.Implementation.JsonStorage
{
    public class JsonStorageRepository : IRepository
    {
        readonly Func<Guid, string> ApiKeyPath = (x) => Path.Combine(_basePath, "ApiKey_" + x.ToString("N") + ".json");
        readonly Func<string, string> UserToCompanyPath = (x) => Path.Combine(_basePath, "UserToCompany_" + x + ".json");
        readonly Func<Guid, string> CompanyPath = (x) => Path.Combine(_basePath, "Company_" + x.ToString("N") + ".json");
        readonly Func<Guid, string> FieldPath = (x) => Path.Combine(_basePath, "Field_" + x.ToString("N") + ".json");
        readonly Func<Guid, string> RulePath = (x) => Path.Combine(_basePath, "Rule_" + x.ToString("N") + ".json");
        readonly Func<Guid, string> CreditCodePath = (x) => Path.Combine(_basePath, "CreditCode_" + x.ToString("N") + ".json");
        
        private static string _basePath;

        private static readonly ConcurrentDictionary<string, object> FileLock = new();

        public JsonStorageRepository(string basePath)
        {
            lock(FileLock.GetOrAdd("_", new object()))
            {
                _basePath = basePath;

                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }
            }            
        }

        public Task DeleteApiKey(string apiKeyId)
        {
            return DeleteRecord<BizApiKey>(ApiKeyPath.Invoke(Guid.Parse(apiKeyId)));
            
        }

        public Task DeleteCompany(string companyId)
        {
            return DeleteRecord<BizCompany>(CompanyPath.Invoke(Guid.Parse(companyId)));
        }

        public Task DeleteRule(string fieldId, string currentRuleId)
        {
            return DeleteRecord<BizRule>(RulePath.Invoke(Guid.Parse(currentRuleId)));
        }

        public Task DeleteTopLevelField(string companyId, string currentFieldId)
        {
            return DeleteRecord<BizField>(FieldPath.Invoke(Guid.Parse(currentFieldId)));
        }

        public Task DeleteUserToCompanyReference(string userId)
        {
            //Technically this is scary, but works because there is only one company locally
            return DeleteRecord<BizUserToCompany>(UserToCompanyPath.Invoke(userId));
        }

        public Task<BizApiKey> GetApiKey(string apiKeyId)
        {
            return Task.Run(() => 
                GetRecord<BizApiKey>(ApiKeyPath.Invoke(Guid.Parse(apiKeyId))) ?? new NullBizApiKey());
        }

        public Task<BizCompany> GetCompany(string companyId)
        {
            return Task.Run(() =>
                GetRecord<BizCompany>(CompanyPath.Invoke(Guid.Parse(companyId))) ?? new NullBizCompany());
        }

        public Task<BizCreditCode> GetCreditCode(string codeId)
        {
            return Task.Run(() =>
                GetRecord<BizCreditCode>(CreditCodePath.Invoke(Guid.Parse(codeId))));
        }

        public Task<BizRule> GetRule(string fieldId, string ruleId)
        {
            return Task.Run(() =>
                GetRecord<BizRule>(RulePath.Invoke(Guid.Parse(ruleId))) ?? new NullBizRule());
        }

        public Task<BizField> GetTopLevelField(string companyId, string fieldId)
        {
            return Task.Run(() =>
                GetRecord<BizField>(FieldPath.Invoke(Guid.Parse(fieldId))) ?? NullBizField.GetInstance());
        }

        public Task<BizUser> GetUser(string companyId, string userId)
        {
            return Task.Run(async () =>
            {
                var foundCompany = await Task.Run(() =>
                    GetRecord<BizCompany>(CompanyPath.Invoke(Guid.Parse(companyId))) ?? new NullBizCompany());

                if (foundCompany is NullBizCompany) return null;

                return foundCompany.Users.FirstOrDefault(x => x.EmailAddress.ToString() == userId);
            });
        }

        public Task<BizUserToCompany> GetUserToCompanyReference(string userId)
        {
            return Task.Run(() =>
                GetRecord<BizUserToCompany>(UserToCompanyPath.Invoke(userId)) ?? new NullBizUserToCompany());
        }

        public Task SaveApiKey(BizApiKey currentApiKey)
        {
            return SaveRecord(ApiKeyPath.Invoke(currentApiKey.Id), currentApiKey);
        }

        public Task SaveCompany(BizCompany currentCompany)
        {
            return SaveRecord(CompanyPath.Invoke(currentCompany.Id), currentCompany);
        }

        public Task SaveCreditCode(BizCreditCode creditCode)
        {
            return SaveRecord(CreditCodePath.Invoke(creditCode.Id), creditCode);
        }

        public Task SaveRule(string fieldId, BizRule currentRule)
        {
            return SaveRecord(RulePath.Invoke(currentRule.Id), currentRule);
        }

        public Task SaveTopLevelField(string companyId, BizField currentField)
        {
            return SaveRecord(FieldPath.Invoke(currentField.Id), currentField);
        }

        public Task SaveUserToCompanyReference(BizUserToCompany currentUserToCompany)
        {
            return SaveRecord(UserToCompanyPath.Invoke(currentUserToCompany.UserId), currentUserToCompany);
        }

        private Task DeleteRecord<T>(string path) where T : IHaveAnId
        {
            return Task.Run(() =>
            {
                lock (FileLock.GetOrAdd(path, new object()))
                {
                    File.WriteAllText(path, "");
                }
            });
        }

        private T GetRecord<T>(string path) where T : IHaveAnId
        {
            lock (FileLock.GetOrAdd(path, new object()))
            {
                if (File.Exists(Path.Combine(_basePath, path)))
                {
                    var storedData = File.ReadAllText(Path.Combine(_basePath, path));
                    return JsonConvert.DeserializeObject<T>(storedData);
                } else
                {
                    return default(T);
                }                
            }
        }

        private Task SaveRecord<T>(string path, T newRecord) where T : IHaveAnId
        { 
            return Task.Run(() =>
            {
                var serializedRecord = JsonConvert.SerializeObject(newRecord);
                lock (FileLock.GetOrAdd(path, new object()))
                {
                    File.WriteAllText(path, serializedRecord);
                }
            });
        }
    }
}
