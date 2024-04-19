using Azure.Data.Tables;
using BusinessRules.Data;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Data;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BusinessRules.UI.Common
{
    public class BackgroundStorageRepository : IBackgroundStorageRepository
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IRepository _repository = null;
        private TableServiceClient _tableServiceClient = null;
        private readonly ILogger<BackgroundStorageRepository> _logger;
        private readonly AppSettings _settings;
        private readonly bool _isMockOnly;

        private readonly ConcurrentQueue<Func<IRepository, Task>> _executeQueue = new();

        //This constructor is needed for the app to run - DO NOT REMOVE
        public BackgroundStorageRepository(
            IServiceScopeFactory serviceScopeFactory,
            TableServiceClient tableServiceClient,
            ILogger<BackgroundStorageRepository> logger,
            IOptions<AppSettings> settings,
            bool isMockOnly = false
            )
        {
            _serviceScopeFactory = serviceScopeFactory;
            _tableServiceClient = tableServiceClient;
            _logger = logger;
            _settings = settings.Value;
            _isMockOnly = isMockOnly;
        }

        public BackgroundStorageRepository(
            IServiceScopeFactory serviceScopeFactory,
            TableServiceClient tableServiceClient,
            ILogger<BackgroundStorageRepository> logger,
            AppSettings settings,
            bool isMockOnly = false
    )
        {
            _serviceScopeFactory = serviceScopeFactory;
            _tableServiceClient = tableServiceClient;
            _logger = logger;
            _settings = settings;
            _isMockOnly = isMockOnly;
        }

        public void Execute(Func<IRepository, Task> repositoryWork)
        {
            if (_isMockOnly) return;

            _executeQueue.Enqueue(repositoryWork);

            Task.Run(async () =>
            {
                while (_executeQueue.TryDequeue(out var workToDo))
                {
                    if (_repository == null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();

                        _repository = RepositoryFactory.GetRepository(_tableServiceClient, this, _settings, false);
                    }
                    await workToDo(_repository);
                }
            });
        }
    }
}
