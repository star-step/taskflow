using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskFlow.Api.Infrastructure;

/// <summary>Documents enums as their string names, matching <see cref="StrictEnumConverterFactory"/>.</summary>
public sealed class EnumNamesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
        {
            return;
        }

        schema.Type = "string";
        schema.Format = null;
        schema.Enum = Enum.GetNames(context.Type).Select(n => (IOpenApiAny)new OpenApiString(n)).ToList();
    }
}
