using BusinessRules.Domain.Common;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Rules.Component;

namespace BusinessRules.Domain.Rules
{
    public class BizRule: IHaveAnId
    {
        public Guid Id { get; set; }
        public Guid ParentFieldId { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public List<BaseComponent> RuleSequence { get; set; }
        public bool IsActivated { get; set; }
        public bool IsActivatedTestOnly { get; set; }
        public List<int> ScopedIndices { get; set; }
        public DateTime StartUsingOn { get; set; }
        public DateTime StopUsingOn { get; set; }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public BizRule()
        {
        }

        public BizRule(string name, BizField parentField)
        {
            Id = Guid.NewGuid();
            if (parentField.ParentFieldId != Guid.Empty)
            {
                throw new ArgumentException("Parent field is not a top level field");
            }
            ParentFieldId = parentField.Id;
            Name = name;
            GroupName = "";
            RuleSequence = new();
            IsActivated = false;
            IsActivatedTestOnly = true;
            ScopedIndices = new();
            StartUsingOn = DateTime.Now.Date;
            StopUsingOn = DateTime.Now.Date.AddYears(1);
        }
    }
}
