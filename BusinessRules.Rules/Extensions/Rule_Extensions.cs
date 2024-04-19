using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Components;

namespace BusinessRules.Rules.Extensions
{
    public static class Rule_Extensions
    {

        public static void Add(this BizRule currentRule, Type newComponent, string[] arguments = null)
        {
            if (!VerifyMatchingType(currentRule, newComponent))
            {
                throw new ArgumentException("Component is not permitted in sequence");
            }
            currentRule.RuleSequence.Add(
                Activator.CreateInstance(newComponent) as BaseComponent);
        }

        public static void Add(this BizRule currentRule, BaseComponent newComponent)
        {
            if (!VerifyMatchingType(currentRule, newComponent.GetType()))
            {
                throw new ArgumentException("Component is not permitted in sequence");
            }
            currentRule.RuleSequence.Add(newComponent);
        }

        public static BaseComponent Instantiate(this BizRule currentRule, Type newComponent, string[] arguments = null)
        {
            if (!VerifyMatchingType(currentRule, newComponent))
            {
                throw new ArgumentException("Component is not permitted in sequence");
            }
            return Activator.CreateInstance(newComponent) as BaseComponent;
        }

        public static bool VerifyMatchingType(this BizRule currentRule, Type newComponentType)
        {
            var foundComponents = currentRule.Then();
            if (foundComponents.Count == 0
                || !foundComponents.Any(foundType =>
                {
                    return ((foundType.IsAssignableTo(typeof(IAmAComparator))
                                && newComponentType.IsAssignableTo(typeof(IAmAComparator)))
                        || (foundType.IsAssignableTo(typeof(IAmAConjunction))
                                && newComponentType.IsAssignableTo(typeof(IAmAConjunction)))
                        || (foundType.IsAssignableTo(typeof(IAmAConsequent))
                                && newComponentType.IsAssignableTo(typeof(IAmAConsequent)))
                        || (foundType.IsAssignableTo(typeof(IAmAnAntecedent))
                                && newComponentType.IsAssignableTo(typeof(IAmAnAntecedent)))
                        || (foundType.IsAssignableTo(typeof(IAmAnAssignment))
                                && newComponentType.IsAssignableTo(typeof(IAmAnAssignment)))
                        || (foundType.IsAssignableTo(typeof(IAmAnOperand))
                                && newComponentType.IsAssignableTo(typeof(IAmAnOperand))));
                }))
            {
                return false;
            }
            return true;
        }

        public static bool Execute(this BizRule currentRule, BizField parentField, bool isTestRequest)
        {
            //Don't execute a rule if it is slated to start later than now
            if (currentRule.StartUsingOn.Date > DateTime.Now.Date) return false;
            //Don't execute a rule if it is slated to stop sooner than now
            if (currentRule.StopUsingOn.Date < DateTime.Now.Date) return false;
            //Don't execute a rule if it isn't activated, or if it isn't a test mode rule
            if ((!isTestRequest && !currentRule.IsActivated) || (isTestRequest && currentRule.IsActivated)) return false;

            currentRule.ScopedIndices = new();

            currentRule = currentRule.ToRuleDTO(parentField, isTestRequest).ToRule(parentField);

            for (int i = 0; i < currentRule.RuleSequence.Count; i++)
            {
                if (!currentRule.RuleSequence[i].Execute(parentField, currentRule)) return false;
            }

            return true;
        }

