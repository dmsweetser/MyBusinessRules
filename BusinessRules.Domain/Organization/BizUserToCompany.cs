using BusinessRules.Domain.Common;

namespace BusinessRules.Domain.Organization
{
    public class BizUserToCompany : IHaveAnId
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid CompanyId { get; set; }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public BizUserToCompany()
        {
        }

        public BizUserToCompany(string userId, Guid companyId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CompanyId = companyId;
        }
    }
}
