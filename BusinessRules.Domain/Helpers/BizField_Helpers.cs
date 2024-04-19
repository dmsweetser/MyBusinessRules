using BusinessRules.Domain.Fields;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BusinessRules.Domain.Helpers
{
    public static class BizField_Helpers
    {

        public static BizField ConvertJTokenToBizField(JToken providedToken, Guid topLevelFieldIdToAssign)
        {
            // Assuming outerObject is initially empty
            JObject outerObject = new JObject();

            // Merge the contents of providedToken into outerObject
            outerObject.Merge(providedToken, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });

            // Create a new BizField object with the name of the root property
            BizField root = new BizField(outerObject.Properties().First().Name);
            root.Id = topLevelFieldIdToAssign;
            root.TopLevelFieldId = topLevelFieldIdToAssign;
            // Recursively convert the children of the root property
            ConvertJTokenToBizField(outerObject.Properties().First().Value, root);

            // Return the root BizField object
            return root;
        }

        private static void ConvertJTokenToBizField(JToken jToken, BizField parent)
        {
            // Check the type of the JToken
            switch (jToken.Type)
            {
                // If it is an array, create a new BizField for each property of the first element and convert its value to a BizField
                case JTokenType.Array:
                    if (jToken.Children().Count() == 0) return;
                    if (jToken.Children<JObject>().FirstOrDefault() is null) return;

                    parent.IsACollection = true;

                    foreach (var property in jToken.Children<JObject>().FirstOrDefault().Properties())
                    {
                        BizField child = new BizField(property.Name);
                        child.ParentFieldId = parent.Id;
                        child.IsACollection = true;
                        child.TopLevelFieldId = parent.TopLevelFieldId;
                        parent.ChildFields.Add(child);
                        ConvertJTokenToBizField(property.Value, child);
                    }
                    // If the array has more than one element, check if there are any additional properties and add them as child fields
                    if (jToken.Children().Count() > 1)
                    {
                        foreach (var element in jToken.Children<JObject>().Skip(1))
                        {
                            foreach (var property in element.Properties())
                            {
                                // If the parent does not have a child field with the same name as the property, create a new one and convert its value to a BizField
                                if (!parent.ChildFields.Any(c => c.SystemName == property.Name))
                                {
                                    BizField child = new BizField(property.Name);
                                    child.ParentFieldId = parent.Id;
                                    child.IsACollection = true;
                                    child.TopLevelFieldId = parent.TopLevelFieldId;
                                    parent.ChildFields.Add(child);
                                    ConvertJTokenToBizField(property.Value, child);
                                }
                            }
                        }
                    }
                    break;
                // If it is an object, create a new BizField for each property and convert its value to a BizField
                case JTokenType.Object:
                    foreach (var property in jToken.Children<JProperty>())
                    {
                        if (property.Value.Type == JTokenType.Array && property.Value.Children().Count() == 0) continue;
                        BizField child = new BizField(property.Name);
                        child.ParentFieldId = parent.Id;
                        child.IsACollection = parent.IsACollection;
                        child.TopLevelFieldId = parent.TopLevelFieldId;
                        parent.ChildFields.Add(child);
                        ConvertJTokenToBizField(property.Value, child);
                    }
                    break;
                // If it is anything else, set the value of the parent BizField to the JToken value
                default:
                    parent.Value = "";
                    break;
            }
        }

        public static BizField MergeJTokenToBizField(
            JToken providedToken, BizField initialField)
        {
            // Assuming outerObject is initially empty
            JObject outerObject = new JObject();

            // Merge the contents of providedToken into outerObject
            outerObject.Merge(providedToken, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });

            BizField mergedField;

            mergedField = new BizField(initialField.SystemName, initialField.Id.ToString());
            mergedField.IsACollection = initialField.IsACollection;
            mergedField.DynamicComponents = initialField.DynamicComponents;
            //TODO- deal with single field with no children - doesn't work
            MergeJTokenToBizField(outerObject.Properties().First().Value, initialField, mergedField);
            return mergedField;
        }

        private static void MergeJTokenToBizField(JToken jToken, BizField initialField, BizField mergedField)
        {

            // Create a dictionary to store the child fields of initialField
            Dictionary<string, BizField> childFieldDictionary =
                initialField.ChildFields.ToDictionary(cf => cf.SystemName);

            switch (jToken.Type)
            {
                case JTokenType.Object:

                    // Loop through all the properties of the object
                    foreach (var property in jToken.Children<JProperty>())
                    {
                        // Find the matching child field in the initial field by comparing the name
                        if (childFieldDictionary.TryGetValue(property.Name, out var match))
                            if (match != null)
                            {
                                if (property.Value.Type == JTokenType.Array)
                                {
                                    foreach (var element in property.Value.Children<JObject>())
                                    {
                                        // Create a new child field with the same name as the match
                                        BizField child = new BizField(match.SystemName, match.Id.ToString());
                                        child.ParentFieldId = mergedField.Id;
                                        child.IsACollection = match.IsACollection;
                                        child.TopLevelFieldId = mergedField.TopLevelFieldId;
                                        // Add the new child field to the merged field
                                        mergedField.ChildFields.Add(child);
                                        // Recursively merge the property value into the new child field
                                        MergeJTokenToBizField(element, match, child);
                                    }
                                }
                                else
                                {
                                    // Create a new child field with the same name as the match
                                    BizField child = new BizField(match.SystemName, match.Id.ToString());
                                    child.ParentFieldId = mergedField.Id;
                                    child.IsACollection = match.IsACollection;
                                    child.TopLevelFieldId = mergedField.TopLevelFieldId;
                                    // Add the new child field to the merged field
                                    mergedField.ChildFields.Add(child);
                                    // Recursively merge the property value into the new child field
                                    MergeJTokenToBizField(property.Value, match, child);
                                }
                            }
                    }

                    // Create a hash set to store the system names of merged child fields
                    HashSet<string> mergedChildSystemNames =
                        new HashSet<string>(mergedField.ChildFields.Select(cf => cf.SystemName));

                    foreach (var childField in initialField.ChildFields)
                    {
                        if (!mergedChildSystemNames.Contains(childField.SystemName))
                        {
                            BizField child = new BizField(childField.SystemName, childField.Id.ToString())
                            {
                                Value = "",
                                ParentFieldId = mergedField.Id,
                                IsACollection = childField.IsACollection,
                                TopLevelFieldId = mergedField.TopLevelFieldId
                            };
                            mergedField.ChildFields.Add(child);
                        }
                    }

                    break;
                case JTokenType.Array:
                    // Handle the case when jToken is an array
                    foreach (var element in jToken.Children<JObject>())
                    {
                        // Create a new child field for each element in the array
                        BizField child = new(initialField.SystemName, initialField.Id.ToString())
                        {
                            ParentFieldId = mergedField.Id,
                            IsACollection = initialField.IsACollection,
                            TopLevelFieldId = mergedField.TopLevelFieldId
                        };
                        // Add the new child field to the merged field
                        mergedField.ChildFields.Add(child);
                        // Recursively merge the element into the new child field
                        MergeJTokenToBizField(element, initialField, child);
                    }
                    break;
                default:
                    // Set the value of the merged field to the string representation of the token
                    mergedField.Value = jToken.ToString();
                    break;
            }
        }

        public static JToken ConvertBizFieldToJToken(BizField bizField)
        {
            JToken jToken = new JObject();
            ConvertBizFieldToJToken(bizField, jToken);
            return jToken;
        }

        private static void ConvertBizFieldToJToken(BizField bizField, JToken jToken)
        {
            if (bizField.ChildFields.Count > 0)
            {
                var groupedChildFields = bizField.ChildFields.GroupBy(c => c.SystemName);
                foreach (var group in groupedChildFields)
                {
                    if (group.Count() > 1)
                    {
                        // Handle arrays
                        JArray array = new JArray();
                        foreach (var child in group)
                        {
                            JObject childObject = new JObject();
                            foreach (var subChild in child.ChildFields)
                            {
                                ConvertBizFieldToJToken(subChild, childObject);
                            }
                            array.Add(childObject);
                        }

                        if (bizField.SystemName == group.Key
                            && jToken[bizField.SystemName] == null)
                        {
                            jToken[bizField.SystemName] = array;
                        }
                        else if (jToken[bizField.SystemName] is not null
                                && jToken[bizField.SystemName][group.Key] == null)
                        {
                            jToken[bizField.SystemName][group.Key] = new JObject();
                            jToken[bizField.SystemName][group.Key] = array;
                        }
                    }
                    else
                    {
                        // Handle objects
                        foreach (var child in group)
                        {
                            if (jToken[bizField.SystemName] == null)
                            {
                                jToken[bizField.SystemName] = new JObject();
                            }
                            ConvertBizFieldToJToken(child, jToken[bizField.SystemName]);
                        }
                    }
                }
            }
            else
            {
                // Handle values
                if (Regex.IsMatch(bizField?.Value ?? "", "^[0-9]*$") && int.TryParse(bizField.Value, out int intValue))
                {
                    jToken[bizField.SystemName] = intValue;
                }
                else if (Regex.IsMatch(bizField?.Value ?? "", "^[0-9\\.]*$") && double.TryParse(bizField.Value, out double doubleValue))
                {
                    jToken[bizField.SystemName] = doubleValue;
                }
                else if (Regex.IsMatch(bizField?.Value ?? "", "^[true|True|false|False]*$") && bool.TryParse(bizField.Value, out bool boolValue))
                {
                    jToken[bizField.SystemName] = boolValue;
                }
                else
                {
                    jToken[bizField.SystemName] = bizField.Value;
                }
            }
        }
    }
}
