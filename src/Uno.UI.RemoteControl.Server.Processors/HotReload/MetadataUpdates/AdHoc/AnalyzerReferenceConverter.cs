using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class AnalyzerReferenceConverter(IAnalyzerAssemblyLoader loader) : JsonConverter<AnalyzerReference>
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
					return new AnalyzerFileReference(fullPath, loader);
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
#pragma warning disable CA1869
		var serializerOptions = new JsonSerializerOptions(options);
#pragma warning restore CA1869
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is AnalyzerReferenceConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}

internal class UnoAnalyzerAssemblyLoader(AssemblyLoadContext context) : IAnalyzerAssemblyLoader
{
	//private static readonly AsyncLocal<UnoAnalyzerAssemblyLoader> _current = new();

	//public static UnoAnalyzerAssemblyLoader Current => _current.Value ??= new UnoAnalyzerAssemblyLoader();

	/// <inheritdoc />
	public Assembly LoadFromPath(string fullPath)
		//=> Assembly.LoadFile(fullPath);
		=> context.LoadFromAssemblyPath(fullPath);

	/// <inheritdoc />
	public void AddDependencyLocation(string fullPath)
	{
	}
}

internal class AnalyzerAssemblyLoader(AssemblyLoadContext context) : IAnalyzerAssemblyLoader
{
	//private static readonly AsyncLocal<UnoAnalyzerAssemblyLoader> _current = new();

	//public static UnoAnalyzerAssemblyLoader Current => _current.Value ??= new UnoAnalyzerAssemblyLoader();

	/// <inheritdoc />
	public Assembly LoadFromPath(string fullPath)
		//=> Assembly.LoadFile(fullPath);
		=> context.LoadFromAssemblyPath(fullPath);

	/// <inheritdoc />
	public void AddDependencyLocation(string fullPath)
	{
	}
}
