using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using BusinessRules.UI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;

namespace BusinessRules.UI.Controllers
{
    [Authorize]
    public class PartialController : Controller
    {
        private readonly ILogger<PartialController> _logger;
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;
        private readonly IRazorPartialToStringRenderer _razorPartialToStringRenderer;


        public PartialController(
            ILogger<PartialController> logger,
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

        [AllowAnonymous]
        public async Task<IActionResult> GetBusinessUserComponent(Guid topLevelFieldId)
        {
            Guid companyId;
            if (_context.User.Identity.IsAuthenticated)
            {
                companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
            }
            else
            {
                companyId = Guid.Parse(_context.Session.GetString("demoCompanyId"));
            }

            var result =
                await ControllerHelpers.GetComponent(
                    _service,
                    _settings,
                    companyId,
                    topLevelFieldId,
                    _context.Session.GetString("EmailAddress"),
                    "~/Views/Shared/Components/_BusinessUserComponent.cshtml",
                    _razorPartialToStringRenderer
                    );

            return Ok(result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetEndUserComponent(Guid topLevelFieldId)
        {
            Guid companyId;
            if (_context.User.Identity.IsAuthenticated)
            {
                companyId = Guid.Parse(_context.Session.GetString("CompanyId"));
            }
            else
            {
                companyId = Guid.Parse(_context.Session.GetString("demoCompanyId"));
            }

            var result =
                await ControllerHelpers.GetComponent(
                    _service,
                    _settings,
                    companyId,
                    topLevelFieldId,
                    _context.Session.GetString("EmailAddress"),
                    "~/Views/Shared/Components/_EndUserComponent.cshtml",
                    _razorPartialToStringRenderer
                    );

            return Ok(result);
        }
    }
}