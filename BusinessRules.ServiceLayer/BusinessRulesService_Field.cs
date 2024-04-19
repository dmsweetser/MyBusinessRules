using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Services;
using Newtonsoft.Json;

namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public async Task<BizField> GetTopLevelField(Guid companyId, Guid fieldId)
        {
            var foundField = await this.ValidateField(companyId, fieldId);
            return foundField;
        }

        public async Task<List<BizField>> GetTopLevelFieldsForCompany(Guid companyId)
        {
            var foundCompany = await Repository.GetCompany(companyId.ToString());

            var fieldsToReturn = new List<BizField>();

            foreach (var fieldId in foundCompany.FieldIds)
            {
                var foundField = await Repository.GetTopLevelField(companyId.ToString(), fieldId.ToString());
                if (foundField is not NullBizField)
                {
                    fieldsToReturn.Add(foundField);
                }
            }

            return fieldsToReturn;
        }

        public async Task SaveTopLevelField(Guid companyId, BizField currentField)
        {
            var foundCompany = await Repository.GetCompany(companyId.ToString());
            if (!foundCompany.FieldIds.Any(x => x == currentField.Id))
            {
                foundCompany.FieldIds.Add(currentField.Id);
                await Repository.SaveCompany(foundCompany);
            }

            await Repository.SaveTopLevelField(companyId.ToString(), currentField);
        }

        public async Task DeleteTopLevelField(Guid companyId, Guid fieldId)
        {
			var foundCompany = await this.ValidateCompany(companyId);

			var foundField = await this.ValidateField(companyId, fieldId);
			var foundRules = new List<BizRule>();

            foreach (var ruleId in foundField.RuleIds)
            {
                foundRules.Add(await Repository.GetRule(foundField.Id.ToString(), ruleId.ToString()));
            }

            if (foundRules.Count > 0)
            {
                throw new Exception($"Existing rules are found for field with ID {fieldId}");
            }


            var foundApiKeys = await GetApiKeysForCompany(foundCompany.Id);

            foreach (var foundApiKey in foundApiKeys)
            {
                if (foundApiKey.TopLevelFieldId.ToString() == fieldId.ToString())
                {
                    foundApiKey.TopLevelFieldId = Guid.Empty;
                    await SaveApiKey(foundApiKey);
                }
            }

            foundCompany.FieldIds.Remove(fieldId);
            await Repository.SaveCompany(foundCompany);
            await Repository.DeleteTopLevelField(companyId.ToString(), foundField.Id.ToString());
        }

        public async Task<BizField> AddNewChildField(Guid companyId, Guid topLevelFieldId, Guid parentFieldId)
        {
            var foundTopLevelField = await GetTopLevelField(companyId, topLevelFieldId);

            BizField parentField;

            if (parentFieldId == foundTopLevelField.Id)
            {
                parentField = foundTopLevelField;
            } else
            {
                parentField = foundTopLevelField.GetChildFieldById(parentFieldId);
                if (parentField is NullBizField) return NullBizField.GetInstance();
            }

            var newField = new BizField("");
            parentField.AddChildField(newField);
            await SaveTopLevelField(companyId, foundTopLevelField);
            return newField;
        }

        public async Task DeleteChildField(Guid companyId, Guid topLevelFieldId, Guid parentFieldId, Guid childFieldId)
        {
            var foundTopLevelField = await GetTopLevelField(companyId, topLevelFieldId);

            var foundChildField = foundTopLevelField.GetChildFieldById(childFieldId);

            if (foundChildField is NullBizField)
            {
                throw new ArgumentException("Child field does not exist");
            }

            var foundRules = await GetRules(companyId, topLevelFieldId);
            var rulesWithReference = foundRules.Where(x =>
                                        x.RuleSequence.Any(y =>
                                            y.Arguments.Any(z =>
                                                z.Value == childFieldId.ToString()))).ToList();
            if (rulesWithReference.Any())
            {
                throw new ArgumentException(
                    $"Child field {foundChildField.SystemName} is referenced by the following rules: " +
                    $"{JsonConvert.SerializeObject(rulesWithReference.Select(x => x.Name).ToList())}");
            }

            var parentField = foundTopLevelField.GetChildFieldById(parentFieldId);
            var childField = parentField.GetChildFieldById(childFieldId);
            parentField.RemoveChildField(childField);
            await SaveTopLevelField(companyId, foundTopLevelField);
        }
    }
}
