namespace BusinessRules.TestMVC.Models
{
    public class Policy
    {
        public Component AgentCode { get; set; }
        public Component IssueDate { get; set; }
        public Component RenewalDate { get; set; }

        public InsuredInfo InsuredInfo { get; set; }
        public CoverageInfo CoverageInfo { get; set; }
        public List<VehicleInfo> VehicleInfo { get; set; }
        public List<DriverInfo> DriverInfo { get; set; }

        public Component PremiumPaid { get; set; }
        public Component PolicyStatus { get; set; }

        public Policy()
        {
            AgentCode = new Component();
            IssueDate = new Component();
            RenewalDate = new Component();
            
            InsuredInfo = new InsuredInfo();
            CoverageInfo = new CoverageInfo();
            VehicleInfo = new List<VehicleInfo>();
            DriverInfo = new List<DriverInfo>();

            PremiumPaid = new Component();
            PolicyStatus = new Component();
        }
    }
}
