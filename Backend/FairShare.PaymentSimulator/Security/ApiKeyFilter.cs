using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace FairShare.PaymentSimulator.Security;

/// <summary>
/// Only FairShare may call the simulator: every request must carry the secret key in the
/// X-Api-Key header. Keys are compared in constant time, so the comparison does not leak how
/// many characters were right.
/// </summary>
public sealed class ApiKeyFilter : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";

    private readonly byte[] _expected;

    public ApiKeyFilter(IOptions<SimulatorOptions> options)
    {
        _expected = Encoding.UTF8.GetBytes(options.Value.ApiKey);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var provided = Encoding.UTF8.GetBytes(context.HttpContext.Request.Headers[HeaderName].ToString());

        if (_expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(provided, _expected))
            return Results.Json(new ErrorResponse("unauthorized", "Missing or wrong API key."), statusCode: StatusCodes.Status401Unauthorized);

        return await next(context);
    }
}
