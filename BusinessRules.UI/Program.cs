using AspNetCore.SEOHelper;
using Auth0.AspNetCore.Authentication;
using Azure.Data.Tables;
using BusinessRules.Domain.Common;
using BusinessRules.Domain.Services;
using BusinessRules.Rules.Components;
using BusinessRules.ServiceLayer;
using BusinessRules.UI.Common;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json.Serialization;
using Stripe;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IRazorPartialToStringRenderer, RazorPartialToStringRenderer>();
builder.Services.AddTransient<IBackgroundServiceWorker, BackgroundServiceWorker>();
builder.Services.AddTransient<IBackgroundStorageRepository, BackgroundStorageRepository>();

Console.WriteLine("Executing in offline mode? " + FeatureFlags.OfflineMode);

if (FeatureFlags.OfflineMode)
{
    DynamicOperand.FunctionUrl = "";
    DynamicComparator.FunctionUrl = "";

    var appSettings = builder.Configuration.Get<AppSettings>();

    Console.WriteLine("Configured Storage Path: " + builder.Configuration.GetSection("AppSettings")["JsonStorageBasePath"]);
    Console.WriteLine("Configured Base URL: " + builder.Configuration.GetSection("AppSettings")["BaseEndpointUrl"]);

    // Add our Config object so it can be injected
    builder.Services.Configure<AppSettings>(config =>
    {
        config.AzureStorageConnectionString = "";
        config.AuthenticationSignupRedirectUri = "";
        config.AuthenticationLoginRedirectUri = "";
        config.DynamicComparatorFunctionUrl = "";
        config.DynamicOperandFunctionUrl = "";
        config.IsTestMode = false;
        config.StorageMode = "json";
        config.JsonStorageBasePath = builder.Configuration.GetSection("AppSettings")["JsonStorageBasePath"];
        config.CreditGracePeriodAmount = 100000;
        config.NewCompanyFreeCreditAmount = 100000;
        config.StripeBillingApiKey = "";
        config.StripePriceId = "";
        config.BaseEndpointUrl = builder.Configuration.GetSection("AppSettings")["BaseEndpointUrl"];
    });
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
} else
{
    builder.Services.AddAzureClients(clientBuilder =>
    {
        clientBuilder.AddTableServiceClient(
            builder.Configuration.GetSection("AppSettings")["AzureStorageConnectionString"]);
    });

    //Initializes these from config when the app starts - not ideal but it should work
    DynamicOperand.FunctionUrl = builder.Configuration.GetSection("AppSettings")["DynamicOperandFunctionUrl"];
    DynamicComparator.FunctionUrl = builder.Configuration.GetSection("AppSettings")["DynamicComparatorFunctionUrl"];

    // Add our Config object so it can be injected
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

    builder.Services.AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = builder.Configuration["Auth0:Domain"];
        options.ClientId = builder.Configuration["Auth0:ClientId"];
    });
}

// Set limits for form options, to accept big data
builder.Services.Configure<FormOptions>(x =>
{
    x.BufferBody = false;
    x.KeyLengthLimit = int.MaxValue;
    x.ValueLengthLimit = int.MaxValue;
    x.ValueCountLimit = int.MaxValue;
    x.MultipartHeadersCountLimit = int.MaxValue;
    x.MultipartHeadersLengthLimit = int.MaxValue;
    x.MultipartBoundaryLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue;
});

builder.Services.AddScoped<IBusinessRulesService, BusinessRulesService>();

builder.Services.AddControllersWithViews()
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOriginsPolicy",
        builder => builder.SetIsOriginAllowed(origin => true).AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRobotsTxt(app.Environment.ContentRootPath); 

app.UseStaticFiles();
app.UseCookiePolicy();

app.UseRouting();

app.UseCors();

app.UseSession();

app.UseMiddleware<CustomAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});

if (FeatureFlags.OfflineMode)
{
    app.Run(builder.Configuration.GetSection("AppSettings")["BaseEndpointUrl"]);
} else
{
    app.Run();
}


//Just doing this to avoid it impacting code coverage stats
[ExcludeFromCodeCoverage] public partial class Program { }