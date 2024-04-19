using BusinessRules.Domain.Common;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;
using BusinessRules.Licensing;
using BusinessRules.UI.Common;
using BusinessRules.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace BusinessRules.UI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBusinessRulesService _service;
        private readonly HttpContext _context;
        private readonly AppSettings _settings;
        private readonly IRazorPartialToStringRenderer _razorPartialToStringRenderer;


        public HomeController(
            ILogger<HomeController> logger,
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
        public async Task<IActionResult> Index()
        {
            return await Task.Run(async () =>
            {
                _context.Session.SetInt32("init", 0);

                if (FeatureFlags.OfflineMode)
                {
                    if (string.IsNullOrWhiteSpace(_context.Session.GetString("bypassEmail")))
                    {
                        var emailAddress = "offline@localhost";
                        _context.Session.SetString("bypassEmail", emailAddress);

                        if (System.IO.File.Exists(Path.Combine(_settings.JsonStorageBasePath, "key.bin")))
                        {
                            var existingCompanyIdRaw = System.IO.File.ReadAllBytes(Path.Combine(_settings.JsonStorageBasePath, "key.bin"));
                            string existingCompanyId = new LicenseManager().DecryptCompanyId(existingCompanyIdRaw).ToString();
                            _context.Session.SetString("CompanyId", existingCompanyId);
                        } else
                        {
                            return Unauthorized();
                        }
                    }
                }

                var bypassEmail = _context.Session.GetString("bypassEmail");

                if (_context.User.Identity.IsAuthenticated || !string.IsNullOrWhiteSpace(bypassEmail))
                {
                    return await Dashboard();
                }
                else
                {
                    return View("Index");
                }
            });
        }

        public async Task<IActionResult> Dashboard()
        {
            return await Task.Run(async () =>
            {
                //This is the one place where an email address isn't required to be in a claim
                //It handles the Try It Out > Sign Up flow, which is the only time "bypassEmail" is present
                string emailAddress = "";
                var bypassEmail = _context.Session.GetString("bypassEmail");
                if (!string.IsNullOrWhiteSpace(bypassEmail))
                {
                    emailAddress = bypassEmail;
                }
                else
                {
                    emailAddress = ControllerHelpers.ParseClaimsToGetEmail(User);
                }

                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    return Unauthorized();
                }

                var demoCompanyIdRaw = _context.Session.GetString("demoCompanyId");

                BizCompany foundCompany = await _service.GetCompanyForUser(emailAddress);

                if (foundCompany is NullBizCompany
                    && !string.IsNullOrWhiteSpace(demoCompanyIdRaw)
                    && Guid.TryParse(demoCompanyIdRaw, out var parsedCompanyId))
                {
                    foundCompany = await _service.GetCompany(parsedCompanyId);
                    _context.Session.SetString("demoCompanyId", "");
                    _context.Session.SetString("CompanyId", foundCompany.Id.ToString());

                    var firstUser = foundCompany.Users.FirstOrDefault();
                    firstUser.EmailAddress = emailAddress;
                    firstUser.Role = UserRole.Administrator;

                    await _service.SaveCompany(foundCompany);
                }
                else if (foundCompany is NullBizCompany)
                {
                    var user = new BizUser(emailAddress, UserRole.Administrator);
                    foundCompany = await ControllerHelpers.GenerateNewCompany(_service, _settings, user, false);
                    _context.Session.SetString("CompanyId", foundCompany.Id.ToString());
                }

                return await ControllerHelpers.SelectCompany(this, _service, _context, _settings, emailAddress, foundCompany.Id.ToString(), false);
            });
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View("Privacy");
        }

        [AllowAnonymous]
        public async Task<IActionResult> TryItOut()
        {
            return await Task.Run(async () =>
            {
                var demoCompanyIdRaw = _context.Session.GetString("demoCompanyId");

                string emailAddress = "";
                string companyId = "";

                if (string.IsNullOrWhiteSpace(demoCompanyIdRaw))
                {
                    var user = new BizUser(Guid.NewGuid().ToString() + "@mybizrules.com", UserRole.Developer);
                    emailAddress = user.EmailAddress;
                    var newCompany = await ControllerHelpers.GenerateNewCompany(_service, _settings, user, true);
                    companyId = newCompany.Id.ToString();
                    _context.Session.SetString("demoCompanyId", newCompany.Id.ToString());
                    _context.Session.SetString("CompanyId", newCompany.Id.ToString());
                }
                else
                {
                    var foundCompany = await _service.GetCompany(Guid.Parse(demoCompanyIdRaw));
                    _context.Session.SetString("CompanyId", foundCompany.Id.ToString());
                    var foundUsers = await _service.GetUsersForCompany(foundCompany.Id);
                    emailAddress = foundUsers.FirstOrDefault()?.EmailAddress;
                    companyId = foundCompany.Id.ToString();
                }

                return await ControllerHelpers.SelectCompany(this, _service, _context, _settings, emailAddress, companyId, true);
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public async Task<IActionResult> Error()
        {
            return await Task.Run(() =>
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? _context.TraceIdentifier });
            });

        }
    }
}