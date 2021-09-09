using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Counter_Emitter.Model
{
    class RecordConverter : JsonConverter<IRecord>
    {
        public override IRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IRecord value, JsonSerializerOptions options)
        {
            switch (value)
            {
            case null:
                JsonSerializer.Serialize(writer, (IRecord) null, options);
                break;
            default:
                {
                    var type = value.GetType();
                    JsonSerializer.Serialize(writer, value, type, options);
                    break;
                }
            }
        }
    }
}
