using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServiceDesk.Api.Infrastructure;

// Applied to every controller. /health and the docs are not controllers, so they stay open.
public class ApiKeyFilter(IConfiguration configuration) : IAuthorizationFilter
{
    public const string HeaderName = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = configuration["ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "API key is not configured on the server."
            }) { StatusCode = StatusCodes.Status500InternalServerError };
            return;
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        // Constant-time comparison: doesn't leak how many characters matched.
        var valid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));

        if (!valid)
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Missing or invalid API key.",
                Detail = $"Send a valid {HeaderName} header."
            });
        }
    }
}