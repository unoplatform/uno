using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class CompilationOutputInfoConverter : JsonConverter<CompilationOutputInfo>
{
	/// <inheritdoc />
	public override CompilationOutputInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return default;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected StartObject token");
		}

		var result = new CompilationOutputInfo();

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
				case nameof(CompilationOutputInfo.AssemblyPath):
					if (reader.TokenType == JsonTokenType.String)
					{
						result = result.WithAssemblyPath(reader.GetString());
					}
					break;

				case nameof(CompilationOutputInfo.GeneratedFilesOutputDirectory):
					if (reader.TokenType == JsonTokenType.String)
					{
						result = result.WithGeneratedFilesOutputDirectory(reader.GetString());
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
	public override void Write(Utf8JsonWriter writer, CompilationOutputInfo value, JsonSerializerOptions options)
	{
		// Create a copy of options without this converter to avoid infinite recursion
#pragma warning disable CA1869
		var serializerOptions = new JsonSerializerOptions(options);
#pragma warning restore CA1869
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is CompilationOutputInfoConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}
