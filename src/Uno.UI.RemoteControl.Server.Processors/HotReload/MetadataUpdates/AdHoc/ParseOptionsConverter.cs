using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class ParseOptionsConverter : JsonConverter<ParseOptions>
{
	/// <inheritdoc />
	public override ParseOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected StartObject token");
		}

		CSharpParseOptions result = new CSharpParseOptions();

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return result;
			}

			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Expected PropertyName token");
			}

			var propertyName = reader.GetString();
			reader.Read(); // Move to the value

			switch (propertyName)
			{
				case nameof(ParseOptions.Kind):
					var kind = JsonSerializer.Deserialize<SourceCodeKind>(ref reader, options);
					result = result.WithKind(kind);
					break;

				case nameof(ParseOptions.DocumentationMode):
					var documentationMode = JsonSerializer.Deserialize<DocumentationMode>(ref reader, options);
					result = result.WithDocumentationMode(documentationMode);
					break;

				case nameof(ParseOptions.Features):
					var features = JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(ref reader, options);
					if (features is not null)
					{
						result = result.WithFeatures(features);
					}
					break;

				case nameof(CSharpParseOptions.LanguageVersion):
					var languageVersion = JsonSerializer.Deserialize<LanguageVersion>(ref reader, options);
					result = result.WithLanguageVersion(languageVersion);
					break;

				case "PreprocessorSymbols": // nameof(CSharpParseOptions.PreprocessorSymbols):
					var preprocessorSymbols = JsonSerializer.Deserialize<IEnumerable<string>>(ref reader, options);
					if (preprocessorSymbols is not null)
					{
						result = result.WithPreprocessorSymbols(preprocessorSymbols);
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
	public override void Write(Utf8JsonWriter writer, ParseOptions value, JsonSerializerOptions options)
	{
		// Create a copy of options without this converter to avoid infinite recursion
#pragma warning disable CA1869
		var serializerOptions = new JsonSerializerOptions(options);
#pragma warning restore CA1869
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is ParseOptionsConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}
