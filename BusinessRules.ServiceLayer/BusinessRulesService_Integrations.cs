using BusinessRules.Domain.Common;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;
using Stripe;
using Stripe.Checkout;
using System.Text.RegularExpressions;


namespace BusinessRules.ServiceLayer
{
    public partial class BusinessRulesService : IBusinessRulesService
    {
        public async Task<string> GenerateNewStripeCustomerForCompany(string stripeApiKey, Guid newCompanyId, string emailAddress)
        {
            return await Task.Run(() =>
            {
                if (FeatureFlags.OfflineMode)
                {
                    return "offline";
                } else
                {
                    StripeConfiguration.ApiKey = stripeApiKey;

                    var options = new CustomerCreateOptions
                    {
                        Description = newCompanyId.ToString(),
                        Name = newCompanyId.ToString(),
                        Email = emailAddress
                    };

                    var service = new CustomerService();
                    var customer = service.Create(options);

                    return customer.Id;
                }
            });
        }

        public async Task<string> GenerateSessionForStripeCustomer(string stripeApiKey, string customerId, string returnUrl)
        {
            return await Task.Run(() =>
            {
                if (FeatureFlags.OfflineMode)
                {
                    return "offline";
                } else
                {
                    StripeConfiguration.ApiKey = stripeApiKey;

                    var options = new Stripe.BillingPortal.SessionCreateOptions
                    {
                        Customer = customerId,
                        ReturnUrl = returnUrl,
                    };

                    var service = new Stripe.BillingPortal.SessionService();
                    var session = service.Create(options);
                    return session.Url;
                }
            });
        }

        public async Task<string> GeneratePurchaseUrlForStripeCustomer(string stripeApiKey, string customerId, string returnUrl, string priceId)
        {
            return await Task.Run(() =>
            {
                if (FeatureFlags.OfflineMode)
                {
                    return "offline";
                } else
                {
                    StripeConfiguration.ApiKey = stripeApiKey;

                    var options = new SessionCreateOptions
                    {
                        Customer = customerId,
                        LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    Price = priceId,
                    Quantity = 1,
                    AdjustableQuantity = new SessionLineItemAdjustableQuantityOptions()
                    {
                        Enabled = true,
                        Minimum = 1,
                        Maximum = 999999
                    }
                  },
                },
                        Mode = "subscription",
                        SuccessUrl = returnUrl,
                        CancelUrl = returnUrl
                    };
                    var service = new SessionService();
                    Session session = service.Create(options);
                    return session.Url;
                }
            });
        }

        public List<Invoice> GetInvoicesForCompany(
            BizCompany currentCompany,
            string stripeApiKey)
        {
            if (FeatureFlags.OfflineMode)
            {
                return new List<Invoice>();
            } else
            {
                StripeConfiguration.ApiKey = stripeApiKey;

                var options = new InvoiceListOptions
                {
                    Limit = 1,
                    Customer = currentCompany.BillingId,
                    Status = "paid"
                };

                return StripeInvoiceService.List().Data;
            }
        }

        public async Task<bool> UpdateBillingInfoForCompany(
            BizCompany currentCompany,
            List<Invoice> invoices)
        {
            return await Task.Run(() =>
            {
                if (FeatureFlags.OfflineMode)
                {
                    currentCompany.BillingId = "offline";
                    return true;
                } else
                {
                    if (invoices.Count > 0)
                    {
                        var firstInvoice = invoices[0];

                        var quantity = firstInvoice.Lines.Data[0].Quantity;
                        var batchSize = Regex.Match(firstInvoice.Lines.Data[0].Description, "(?<=[(])([0-9]*)(?= Credits[)])");
                        var parsedBatchSize = int.Parse(batchSize.Value);

                        var paidDate = firstInvoice.StatusTransitions?.PaidAt ?? DateTime.MinValue;

                        if (paidDate > currentCompany.LastBilledDate)
                        {
                            currentCompany.CreditsAvailable += (int)quantity * parsedBatchSize;
                            currentCompany.LastBilledDate = paidDate;

                            return true;
                        }
                    }

                    return false;
                }
            });
        }
    }
}
