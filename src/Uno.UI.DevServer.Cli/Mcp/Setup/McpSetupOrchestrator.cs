using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Top-level orchestrator for MCP setup operations: status, install, uninstall.
/// Composes <see cref="ConfigScanner"/>, <see cref="ConfigWriter"/>,
/// <see cref="ServerDefinitionResolver"/>, and <see cref="DuplicateDetector"/>.
/// </summary>
internal sealed class McpSetupOrchestrator(IFileSystem fs, ILogger<McpSetupOrchestrator> logger, CliCommandRunner? cliRunner = null)
{
	private readonly IFileSystem _fs = fs;
	private readonly ILogger<McpSetupOrchestrator> _logger = logger;
	private readonly CliCommandRunner? _cliRunner = cliRunner;

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
			var cliServerNames = QueryCliList(profile, workspace);

			if ((scanResult.Detected || cliServerNames.Count > 0) && !profile.ExcludeFromDetection)
			{
				detectedIdes.Add(ideName);
			}
			supportedIdes.Add(new SupportedIdeEntry(
				ideName, profile.Strategy,
				(scanResult.Detected || cliServerNames.Count > 0) && !profile.ExcludeFromDetection));

			foreach (var (serverName, serverResult) in scanResult.ServerResults)
			{
				if (!ideStatusMap.TryGetValue(serverName, out var map))
				{
					map = [];
					ideStatusMap[serverName] = map;
				}

				// If file scan says missing but CLI list found it, upgrade to registered
				var status = serverResult.Status;
				var locations = serverResult.Locations;
				if (status == "missing" && cliServerNames.Contains(serverName))
				{
					status = "registered";
					locations = [new LocationEntry($"(via {profile.Cli!.Executable} CLI)", serverName, "cli")];
				}

				map[ideName] = new ServerIdeStatus(
					ideName,
					status,
					locations.Count > 0 ? locations : null,
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

		// CLI-first: if the agent provides a CLI and it is available, delegate to it.
		if (profile.Cli is { } cli && _cliRunner is not null && _cliRunner.IsAvailable(cli))
		{
			return InstallViaCli(defs, workspace, targetIde, profile, cli, expectedVariant, serverFilter, dryRun);
		}

		// Native strategy: no file-based config, no CLI available → manual registration guidance
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

			if (!IsTransportSupported(profile, serverDef))
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
					targetIde, profile, scanResult, serverName, serverDef, serverResult, definition, defs.Servers, dryRun, backedUpFiles);
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

		// CLI-first: if the agent provides a CLI and it is available, delegate to it.
		if (profile.Cli is { Remove: not null } cli && _cliRunner is not null && _cliRunner.IsAvailable(cli))
		{
			return UninstallViaCli(defs, workspace, targetIde, profile, cli, serverFilter, dryRun);
		}

		// Native strategy: no file-based config, no CLI available → manual registration guidance
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

					if (_fs.IsReadOnly(configPath))
					{
						operations.Add(new OperationEntry(serverName, targetIde, "error", configPath, "File is read-only"));
						found = true;
						continue;
					}

					if (!dryRun)
					{
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
		IReadOnlyDictionary<string, ServerDefinition> allServers,
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
			var matchedKey = FindServerKeyInConfig(existingContent, profile.JsonRootKey, serverName, serverDef, allServers);
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

	private OperationResponse InstallViaCli(
		Definitions defs,
		string workspace,
		string targetIde,
		IdeProfile profile,
		CliProfile cli,
		string expectedVariant,
		IReadOnlyList<string>? serverFilter,
		bool dryRun)
	{
		var operations = new List<OperationEntry>();
		var expectedDefinitions = BuildExpectedDefinitions(defs.Servers, expectedVariant);

		foreach (var (serverName, serverDef) in defs.Servers)
		{
			if (serverFilter is not null && !serverFilter.Contains(serverName, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!IsTransportSupported(profile, serverDef))
			{
				continue;
			}

			if (!expectedDefinitions.TryGetValue(serverName, out var definition))
			{
				continue;
			}

			try
			{
				var entry = InstallServerViaCli(targetIde, cli, serverName, serverDef, definition, workspace, dryRun);
				operations.Add(entry);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "CLI install failed for {Server}, falling back to file", serverName);
				var scanner = new ConfigScanner(_fs);
				var scanResult = scanner.Scan(profile, workspace, defs.Servers, expectedDefinitions);
				var serverResult = scanResult.ServerResults.TryGetValue(serverName, out var sr) ? sr : null;
				var backedUp = new HashSet<string>(FileSystem.PathComparer);
				operations.Add(InstallServer(targetIde, profile, scanResult, serverName, serverDef, serverResult, definition, defs.Servers, dryRun, backedUp));
			}
		}

		return new OperationResponse(McpSetupProtocol.Version, operations);
	}

	private OperationEntry InstallServerViaCli(
		string targetIde,
		CliProfile cli,
		string serverName,
		ServerDefinition serverDef,
		JsonObject definition,
		string workingDirectory,
		bool dryRun)
	{
		var template = string.Equals(serverDef.Transport, "http", StringComparison.OrdinalIgnoreCase)
			? cli.AddHttp
			: cli.AddStdio;

		if (template is null)
		{
			return new OperationEntry(serverName, targetIde, "error", null,
				$"CLI does not support {serverDef.Transport} transport.");
		}

		var placeholders = BuildPlaceholders(serverName, serverDef, definition);

		if (dryRun)
		{
			var args = CliCommandRunner.ExpandArgs(template, placeholders);
			return new OperationEntry(serverName, targetIde, "created", null, null,
				$"Dry-run: {cli.Executable} {string.Join(' ', args)}");
		}

		var (exitCode, stdout, stderr) = _cliRunner!.Execute(
			cli.Executable, template, placeholders, workingDirectory);

		if (exitCode != 0)
		{
			throw new InvalidOperationException(
				$"CLI exited with code {exitCode}: {stderr.Trim()}");
		}

		return new OperationEntry(serverName, targetIde, "created", null, null,
			$"Registered via {cli.Executable} CLI");
	}

	private OperationResponse UninstallViaCli(
		Definitions defs,
		string workspace,
		string targetIde,
		IdeProfile profile,
		CliProfile cli,
		IReadOnlyList<string>? serverFilter,
		bool dryRun)
	{
		var operations = new List<OperationEntry>();

		foreach (var (serverName, serverDef) in defs.Servers)
		{
			if (serverFilter is not null && !serverFilter.Contains(serverName, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!IsTransportSupported(profile, serverDef))
			{
				continue;
			}

			var placeholders = new Dictionary<string, object> { ["name"] = serverName };

			if (dryRun)
			{
				var args = CliCommandRunner.ExpandArgs(cli.Remove!, placeholders);
				operations.Add(new OperationEntry(serverName, targetIde, "removed", null, null,
					$"Dry-run: {cli.Executable} {string.Join(' ', args)}"));
				continue;
			}

			try
			{
				var (exitCode, _, stderr) = _cliRunner!.Execute(
					cli.Executable, cli.Remove!, placeholders, workspace);

				operations.Add(exitCode == 0
					? new OperationEntry(serverName, targetIde, "removed", null, null,
						$"Removed via {cli.Executable} CLI")
					: new OperationEntry(serverName, targetIde, "error", null,
						$"CLI exited with code {exitCode}: {stderr.Trim()}"));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CLI uninstall failed for {Server}", serverName);
				operations.Add(new OperationEntry(serverName, targetIde, "error", null, ex.Message));
			}
		}

		return new OperationResponse(McpSetupProtocol.Version, operations);
	}

	private static Dictionary<string, object> BuildPlaceholders(
		string serverName,
		ServerDefinition serverDef,
		JsonObject definition)
	{
		var placeholders = new Dictionary<string, object> { ["name"] = serverName };

		if (string.Equals(serverDef.Transport, "http", StringComparison.OrdinalIgnoreCase))
		{
			if (definition["url"]?.GetValue<string>() is { } url)
			{
				placeholders["url"] = url;
			}
		}
		else
		{
			if (definition["command"]?.GetValue<string>() is { } command)
			{
				placeholders["command"] = command;
			}

			if (definition["args"] is JsonArray argsArray)
			{
				placeholders["args"] = argsArray
					.Select(a => a?.GetValue<string>() ?? string.Empty)
					.ToArray();
			}
		}

		return placeholders;
	}

	private static bool IsTransportSupported(IdeProfile profile, ServerDefinition serverDef)
	{
		return profile.SupportedTransports is null
			|| profile.SupportedTransports.Any(t => string.Equals(t, serverDef.Transport, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Queries the agent's CLI list command to discover registered server names.
	/// Returns an empty set if the CLI is unavailable or the command fails.
	/// </summary>
	private HashSet<string> QueryCliList(IdeProfile profile, string workspace)
	{
		if (profile.Cli is not { List: not null } cli
			|| _cliRunner is null
			|| !_cliRunner.IsAvailable(cli))
		{
			return [];
		}

		try
		{
			var (exitCode, stdout, _) = _cliRunner.Execute(
				cli.Executable, cli.List, new Dictionary<string, object>(), workspace, TimeSpan.FromSeconds(15));

			if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout))
			{
				return [];
			}

			// Match known server names in the output (case-insensitive text search)
			var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var line in stdout.Split('\n'))
			{
				var trimmed = line.Trim();
				if (trimmed.Contains("UnoApp", StringComparison.OrdinalIgnoreCase)
					|| trimmed.Contains("uno-app", StringComparison.OrdinalIgnoreCase)
					|| trimmed.Contains("uno_app", StringComparison.OrdinalIgnoreCase))
				{
					found.Add("UnoApp");
				}

				if (trimmed.Contains("UnoDocs", StringComparison.OrdinalIgnoreCase)
					|| trimmed.Contains("uno-docs", StringComparison.OrdinalIgnoreCase)
					|| trimmed.Contains("uno_docs", StringComparison.OrdinalIgnoreCase))
				{
					found.Add("UnoDocs");
				}
			}

			return found;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			_logger.LogDebug(ex, "Failed to query CLI list for {Executable}", cli.Executable);
			return [];
		}
	}

	private static OperationResponse ErrorResponse(string message) =>
		new(McpSetupProtocol.Version, [new OperationEntry("*", "*", "error", null, message)]);
}
