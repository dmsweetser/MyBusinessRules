
using BusinessRules.Domain.Data;

namespace BusinessRules.Domain.Services
{
    public interface IBackgroundStorageRepository
    {
        void Execute(Func<IRepository, Task> serviceWork);
    }
}
