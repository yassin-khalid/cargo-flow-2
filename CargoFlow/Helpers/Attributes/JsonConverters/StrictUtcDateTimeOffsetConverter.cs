using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CargoFlow.Helpers.Attributes.JsonConverters;

public sealed class StrictUtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string format = "yyyy-MM-ddTHH:mm:ssZ";

    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var raw = reader.GetString();
        if (
            string.IsNullOrWhiteSpace(raw)
            || !DateTimeOffset.TryParseExact(
                raw,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var result
            )
        )
        {
            throw new JsonException($"Invalid date format. Expected format '{format}'");
        }
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(
            value.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture)
        );
    }
}
