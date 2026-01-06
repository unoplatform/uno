using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.UI.RemoteControl.Host.HotReload;

public class CompilationOptionsConverter : JsonConverter<CompilationOptions>
{
	/// <inheritdoc />
	public override CompilationOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected StartObject token");
		}

		CSharpCompilationOptions result = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

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
				case nameof(CompilationOptions.OutputKind):
					var outputKind = JsonSerializer.Deserialize<OutputKind>(ref reader, options);
					result = result.WithOutputKind(outputKind);
					break;

				case nameof(CompilationOptions.ModuleName):
					if (reader.TokenType == JsonTokenType.String)
					{
						result = result.WithModuleName(reader.GetString());
					}
					break;

				case nameof(CompilationOptions.MainTypeName):
					if (reader.TokenType == JsonTokenType.String)
					{
						result = result.WithMainTypeName(reader.GetString());
					}
					break;

				case nameof(CompilationOptions.ScriptClassName):
					if (reader.TokenType == JsonTokenType.String)
					{
						result = result.WithScriptClassName(reader.GetString()!);
					}
					break;

				case nameof(CompilationOptions.CheckOverflow):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						result = result.WithOverflowChecks(reader.GetBoolean());
					}
					break;

				case nameof(CompilationOptions.Deterministic):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						result = result.WithDeterministic(reader.GetBoolean());
					}
					break;

				case nameof(CompilationOptions.ConcurrentBuild):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						result = result.WithConcurrentBuild(reader.GetBoolean());
					}
					break;

				case nameof(CompilationOptions.PublicSign):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						result = result.WithPublicSign(reader.GetBoolean());
					}
					break;

				case nameof(CompilationOptions.OptimizationLevel):
					var optimizationLevel = JsonSerializer.Deserialize<OptimizationLevel>(ref reader, options);
					result = result.WithOptimizationLevel(optimizationLevel);
					break;

				case nameof(CompilationOptions.Platform):
					var platform = JsonSerializer.Deserialize<Platform>(ref reader, options);
					result = result.WithPlatform(platform);
					break;

				case nameof(CompilationOptions.ReportSuppressedDiagnostics):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						result = result.WithReportSuppressedDiagnostics(reader.GetBoolean());
					}
					break;

				case nameof(CompilationOptions.WarningLevel):
					if (reader.TokenType == JsonTokenType.Number)
					{
						result = result.WithWarningLevel(reader.GetInt32());
					}
					break;

				case nameof(CompilationOptions.GeneralDiagnosticOption):
					var generalDiagnosticOption = JsonSerializer.Deserialize<ReportDiagnostic>(ref reader, options);
					result = result.WithGeneralDiagnosticOption(generalDiagnosticOption);
					break;

				case nameof(CompilationOptions.SpecificDiagnosticOptions):
					var specificDiagnosticOptions = JsonSerializer.Deserialize<ImmutableDictionary<string, ReportDiagnostic>>(ref reader, options);
					if (specificDiagnosticOptions is not null)
					{
						result = result.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
					}
					break;

				case nameof(CompilationOptions.MetadataImportOptions):
					var metadataImportOptions = JsonSerializer.Deserialize<MetadataImportOptions>(ref reader, options);
					result = result.WithMetadataImportOptions(metadataImportOptions);
					break;

				case nameof(CSharpCompilationOptions.AllowUnsafe):
					if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
					{
						result = result.WithAllowUnsafe(reader.GetBoolean());
					}
					break;

				case nameof(CSharpCompilationOptions.NullableContextOptions):
					var nullableContextOptions = JsonSerializer.Deserialize<NullableContextOptions>(ref reader, options);
					result = result.WithNullableContextOptions(nullableContextOptions);
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
	public override void Write(Utf8JsonWriter writer, CompilationOptions value, JsonSerializerOptions options)
	{
		// Create a copy of options without this converter to avoid infinite recursion
#pragma warning disable CA1869
		var serializerOptions = new JsonSerializerOptions(options);
#pragma warning restore CA1869
		serializerOptions.Converters.Remove(serializerOptions.Converters.First(c => c is CompilationOptionsConverter));

		// Use the standard serialization
		JsonSerializer.Serialize(writer, value, value.GetType(), serializerOptions);
	}
}
