#pragma warning disable CA1869
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class MetadataReferenceConverter : JsonConverter<MetadataReference>
{
	/// <inheritdoc />
	public override MetadataReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected StartObject token");
		}

		string? filePath = null;
		MetadataReferenceProperties? properties = null;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				if (filePath is not null)
				{
					return MetadataReference.CreateFromFile(filePath, properties ?? default);
				}

				throw new JsonException("Missing required property 'Display' or 'FilePath'");
			}

			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Expected PropertyName token");
			}

			var propertyName = reader.GetString();
			reader.Read(); // Move to the value

			switch (propertyName)
			{
				case "Display":
				case "FilePath":
					filePath = reader.GetString();
					break;

				case "Properties":
					properties = JsonSerializer.Deserialize<MetadataReferenceProperties>(ref reader, options);
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
	public override void Write(Utf8JsonWriter writer, MetadataReference value, JsonSerializerOptions options)
	{
		// Create a copy of options without this converter to avoid infinite recursion
		var serializerOptions = new JsonSerializerOptions(options);
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is MetadataReferenceConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}

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
		var serializerOptions = new JsonSerializerOptions(options);
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is MetadataReferencePropertiesConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}


internal class UnoAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
	private static readonly AsyncLocal<UnoAnalyzerAssemblyLoader> _current = new();

	public static UnoAnalyzerAssemblyLoader Current => _current.Value ??= new UnoAnalyzerAssemblyLoader();

	/// <inheritdoc />
	public Assembly LoadFromPath(string fullPath)
		=> Assembly.LoadFile(fullPath);

	/// <inheritdoc />
	public void AddDependencyLocation(string fullPath)
	{
	}
}

public class AnalyzerReferenceConverter : JsonConverter<AnalyzerReference>
{
	/// <inheritdoc />
	public override AnalyzerReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected StartObject token");
		}

		string? fullPath = null;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				if (fullPath is not null)
				{
					return new AnalyzerFileReference(fullPath, UnoAnalyzerAssemblyLoader.Current);
				}

				throw new JsonException("Missing required property 'FullPath'");
			}

			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Expected PropertyName token");
			}

			var propertyName = reader.GetString();
			reader.Read(); // Move to the value

			switch (propertyName)
			{
				case nameof(AnalyzerFileReference.FullPath):
					fullPath = reader.GetString();
					break;

				case "Display":
				case "Id":
					// These properties are informational only, we ignore them
					// because AnalyzerFileReference will recalculate them from the file
					if (!reader.TrySkip())
					{
						throw new JsonException($"Failed to skip property {propertyName}");
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
	public override void Write(Utf8JsonWriter writer, AnalyzerReference value, JsonSerializerOptions options)
	{
		// Create a copy of options without this converter to avoid infinite recursion
		var serializerOptions = new JsonSerializerOptions(options);
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is AnalyzerReferenceConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}

