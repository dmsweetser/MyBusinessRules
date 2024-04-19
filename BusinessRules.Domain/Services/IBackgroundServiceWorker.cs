
namespace BusinessRules.Domain.Services
{
    public interface IBackgroundServiceWorker
    {
        void Execute(Func<IBusinessRulesService, Task> serviceWork);
    }
}
