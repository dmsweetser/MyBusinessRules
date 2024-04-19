namespace BusinessRules.TestMVC.Models
{
    public class CoverageInfo
    {
        public Component LiabilityCoverage { get; set; }
        public Component PropertyDamageCoverage { get; set; }
        public CoverageInfo()
        {
            LiabilityCoverage = new Component();
            PropertyDamageCoverage = new Component();
        }
    }
}
