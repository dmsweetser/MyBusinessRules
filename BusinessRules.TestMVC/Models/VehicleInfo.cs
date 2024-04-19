namespace BusinessRules.TestMVC.Models
{
    public class VehicleInfo
    {
        public int Id { get; set; }
        public Component Make { get; set; }
        public Component Model { get; set; }
        public Component Year { get; set; }
        public Component CostNew { get; set; }
        public Component ComprehensiveDeductible { get; set; }
        public Component CollisionDeductible { get; set; }

        public VehicleInfo()
        {
            Id = 0;
            Make = new Component();
            Model = new Component();
            Year = new Component();
            CostNew = new Component();
            ComprehensiveDeductible = new Component();
            CollisionDeductible = new Component();
        }
    }
}
