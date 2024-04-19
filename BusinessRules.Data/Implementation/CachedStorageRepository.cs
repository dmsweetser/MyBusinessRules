using BusinessRules.Domain.Data;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace BusinessRules.Data.Implementation
{
    public class CachedStorageRepository : IRepository
    {
        private static ConcurrentDictionary<string, BizApiKey> ApiKeyCache = new();
        private static ConcurrentDictionary<string, BizUserToCompany> UserToCompanyCache = new();
        private static ConcurrentDictionary<string, BizCompany> CompanyCache = new();
        private static ConcurrentDictionary<string, BizField> FieldCache = new();
        private static ConcurrentDictionary<string, BizRule> RuleCache = new();
        private static ConcurrentDictionary<string, BizCreditCode> CreditCodeCache = new();

        private IRepository _repository;
        private IBackgroundStorageRepository _backgroundStorageRepository;

        public CachedStorageRepository(
                IRepository repository,
                IBackgroundStorageRepository backgroundStorageRepository
            )
        {
            _repository = repository;
            _backgroundStorageRepository = backgroundStorageRepository;
        }

        public static void ClearCache()
        {
            ApiKeyCache = new();
            UserToCompanyCache = new();
            CompanyCache = new();
            FieldCache = new();
            RuleCache = new();
        }

        public async Task<BizApiKey> GetApiKey(string apiKeyId)
        {
            if (!ApiKeyCache.TryGetValue(apiKeyId + apiKeyId, out var cacheValue))
            {
                var tableValue = await _repository.GetApiKey(apiKeyId);
                if (tableValue is null) return new NullBizApiKey();
                ApiKeyCache.AddOrUpdate(tableValue.Id.ToString() + tableValue.Id.ToString(), tableValue, (key, oldValue) => tableValue);
                return tableValue;
            }
            else
            {
                return cacheValue;
            };
        }

        public async Task<BizUserToCompany> GetUserToCompanyReference(string userId)
        {
            if (!UserToCompanyCache.TryGetValue(userId + userId, out var cacheValue))
            {
                var tableValue = await _repository.GetUserToCompanyReference(userId);
                if (tableValue is null) return new NullBizUserToCompany();
                UserToCompanyCache.AddOrUpdate(tableValue.Id.ToString() + tableValue.Id.ToString(), tableValue, (key, oldValue) => tableValue);
                return tableValue;
            }
            else
            {
                return cacheValue;
            };
        }

        public async Task<BizCompany> GetCompany(string companyId)
        {
            if (!CompanyCache.TryGetValue(companyId + companyId, out var cacheValue))
            {
                var tableValue = await _repository.GetCompany(companyId);
                if (tableValue is NullBizCompany) return new NullBizCompany();
                CompanyCache.AddOrUpdate(tableValue.Id.ToString() + tableValue.Id.ToString(), tableValue, (key, oldValue) => tableValue);
                return tableValue;
            }
            else
            {
                return cacheValue;
            };
        }

        public async Task<BizField> GetTopLevelField(string companyId, string fieldId)
        {
            if (!FieldCache.TryGetValue(companyId + fieldId, out var cacheValue))
            {
                var tableValue = await _repository.GetTopLevelField(companyId, fieldId);
                if (tableValue is NullBizField) return NullBizField.GetInstance();
                FieldCache.AddOrUpdate(tableValue.Id.ToString() + tableValue.Id.ToString(), tableValue, (key, oldValue) => tableValue);
                return tableValue;
            }
            else
            {
                return cacheValue;
            };
        }

        public async Task<BizRule> GetRule(string fieldId, string ruleId)
        {
            if (!RuleCache.TryGetValue(fieldId + ruleId, out var cacheValue))
            {
                var tableValue = await _repository.GetRule(fieldId, ruleId);
                if (tableValue is null) return new NullBizRule();
                RuleCache.AddOrUpdate(tableValue.Id.ToString() + tableValue.Id.ToString(), tableValue, (key, oldValue) => tableValue);
                return tableValue;
            }
            else
            {
                return cacheValue;
            };
        }

        public async Task<BizCreditCode> GetCreditCode(string codeId)
        {
            if (!CreditCodeCache.TryGetValue(codeId + codeId, out var cacheValue))
            {
                var tableValue = await _repository.GetCreditCode(codeId);
                if (tableValue is null) return null;
                CreditCodeCache.AddOrUpdate(
                    tableValue.Id.ToString() + tableValue.Id.ToString(), tableValue, (key, oldValue) => tableValue);
                return tableValue;
            }
            else
            {
                return cacheValue;
            };
        }

        public async Task<BizUser> GetUser(string companyId, string userId)
        {
            return await Task.Run(async () =>
            {
                var foundCompany = await GetCompany(companyId);
                return foundCompany.Users.FirstOrDefault(x => x.EmailAddress == userId);
            });
        }

        public async Task SaveApiKey(BizApiKey currentApiKey)
        {
            await Task.Run(() => {
                ApiKeyCache.AddOrUpdate(
                    currentApiKey.Id.ToString() + currentApiKey.Id.ToString(),
                    currentApiKey,
                    (key, oldValue) => currentApiKey);
                _backgroundStorageRepository.Execute(async (repo) => await repo.SaveApiKey(currentApiKey));
            });
        }

        public async Task SaveCompany(BizCompany currentCompany)
        {
            await Task.Run(() =>
            {
                CompanyCache.AddOrUpdate(
                currentCompany.Id.ToString() + currentCompany.Id.ToString(),
                currentCompany,
                (key, oldValue) => currentCompany);
                var serializedCompany = JsonConvert.SerializeObject(currentCompany);
                _backgroundStorageRepository.Execute(async (repo) => await repo.SaveCompany(JsonConvert.DeserializeObject<BizCompany>(serializedCompany)));
            });
        }

        public async Task SaveTopLevelField(string companyId, BizField currentField)
        {
            await Task.Run(() =>
            {
                FieldCache.AddOrUpdate(
                companyId + currentField.Id.ToString(),
                currentField,
                (key, oldValue) => currentField);
                _backgroundStorageRepository.Execute(async (repo) => await repo.SaveTopLevelField(companyId, currentField));
            });
        }

        public async Task SaveRule(string fieldId, BizRule currentRule)
        {
            await Task.Run(() =>
            {
                RuleCache.AddOrUpdate(
                fieldId + currentRule.Id.ToString(),
                currentRule,
                (key, oldValue) => currentRule);
                _backgroundStorageRepository.Execute(async (repo) => await repo.SaveRule(fieldId, currentRule));
            });
        }

        public async Task SaveCreditCode(BizCreditCode currentCreditCode)
        {
            await Task.Run(() =>
            {
                CreditCodeCache.AddOrUpdate(
                currentCreditCode.Id.ToString() + currentCreditCode.Id.ToString(),
                currentCreditCode,
                (key, oldValue) => currentCreditCode);
                _backgroundStorageRepository.Execute(async (repo) => await repo.SaveCreditCode(currentCreditCode));
            });
        }

        public async Task SaveUserToCompanyReference(BizUserToCompany currentUserToCompany)
        {
            await Task.Run(() =>
            {
                UserToCompanyCache.AddOrUpdate(
                currentUserToCompany.UserId + currentUserToCompany.UserId,
                currentUserToCompany,
                (key, oldValue) => currentUserToCompany);
                _backgroundStorageRepository.Execute(async (repo) => await repo.SaveUserToCompanyReference(currentUserToCompany));
            });
        }

        public async Task DeleteApiKey(string currentApiKeyId)
        {
            ApiKeyCache.TryRemove(currentApiKeyId + currentApiKeyId, out _);
            await _repository.DeleteApiKey(currentApiKeyId);
        }

        public async Task DeleteCompany(string companyId)
        {
            CompanyCache.TryRemove(companyId + companyId, out _);
            await _repository.DeleteCompany(companyId);
        }

        public async Task DeleteTopLevelField(string companyId, string currentFieldId)
        {
            FieldCache.TryRemove(companyId + currentFieldId, out _);
            await _repository.DeleteTopLevelField(companyId, currentFieldId);
        }

        public async Task DeleteRule(string fieldId, string currentRuleId)
        {
            RuleCache.TryRemove(fieldId + currentRuleId, out _);
            await _repository.DeleteRule(fieldId, currentRuleId);
        }

        public async Task DeleteUserToCompanyReference(string userId)
        {
            UserToCompanyCache.TryRemove(userId + userId, out _);
            await _repository.DeleteUserToCompanyReference(userId);
        }
    }
}
