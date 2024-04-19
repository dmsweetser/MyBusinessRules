using BusinessRules.Domain.Common;

namespace BusinessRules.Domain.Organization
{
    public class BizUser : IHaveAnId
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; }
        public UserRole Role { get; set; }
        
        /// <summary>
        /// DO NOT USE
        /// </summary>
        public BizUser()
        {
        }

        public BizUser(string emailAddress, UserRole role)
        {
            Id = Guid.NewGuid();
            EmailAddress = emailAddress;
            Role = role;
        }
    }
}
