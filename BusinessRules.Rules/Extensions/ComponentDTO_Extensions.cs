using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;
using System.Reflection;

namespace BusinessRules.Rules
{
    public static class ComponentDTO_Extensions
    {
        public static BizComponentDTO ToComponentDTO(this IAmAComponent component, BizField parentField, bool isActivated)
        {
            if (component.Arguments == null)
            {
                component.Arguments = new();
            }

            var specificComponent = RuleComponentFactory.GetComponentById(component.DefinitionId);

            specificComponent.Arguments = component.Arguments;
            specificComponent.Id = component.Id;

            if (specificComponent is DynamicComponent castComponent)
            {
                var matchingComponent =
                    parentField.DynamicComponents.FirstOrDefault(x => x.DefinitionId == component.DefinitionId);
                castComponent.Description = matchingComponent.Description;
                castComponent.Body = matchingComponent.Body;
            }

            MethodInfo overriddenMethod = specificComponent.GetType().GetMethod("GetFormattedDescription");
            var formattedDescription = overriddenMethod.Invoke(specificComponent, new object[] { parentField, isActivated });

            return new BizComponentDTO()
            {
                Id = component.Id,
                DefinitionId = component.DefinitionId,
                Description = formattedDescription.ToString(),
                Arguments = component.Arguments
            };
        }

        public static BaseComponent ToComponent(this BizComponentDTO componentDTO)
        {
            var newComponent = RuleComponentFactory.GetComponentById(componentDTO.DefinitionId);
            newComponent.Id = componentDTO.Id;
            newComponent.Arguments = componentDTO.Arguments ?? new();
            return newComponent;
        }
    }
}
