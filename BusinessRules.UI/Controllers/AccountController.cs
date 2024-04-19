using BusinessRules.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using BusinessRules.Domain.Common;
using Microsoft.Extensions.Options;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics.CodeAnalysis;
using BusinessRules.Domain.Organization;

namespace BusinessRules.UI.Controllers
{
    [ExcludeFromCodeCoverage]
    [Authorize]
	public class AccountController : Controller
    {
        private ILogger<AccountController> _logger;
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;

        public AccountController(
            ILogger<AccountController> logger,
            IBusinessRulesService service,
            IHttpContextAccessor context,
            IOptions<AppSettings> settings)
        {
            _service = service;
            _context = context.HttpContext;
            _settings = settings.Value;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task Login()
        {
            var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                .WithParameter("redirect_uri", _settings.AuthenticationLoginRedirectUri)
                .WithScope("email")
                .Build();

            await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SignUp([FromQuery] string emailAddress)
        {
            var existingCompany = await _service.GetCompanyForUser(emailAddress);

            if (emailAddress is null)
            {
                return Redirect("/Home/Index");
            }
            else if (existingCompany is not NullBizCompany)
            {
                return BadRequest("Email address is already associated with a company. Please log in instead.");
            }
            else
            {
                _context.Session.SetString("bypassEmail", emailAddress);
                return Redirect("/Home/Dashboard");
            }
        }

        public async Task<IActionResult> Logout()
        {
            var bypassEmail = _context.Session.GetString("bypassEmail");
            if (!string.IsNullOrWhiteSpace(bypassEmail))
            {
                _context.Session.SetString("bypassEmail", "");
                return Redirect("/Home/Index");
            }

            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                .WithRedirectUri(_settings.BaseEndpointUrl)
                .Build();

            await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/Home/Index");
        }
    }
}
