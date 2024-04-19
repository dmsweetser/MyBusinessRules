namespace BusinessRules.Domain.Organization
{
    public class NullBizApiKey: BizApiKey
    {
        public NullBizApiKey(): base(Guid.Empty, Guid.Empty)
        {
                
        }
    }
}
