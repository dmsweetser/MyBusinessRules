using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using BusinessRules.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Helpers;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using FluentFTP;
using FluentFTP.Client.BaseClient;

namespace BusinessRules.UI.Common
{
    public class ControllerHelpers
    {
        public static string ParseClaimsToGetEmail(ClaimsPrincipal user)
        {
            var foundEmail =
                user?.Claims
                ?.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                ?.Value ?? null;

            return foundEmail;
        }

        public static async Task<BizCompany> GenerateNewCompany(
            IBusinessRulesService _service,
            AppSettings _settings,
            BizUser newUser,
            bool isDemoUser,
            string defaultId = "")
        {
            var newCompany = new BizCompany("New Company");
            if (defaultId != "")
            {
                newCompany.Id = Guid.Parse(defaultId);
            }
            newCompany.CreditsAvailable = _settings.NewCompanyFreeCreditAmount;
            newCompany.CreditsUsed = 0;
            newCompany.Users.Add(newUser);

            if (isDemoUser)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                //This is done on purpose to avoid delays in loading the demo, but we still want it to track the user
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    var billingId = await _service.GenerateNewStripeCustomerForCompany(_settings.StripeBillingApiKey, newCompany.Id, newUser.EmailAddress);
                    var updatedCompany = await _service.GetCompany(newCompany.Id);
                    updatedCompany.BillingId = billingId;
                    await _service.SaveCompany(updatedCompany);
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                var billingId = await _service.GenerateNewStripeCustomerForCompany(_settings.StripeBillingApiKey, newCompany.Id, newUser.EmailAddress);
                newCompany.BillingId = billingId;
            }

            await _service.SaveCompany(newCompany);

            return newCompany;
        }

        public static async Task<IActionResult> SelectCompany(
            Controller controller,
            IBusinessRulesService _service,
            HttpContext _context,
            AppSettings _settings,
            string emailAddress,
            string companyId,
            bool isDemoUser,
            IRazorPartialToStringRenderer razorPartialToStringRenderer = null)
        {
            var foundCompany = await _service.GetCompany(Guid.Parse(companyId));
            var foundApiKeys = await _service.GetApiKeysForCompany(foundCompany.Id);

            _context.Session.SetString("EmailAddress", foundCompany.Id.ToString());
            _context.Session.SetString("CompanyId", foundCompany.Id.ToString());

            var foundUser = await _service.GetUser(foundCompany.Id, emailAddress);

            var billingSessionUrl = "";
            var purchaseUrl = "";

            if (!isDemoUser)
            {
                await _service.UpdateBillingInfoForCompany(
                    foundCompany,
                    _service.GetInvoicesForCompany(foundCompany, _settings.StripeBillingApiKey)
                    );
                await _service.SaveCompany(foundCompany);

                try
                {
                    billingSessionUrl = await _service.GenerateSessionForStripeCustomer(
                            _settings.StripeBillingApiKey,
                            foundCompany.BillingId,
                            _settings.BaseEndpointUrl);
                    purchaseUrl = await _service.GeneratePurchaseUrlForStripeCustomer(
                        _settings.StripeBillingApiKey,
                        foundCompany.BillingId,
                        $"{_context.Request.Scheme}://{_context.Request.Host}",
                        _settings.StripePriceId);
                }
                catch (Exception)
                {
                    var billingId = await _service.GenerateNewStripeCustomerForCompany(_settings.StripeBillingApiKey, foundCompany.Id, foundUser.EmailAddress);
                    foundCompany.BillingId = billingId;
                    foundCompany.LastBilledDate = DateTime.MinValue;
                    await _service.SaveCompany(foundCompany);
                    billingSessionUrl = await _service.GenerateSessionForStripeCustomer(
                            _settings.StripeBillingApiKey,
                            foundCompany.BillingId,
                            _settings.BaseEndpointUrl);
                    purchaseUrl = await _service.GeneratePurchaseUrlForStripeCustomer(
                        _settings.StripeBillingApiKey,
                        foundCompany.BillingId,
                        $"{_context.Request.Scheme}://{_context.Request.Host}",
                        _settings.StripePriceId);
                }
            }

            if (razorPartialToStringRenderer is not null)
            {
                return new ObjectResult(await razorPartialToStringRenderer.RenderToString(
                    "~/Views/Shared/Components/_CompanyComponent.cshtml",
                    await foundCompany.ToAdministratorDTO(_service, _settings.BaseEndpointUrl, billingSessionUrl, purchaseUrl)));
            }
            else
            {
                var fieldsAndRules = await _service.GetFieldsAndRulesForCompany(foundCompany.Id);

                foreach (var foundApiKey in foundApiKeys)
                {
                    if (fieldsAndRules.Keys.Count(x => x.Id == foundApiKey.TopLevelFieldId) == 0)
                    {
                        foundApiKey.TopLevelFieldId = Guid.Empty;
                        await _service.SaveApiKey(foundApiKey);
                    }
                }

                return controller.View("~/Views/Home/Dashboard.cshtml", new SystemViewModel(
                    _service,
                    _settings,
                    foundCompany,
                    foundUser,
                    fieldsAndRules,
                    foundApiKeys,
                    billingSessionUrl,
                    purchaseUrl
                ));
            }
        }

