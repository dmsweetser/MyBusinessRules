using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Domain.Services;
using Newtonsoft.Json;
using System.Globalization;

namespace BusinessRules.Domain.Fields
{
    public static class BizField_Extensions
    {
        public static string GetValue(this BizField currentField)
        {
            return currentField.Value;
        }

        public static void SetValue(this BizField currentField, string newValue)
        {
            currentField.Value = newValue;
        }

        public static BizField GetChildFieldByName(this BizField parentField, string name)
        {
            if (parentField.SystemName.ToString() == name)
            {
                return parentField;
            }

            foreach (var child in parentField.ChildFields)
            {
                var childFoundField = GetChildFieldByName(child, name);
                if (childFoundField is not NullBizField) return childFoundField;
            }

            //This needs to be null for upstream callers to handle things properly
            return NullBizField.GetInstance();
        }

        public static List<BizField> GetChildFieldsByName(this BizField parentField, string name)
        {
            var foundFields = new List<BizField>();
            IterateAndFindChildFieldsByName(parentField, name, ref foundFields);
            return foundFields;
        }

        private static void IterateAndFindChildFieldsByName(BizField parentField, string name, ref List<BizField> foundFields)
        {
            if (parentField.SystemName.ToString() == name)
            {
                foundFields.Add(parentField);
            }

            foreach (var child in parentField.ChildFields)
            {
                IterateAndFindChildFieldsByName(child, name, ref foundFields);
            }
        }

        public static BizField GetChildFieldById(this BizField parentField, Guid id)
        {
            if (parentField.Id == id) return parentField;

            foreach (var child in parentField.ChildFields)
            {
                if (child.Id == id) return child;

                var foundChild = GetChildFieldById(child, id);
                if (foundChild is not NullBizField)
                {
                    return foundChild;
                }
            }

            return NullBizField.GetInstance();
        }

        public static void GetChildFieldMultiplesById(this BizField parentField, string id, List<BizField> results)
        {
            // Use the same logic as before, but do not cache the results
            if (parentField.Id.ToString("D", CultureInfo.InvariantCulture).Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(parentField);
            }

            foreach (var child in parentField.ChildFields)
            {
                GetChildFieldMultiplesById(child, id, results);
            }
        }


        public static void AddChildField(this BizField parentField, BizField childField)
        {
            //The field is always cloned so we don't inadvertently pass by reference
            //In theory, the client could send multiples of a certain field and we need to handle that
            var clonedField = JsonConvert.DeserializeObject<BizField>(JsonConvert.SerializeObject(childField));
            clonedField.ParentFieldId = parentField.Id;
            clonedField.TopLevelFieldId = parentField.TopLevelFieldId;
            clonedField.IsACollection = parentField.IsACollection;
            parentField.ChildFields.Add(clonedField);
        }

        public static void RemoveChildField(this BizField parentField, BizField childField)
        {
            if (childField == null)
            {
                throw new ArgumentException("Child field is null");
            }

            foreach (var currentChildField in parentField.ChildFields)
            {
                if (currentChildField.Id == childField.Id)
                {
                    parentField.ChildFields.Remove(childField);
                    return;
                }

                RemoveChildField(currentChildField, childField);
            }
        }

        public static BaseComponent GetComponentById(this BizRule currentRule, Guid componentId)
        {
            var foundComponent = currentRule.RuleSequence.FirstOrDefault(x => x.Id == componentId);
            if (foundComponent == null)
            {
                throw new ArgumentException($"Component not found with ID {componentId}");
            }
            return foundComponent;
        }

        public static Dictionary<Guid, string> FlattenFieldsWithDescription(this BizField root, bool clearCache = false)
        {
            if (!BizField.FlattenedFieldsCache.TryGetValue(root.TopLevelFieldId, out var flattenedFields) || clearCache) {
                flattenedFields = new();
                FlattenBizFieldsRecursive(root, string.Empty, flattenedFields);
                BizField.FlattenedFieldsCache.AddOrUpdate(root.TopLevelFieldId, flattenedFields, (key, oldValue) => flattenedFields);
            }
            return flattenedFields;
        }

        private static void FlattenBizFieldsRecursive(BizField field, string parentName, Dictionary<Guid, string> flattenedFields)
        {
            string fieldName = string.IsNullOrEmpty(field.FriendlyName) ? field.SystemName : field.FriendlyName;

            if (!string.IsNullOrEmpty(parentName))
            {
                fieldName = string.Concat(parentName, ".", fieldName);
            }

            if (field.DisplayForBusinessUser && field.ChildFields.Count == 0)
            {
                flattenedFields[field.Id] = fieldName;
            }

            foreach (var childField in field.ChildFields)
            {
                FlattenBizFieldsRecursive(childField, fieldName, flattenedFields);
            }
        }

        public static DeveloperDTO ToDeveloperDTO(
            this BizField currentField,
            Guid companyId,
            IBusinessRulesService service,
            AppSettings settings)
        {
            var foundRules = service.GetRules(companyId, currentField.Id).Result;
            return new DeveloperDTO(
                    currentField,
                    foundRules.Select(x => x.Id).ToList(),
                    settings.BaseEndpointUrl);
        }

        public static EndUserDTO ToEndUserDTO(
            this BizField currentField,
            Guid currentApiKey,
            AppSettings settings)
        {
            return new EndUserDTO(
                    currentField,
                    currentApiKey,
                    settings.BaseEndpointUrl);
        }
    }
}
