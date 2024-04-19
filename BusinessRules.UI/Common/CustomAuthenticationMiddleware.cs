using System.Security.Claims;


namespace BusinessRules.UI.Common
{
    public class CustomAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var bypassEmail = context.Session.GetString("bypassEmail");

            if (!string.IsNullOrWhiteSpace(bypassEmail))
            {
                var claims = new[] { new Claim(ClaimTypes.Email, bypassEmail) };

                var identity = new ClaimsIdentity(claims, "custom");

                // Create a ClaimsPrincipal with the identity
                var principal = new ClaimsPrincipal(identity);

                // Set the principal for the current request context
                context.User = principal;
            }

            // Continue processing the request pipeline
            await _next(context);
        }
    }

}
