using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Rules;
using BusinessRules.Domain.Rules.Component;
using BusinessRules.Domain.Services;
using BusinessRules.Rules;
using BusinessRules.Rules.Components;
using BusinessRules.Rules.Extensions;
using BusinessRules.UI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BusinessRules.UI.Controllers
{
    [Authorize]
    public class RuleController : Controller
    {
        private readonly ILogger<RuleController> _logger;
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;
        private readonly IRazorPartialToStringRenderer _razorPartialToStringRenderer;

        public RuleController(
            ILogger<RuleController> logger,
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
        public async Task<IActionResult> SaveRules(
            [FromForm] BusinessUserDTO currentDto,
            Guid topLevelFieldId,
            Guid ruleId)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var topLevelField = await _service.GetTopLevelField(companyId, topLevelFieldId);
                var foundRule = currentDto.Rules.FirstOrDefault(x => x.Id == ruleId);

                var convertedRule = foundRule.ToRule(topLevelField);
                 
                if (convertedRule.IsActivatedTestOnly)
                {
                    convertedRule.IsActivated = false;
                }

                await _service.SaveRule(companyId, convertedRule);

                var result = await _razorPartialToStringRenderer.RenderToString(
                     "~/Views/Shared/EditorTemplates/BusinessUserDTO.cshtml",
                    topLevelField.ToBusinessUserDTO(
                        await _service.GetRules(companyId, topLevelField.Id), 
                        _settings.BaseEndpointUrl, 
                        _settings.IsTestMode));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> AddNewRule(Guid topLevelFieldId)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var topLevelField = await _service.GetTopLevelField(companyId, topLevelFieldId);
                var newRule = new BizRule("_New Rule " + DateTime.Now.ToString("G"), topLevelField);
                newRule.Add(newRule.Then().FirstOrDefault());
                topLevelField.RuleIds.Add(newRule.Id);
                await _service.SaveRule(companyId, newRule);
                await _service.SaveTopLevelField(companyId, topLevelField);

                return Ok(
                    await ControllerHelpers.GetComponent(
                        _service,
                        _settings,
                        companyId,
                        topLevelFieldId,
                        _context.Session.GetString("EmailAddress"),
                        "~/Views/Shared/Components/_BusinessUserComponent.cshtml",
                        _razorPartialToStringRenderer
                        )
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
        public async Task<IActionResult> AddNewComponent(
            Guid topLevelFieldId,
            Guid ruleId,
            string nextComponentKey,
            int ruleIndex)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundRule = await _service.GetRuleById(companyId, topLevelFieldId, ruleId);
                var topLevelField = await _service.GetTopLevelField(companyId, topLevelFieldId);

                var nextComponents = foundRule.GetNextComponents(topLevelField, _settings.IsTestMode);

                if (nextComponents.Any(x => x.Key == nextComponentKey))
                {
                    var foundComponent = nextComponents.FirstOrDefault(x => x.Key == nextComponentKey).Value.ToComponent();

                    if (foundComponent is DynamicComponent castComponent)
                    {
                        var matchingComponent =
                            topLevelField.DynamicComponents.FirstOrDefault(x => x.DefinitionId == foundComponent.DefinitionId);
                        castComponent.Description = matchingComponent.Description;
                        castComponent.Body = matchingComponent.Body;
                        foundRule.RuleSequence.Add(castComponent);
                    }
                    else
                    {
                        foundRule.RuleSequence.Add(foundComponent);
                    }

                    await _service.SaveRule(companyId, foundRule);
                }

                var result = await _razorPartialToStringRenderer.RenderToString(
                     "~/Views/Shared/EditorTemplates/BusinessUserDTO.cshtml",
                    topLevelField.ToBusinessUserDTO(
                        await _service.GetRules(companyId, topLevelField.Id),
                        _settings.BaseEndpointUrl,
                        _settings.IsTestMode));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveLatestComponent(
                    Guid topLevelFieldId,
                    Guid ruleId,
                    int ruleIndex)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundRule = await _service.GetRuleById(companyId, topLevelFieldId, ruleId);
                var topLevelField = await _service.GetTopLevelField(companyId, topLevelFieldId);
                foundRule.RuleSequence.RemoveAt(foundRule.RuleSequence.Count - 1);

                if (foundRule.RuleSequence.Count == 0)
                {
                    foundRule.Add(new IfAntecedent());
                }

                await _service.SaveRule(companyId, foundRule);

                var result = await _razorPartialToStringRenderer.RenderToString(
                     "~/Views/Shared/EditorTemplates/BusinessUserDTO.cshtml",
                    topLevelField.ToBusinessUserDTO(
                        await _service.GetRules(companyId, topLevelField.Id),
                        _settings.BaseEndpointUrl,
                        _settings.IsTestMode));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveRule(
                    Guid topLevelFieldId,
                    Guid ruleId)
        {
            try
            {
                var companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
                var foundRule = await _service.GetRuleById(companyId, topLevelFieldId, ruleId);
                var topLevelField = await _service.GetTopLevelField(companyId, topLevelFieldId);

                if (foundRule is NullBizRule)
                {
                    _logger.LogError($"Rule not found with rule ID {ruleId} for field with ID {topLevelFieldId}");
                    return Redirect("/Home/Error");
                }

                await _service.DeleteRule(companyId, topLevelFieldId, foundRule);

                var foundRules = await _service.GetRules(companyId, topLevelField.Id);

                if (foundRules.Count == 0)
                {
                    var demoCompanyId = _context.Session.GetString("demoCompanyId");
                    if (!string.IsNullOrWhiteSpace(demoCompanyId))
                    {
                        return Redirect("Home/TryItOut");
                    } else
                    {
                        return Redirect("Home/Dashboard");
                    }
                    
                }
                else
                {
                    return Ok(
                        await ControllerHelpers.GetComponent(
                            _service,
                            _settings,
                            companyId,
                            topLevelFieldId,
                            _context.Session.GetString("EmailAddress"),
                            "~/Views/Shared/Components/_BusinessUserComponent.cshtml",
                            _razorPartialToStringRenderer
                            )
                        );

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect("/Home/Error");
            }
        }
    }
}
