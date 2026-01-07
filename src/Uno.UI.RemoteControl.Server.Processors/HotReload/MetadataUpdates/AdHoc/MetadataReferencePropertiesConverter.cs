using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class MetadataReferencePropertiesConverter : JsonConverter<MetadataReferenceProperties>
{
	/// <inheritdoc />
	public override MetadataReferenceProperties Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return default;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected StartObject token");
		}

		var kind = MetadataImageKind.Assembly;
		var aliases = ImmutableArray<string>.Empty;
		var embedInteropTypes = false;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return new MetadataReferenceProperties(kind, aliases, embedInteropTypes);
			}

			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Expected PropertyName token");
			}

			var propertyName = reader.GetString();
			reader.Read(); // Move to the value

			switch (propertyName)
			{
				case nameof(MetadataReferenceProperties.Kind):
					kind = JsonSerializer.Deserialize<MetadataImageKind>(ref reader, options);
					break;

				case nameof(MetadataReferenceProperties.Aliases):
					aliases = JsonSerializer.Deserialize<ImmutableArray<string>>(ref reader, options);
					break;

				case nameof(MetadataReferenceProperties.EmbedInteropTypes):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						embedInteropTypes = reader.GetBoolean();
					}
					break;

				default:
					// Skip unknown properties
					if (!reader.TrySkip())
					{
						throw new JsonException($"Unexpected property {propertyName}");
					}
					break;
			}
		}

		throw new JsonException("Unexpected end of JSON");
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, MetadataReferenceProperties value, JsonSerializerOptions options)
	{
		// Create a copy of options without this converter to avoid infinite recursion
#pragma warning disable CA1869
		var serializerOptions = new JsonSerializerOptions(options);
#pragma warning restore CA1869
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is MetadataReferencePropertiesConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}
