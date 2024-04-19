namespace BusinessRules.Domain.Common
{
    public class AppSettings
    {
        public bool IsTestMode { get; set; }
        public string StorageMode { get; set; }
        public string JsonStorageBasePath { get; set; }
        public string AzureStorageConnectionString { get; set; }
        public int CreditGracePeriodAmount { get; set; }
        public int NewCompanyFreeCreditAmount { get; set; }
        public string AuthenticationLoginRedirectUri { get; set; }
        public string AuthenticationSignupRedirectUri { get; set; }
        public string BaseEndpointUrl { get; set; }
        public string StripeBillingApiKey { get; set; }
        public string StripePriceId { get; set; }
        public string DynamicComparatorFunctionUrl { get; set; }
        public string DynamicOperandFunctionUrl { get; set; }
        public string AzureBlobStorageContainerName { get; set; }
    }
}
