using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Top-level orchestrator for MCP setup operations: status, install, uninstall.
/// Composes <see cref="ConfigScanner"/>, <see cref="ConfigWriter"/>,
/// <see cref="ServerDefinitionResolver"/>, and <see cref="DuplicateDetector"/>.
/// </summary>
internal sealed class McpSetupOrchestrator
{
	private const string ProtocolVersion = "1.0";

	private readonly IFileSystem _fs;
	private readonly ILogger<McpSetupOrchestrator> _logger;

	public McpSetupOrchestrator(IFileSystem fs, ILogger<McpSetupOrchestrator> logger)
	{
		_fs = fs;
		_logger = logger;
	}

	/// <summary>
	/// Reports the installation state of all MCP servers across all IDEs.
	/// </summary>
	public StatusResponse Status(
		Definitions defs,
		string workspace,
		string? callerIde,
		string expectedVariant,
		string toolVersion)
	{
		var scanner = new ConfigScanner(_fs);
		var expectedDefinitions = BuildExpectedDefinitions(defs.Servers, expectedVariant);
		var detectedIdes = new List<string>();
		var serverEntries = new List<ServerStatusEntry>();

		// Build per-server, per-IDE status
		var ideStatusMap = new Dictionary<string, Dictionary<string, ServerIdeStatus>>();

		foreach (var (ideName, profile) in defs.Ides)
		{
			var scanResult = scanner.Scan(profile, workspace, defs.Servers, expectedDefinitions);
			if (scanResult.Detected)
			{
				detectedIdes.Add(ideName);
			}

			foreach (var (serverName, serverResult) in scanResult.ServerResults)
			{
				if (!ideStatusMap.TryGetValue(serverName, out var map))
				{
					map = new Dictionary<string, ServerIdeStatus>();
					ideStatusMap[serverName] = map;
				}

				map[ideName] = new ServerIdeStatus(
					ideName,
					serverResult.Status,
					serverResult.Locations.Count > 0 ? serverResult.Locations : null,
					serverResult.Warnings.Count > 0 ? serverResult.Warnings : null);
			}
		}

		// Build server entries
		foreach (var (serverName, serverDef) in defs.Servers)
		{
			var ideStatuses = ideStatusMap.TryGetValue(serverName, out var map)
				? map.Values.ToList()
				: new List<ServerIdeStatus>();

			serverEntries.Add(new ServerStatusEntry(
				serverName,
				serverDef.Transport,
				expectedDefinitions.TryGetValue(serverName, out var def) ? def : new JsonObject(),
				ideStatuses));
		}

		return new StatusResponse(
			ProtocolVersion,
			callerIde,
			toolVersion,
			expectedVariant,
			detectedIdes,
			serverEntries);
	}

