using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;

namespace FestivalHoa.Properties.Converters
{
    public class ObjectToStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string) || objectType == typeof(object);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
            {
                return JToken.Load(reader).ToString(Formatting.None);
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return reader.Value.ToString();
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            return reader.Value?.ToString();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is string strValue)
            {
                try
                {
                    var token = JToken.Parse(strValue);
                    token.WriteTo(writer);
                }
                catch
                {
                    writer.WriteValue(strValue);
                }
            }
            else if (value != null)
            {
                serializer.Serialize(writer, value);
            }   
            else
            {
                writer.WriteNull();
            }
        }
    }
}
