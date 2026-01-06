using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class EncodingJsonConverter : JsonConverter<Encoding>
{
	public override Encoding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var encodingName = reader.GetString();
		return encodingName is null ? null : Encoding.GetEncoding(encodingName);
	}

	public override void Write(Utf8JsonWriter writer, Encoding value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.WebName);
	}
}
