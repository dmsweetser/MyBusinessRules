namespace BusinessRules.TestMVC.Models
{
    public class DriverInfo
    {  
        public int Id { get; set; }
        public Component Name { get; set; }
        public Component DateOfBirth { get; set; }
        public Component MaritalStatus { get; set; }
        public Component VehicleDrivenMost { get; set; }
        public Component IsPrincipalDriver { get; set; }

        public DriverInfo()
        {
            Id = 0;
            Name = new Component();
            DateOfBirth = new Component();
            MaritalStatus = new Component();
            VehicleDrivenMost = new Component();
            IsPrincipalDriver = new Component();
        }
    }
}
