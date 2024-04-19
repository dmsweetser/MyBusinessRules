using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Fields;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;
using BusinessRules.UI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BusinessRules.UI.Controllers
{
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly ILogger<CompanyController> _logger;
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;
        private readonly IRazorPartialToStringRenderer _razorPartialToStringRenderer;

        public bool IsMockOnly { get; set; } = false;

        public CompanyController(
            ILogger<CompanyController> logger,
            IBusinessRulesService service,
            IHttpContextAccessor httpContextAccessor,
            IOptions<AppSettings> settings,
            IRazorPartialToStringRenderer razorPartialToStringRenderer)
        {
            _logger = logger;
            _service = service;
            _context = httpContextAccessor.HttpContext;
            _settings = settings.Value;
            _razorPartialToStringRenderer = razorPartialToStringRenderer;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> SaveChanges(
            [FromForm] AdministratorDTO currentCompanyDto)
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                foundCompany.SyncValues(currentCompanyDto);
                foreach (var apiKey in currentCompanyDto.ApiKeys)
                {
                    await _service.SaveApiKey(apiKey);
                }
                await _service.SaveCompany(foundCompany);
                var currentResult = await ControllerHelpers.SelectCompany(
                    this, _service, _context, _settings, emailAddress, foundCompany.Id.ToString(), IsMockOnly, _razorPartialToStringRenderer) as ObjectResult;
                return Ok(currentResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddNewApiKey()
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                var foundTopLevelFields = await _service.GetTopLevelFieldsForCompany(companyId);
                var newApiKey = new BizApiKey(foundCompany.Id, foundTopLevelFields.First().Id);
                await _service.SaveApiKey(newApiKey);
                var currentResult = await ControllerHelpers.SelectCompany(
                    this, _service, _context, _settings, emailAddress, foundCompany.Id.ToString(), IsMockOnly, _razorPartialToStringRenderer) as ObjectResult;
                return Ok(currentResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> DownloadKeyFile()
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    return Ok();
                }

                var companyId = _context.Session.GetString("CompanyId");
                var foundCompany = await _service.GetCompany(Guid.Parse(companyId));

                if (new Licensing.LicenseManager().AllowOfflineMode(foundCompany.LastBilledDate))
                {
                    var key = new Licensing.LicenseManager().EncryptCompanyId(foundCompany.Id);
                    return File(key, "application/octet-stream", "key.bin");
                }
                else
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Ok();
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveApiKey(Guid apiKeyId)
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                var foundApiKeys = await _service.GetApiKeysForCompany(companyId);
                if (foundApiKeys.Count == 0)
                {
                    return Redirect("/Home/Error");
                }
                else
                {
                    var matchingApiKey = foundApiKeys.FirstOrDefault(x => x.Id == apiKeyId);
                    await _service.DeleteApiKey(foundCompany.Id, apiKeyId);
                    var currentResult = await ControllerHelpers.SelectCompany(
                        this, _service, _context, _settings, emailAddress, foundCompany.Id.ToString(), IsMockOnly, _razorPartialToStringRenderer) as ObjectResult;
                    return Ok(currentResult.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddNewUser()
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);
                
                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                var newUser = new BizUser("newuser" + DateTime.Now.Ticks + "@yourdomain.com", UserRole.BusinessUser);
                foundCompany.Users.Add(newUser);
                await _service.SaveCompany(foundCompany);
                var currentResult = await ControllerHelpers.SelectCompany(
                    this, _service, _context, _settings, emailAddress, foundCompany.Id.ToString(), IsMockOnly, _razorPartialToStringRenderer) as ObjectResult;
                return Ok(currentResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveUser(string userId)
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                await _service.DeleteUser(foundCompany, userId);
                var currentResult = await ControllerHelpers.SelectCompany(
                    this, _service, _context, _settings, emailAddress, foundCompany.Id.ToString(), IsMockOnly, _razorPartialToStringRenderer) as ObjectResult;
                return Ok(currentResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveCompany()
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                var foundFields = await _service.GetTopLevelFieldsForCompany(companyId);
                var foundUsers = await _service.GetUsersForCompany(companyId);
                var foundApiKeys = await _service.GetApiKeysForCompany(companyId);

                foreach (var field in foundFields)
                {
                    var foundRules = await _service.GetRules(companyId, field.Id);
                    foreach (var rule in foundRules)
                    {
                        await _service.DeleteRule(companyId, field.Id, rule);
                    }
                    await _service.DeleteTopLevelField(companyId, field.Id);
                }

                foreach (var apiKey in foundApiKeys)
                {
                    await _service.DeleteApiKey(foundCompany.Id, apiKey.Id);
                }

                foreach (var user in foundUsers)
                {
                    await _service.DeleteUser(foundCompany, user.EmailAddress);
                }

                await _service.DeleteCompany(foundCompany);

                return Redirect("/Home/Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ApplyCreditCode(Guid codeId)
        {
            try
            {
                var emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);

                if (emailAddress is null)
                {
                    return Unauthorized();
                }

                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundCompany = await _service.GetCompany(companyId);
                
                await _service.ApplyCreditCode(foundCompany, codeId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }
    }
}
