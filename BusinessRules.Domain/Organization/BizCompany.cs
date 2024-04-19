using BusinessRules.Domain.Common;

namespace BusinessRules.Domain.Organization
{
    public class BizCompany : IHaveAnId
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<BizUser> Users { get; set; }
        public List<Guid> FieldIds { get; set; }
        public List<Guid> ApiKeyIds { get; set; }
        public string BillingId { get; set; }
        public DateTime LastBilledDate { get; set; }
        public int CreditsAvailable { get; set; }
        public int CreditsUsed { get; set; }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public BizCompany()
        {
            Users = new List<BizUser>();
            FieldIds = new List<Guid>();
            ApiKeyIds = new List<Guid>();
        }

        public BizCompany(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
            Users = new List<BizUser>();
            FieldIds = new List<Guid>();
            ApiKeyIds = new List<Guid>();   
            CreditsAvailable = 0;
            CreditsUsed = 0;
            LastBilledDate = DateTime.MinValue;
        }
    }
}