        public static async Task<string> GetComponent(
            IBusinessRulesService _service,
            AppSettings _settings,
            Guid companyId,
            Guid fieldId,
            string emailAddress,
            string templateName,
            IRazorPartialToStringRenderer razorPartialToStringRenderer)
        {
            var foundCompany = await _service.GetCompany(companyId);
            var foundUser = await _service.GetUser(foundCompany.Id, emailAddress);
            var fieldsAndRules = await _service.GetFieldsAndRulesForCompany(foundCompany.Id);
            var foundApiKeys = await _service.GetApiKeysForCompany(foundCompany.Id);

            return await razorPartialToStringRenderer.RenderToString(
                    templateName,
                    new SystemViewModel(
                    _service,
                    _settings,
                    foundCompany,
                    foundUser,
                    fieldsAndRules,
                    foundApiKeys,
                    //These are sent blank in this case because they aren't used by this component
                    "",
                    ""
                ));
        }

        public static async Task BuildTopLevelField(
            IBusinessRulesService service,
            string stringifiedObject,
            Guid companyId,
            Guid topLevelFieldIdToAssign,
            BizApiKey existingApiKey)
        {
            BizField castObject = NullBizField.GetInstance();

            JToken jToken;

            try
            {
                jToken = JObject.Parse(stringifiedObject);
            }
            catch (Exception)
            {
                try
                {
                    jToken = JArray.Parse(stringifiedObject);
                }
                catch (Exception ex)
                {
                    throw new Exception("The serialized string is not JSON", ex);
                }
            }

            castObject =
                BizField_Helpers.ConvertJTokenToBizField(jToken, topLevelFieldIdToAssign);

            castObject.Id = topLevelFieldIdToAssign;
            castObject.TopLevelFieldId = topLevelFieldIdToAssign;

            await service.SaveTopLevelField(companyId, castObject);

            var foundCompany = await service.GetCompany(companyId);
            var foundFields = await service.GetTopLevelFieldsForCompany(foundCompany.Id);

            var matchingField = foundFields.First(x => x.Id == castObject.Id);

            var foundApiKeys = await service.GetApiKeysForCompany(foundCompany.Id);

            if (existingApiKey is not NullBizApiKey)
            {
                existingApiKey.TopLevelFieldId = castObject.Id;
                await service.SaveApiKey(existingApiKey);
            }
            else if (!foundApiKeys.Any(x => x.TopLevelFieldId == matchingField.Id))
            {
                var newApiKey = new BizApiKey(foundCompany.Id, matchingField.Id);
                await service.SaveApiKey(newApiKey);
            }
        }

        public static void UploadToFtps(
            string ftpsServer,
            string port,
            string username,
            string password,
            string remoteDirectory,
            string fileNameToSend,
            string dataToSend)
        {
            var config = new FtpConfig()
            {
                EncryptionMode = FtpEncryptionMode.Explicit,
                ValidateAnyCertificate = true
            };

            using (var ftpClient = new FtpClient(
                ftpsServer,
                user: username,
                pass: password,
                port: int.Parse(port),
                config: config))
            {
                ftpClient.Credentials = new NetworkCredential(username, password);
                ftpClient.Connect();
                ftpClient.UploadBytes(Encoding.UTF8.GetBytes(dataToSend), Path.Combine(remoteDirectory, fileNameToSend));
                ftpClient.Disconnect();
            }
        }
    }
}