        public static Dictionary<string, BizComponentDTO> GetNextComponents(
            this BizRule foundRule,
            BizField foundTopLevelField,
            bool isTestMode)
        {
            var eligibleComponents = foundRule.Then();
            var componentsToReturn = new Dictionary<string, BizComponentDTO>();
            var flattenedFields = foundTopLevelField.FlattenFieldsWithDescription();
            foreach (var component in eligibleComponents)
            {

                //Don't include test components in non-test environments
                if (typeof(IAmATestComponent).IsAssignableFrom(component)
                    && !isTestMode)
                {
                    continue;
                }

                if (component == typeof(FieldOperand))
                {
                    var flattenedFieldList = flattenedFields.ToList();
                    flattenedFieldList.ForEach(x =>
                    {
                        var foundField = foundTopLevelField.GetChildFieldById(x.Key);
                        if (foundField.IsACollection)
                        {
                            if (!foundRule.RuleSequence.Any(x => x is IAmAConsequent))
                            {
                                var anyFieldInstance = foundRule.Instantiate(component, new[] { foundRule.Id.ToString() })
                                .WithArgumentValues(new string[] {
                                        x.Key.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
                                if (!componentsToReturn.ContainsKey("any " + x.Value))
                                {
                                    componentsToReturn.Add("any " + x.Value, anyFieldInstance.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
                                }

                                var allFieldInstance = foundRule.Instantiate(component, new[] { foundRule.Id.ToString() })
                                                                .WithArgumentValues(new string[] {
                                        x.Key.ToString(), BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString() });
                                if (!componentsToReturn.ContainsKey("every " + x.Value))
                                {
                                    componentsToReturn.Add("every " + x.Value, allFieldInstance.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
                                }
                            }
                            
                            if (foundRule.RuleSequence.Any(x =>
                                x is FieldOperand
                                && (x.Arguments[1].Value == BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString()
                                    || x.Arguments[1].Value == BizFieldCollectionTypeEnum.AllRecordsInCollection.ToString())))
                            {
                                var correspondingFieldInstance = foundRule.Instantiate(component, new[] { foundRule.Id.ToString() })
                                    .WithArgumentValues(new string[] {
                                        x.Key.ToString(), BizFieldCollectionTypeEnum.CorrespondingRecordInCollection.ToString() });
                                if (!componentsToReturn.ContainsKey("the associated " + x.Value))
                                {
                                    componentsToReturn.Add("the associated " + x.Value, correspondingFieldInstance.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
                                }
                            }
                        } else
                        {
                            var fieldInstance = foundRule.Instantiate(component, new[] { foundRule.Id.ToString() })
                                .WithArgumentValues(new string[] {
                                        x.Key.ToString(), BizFieldCollectionTypeEnum.AnyRecordInCollection.ToString() });
                            if (!componentsToReturn.ContainsKey(x.Value))
                            {
                                componentsToReturn.Add(x.Value, fieldInstance.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
                            }
                        }
                    });
                    continue;
                }

                var newInstance = foundRule.Instantiate(component, new[] { foundRule.Id.ToString() });
                componentsToReturn.Add(
                    newInstance.GetFormattedDescription(foundTopLevelField, foundRule.IsActivated),
                    newInstance.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
            }

            var dynamicOperandDefinitionId = new DynamicOperand().DefinitionId;
            var dynamicComparatorDefinitionId = new DynamicComparator().DefinitionId;

            if (eligibleComponents.Any(x => typeof(IAmAnOperand).IsAssignableFrom(x)))
            {
                var matchingDynamicComponents =
                    foundTopLevelField.DynamicComponents.Where(x => x.DefinitionId == dynamicOperandDefinitionId);
                foreach (var component in matchingDynamicComponents)
                {
                    componentsToReturn.Add(
                        component.GetFormattedDescription(foundTopLevelField, foundRule.IsActivated),
                        component.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
                }
            }

            if (eligibleComponents.Any(x => typeof(IAmAComparator).IsAssignableFrom(x)))
            {
                var matchingDynamicComponents =
                    foundTopLevelField.DynamicComponents.Where(x => x.DefinitionId == dynamicComparatorDefinitionId);
                foreach (var component in matchingDynamicComponents)
                {
                    componentsToReturn.Add(
                        component.GetFormattedDescription(foundTopLevelField, foundRule.IsActivated),
                        component.ToComponentDTO(foundTopLevelField, foundRule.IsActivated));
                }
            }

            return componentsToReturn;
        }
    }
}
