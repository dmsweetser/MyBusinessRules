namespace BusinessRules.Domain.DTO
{
    public class BizRuleDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public List<BizComponentDTO> RuleSequence { get; set; }
        public bool IsActivated { get; set; }
        public bool IsTestMode { get; set; }
        public DateTime StartUsingOn { get; set; }
        public DateTime StopUsingOn { get; set; }
        public List<KeyValuePair<string, BizComponentDTO>> NextComponents { get; set; }

        /// <summary>
        /// DO NOT USE - required by model binder
        /// </summary>
        public BizRuleDTO()
        {
            GroupName = "";
        }
    }
}
