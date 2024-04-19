using BusinessRules.Domain.Common;

namespace BusinessRules.Domain.Organization
{
    public class BizCreditCode : IHaveAnId
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; }
        public Guid RedeemedByCompanyId { get; set; }
        public DateTime RedeemedDate { get; set; }
        public int CreditValue { get; set; }

        /// <summary>
        /// DO NOT USE - required by serialization
        /// </summary>
        public BizCreditCode()
        {
            
        }

        public BizCreditCode(Guid id, int creditValue, string groupName)
        {
            Id = id;
            CreditValue = creditValue;
            RedeemedDate = DateTime.MinValue;
            RedeemedByCompanyId = Guid.Empty;
            GroupName = groupName;
        }
    }
}
