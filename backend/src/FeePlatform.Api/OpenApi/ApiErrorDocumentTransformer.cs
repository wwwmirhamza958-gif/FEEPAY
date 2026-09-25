using FeePlatform.Api.Auth;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FeePlatform.Api.OpenApi;

/// <summary>Registers CONTRACT §3 ApiError / ApiErrorDetail schemas on the OpenAPI document.</summary>
public sealed class ApiErrorDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title ??= "Fee Platform API";
        document.Info.Version ??= "v1";
        document.Info.Description ??=
            "Fee Platform API. Error bodies use CONTRACT section 3 envelope (components.schemas.ApiError).";

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        document.Components.Schemas["ApiErrorDetail"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string> { "field", "code", "message", "messageKey" },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["field"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String | JsonSchemaType.Null,
                    Description = "Property path or null for non-field errors.",
                },
                ["code"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["messageKey"] = new OpenApiSchema { Type = JsonSchemaType.String },
            },
        };

        document.Components.Schemas["ApiError"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string>
            {
                "code", "message", "messageKey", "correlationId", "details", "retryable",
            },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["code"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description = "SCREAMING_SNAKE_CASE stable code.",
                },
                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["messageKey"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["correlationId"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "uuid",
                },
                ["details"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchemaReference("ApiErrorDetail", document),
                },
                ["retryable"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
            },
        };

        document.Components.Schemas["ApiErrorEnvelope"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string> { "error" },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["error"] = new OpenApiSchemaReference("ApiError", document),
            },
        };

        return Task.CompletedTask;
    }
}
