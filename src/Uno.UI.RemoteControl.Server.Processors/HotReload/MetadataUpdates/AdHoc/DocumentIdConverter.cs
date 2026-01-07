using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Host.HotReload;

public sealed class DocumentIdConverter : JsonConverter<DocumentId>
{
	/// <inheritdoc />
	public override DocumentId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var docId = JsonSerializer.Deserialize<DocId>(ref reader, options);
		return DocumentId.CreateFromSerialized(ProjectId.CreateFromSerialized(docId.Project), docId.Value);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, DocumentId value, JsonSerializerOptions options)
	{
		var docId = new DocId(value.ProjectId.Id, value.Id);
		JsonSerializer.Serialize(writer, docId, typeof(DocId), options);
	}

	private record struct DocId(Guid Project, Guid Value);
}
