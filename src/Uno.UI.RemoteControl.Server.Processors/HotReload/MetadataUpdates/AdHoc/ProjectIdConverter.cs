using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Host.HotReload;

public sealed class ProjectIdConverter : JsonConverter<ProjectId>
{
	/// <inheritdoc />
	public override ProjectId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> ProjectId.CreateFromSerialized(reader.GetGuid());

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, ProjectId value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.Id);
}
