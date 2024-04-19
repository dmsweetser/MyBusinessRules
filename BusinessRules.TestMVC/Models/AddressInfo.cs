namespace BusinessRules.TestMVC.Models
{
    public class AddressInfo
    {
        public Component Street { get; set; }
        public Component City { get; set; }
        public Component State { get; set; }
        public Component ZipCode { get; set; }

        public AddressInfo()
        {
            Street = new Component();
            City = new Component();
            State = new Component();
            ZipCode = new Component();
        }
    }
}
