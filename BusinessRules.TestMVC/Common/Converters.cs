using BusinessRules.TestMVC.Models;
using Newtonsoft.Json;

namespace BusinessRules.TestMVC.Common
{
    public static class Converters
    {
        public static class PolicyConverter
        {
            public static Policy ConvertToPolicy(Quote quote)
            {
                Policy policy = new Policy();

                // Copy AgentCode
                policy.AgentCode = CopyComponent(quote.AgentCode);

                // Set IssueDate (can be the same as EffectiveDate from Quote)
                policy.IssueDate = CopyComponent(quote.EffectiveDate, "Issue Date");

                // Set RenewalDate (you may need to calculate this based on business rules)
                policy.RenewalDate = new Component
                {
                    Name = "Renewal Date",
                    Value = "2024-01-01",  // You may need to adjust this based on business rules
                    IsHidden = false,
                    ErrorMessage = "",
                    IsReadOnly = false,
                    AllowedValues = ""
                };

                // Copy InsuredInfo
                policy.InsuredInfo = new InsuredInfo
                {
                    Name = CopyComponent(quote.InsuredInfo.Name),
                    AddressInfo = new AddressInfo
                    {
                        Street = CopyComponent(quote.InsuredInfo.AddressInfo.Street),
                        City = CopyComponent(quote.InsuredInfo.AddressInfo.City),
                        State = CopyComponent(quote.InsuredInfo.AddressInfo.State),
                        ZipCode = CopyComponent(quote.InsuredInfo.AddressInfo.ZipCode)
                    }
                };

                //Copy coverage info
                policy.CoverageInfo = new CoverageInfo
                {
                    LiabilityCoverage = CopyComponent(quote.CoverageInfo.LiabilityCoverage),
                    PropertyDamageCoverage = CopyComponent(quote.CoverageInfo.PropertyDamageCoverage)
                };

                // Copy VehicleInfo
                policy.VehicleInfo = quote.VehicleInfo.Select(v => new VehicleInfo
                {
                    Id = v.Id,
                    Make = CopyComponent(v.Make),
                    Model = CopyComponent(v.Model),
                    Year = CopyComponent(v.Year),
                    CostNew = CopyComponent(v.CostNew),
                    ComprehensiveDeductible = CopyComponent(v.ComprehensiveDeductible),
                    CollisionDeductible = CopyComponent(v.CollisionDeductible)
                }).ToList();

                // Copy DriverInfo
                policy.DriverInfo = quote.DriverInfo.Select(d => new DriverInfo
                {
                    Id = d.Id,
                    Name = CopyComponent(d.Name),
                    DateOfBirth = CopyComponent(d.DateOfBirth),
                    MaritalStatus = CopyComponent(d.MaritalStatus),
                    VehicleDrivenMost = CopyComponent(d.VehicleDrivenMost),
                    IsPrincipalDriver = CopyComponent(d.IsPrincipalDriver)
                }).ToList();

                // Set PremiumPaid (assuming it's the same as PremiumAmount from Quote)
                policy.PremiumPaid = CopyComponent(quote.PremiumAmount, "PremiumPaid");

                // Set PolicyStatus (you may need to determine this based on business rules)
                policy.PolicyStatus = new Component
                {
                    Name = "Policy Status",
                    Value = "Active",  // You may need to adjust this based on business rules
                    IsHidden = false,
                    ErrorMessage = "",
                    IsReadOnly = false,
                    AllowedValues = JsonConvert.SerializeObject(new List<string> { "Active|Active", "Suspended|Suspended" })
                };

                return policy;
            }

            private static Component CopyComponent(Component source, string newName = null)
            {
                return new Component
                {
                    Name = newName ?? source.Name,
                    Value = source.Value,
                    IsHidden = source.IsHidden,
                    ErrorMessage = source.ErrorMessage,
                    IsReadOnly = source.IsReadOnly,
                    AllowedValues = source.AllowedValues
                };
            }
        }
    }
}
