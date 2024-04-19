using Azure.Data.Tables;
using BusinessRules.Data;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Data;
using BusinessRules.Domain.Services;
using Microsoft.Extensions.Options;
using Stripe;

namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public readonly IRepository Repository;
        public InvoiceService StripeInvoiceService { get; set; }
        public AppSettings Settings { get; set; }

        public BusinessRulesService(
            TableServiceClient tableServiceClient,
            IBackgroundStorageRepository backgroundStorageRepository,
            IOptions<AppSettings> settings,
            bool useCache = true)
        {
            Settings = settings.Value;
            Repository = RepositoryFactory.GetRepository(tableServiceClient, backgroundStorageRepository, Settings, useCache);
            StripeInvoiceService = new InvoiceService();
        }

        public BusinessRulesService(
            TableServiceClient tableServiceClient,
            IBackgroundStorageRepository backgroundStorageRepository,
            AppSettings settings,
            bool useCache = true)
        {
            Settings = settings;
            Repository = RepositoryFactory.GetRepository(tableServiceClient, backgroundStorageRepository, Settings, useCache);
            StripeInvoiceService = new InvoiceService();
        }
    }
}
