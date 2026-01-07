using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Host.HotReload;

public sealed class SolutionIdConverter : JsonConverter<SolutionId>
{
	/// <inheritdoc />
	public override SolutionId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> SolutionId.CreateFromSerialized(reader.GetGuid());

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, SolutionId value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.Id);
}
