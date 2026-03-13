using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Top-level orchestrator for MCP setup operations: status, install, uninstall.
/// Composes <see cref="ConfigScanner"/>, <see cref="ConfigWriter"/>,
/// <see cref="ServerDefinitionResolver"/>, and <see cref="DuplicateDetector"/>.
/// </summary>
internal sealed class McpSetupOrchestrator(IFileSystem fs, ILogger<McpSetupOrchestrator> logger)
{
	private readonly IFileSystem _fs = fs;
	private readonly ILogger<McpSetupOrchestrator> _logger = logger;

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
		var supportedIdes = new List<SupportedIdeEntry>();
		var serverEntries = new List<ServerStatusEntry>();

		// Build per-server, per-client status
		var ideStatusMap = new Dictionary<string, Dictionary<string, ServerIdeStatus>>();

		foreach (var (ideName, profile) in defs.Ides)
		{
			var scanResult = scanner.Scan(profile, workspace, defs.Servers, expectedDefinitions);
			if (scanResult.Detected)
			{
				detectedIdes.Add(ideName);
			}
			supportedIdes.Add(new SupportedIdeEntry(ideName, profile.Strategy, scanResult.Detected));

			foreach (var (serverName, serverResult) in scanResult.ServerResults)
			{
				if (!ideStatusMap.TryGetValue(serverName, out var map))
				{
					map = [];
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
				? [.. map.Values]
				: new List<ServerIdeStatus>();

			serverEntries.Add(new ServerStatusEntry(
				serverName,
				serverDef.Transport,
				expectedDefinitions.TryGetValue(serverName, out var def) ? def : [],
				ideStatuses));
		}

		return new StatusResponse(
			McpSetupProtocol.Version,
			callerIde,
			toolVersion,
			expectedVariant,
			detectedIdes,
			supportedIdes,
			serverEntries);
	}

	public OperationResponse Install(
		Definitions defs,
		string workspace,
		string targetIde,
		string expectedVariant,
		string toolVersion,
		IReadOnlyList<string>? serverFilter,
		bool dryRun = false)
	{
		if (!defs.Ides.TryGetValue(targetIde, out var profile))
		{
			return ErrorResponse($"Unknown client: {targetIde}");
		}

		if (!string.Equals(profile.Strategy, "file", StringComparison.OrdinalIgnoreCase))
		{
			return NativeRegistrationResponse(defs, targetIde, profile, serverFilter);
		}

		var operations = new List<OperationEntry>();
		var scanner = new ConfigScanner(_fs);
		var expectedDefinitions = BuildExpectedDefinitions(defs.Servers, expectedVariant);
		var scanResult = scanner.Scan(profile, workspace, defs.Servers, expectedDefinitions);
		var backedUpFiles = new HashSet<string>(FileSystem.PathComparer);
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
					targetIde, profile, scanResult, serverName, serverDef, serverResult, definition, dryRun, backedUpFiles);
				operations.Add(entry);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to install server {Server} for {Ide}", serverName, targetIde);
				operations.Add(new OperationEntry(serverName, targetIde, "error", scanResult.ResolvedWriteTarget, ex.Message));
			}
		}

		return new OperationResponse(McpSetupProtocol.Version, operations);
	}

	public OperationResponse Uninstall(
		Definitions defs,
		string workspace,
		string targetIde,
		IReadOnlyList<string>? serverFilter,
		bool allScopes = false,
		bool dryRun = false)
	{
		if (!defs.Ides.TryGetValue(targetIde, out var profile))
		{
			return ErrorResponse($"Unknown client: {targetIde}");
		}

		if (!string.Equals(profile.Strategy, "file", StringComparison.OrdinalIgnoreCase))
		{
			return NativeRegistrationResponse(defs, targetIde, profile, serverFilter);
		}

		var operations = new List<OperationEntry>();
		var scanner = new ConfigScanner(_fs);
		var home = _fs.GetUserHomePath();
		var appdata = _fs.GetAppDataPath();
		var backedUpFiles = new HashSet<string>(FileSystem.PathComparer);

		foreach (var (serverName, serverDef) in defs.Servers)
		{
			if (serverFilter is not null && !serverFilter.Contains(serverName, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			var found = false;

			var uninstallPaths = allScopes
				? profile.ConfigPaths
				: [profile.WriteTarget];

			foreach (var configTemplate in uninstallPaths.Distinct(FileSystem.PathComparer))
			{
				var configPath = ConfigScanner.ResolvePath(configTemplate, workspace, home, appdata);

				if (!_fs.FileExists(configPath))
				{
					continue;
				}

				try
				{
					var content = _fs.ReadAllText(configPath);
					var updatedContent = content;
					var removedAny = false;

					while (true)
					{
						var matchedKey = FindServerKeyInConfig(updatedContent, profile.JsonRootKey, serverName, serverDef, defs.Servers);
						if (matchedKey is null)
						{
							break;
						}

						var updated = ConfigWriter.RemoveServer(updatedContent, profile.JsonRootKey, matchedKey);
						if (updated is null)
						{
							break;
						}

						updatedContent = updated;
						removedAny = true;
					}

					if (!removedAny)
					{
						continue;
					}

					if (!dryRun)
					{
						if (_fs.IsReadOnly(configPath))
						{
							operations.Add(new OperationEntry(serverName, targetIde, "error", configPath, "File is read-only"));
							found = true;
							continue;
						}

						if (backedUpFiles.Add(configPath))
						{
							_fs.BackupFile(configPath);
						}

						_fs.WriteAllText(configPath, updatedContent);
					}

					operations.Add(new OperationEntry(serverName, targetIde, "removed", configPath, null, $"Modified {targetIde} configuration file"));
					found = true;
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

		return new OperationResponse(McpSetupProtocol.Version, operations);
	}

	private OperationEntry InstallServer(
		string targetIde,
		IdeProfile profile,
		ScanResult scanResult,
		string serverName,
		ServerDefinition serverDef,
		ServerScanResult? serverResult,
		JsonObject definition,
		bool dryRun,
		HashSet<string> backedUpFiles)
	{
		var targetPath = scanResult.ResolvedWriteTarget;

		if (serverResult is not null && serverResult.Status == "registered")
		{
			return new OperationEntry(serverName, targetIde, "skipped", targetPath, "Already registered and up-to-date");
		}

		// Determine the key to write: use existing key if found (preserve user naming)
		var writeKey = serverName;
		string? existingContent = null;

		if (_fs.FileExists(targetPath))
		{
			if (_fs.IsReadOnly(targetPath))
			{
				return new OperationEntry(serverName, targetIde, "error", targetPath, "File is read-only");
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

		// Apply client-specific transformations
		var installDef = profile.MergeCommandArgs ? MergeCommandAndArgs(definition) : definition;
		var transportLabel = profile.TypeMap is not null
			&& profile.TypeMap.TryGetValue(serverDef.Transport, out var mapped)
			? mapped
			: serverDef.Transport;

		var updated = ConfigWriter.MergeServer(
			existingContent,
			profile.JsonRootKey,
			writeKey,
			installDef,
			profile.IncludeType,
			transportLabel,
			profile.UrlKey);

		if (!dryRun)
		{
			if (existingContent is not null && backedUpFiles.Add(targetPath))
			{
				_fs.BackupFile(targetPath);
			}

			_fs.WriteAllText(targetPath, updated);
		}

		var note = existingContent is not null ? $"Modified {targetIde} configuration file" : null;
		return new OperationEntry(serverName, targetIde, action, targetPath, null, note);
	}

	private static JsonObject MergeCommandAndArgs(JsonObject definition)
	{
		var clone = definition.DeepClone().AsObject();

		if (clone["command"]?.GetValue<string>() is { } cmd
			&& clone.Remove("args", out var argsNode)
			&& argsNode is JsonArray argsArray)
		{
			var cmdArray = new JsonArray { JsonValue.Create(cmd) };
			foreach (var arg in argsArray)
			{
				cmdArray.Add(arg?.DeepClone());
			}

			clone["command"] = cmdArray;
		}

		return clone;
	}

	private static OperationResponse NativeRegistrationResponse(
		Definitions defs,
		string targetIde,
		IdeProfile profile,
		IReadOnlyList<string>? serverFilter)
	{
		var operations = defs.Servers.Keys
			.Where(serverName => serverFilter is null || serverFilter.Contains(serverName, StringComparer.OrdinalIgnoreCase))
			.Select(serverName => new OperationEntry(
				serverName,
				targetIde,
				"error",
				null,
				profile.ManualRegistrationMessage ?? $"Use the native registration flow for {targetIde}."))
			.ToList();

		return new OperationResponse(McpSetupProtocol.Version, operations);
	}

	private static string? FindServerKeyInConfig(
		string content,
		string rootKey,
		string serverName,
		ServerDefinition serverDef,
		IReadOnlyDictionary<string, ServerDefinition> servers)
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
		new(McpSetupProtocol.Version, [new OperationEntry("*", "*", "error", null, message)]);
}