	/// <summary>
	/// Registers MCP servers in the target IDE's config files.
	/// </summary>
	public OperationResponse Install(
		Definitions defs,
		string workspace,
		string targetIde,
		string expectedVariant,
		string toolVersion,
		IReadOnlyList<string>? serverFilter)
	{
		if (!defs.Ides.TryGetValue(targetIde, out var profile))
		{
			return ErrorResponse($"Unknown IDE: {targetIde}");
		}

		var operations = new List<OperationEntry>();
		var scanner = new ConfigScanner(_fs);
		var expectedDefinitions = BuildExpectedDefinitions(defs.Servers, expectedVariant);
		var scanResult = scanner.Scan(profile, workspace, defs.Servers, expectedDefinitions);
		var includeType = profile.JsonRootKey == "servers";

		foreach (var (serverName, serverDef) in defs.Servers)
		{
			if (serverFilter is not null && !serverFilter.Contains(serverName, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!expectedDefinitions.TryGetValue(serverName, out var definition))
			{
				continue;
			}

			var serverResult = scanResult.ServerResults.TryGetValue(serverName, out var sr) ? sr : null;

			try
			{
				var entry = InstallServer(
					profile, scanResult, serverName, serverDef, serverResult, definition, includeType);
				operations.Add(entry);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to install server {Server} for {Ide}", serverName, targetIde);
				operations.Add(new OperationEntry(serverName, targetIde, "error", null, ex.Message));
			}
		}

		return new OperationResponse(ProtocolVersion, operations);
	}

	/// <summary>
	/// Removes MCP servers from the target IDE's config files.
	/// </summary>
	public OperationResponse Uninstall(
		Definitions defs,
		string workspace,
		string targetIde,
		IReadOnlyList<string>? serverFilter)
	{
		if (!defs.Ides.TryGetValue(targetIde, out var profile))
		{
			return ErrorResponse($"Unknown IDE: {targetIde}");
		}

		var operations = new List<OperationEntry>();
		var scanner = new ConfigScanner(_fs);
		var home = _fs.GetUserHomePath();
		var appdata = _fs.GetAppDataPath();

		foreach (var (serverName, serverDef) in defs.Servers)
		{
			if (serverFilter is not null && !serverFilter.Contains(serverName, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			var found = false;

			foreach (var configTemplate in profile.ConfigPaths)
			{
				var configPath = ConfigScanner.ResolvePath(configTemplate, workspace, home, appdata);

				if (!_fs.FileExists(configPath))
				{
					continue;
				}

				try
				{
					var content = _fs.ReadAllText(configPath);
					var matchedKey = FindServerKeyInConfig(content, profile.JsonRootKey, serverName, serverDef, defs.Servers);

					if (matchedKey is null)
					{
						continue;
					}

					var updated = ConfigWriter.RemoveServer(content, profile.JsonRootKey, matchedKey);
					if (updated is not null)
					{
						_fs.WriteAllText(configPath, updated);
						operations.Add(new OperationEntry(serverName, targetIde, "removed", configPath, null));
						found = true;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to uninstall server {Server} from {Path}", serverName, configPath);
					operations.Add(new OperationEntry(serverName, targetIde, "error", configPath, ex.Message));
					found = true;
				}
			}

			if (!found)
			{
				operations.Add(new OperationEntry(serverName, targetIde, "not_found", null, null));
			}
		}

		return new OperationResponse(ProtocolVersion, operations);
	}

	private OperationEntry InstallServer(
		IdeProfile profile,
		ScanResult scanResult,
		string serverName,
		ServerDefinition serverDef,
		ServerScanResult? serverResult,
		JsonObject definition,
		bool includeType)
	{
		var targetPath = scanResult.ResolvedWriteTarget;
		var ide = profile.JsonRootKey; // used for logging context

		if (serverResult is not null && serverResult.Status == "registered")
		{
			return new OperationEntry(serverName, ide, "skipped", targetPath, "Already registered and up-to-date");
		}

		// Determine the key to write: use existing key if found (preserve user naming)
		var writeKey = serverName;
		string? existingContent = null;

		if (_fs.FileExists(targetPath))
		{
			if (_fs.IsReadOnly(targetPath))
			{
				return new OperationEntry(serverName, ide, "error", targetPath, "File is read-only");
			}

			existingContent = _fs.ReadAllText(targetPath);

			// Find if there's an existing entry under a different key name
			var matchedKey = FindServerKeyInConfig(existingContent, profile.JsonRootKey, serverName, serverDef, new Dictionary<string, ServerDefinition> { [serverName] = serverDef });
			if (matchedKey is not null)
			{
				writeKey = matchedKey;
			}
		}

		var isUpdate = serverResult is not null && serverResult.Status == "outdated";
		var action = isUpdate ? "updated" : "created";

		var updated = ConfigWriter.MergeServer(
			existingContent,
			profile.JsonRootKey,
			writeKey,
			definition,
			includeType,
			serverDef.Transport);

		_fs.WriteAllText(targetPath, updated);

		return new OperationEntry(serverName, ide, action, targetPath, null);
	}

	private static string? FindServerKeyInConfig(
		string content,
		string rootKey,
		string serverName,
		ServerDefinition serverDef,
		IReadOnlyDictionary<string, ServerDefinition> servers)
	{
		try
		{
			using var doc = System.Text.Json.JsonDocument.Parse(content, new System.Text.Json.JsonDocumentOptions
			{
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
			});
			var cleanJson = System.Text.Json.JsonSerializer.Serialize(doc.RootElement);
			var root = JsonNode.Parse(cleanJson)?.AsObject();

			if (root?[rootKey] is not JsonObject serversObj)
			{
				return null;
			}

			foreach (var (keyName, value) in serversObj)
			{
				if (value is not JsonObject entryJson)
				{
					continue;
				}

				var match = DuplicateDetector.FindMatchingServer(keyName, entryJson, servers);
				if (match == serverName)
				{
					return keyName;
				}
			}
		}
		catch
		{
			// malformed JSON
		}

		return null;
	}

	private static IReadOnlyDictionary<string, JsonObject> BuildExpectedDefinitions(
		IReadOnlyDictionary<string, ServerDefinition> servers,
		string expectedVariant)
	{
		var result = new Dictionary<string, JsonObject>();
		foreach (var (name, def) in servers)
		{
			result[name] = ServerDefinitionResolver.ResolveDefinition(def, expectedVariant);
		}

		return result;
	}

	private static OperationResponse ErrorResponse(string message) =>
		new(ProtocolVersion, [new OperationEntry("*", "*", "error", null, message)]);
}
