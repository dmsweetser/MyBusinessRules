namespace BusinessRules.Domain.Fields
{
    public class NullBizField : BizField
    {
        private static NullBizField _instance = new NullBizField()
        {
            Id = Guid.Empty,
            ParentFieldId = Guid.Empty,
            SystemName = "",
            ChildFields = new()
        };

        public NullBizField()
        {
            Id = Guid.Empty;
            ParentFieldId = Guid.Empty;
            SystemName = "";
            ChildFields = new();
        }

        public static NullBizField GetInstance()
        {
            return _instance;
        }
    }
}