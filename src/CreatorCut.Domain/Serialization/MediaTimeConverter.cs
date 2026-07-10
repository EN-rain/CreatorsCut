using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreatorCut.Domain.Serialization;

public sealed class MediaTimeConverter : JsonConverter<MediaTime>
{
    public override MediaTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject for MediaTime");

        long numerator = 0, denominator = 1;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var prop = reader.GetString();
            reader.Read();
            switch (prop)
            {
                case "numerator": numerator = reader.GetInt64(); break;
                case "denominator": denominator = reader.GetInt64(); break;
            }
        }

        return new MediaTime(numerator, denominator);
    }

    public override void Write(Utf8JsonWriter writer, MediaTime value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("numerator", value.Numerator);
        writer.WriteNumber("denominator", value.Denominator);
        writer.WriteEndObject();
    }
}
