namespace BusinessRules.TestMVC.Models
{
    public class InsuredInfo
    {
        public Component Name { get; set; }
        public AddressInfo AddressInfo { get; set; }

        public InsuredInfo()
        {
            Name = new Component();
            AddressInfo = new AddressInfo();
        }
    }
}
