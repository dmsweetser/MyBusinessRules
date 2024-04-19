using BusinessRules.TestMVC.Common;
using BusinessRules.TestMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MyBusinessRules;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BusinessRules.TestMVC.Controllers
{
    public class HomeController : Controller
    {
        public static string MyBusinessRulesQuoteApiKey { get; set; }
        public static string MyBusinessRulesPolicyApiKey { get; set; }
        private static MyBusinessRulesClient _myBusinessRulesClientQuote;
        private static MyBusinessRulesClient _myBusinessRulesClientPolicy;
        private static Settings _settings;

        private readonly ILogger<HomeController> _logger;

        // Static ConcurrentDictionary to store Quote and Policy instances
        private static ConcurrentDictionary<string, Quote> quoteDataStore = new ConcurrentDictionary<string, Quote>();
        private static ConcurrentDictionary<string, Policy> policyDataStore = new ConcurrentDictionary<string, Policy>();

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (_settings == null)
            {
                _settings = new Settings()
                {
                    MyBusinessRulesBaseUrl = "https://localhost:7282"
                };
            }
            return View(_settings);
        }

        [HttpPost] 
        public async Task<IActionResult> ApplySettings([FromForm] Settings settings) {
            return await Task.Run(() =>
            {
                var quoteApiKey = settings.MyBusinessRulesQuoteApiKey;
                MyBusinessRulesQuoteApiKey = quoteApiKey.ToString();
                var policyApiKey = settings.MyBusinessRulesPolicyApiKey;
                MyBusinessRulesQuoteApiKey = quoteApiKey.ToString();
                MyBusinessRulesClient.BaseUrl = settings.MyBusinessRulesBaseUrl;
                _myBusinessRulesClientQuote = new MyBusinessRulesClient(quoteApiKey.ToString());
                _myBusinessRulesClientPolicy = new MyBusinessRulesClient(policyApiKey.ToString());
                _settings = settings;
                return RedirectToAction("Index");
            });
        }

        public IActionResult Quote()
        {
            // Assuming you want to get the Quote and display it on the Index page
            Quote quote = GetOrCreateQuote();
            return View(quote);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuote([FromForm] Quote updatedQuote)
        {
            var result = await _myBusinessRulesClientQuote.ExecuteRulesAsync(new { Quote = updatedQuote });
            if (result.wasSuccessful)
            {
                updatedQuote = result.outputObject.Quote;
            };

            quoteDataStore.AddOrUpdate("default", updatedQuote, (key, existingQuote) => updatedQuote);
            return RedirectToAction("Quote");
        }

        [HttpPost]
        public async Task<IActionResult> IssueQuote([FromForm] Quote issuedQuote)
        {
            var result = await _myBusinessRulesClientQuote.ExecuteRulesAsync(new { Quote = issuedQuote });
            if (result.wasSuccessful)
            {
                issuedQuote = result.outputObject.Quote;
            };

            // Assuming you want to issue the Quote as a Policy
            // Convert the Quote to a Policy and add it to the data store
            Policy policy = Converters.PolicyConverter.ConvertToPolicy(issuedQuote);
            policyDataStore.AddOrUpdate("issued", policy, (key, existingPolicy) => policy);

            // Return the Policy view
            return RedirectToAction("Policy");
        }

        public IActionResult Policy()
        {
            // Assuming you want to get an existing Policy from the data store
            // For simplicity, we'll just return the first policy in the data store
            if (policyDataStore.TryGetValue("issued", out Policy policy))
            {
                return View(policy);
            }

            // If no policy exists, you might want to handle this case accordingly
            return RedirectToAction("Quote");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePolicy([FromForm] Policy updatedPolicy)
        {
            var result = await _myBusinessRulesClientPolicy.ExecuteRulesAsync(new { Policy = updatedPolicy });
            if (result.wasSuccessful)
            {
                updatedPolicy = result.outputObject.Policy;
            };

            // Assuming you want to update the existing Policy in the data store
            policyDataStore.AddOrUpdate("issued", updatedPolicy, (key, existingPolicy) => updatedPolicy);
            return RedirectToAction("Policy");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private Quote GetOrCreateQuote()
        {
            // Logic to get or create a new Quote
            // For simplicity, we'll just create a new Quote if it doesn't exist
            if (quoteDataStore.TryGetValue("default", out Quote existingQuote))
            {
                return existingQuote;
            }

            Quote newQuote = new(DateTime.Now);
            quoteDataStore.TryAdd("default", newQuote);
            return newQuote;
        }
    }
}
