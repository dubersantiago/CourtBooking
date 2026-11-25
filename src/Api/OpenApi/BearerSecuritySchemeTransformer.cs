using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Api.OpenApi;

/// <summary>
/// Declara el esquema de seguridad Bearer (JWT) en el documento OpenAPI y lo
/// marca como requerido solo en los endpoints que tienen [Authorize].
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string SchemeName = JwtBearerDefaults.AuthenticationScheme; // "Bearer"

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == SchemeName))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Pega el accessToken devuelto por /api/auth/login"
        };
    }

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        var requiresAuth = metadata.OfType<IAuthorizeData>().Any()
                           && !metadata.OfType<IAllowAnonymous>().Any();

        if (requiresAuth)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = SchemeName
                    }
                }] = Array.Empty<string>()
            });
        }

        return Task.CompletedTask;
    }
}
