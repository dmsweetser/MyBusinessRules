namespace BusinessRules.TestMVC.Models
{
    public class Quote
    {
        public Component QuoteNumber { get; set; }
        public Component AgentCode { get; set; }
        public Component EffectiveDate { get; set; }
        public Component ExpirationDate { get; set; }

        public InsuredInfo InsuredInfo { get; set; }
        public CoverageInfo CoverageInfo { get; set; }
        public List<VehicleInfo> VehicleInfo { get; set; }
        public List<DriverInfo> DriverInfo { get; set; }

        public Component IsReferred { get; set; }
        public Component ReferralReason { get; set; }
        public Component IsDeclined { get; set; }
        public Component DeclineReason { get; set; }
                
        public Component PremiumAmount { get; set; }

        public Quote()
        {
            QuoteNumber = new Component();
            AgentCode = new Component();
            EffectiveDate = new Component();
            ExpirationDate = new Component();

            InsuredInfo = new InsuredInfo();
            CoverageInfo = new CoverageInfo();
            VehicleInfo = new List<VehicleInfo>();
            DriverInfo = new List<DriverInfo>();

            ReferralReason = new Component();
            DeclineReason = new Component();
            PremiumAmount = new Component();
        }

        public Quote(DateTime effectiveDate)
        {
            // Initialize QuoteNumber
            QuoteNumber = new Component("Quote Number", "PAQ12345", false, "", false, new List<string>());

            // Initialize AgentCode
            AgentCode = new Component("Agent Code", "AGT98765", false, "", false, new List<string>());

            // Initialize EffectiveDate
            EffectiveDate = new Component("Effective Date", effectiveDate.ToString("yyyy-MM-dd"), false, "", false, new List<string>());

            // Initialize ExpirationDate
            ExpirationDate = new Component("Expiration Date", effectiveDate.AddYears(1).ToString("yyyy-MM-dd"), false, "", false, new List<string>());

            // Initialize InsuredInfo
            InsuredInfo = new InsuredInfo
            {
                Name = new Component("Name", "John Doe", false, "", false, new List<string>()),
                AddressInfo = new AddressInfo
                {
                    Street = new Component("Street", "123 Main St", false, "", false, new List<string>()),
                    City = new Component("City", "Anytown", false, "", false, new List<string>()),
                    State = new Component("State", "NY", false, "", false, new List<string>()),
                    ZipCode = new Component("ZipCode", "12345", false, "", false, null)
                }
            };

            CoverageInfo = new CoverageInfo
            {
                LiabilityCoverage = new Component("Liability Coverage", "100/300", false, "", false, new List<string> { ("100/300|$100,000/$300,000"), ("300/500|$300,000/$500,000") }),
                PropertyDamageCoverage = new Component("Property Damage Coverage", "25000", false, "", false, new List<string> { ("25000|$25,000"), ("50000|$50,000") }),
            };

            // Initialize VehicleInfo
            VehicleInfo = new List<VehicleInfo>
            {
                new VehicleInfo
                {
                    Id = 1,
                    Make = new Component("Make", "Toyota", false, "", false, new List<string> { ("Toyota|Toyota"), ("Ford|Ford") }),
                    Model = new Component("Model", "Camry", false, "", false, new List<string> { ("Camry|Camry"), ("Fusion|Fusion") }),
                    Year = new Component("Year", "2010", false, "", false, new List<string>()),
                    CostNew = new Component("Cost New", "20000", false, "", false, new List<string>()),
                    ComprehensiveDeductible = new Component("Comprehensive Deductible", "500", false, "", false, new List<string> { ("500|$500"), ("1000|$1000") }),
                    CollisionDeductible = new Component("Collision Deductible", "500", false, "", false, new List<string> { ("500|$500"), ("1000|$1000") })
                },
                new VehicleInfo
                {
                    Id = 2,
                    Make = new Component("Make", "Ford", false, "", false, new List<string> { ("Toyota|Toyota"), ("Ford|Ford") }),
                    Model = new Component("Model", "Fusion", false, "", false, new List<string> { ("Camry|Camry"), ("Fusion|Fusion") }),
                    Year = new Component("Year", "2015", false, "", false, new List<string>()),
                    CostNew = new Component("Cost New", "25000", false, "", false, new List<string>()),
                    ComprehensiveDeductible = new Component("Comprehensive Deductible", "1000", false, "", false, new List<string> { ("500|$500"), ("1000|$1000") }),
                    CollisionDeductible = new Component("Collision Deductible", "1000", false, "", false, new List<string> { ("500|$500"), ("1000|$1000") })
                }
            };

            // Initialize DriverInfo
            DriverInfo = new List<DriverInfo>
            {
                new DriverInfo
                {
                    Id = 1,
                    Name = new Component("Name", "John Doe", false, "", false, new List<string>()),
                    DateOfBirth = new Component("Date Of Birth", "1990-01-01", false, "", false, new List<string>()),
                    MaritalStatus = new Component("Marital Status", "Single", false, "", false, new List<string> { ("Single|Single"), ("Married|Married") }),
                    VehicleDrivenMost = new Component("Vehicle Driven Most", "1", false, "", false, VehicleInfo.Select(v => (v.Id.ToString() + "|" + $"{v.Year.Value} {v.Make.Value} {v.Model.Value}")).ToList()),
                    IsPrincipalDriver = new Component("Is Principal Driver", "Yes", false, "", false, new List<string> { ("Yes|Yes"), ("No|No") })
                },
                new DriverInfo
                {
                    Id = 2,
                    Name = new Component("Name", "Jane Doe", false, "", false, new List<string>()),
                    DateOfBirth = new Component("Date Of Birth", "1995-03-15", false, "", false, new List<string>()),
                    MaritalStatus = new Component("Marital Status", "Married", false, "", false, new List<string> { ("Single|Single"), ("Married|Married") }),
                    VehicleDrivenMost = new Component("Vehicle Driven Most", "2", false, "", false, VehicleInfo.Select(v => (v.Id.ToString() + "|" + $"{v.Year.Value} {v.Make.Value} {v.Model.Value}")).ToList()),
                    IsPrincipalDriver = new Component("Is Principal Driver", "No", false, "", false, new List<string> { ("Yes|Yes"), ("No|No") })
                },
                // Add more drivers as needed
            };

            // Initialize ReferralReason
            IsReferred = new Component("Is Referred", "false", false, "", false, new List<string>());
            ReferralReason = new Component("Referral Reason", "None", false, "", false, new List<string>());

            // Initialize DeclineReason
            IsDeclined = new Component("Is Declined", "false", false, "", false, new List<string>());
            DeclineReason = new Component("Decline Reason", "None", false, "", false, new List<string>());

            // Initialize PremiumAmount
            PremiumAmount = new Component("Premium Amount", "", false, "", true, new List<string>());
        }
    }
}
