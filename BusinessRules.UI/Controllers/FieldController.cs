using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BusinessRules.Domain.Fields;
using Microsoft.AspNetCore.Authorization;
using BusinessRules.UI.Common;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Rules.Components;
using Newtonsoft.Json;

namespace BusinessRules.UI.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class FieldController : Controller
    {
        private readonly ILogger<FieldController> _logger;
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;
        private readonly IRazorPartialToStringRenderer _razorPartialToStringRenderer;

        public FieldController(
            ILogger<FieldController> logger,
            IBusinessRulesService service,
            IHttpContextAccessor httpContextAccessor,
            IOptions<AppSettings> settings,
            IRazorPartialToStringRenderer razorPartialToStringRenderer)
        {
            _service = service;
            _context = httpContextAccessor.HttpContext;
            _settings = settings.Value;
            _logger = logger;
            _razorPartialToStringRenderer = razorPartialToStringRenderer;
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveFieldChanges(
            [FromForm] BizField currentField)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var existingField = await _service.GetTopLevelField(companyId, currentField.Id);
                currentField.RuleIds = existingField.RuleIds;
                await _service.SaveTopLevelField(companyId, currentField);
                currentField.FlattenFieldsWithDescription(true);
                return Ok(
                    await ControllerHelpers.GetComponent(
                        _service,
                        _settings,
                        companyId,
                        existingField.Id,
                        _context.Session.GetString("EmailAddress"),
                        "~/Views/Shared/Components/_DeveloperComponent.cshtml",
                        _razorPartialToStringRenderer)
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> AddNewChildField(
                    [FromForm] BizField currentField,
                    [FromQuery] Guid parentFieldId)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));

                var existingField = await _service.GetTopLevelField(companyId, currentField.Id);
                currentField.RuleIds = existingField.RuleIds;
                await _service.SaveTopLevelField(companyId, currentField);

                var newField = await _service.AddNewChildField(companyId, currentField.Id, parentFieldId);
                var foundField = await _service.GetTopLevelField(companyId, currentField.Id);
                currentField.FlattenFieldsWithDescription(true);
                return Ok(
                    await ControllerHelpers.GetComponent(
                        _service,
                        _settings,
                        companyId,
                        foundField.Id,
                        _context.Session.GetString("EmailAddress"),
                        "~/Views/Shared/Components/_DeveloperComponent.cshtml",
                        _razorPartialToStringRenderer)
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }


        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> AddNewTopLevelField()
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));

                var newField = new BizField("New Field");
                await _service.SaveTopLevelField(companyId, newField);

                var newApiKey = new BizApiKey(companyId, newField.Id);
                await _service.SaveApiKey(newApiKey);

                var demoCompanyId = _context.Session.GetString("demoCompanyId");

                if (!string.IsNullOrWhiteSpace(demoCompanyId))
                {
                    return Redirect("/Home/TryItOut");
                }
                else
                {
                    return Redirect("/Home/Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveField(
                [FromForm] BizField currentField,
                [FromQuery] Guid parentFieldId,
                [FromQuery] Guid childFieldId)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));

                if (currentField.Id == childFieldId)
                {
                    return Redirect("/Home/Error");
                }
                else
                {
                    await _service.DeleteChildField(companyId, currentField.Id, parentFieldId, childFieldId);
                }

                var foundField = await _service.GetTopLevelField(companyId, currentField.Id);
                foundField.FlattenFieldsWithDescription(true);
                return Ok(
                    await ControllerHelpers.GetComponent(
                        _service,
                        _settings,
                        companyId,
                        foundField.Id,
                        _context.Session.GetString("EmailAddress"),
                        "~/Views/Shared/Components/_DeveloperComponent.cshtml",
                        _razorPartialToStringRenderer)
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> BuildTopLevelField(
                    [FromForm] string stringifiedObject)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                await ControllerHelpers.BuildTopLevelField(
                    _service, 
                    stringifiedObject, 
                    companyId, 
                    Guid.NewGuid(),
                    new NullBizApiKey());

                var demoCompanyId = _context.Session.GetString("demoCompanyId");

                if (!string.IsNullOrWhiteSpace(demoCompanyId))
                {
                    return Redirect("/Home/TryItOut");
                }
                else
                {
                    return Redirect("/Home/Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }



        [HttpGet("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateInitialField()
        {
            try
            {
                var demoCompanyId = _context.Session.GetString("demoCompanyId");
                var companyId = _context.Session.GetString("CompanyId");

                var result = await FieldAndRuleHelpers.GenerateTopLevelFieldAndRules(_service, Guid.Parse(companyId));

                var foundCompany = await _service.GetCompany(Guid.Parse(companyId));

                var existingApiKeys = await _service.GetApiKeysForCompany(foundCompany.Id);
                if (!existingApiKeys.Any(x => x.TopLevelFieldId == result.TopLevelField.Id))
                {
                    var newApiKey = new BizApiKey(foundCompany.Id, result.TopLevelField.Id);
                    await _service.SaveApiKey(newApiKey);
                }                

                var allFields = await _service.GetTopLevelFieldsForCompany(Guid.Parse(companyId));
                foreach (var field in allFields)
                {
                    if (field.Id != result.TopLevelField.Id)
                    {
                        await _service.DeleteTopLevelField(Guid.Parse(companyId), field.Id);
                    }
                }

                if (!string.IsNullOrWhiteSpace(demoCompanyId?.ToString()))
                {
                    return Redirect("/Home/TryItOut");
                }
                else
                {
                    return Redirect("/Home/Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> AddNewDynamicComponent(
                [FromQuery] Guid parentFieldId,
                [FromQuery] bool isComparator)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));

                var foundField = await _service.GetTopLevelField(companyId, parentFieldId);

                DynamicComponent newComponent;

                if (isComparator)
                {
                    var script =
@"
return true;
";
                    newComponent = new DynamicComparator("New Comparator", script);
                }
                else
                {
                    var script =
@"
return """";
";
                    newComponent = new DynamicOperand("New Operand", script);
                }

                foundField.DynamicComponents.Add(newComponent);
                await _service.SaveTopLevelField(companyId, foundField);

                return Ok(
                    await ControllerHelpers.GetComponent(
                        _service,
                        _settings,
                        companyId,
                        foundField.Id,
                        _context.Session.GetString("EmailAddress"),
                        "~/Views/Shared/Components/_DeveloperComponent.cshtml",
                        _razorPartialToStringRenderer)
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveDynamicComponent(
                [FromQuery] Guid parentFieldId,
                [FromQuery] Guid componentId)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));

                var foundField = await _service.GetTopLevelField(companyId, parentFieldId);

                foundField.DynamicComponents =
                    foundField.DynamicComponents.Where(x => x.Id != componentId).ToList();

                await _service.SaveTopLevelField(companyId, foundField);

                return Ok(
                    await ControllerHelpers.GetComponent(
                        _service,
                        _settings,
                        companyId,
                        foundField.Id,
                        _context.Session.GetString("EmailAddress"),
                        "~/Views/Shared/Components/_DeveloperComponent.cshtml",
                        _razorPartialToStringRenderer)
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }
    }
}
