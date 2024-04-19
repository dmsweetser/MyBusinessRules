using BusinessRules.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using BusinessRules.Rules.Extensions;
using BusinessRules.Domain.Common;
using Microsoft.Extensions.Options;
using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Newtonsoft.Json.Linq;
using BusinessRules.Domain.Helpers;
using BusinessRules.UI.Common;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Fields;

namespace BusinessRules.UI.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [EnableCors("AllowAllOriginsPolicy")]
    public class PublicController : Controller
    {
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;
        private readonly IBackgroundServiceWorker _backgroundServiceWorker;

        public PublicController(
            IBusinessRulesService service,
            IHttpContextAccessor context,
            IOptions<AppSettings> settings,
            IBackgroundServiceWorker backgroundServiceWorker)
        {
            _service = service;
            _context = context.HttpContext;
            _settings = settings.Value;
            _backgroundServiceWorker = backgroundServiceWorker;
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string emailAddress = "")
        {
            return await Task.Run(async () =>
            {
                IActionResult result;

                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    using (StreamReader stream = new StreamReader(Request.Body))
                    {
                        emailAddress = await stream.ReadToEndAsync();
                        emailAddress = JsonConvert.DeserializeObject<string>(emailAddress);
                    }
                }

                var foundCompany = await _service.GetCompanyForUser(emailAddress);

                if (foundCompany is not NullBizCompany)
                {
                    result = BadRequest("Account already exists for user");
                }
                else
                {
                    var newUser = new BizUser(emailAddress, UserRole.Administrator);
                    var newCompany = await ControllerHelpers.GenerateNewCompany(_service, _settings, newUser, false);
                    var fieldsAndRules = await _service.GetFieldsAndRulesForCompany(newCompany.Id);
                    var newApiKey = new BizApiKey(newCompany.Id, fieldsAndRules.Keys.FirstOrDefault().Id);
                    await _service.SaveApiKey(newApiKey);

                    result = new ContentResult
                    {
                        Content = JsonConvert.SerializeObject(newApiKey.Id),
                        ContentType = "application/json",
                        StatusCode = (int)HttpStatusCode.OK
                    };
                }
                return result;
            });
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> ExecuteRules(
                [FromQuery] Guid apiKey,
                string stringifiedObject = "")
        {
            var isTestMode =
                    //Test mode has been set explicitly
                    _settings.IsTestMode
                    //Or if it is detected that the user had clicked Try It Out
                    || !string.IsNullOrWhiteSpace(_context.Session.GetString("demoCompanyId"));

            return await ExecuteRules(apiKey, isTestMode, stringifiedObject);
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateBizField(
        [FromQuery] Guid apiKey,
        string stringifiedObject = "")
        {
            if (string.IsNullOrWhiteSpace(stringifiedObject))
            {
                using (StreamReader stream = new StreamReader(Request.Body))
                {
                    stringifiedObject = await stream.ReadToEndAsync();
                }
            }

            JToken jToken;

            try
            {
                jToken = JObject.Parse(stringifiedObject);
            }
            catch (JsonReaderException)
            {
                jToken = JArray.Parse(stringifiedObject);
            }

            var foundApiKey = await _service.GetApiKey(apiKey);
            var allowedDomains = foundApiKey.AllowedDomains?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

            if (!_settings.BaseEndpointUrl.Contains(Request.Host.Value)
                && allowedDomains.Count > 0
                && !allowedDomains.Any(x => x.Contains(Request.Host.Value))
                && !foundApiKey.AllowUpdateBizField)
            {
                return Unauthorized("Request is not authorized for domain " + Request.Host.Value);
            }

            var foundCompany = await _service.GetCompanyForApiKey(apiKey);

            var foundTopLevelField = await _service.GetTopLevelField(foundCompany.Id, foundApiKey.TopLevelFieldId);

            var convertedField = BizField_Helpers.ConvertJTokenToBizField(jToken, foundApiKey.TopLevelFieldId);

            var mergedField = BizField_Helpers.MergeJTokenToBizField(jToken, foundTopLevelField);

            return Ok();
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> ExecuteTestRules(
        [FromQuery] Guid apiKey,
        string stringifiedObject = "")
        {
            return await ExecuteRules(apiKey, true, stringifiedObject);
        }

        private async Task<IActionResult> ExecuteRules(
            Guid apiKey,
            bool isTestMode,
            string stringifiedObject)
        {
            if (string.IsNullOrWhiteSpace(stringifiedObject))
            {
                using (StreamReader stream = new StreamReader(Request.Body))
                {
                    stringifiedObject = await stream.ReadToEndAsync();
                }
            }

            JToken jToken;

            try
            {
                jToken = JObject.Parse(stringifiedObject);
                return await ExecuteRulesForObject(apiKey, isTestMode, jToken);
            }
            catch (JsonReaderException)
            {
                jToken = JArray.Parse(stringifiedObject);

                JArray result = new();

                foreach (var entry in (JArray)jToken)
                {
                    var currentResult = await ExecuteRulesForObject(apiKey, isTestMode, entry);
                    if (currentResult is ContentResult castResult)
                    {
                        result.Add(JsonConvert.DeserializeObject(castResult.Content));
                    }
                }

                return new ContentResult
                {
                    Content = JsonConvert.SerializeObject(result),
                    ContentType = "application/json",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
        }

        private async Task<IActionResult> ExecuteRulesForObject(Guid apiKey, bool isTestMode, JToken jToken)
        {
            IActionResult result;

            var foundApiKey = await _service.GetApiKey(apiKey);
            var allowedDomains = foundApiKey.AllowedDomains?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

            if (!_settings.BaseEndpointUrl.Contains(Request.Host.Value)
                && allowedDomains.Count > 0
                && !allowedDomains.Any(x => x.Contains(Request.Host.Value)))
            {
                result = Unauthorized("Request is not authorized for domain " + Request.Host.Value);
                return result;
            }

            var foundCompany = await _service.GetCompanyForApiKey(apiKey);

            if (foundCompany.CreditsUsed > foundCompany.CreditsAvailable + _settings.CreditGracePeriodAmount)
            {
                result = Unauthorized("No credits are currently available for this company");
                return result;
            }

            var foundTopLevelField = await _service.GetTopLevelField(foundCompany.Id, foundApiKey.TopLevelFieldId);

            var foundRules = await _service.GetRules(foundCompany.Id, foundTopLevelField.Id);

            //If the field is staged to be reset, clear the existing and add new
            if (foundTopLevelField.TopLevelFieldId == foundTopLevelField.Id
                && foundTopLevelField.ChildFields.Count == 0
                && foundRules.Count == 0)
            {
                await _service.DeleteTopLevelField(foundCompany.Id, foundTopLevelField.Id);

                await ControllerHelpers.BuildTopLevelField(
                    _service,
                    jToken.ToString(),
                    foundCompany.Id,
                    foundTopLevelField.Id,
                    foundApiKey);

                foundTopLevelField =
                    (await _service.GetTopLevelFieldsForCompany(foundCompany.Id)).First(x => x.Id == foundTopLevelField.Id);

                foundApiKey.TopLevelFieldId = foundTopLevelField.Id;
                await _service.SaveApiKey(foundApiKey);
            }

            var convertedField = BizField_Helpers.MergeJTokenToBizField(jToken, foundTopLevelField);

            var orderedRules = foundRules.OrderBy(x => x.GroupName).ThenBy(x => x.Name).ToList();

            var ruleResults = new Dictionary<string, bool>();

            foreach (var rule in orderedRules)
            {
                var ruleResult = rule.Execute(
                    convertedField,
                    isTestMode);

                ruleResults.Add(string.Concat(rule.GroupName, "_", rule.Name), ruleResult);
            }

            var returnObject = BizField_Helpers.ConvertBizFieldToJToken(convertedField);

            var resultToLog = new
            {
                InputObject = jToken,
                OutputObject = returnObject,
                RuleResults = ruleResults
            };

            var resultName =
                string.Concat(foundApiKey.Id.ToString("N"), "_", DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss_ffff"), ".json");

            if (!FeatureFlags.OfflineMode)
            {
                _backgroundServiceWorker.Execute(async (x) =>
                {
                    await x.IncrementBillingStats(foundCompany.Id, _settings.StripeBillingApiKey);
                    if (foundApiKey.CanLogResult())
                    {
                        var capturedResult = JsonConvert.SerializeObject(resultToLog);
                        ControllerHelpers.UploadToFtps(
                            foundApiKey.FtpsServer,
                            foundApiKey.FtpsPort,
                            foundApiKey.FtpsUsername,
                            foundApiKey.FtpsPassword,
                            foundApiKey.FtpsRemoteDirectory,
                            resultName,
                            capturedResult
                        );
                    }
                });
            }
            else
            {
                _backgroundServiceWorker.Execute(async (x) =>
                {
                    await Task.Run(() =>
                    {
                        if (foundApiKey.CanLogResult())
                        {
                            var capturedResult = JsonConvert.SerializeObject(resultToLog);
                            System.IO.File.WriteAllText(
                                Path.Combine(foundApiKey.LocalLoggingDirectory, resultName), capturedResult);
                        }
                    });                    
                });
            }

            result = new ContentResult
            {
                Content = JsonConvert.SerializeObject(returnObject),
                ContentType = "application/json",
                StatusCode = (int)HttpStatusCode.OK
            };

            return result;
        }
    }
}
