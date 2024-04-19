using BusinessRules.Domain.Services;

namespace BusinessRules.UI.Common
{
    public class BackgroundServiceWorker : IBackgroundServiceWorker
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IBusinessRulesService _service = null;
        private readonly ILogger<BackgroundServiceWorker> _logger;

        public BackgroundServiceWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BackgroundServiceWorker> logger
            )
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public void Execute(Func<IBusinessRulesService, Task> serviceWork)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_service == null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        _service = scope.ServiceProvider.GetRequiredService<IBusinessRulesService>();
                    }
                    await serviceWork(_service);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
        }
    }
}
