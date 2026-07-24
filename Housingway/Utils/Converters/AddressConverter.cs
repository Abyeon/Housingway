using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Housingway.Profiles;

namespace Housingway.Utils.Converters;

public class AddressConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        if (propertyName is null)
        {
            throw new JsonException("Ran into null value when trying to read property name.");
        }
        
        var parts = propertyName.Split('_');
        return new Address(uint.Parse(parts[0]), uint.Parse(parts[1]), sbyte.Parse(parts[2]), sbyte.Parse(parts[3]), short.Parse(parts[4]));
    }

    public override Address ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        writer.WritePropertyName($"{value.WorldId}_{value.TerritoryId}_{value.Ward}_{value.Plot}_{value.Room}");
    }
}
