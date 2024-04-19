using BusinessRules.Domain.Rules.Component;
using System.Reflection;
namespace BusinessRules.Rules
{
    public static class RuleComponentFactory
    {
        private static Dictionary<Guid, Type> ComponentCache { get; } = new();
        static RuleComponentFactory()
        {
            var foundComponents = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(p => typeof(IAmAComponent).IsAssignableFrom(p))
                .ToList();

            foreach (var foundComponentType in foundComponents)
            {
                var instance = Activator.CreateInstance(foundComponentType) as BaseComponent;
                ComponentCache.Add(instance.DefinitionId, foundComponentType);
            }
        }

        public static List<Type> GetComponents<T>()
            where T : IAmAComponent
        {
            return ComponentCache.Values.Where(x =>
            {
                var componentInterfaces = x.GetInterfaces();
                var requestedType = typeof(T);
                var isMatch = componentInterfaces.Contains(requestedType);
                return isMatch;
            })
            .Where(x => !typeof(DynamicComponent).IsAssignableFrom(x)).ToList();
        }

        public static BaseComponent GetComponentById(Guid id)
        {
            var foundComponentType = ComponentCache.Where(x =>
            {
                return x.Key == id;
            }).FirstOrDefault();

            return Activator.CreateInstance(
                foundComponentType.Value) as BaseComponent;
        }
    }
}
