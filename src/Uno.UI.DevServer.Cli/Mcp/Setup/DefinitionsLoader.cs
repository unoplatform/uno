using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Loads IDE profiles and server definitions from embedded resources or external files.
/// </summary>
internal static class DefinitionsLoader
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};

	public static Definitions Load(
		IFileSystem? fs = null,
		string? ideDefinitionsPath = null,
		string? serverDefinitionsPath = null)
	{
		var ideProfilesJson = LoadJson(fs, ideDefinitionsPath, "ide-profiles.json");
		var serverDefsJson = LoadJson(fs, serverDefinitionsPath, "server-definitions.json");

		var ides = ParseIdeProfiles(ideProfilesJson);
		var servers = ParseServerDefinitions(serverDefsJson);

		return new Definitions(ides, servers);
	}

	private static string LoadJson(IFileSystem? fs, string? externalPath, string embeddedName)
	{
		if (externalPath is not null)
		{
			if (fs is null || !fs.FileExists(externalPath))
			{
				throw new FileNotFoundException($"Definitions file not found: {externalPath}", externalPath);
			}

			return fs.ReadAllText(externalPath);
		}

		return LoadEmbeddedResource(embeddedName);
	}

	private static string LoadEmbeddedResource(string name)
	{
		var assembly = typeof(DefinitionsLoader).Assembly;
		using var stream = assembly.GetManifestResourceStream(name)
			?? throw new InvalidOperationException($"Embedded resource '{name}' not found in assembly.");

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private static IReadOnlyDictionary<string, IdeProfile> ParseIdeProfiles(string json)
	{
		var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _options)
			?? throw new JsonException("Failed to parse IDE profiles JSON.");

		var result = new Dictionary<string, IdeProfile>(StringComparer.OrdinalIgnoreCase);

		foreach (var (key, element) in root)
		{
			var configPaths = element.GetProperty("configPaths").Deserialize<string[]>(_options)
				?? throw new JsonException($"IDE profile '{key}': missing configPaths.");
			var writeTarget = element.GetProperty("writeTarget").GetString()
				?? throw new JsonException($"IDE profile '{key}': missing writeTarget.");
			var jsonRootKey = element.GetProperty("jsonRootKey").GetString()
				?? throw new JsonException($"IDE profile '{key}': missing jsonRootKey.");

			result[key] = new IdeProfile(configPaths, writeTarget, jsonRootKey);
		}

		return result;
	}

	private static IReadOnlyDictionary<string, ServerDefinition> ParseServerDefinitions(string json)
	{
		var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _options)
			?? throw new JsonException("Failed to parse server definitions JSON.");

		var result = new Dictionary<string, ServerDefinition>(StringComparer.OrdinalIgnoreCase);

		foreach (var (key, element) in root)
		{
			var transport = element.GetProperty("transport").GetString()
				?? throw new JsonException($"Server '{key}': missing transport.");

			var variantsElement = element.GetProperty("variants");
			var variants = new Dictionary<string, JsonObject>();
			foreach (var prop in variantsElement.EnumerateObject())
			{
				var node = JsonNode.Parse(prop.Value.GetRawText())?.AsObject()
					?? throw new JsonException($"Server '{key}': variant '{prop.Name}' is not a valid object.");
				variants[prop.Name] = node;
			}

			var detectionElement = element.GetProperty("detection");
			var keyPatterns = detectionElement.GetProperty("keyPatterns").Deserialize<string[]>(_options)
				?? throw new JsonException($"Server '{key}': missing detection.keyPatterns.");

			string[]? commandPatterns = null;
			if (detectionElement.TryGetProperty("commandPatterns", out var cp) && cp.ValueKind != JsonValueKind.Null)
			{
				commandPatterns = cp.Deserialize<string[]>(_options);
			}

			string[]? urlPatterns = null;
			if (detectionElement.TryGetProperty("urlPatterns", out var up) && up.ValueKind != JsonValueKind.Null)
			{
				urlPatterns = up.Deserialize<string[]>(_options);
			}

			result[key] = new ServerDefinition(
				transport,
				variants,
				new DetectionPatterns(keyPatterns, commandPatterns, urlPatterns));
		}

		return result;
	}
}
