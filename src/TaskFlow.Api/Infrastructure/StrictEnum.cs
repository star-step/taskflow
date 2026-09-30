using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskFlow.Api.Infrastructure;

/// <summary>
/// The only accepted form of an enum value: exactly one declared name, case-insensitive.
/// The framework defaults also accept numbers ("3") and comma-separated [Flags] lists ("Medium, High"),
/// which OR the values together and silently produce a different member (Medium|High = Critical).
/// </summary>
public static class StrictEnum
{
    public static bool TryParse(Type enumType, string? text, out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var name = Enum.GetNames(enumType).FirstOrDefault(n => string.Equals(n, text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            return false;
        }

        value = Enum.Parse(enumType, name);
        return true;
    }

    public static string AllowedNames(Type enumType) => string.Join(", ", Enum.GetNames(enumType));
}

/// <summary>JSON: enums read and written as names only (also as dictionary keys).</summary>
public sealed class StrictEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(StrictEnumConverter<>).MakeGenericType(typeToConvert))!;

    private sealed class StrictEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected one of: {StrictEnum.AllowedNames(typeof(T))}.");
            }

            return Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString());

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WritePropertyName(value.ToString());

        private static T Parse(string? text) =>
            StrictEnum.TryParse(typeof(T), text, out var value)
                ? (T)value!
                : throw new JsonException($"Expected one of: {StrictEnum.AllowedNames(typeof(T))}.");
    }
}

/// <summary>Query strings and route values: the same rule for enum parameters, including arrays of enums.</summary>
public sealed class StrictEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type.IsEnum ? new Binder(type) : null;
    }

    private sealed class Binder : IModelBinder
    {
        private readonly Type _enumType;

        public Binder(Type enumType) => _enumType = enumType;

        public Task BindModelAsync(ModelBindingContext context)
        {
            var result = context.ValueProvider.GetValue(context.ModelName);
            if (result == ValueProviderResult.None)
            {
                return Task.CompletedTask; // not supplied: keep the default
            }

            context.ModelState.SetModelValue(context.ModelName, result);
            if (StrictEnum.TryParse(_enumType, result.FirstValue, out var value))
            {
                context.Result = ModelBindingResult.Success(value);
            }
            else
            {
                var field = context.ModelMetadata.Name ?? context.ModelName;
                context.ModelState.TryAddModelError(context.ModelName,
                    $"{field} must be one of: {StrictEnum.AllowedNames(_enumType)}.");
                context.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
}
