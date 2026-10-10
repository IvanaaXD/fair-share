using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FairShare.API.OpenApi
{
    /// <summary>
    /// Adds the JWT "Bearer" security scheme to the generated OpenAPI document, so Swagger UI shows
    /// the "Authorize" button and sends the token with every request. Follows the ASP.NET Core 10
    /// documentation sample (Microsoft.OpenApi 2.x types).
    /// </summary>
    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
        : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (!authenticationSchemes.Any(scheme => scheme.Name == "Bearer"))
                return;

            // The scheme itself, declared once at document level.
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Description = "Access token from POST /api/auth/login (without the \"Bearer \" prefix)."
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

            // Required on every operation. The anonymous endpoints in AuthController also show the
            // padlock, but they work the same with or without a token.
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}
