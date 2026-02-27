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

			foreach (var configPath in resolvedPaths)
			{
				ScanConfigFile(configPath, profile.JsonRootKey, serverName, serverDef, locations);
			}

			if (locations.Count > 1)
			{
				warnings.Add("Registered in multiple config files");
			}

			var status = DetermineStatus(serverName, locations, expectedDefinitions);
			serverResults[serverName] = new ServerScanResult(status, locations, warnings);
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
		List<LocationEntry> locations)
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
		catch
		{
			return; // unreadable file — skip
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
		catch
		{
			return; // malformed JSON — skip for scanning
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

			var matchedServer = DuplicateDetector.FindMatchingServer(keyName, entryJson, new Dictionary<string, ServerDefinition>
			{
				[serverName] = serverDef,
			});

			if (matchedServer is not null)
			{
				var variant = DuplicateDetector.DetectVariant(entryJson, serverDef);
				var transport = DetectTransport(entryJson);
				locations.Add(new LocationEntry(configPath, variant, transport));
			}
		}
	}

	private static string DetermineStatus(
		string serverName,
		IReadOnlyList<LocationEntry> locations,
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

		var firstVariant = locations[0].Variant;
		var expectedVariant = DetectExpectedVariantKey(expectedDef);

		if (firstVariant == "legacy-http")
		{
			return "outdated";
		}

		if (firstVariant != expectedVariant)
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

		if (entryJson["url"] is not null)
		{
			return "http";
		}

		return "unknown";
	}

	/// <summary>
	/// Infers the expected variant key from the resolved definition by looking at its args.
	/// </summary>
	private static string DetectExpectedVariantKey(JsonObject definition)
	{
		var args = definition["args"]?.AsArray();
		if (args is null)
		{
			return "stable"; // HTTP definitions have no args
		}

		for (int i = 0; i < args.Count; i++)
		{
			var arg = args[i]?.GetValue<string>();
			if (arg == "--version" && i + 1 < args.Count)
			{
				var version = args[i + 1]?.GetValue<string>();
				return version is not null ? $"pinned:{version}" : "stable";
			}

			if (arg == "--prerelease")
			{
				return "prerelease";
			}
		}

		return "stable";
	}
}
