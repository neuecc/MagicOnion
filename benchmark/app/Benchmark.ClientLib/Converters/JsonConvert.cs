using System;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benchmark.ClientLib.Converters
{
    public static class JsonConvert
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // allow Unicode characters
            WriteIndented = true, // prety print
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // ignore null
            Converters =
            {
                new TimeSpanConverter(),
            },
        };

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public static string Serialize<T>(T source)
        {
            return JsonSerializer.Serialize(source, options);
        }
    }

    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(format: null, CultureInfo.InvariantCulture));
        }
    }

    public class TimeSpanConverterISO8601 : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = reader.GetString();
            return System.Xml.XmlConvert.ToTimeSpan(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            var stringValue = System.Xml.XmlConvert.ToString(value);
            writer.WriteStringValue(stringValue);
        }
    }
}
