using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;

namespace FestivalHoa.Properties.Converters
{
    public class ObjectToStringJsonConverter : JsonConverter<string>
    {
        public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var jObj = JObject.Load(reader);
                return jObj.ToString(Formatting.None);
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return (string)reader.Value;
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            else
            {
                return reader.Value?.ToString();
            }
        }

        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var token = JToken.Parse(value);
                    token.WriteTo(writer);
                }
                catch
                {
                    writer.WriteValue(value);
                }
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
