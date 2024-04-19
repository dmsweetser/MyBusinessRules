using Azure.Data.Tables;
using Azure.Storage.Blobs;
using BusinessRules.Admin;
using BusinessRules.Data.Implementation.AzureBlobStorage;
using BusinessRules.Domain.Common;
using BusinessRules.ServiceLayer;
using BusinessRules.Tests.Mocks;
using BusinessRules.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;


var tableServiceClient = new TableServiceClient("ADD HERE");

var settings = new AppSettings();
settings.StorageMode = "azure";

IServiceScopeFactory serviceScopeFactory = new MockIServiceScopeFactory();
var backgroundStorageRepository = new BackgroundStorageRepository(
    serviceScopeFactory,
    tableServiceClient,
    new NullLogger<BackgroundStorageRepository>(), settings);

var service = new BusinessRulesService(tableServiceClient, backgroundStorageRepository, settings);



//await Helpers.AddCreditCodes(service, "test", 500000, 1);
//await Helpers.ResetMyCompany(service);
//await Helpers.MigrateCompanyToBlob(service);


Console.ReadLine();