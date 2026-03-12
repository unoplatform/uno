using System.Text.Json;
using System.Text.Json.Nodes;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Reads IDE config files and determines per-server installation status.
/// </summary>
internal sealed class ConfigScanner(IFileSystem fs)
{
	/// <summary>
	/// Scans all config paths for an IDE profile, resolving path template tokens.
	/// Returns per-server scan results with status, locations, and warnings.
	/// </summary>
	public ScanResult Scan(
		IdeProfile profile,
		string workspace,
		IReadOnlyDictionary<string, ServerDefinition> servers,
		IReadOnlyDictionary<string, JsonObject> expectedDefinitions)
	{
		var home = fs.GetUserHomePath();
		var appdata = fs.GetAppDataPath();

		var resolvedPaths = profile.ConfigPaths
			.Select(p => ResolvePath(p, workspace, home, appdata))
			.ToList();
		var resolvedWriteTarget = ResolvePath(profile.WriteTarget, workspace, home, appdata);

		// Check if any config directory exists (IDE detection)
		var detected = resolvedPaths.Any(p =>
		{
			var dir = Path.GetDirectoryName(p);
			return dir is not null && fs.DirectoryExists(dir);
		});

		// Scan each config file for server entries
		var serverResults = new Dictionary<string, ServerScanResult>();
		foreach (var (serverName, serverDef) in servers)
		{
			var locations = new List<LocationEntry>();
			var warnings = new List<string>();
			JsonObject? effectiveEntry = null;

			foreach (var configPath in resolvedPaths)
			{
				ScanConfigFile(configPath, profile.JsonRootKey, serverName, serverDef, servers, locations, warnings, ref effectiveEntry);
			}

			var distinctConfigPathCount = locations
				.Select(static location => location.Path)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count();
			if (distinctConfigPathCount > 1)
			{
				warnings.Add("Registered in multiple config files");
			}
			if (locations.GroupBy(static location => location.Path, StringComparer.OrdinalIgnoreCase)
				.Any(static group => group.Count() > 1))
			{
				warnings.Add("Multiple entries found in the same config file");
			}

			var status = DetermineStatus(serverName, serverDef, locations, effectiveEntry, expectedDefinitions);
			serverResults[serverName] = new ServerScanResult(status, locations, warnings, effectiveEntry);
		}

		return new ScanResult(resolvedPaths, resolvedWriteTarget, detected, serverResults);
	}

	/// <summary>
	/// Resolves path template tokens: <c>{workspace}</c>, <c>{home}</c>, <c>{appdata}</c>.
	/// </summary>
	internal static string ResolvePath(string template, string workspace, string home, string appdata)
	{
		var resolved = template
			.Replace("{workspace}", workspace)
			.Replace("{home}", home)
			.Replace("{appdata}", appdata);
		// Normalize mixed separators (templates use '/' but OS paths may use '\')
		return resolved.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
	}

	private void ScanConfigFile(
		string configPath,
		string rootKey,
		string serverName,
		ServerDefinition serverDef,
		IReadOnlyDictionary<string, ServerDefinition> servers,
		List<LocationEntry> locations,
		List<string> warnings,
		ref JsonObject? effectiveEntry)
	{
		if (!fs.FileExists(configPath))
		{
			return;
		}

		string content;
		try
		{
			content = fs.ReadAllText(configPath);
		}
		catch (Exception ex)
		{
			warnings.Add($"Could not read config file '{configPath}': {ex.Message}");
			return;
		}

		JsonObject? root;
		try
		{
			using var doc = JsonDocument.Parse(content, new JsonDocumentOptions
			{
				CommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
			});
			var cleanJson = JsonSerializer.Serialize(doc.RootElement);
			root = JsonNode.Parse(cleanJson)?.AsObject();
		}
		catch (JsonException ex)
		{
			warnings.Add($"Invalid JSON in config file '{configPath}': {ex.Message}");
			return;
		}

		if (root?[rootKey] is not JsonObject serversObj)
		{
			return;
		}

		foreach (var (keyName, value) in serversObj)
		{
			if (value is not JsonObject entryJson)
			{
				continue;
			}

			var matchedServer = DuplicateDetector.FindMatchingServer(keyName, entryJson, servers);

			if (string.Equals(matchedServer, serverName, StringComparison.OrdinalIgnoreCase))
			{
				var variant = DuplicateDetector.DetectVariant(entryJson, serverDef);
				var transport = DetectTransport(entryJson);
				locations.Add(new LocationEntry(configPath, variant, transport));
				effectiveEntry ??= entryJson.DeepClone().AsObject();
			}
		}
	}

	private static string DetermineStatus(
		string serverName,
		ServerDefinition serverDef,
		IReadOnlyList<LocationEntry> locations,
		JsonObject? effectiveEntry,
		IReadOnlyDictionary<string, JsonObject> expectedDefinitions)
	{
		if (locations.Count == 0)
		{
			return "missing";
		}

		// Use first location (highest priority) for status determination
		if (!expectedDefinitions.TryGetValue(serverName, out var expectedDef))
		{
			return "registered"; // no expected definition to compare against
		}

		if (effectiveEntry is null)
		{
			return "registered";
		}

		if (!DuplicateDetector.IsUpToDate(effectiveEntry, expectedDef, serverDef))
		{
			return "outdated";
		}

		return "registered";
	}

	private static string DetectTransport(JsonObject entryJson)
	{
		if (entryJson["command"] is not null)
		{
			return "stdio";
		}

		if (DuplicateDetector.GetUrl(entryJson) is not null)
		{
			return "http";
		}

		return "unknown";
	}
}
