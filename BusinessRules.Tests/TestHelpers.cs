using BusinessRules.Domain.Common;
using BusinessRules.Domain.Fields;
using BusinessRules.Licensing;
using BusinessRules.UI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Security.Claims;

namespace BusinessRules.Tests
{
    public static class TestHelpers
    {
        public static AppSettings InitConfiguration(string jsonStorageBasePath = "")
        {
            if (jsonStorageBasePath == "")
            {
                jsonStorageBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            }

            if (!Directory.Exists(jsonStorageBasePath))
            {
                Directory.CreateDirectory(jsonStorageBasePath);
            }

            LicenseManager.BaseAllowOfflineMode = (DateTime dateToVerify) => true;

            var storageMode = FeatureFlags.OfflineMode ? "json" : "azure_blob";

            var customConfiguration = new Dictionary<string, string>
            {
                {"AppSettings:IsTestMode", "true"},
                {"AppSettings:AzureStorageConnectionString", Environment.GetEnvironmentVariable("AzureStorageConnectionString") },
                {"AppSettings:AzureBlobStorageContainerName", Environment.GetEnvironmentVariable("AzureBlobStorageContainerName") },
                {"AppSettings:StripeBillingApiKey", Environment.GetEnvironmentVariable("StripeBillingApiKey") },
                {"AppSettings:StripePriceId", Environment.GetEnvironmentVariable("StripePriceId") },
                {"AppSettings:DynamicComparatorFunctionUrl", Environment.GetEnvironmentVariable("DynamicComparatorFunctionUrl") },
                {"AppSettings:DynamicOperandFunctionUrl", Environment.GetEnvironmentVariable("DynamicOperandFunctionUrl") }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(customConfiguration)
                .AddEnvironmentVariables()
                .AddUserSecrets(Assembly.GetAssembly(typeof(AccountController)))
                .Build();

            var appSettings = config.Get<AppSettings>();
            appSettings.IsTestMode = true;
            appSettings.StorageMode = storageMode;
            appSettings.JsonStorageBasePath = jsonStorageBasePath;
            appSettings.BaseEndpointUrl = "https://localhost";
            return appSettings;
        }

        public static void PopulateMockClaimsPrincipalForController(ControllerBase controller, out string userEmail)
        {
            userEmail = Guid.NewGuid().ToString() + "@mybizrules.com";

            IList<Claim> claimCollection = 
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", userEmail) };

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.User = principal;

            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            controller.ControllerContext = controllerContext;
        }

        public static void PopulateMockClaimsPrincipalForControllerWithBlankEmail(ControllerBase controller)
        {

            IList<Claim> claimCollection =
                new List<Claim> { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "") };

            var identity = new ClaimsIdentity(claimCollection, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.User = principal;

            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            controller.ControllerContext = controllerContext;
        }

        public static void ScrubId(BizField field)
        {
            field.Id = Guid.Empty;
            field.ParentFieldId = Guid.Empty;
            field.TopLevelFieldId = Guid.Empty;
            foreach (var childField in field.ChildFields)
            {
                ScrubId(childField);
            }
        }
    }
}
