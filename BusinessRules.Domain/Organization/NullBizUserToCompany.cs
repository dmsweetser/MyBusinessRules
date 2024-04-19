namespace BusinessRules.Domain.Organization
{
    public class NullBizUserToCompany: BizUserToCompany
    {
        public NullBizUserToCompany():base("", Guid.Empty)
        {
        }
    }
}
