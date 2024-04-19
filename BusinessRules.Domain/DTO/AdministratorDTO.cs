using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;

namespace BusinessRules.Domain.DTO
{
    public class AdministratorDTO
    {
        public string Name { get; set; }
        public List<BizUser> Users { get; set; }
        public List<BizApiKey> ApiKeys { get; set; }
        public List<Guid> FieldIds { get; set; }
        public List<Guid> ApiKeyIds { get; set; }
        public int RemainingCredits { get; set; }
        public string BaseEndpointUrl { get; set; }
        public string BillingUrl { get; set; }
        public string PurchaseUrl { get; set; }
        public DateTime LastBilledDate { get; set; }

        public List<BizField> Fields { get; set; }

        public AdministratorDTO()
        {
            Users = new();
            ApiKeys = new();
            FieldIds = new();
            Fields = new();
            ApiKeyIds = new();
        }
    }
}
