using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli;

/// <summary>
/// Parsed arguments for the <c>app</c> command: one tool call against the host's <c>/mcp</c> endpoint.
/// </summary>
internal sealed record AppCommandArgs(
	string ToolName,
	IReadOnlyDictionary<string, object?> Arguments,
	bool Json,
	string? OutputFile,
	bool NoStart,
	string? Error);

/// <summary>
/// Handles the <c>app</c> command: <c>uno-devserver app &lt;verb&gt; [--param value]... [--json] [--out file]</c>.
/// Every verb is one App MCP tool (<c>uno_app_*</c>) called over the same HTTP MCP endpoint the stdio proxy
/// uses, so a shell, a hook, or a CI script gets the same tools as an MCP client, plus an exit code.
/// </summary>
internal sealed class AppCommandHandler
{
	/// <summary>Short verbs for the App MCP tools. Any other name is passed to the host verbatim.</summary>
	internal static readonly IReadOnlyDictionary<string, string> Verbs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["start"] = "uno_app_start",
		["close"] = "uno_app_close",
		["restart"] = "uno_app_restart",
		["info"] = "uno_app_get_runtime_info",
		["hot-reload-status"] = "uno_app_hot_reload_status",
		["screenshot"] = "uno_app_get_screenshot",
		["tree"] = "uno_app_visualtree_snapshot",
		["find"] = "uno_app_find_elements",
		["invoke"] = "uno_app_element_peer_default_action",
		["action"] = "uno_app_element_peer_action",
		["click"] = "uno_app_pointer_click",
		["key"] = "uno_app_key_press",
		["type"] = "uno_app_type_text",
		["datacontext"] = "uno_app_get_element_datacontext",
		["memory"] = "uno_app_get_memory_counters",
		["diag"] = "uno_devserver_diagnostics",
	};

	internal const string ToolsVerb = "tools";

	/// <summary><c>app wait [--timeoutSeconds 120] [--ready]</c>: until an app is connected (and Hot Reload is Ready).</summary>
	internal const string WaitVerb = "wait";

	/// <summary><c>app assert &lt;find selectors&gt; [--state tok,..] [--absent] [--count N]</c>: a find with a verdict.</summary>
	internal const string AssertVerb = "assert";

	/// <summary>The reference printed by <c>app --help</c>, <c>app help</c> and a bare <c>app</c>.</summary>
	internal static string HelpText => $$"""
		Usage: uno-devserver [--solution-dir <path>] app <verb|tool> [--param value]... [--json] [--out <file>] [--no-start]

		Calls one App MCP tool on the DevServer of the solution in the current folder (or --solution-dir) and exits
		with a code a script can test. Starts the DevServer when none runs, unless --no-start.
		The tool result goes to stdout, logs go to stderr.

		Discover the tools and their parameters (the list depends on the App MCP version and the license):
		  app tools                 One line per tool: verb (tool name): description
		  app tools --json          The tools with their input schemas: parameter names, types, required ones

		Verbs (any other name is sent as a tool name, e.g. app uno_app_get_screenshot --fileType png):
		{{string.Join(Environment.NewLine, Verbs.Select(v => $"  {v.Key,-24}{v.Value}"))}}

		Script verbs:
		  wait [--timeoutSeconds 120] [--ready]
		      Wait until an app is connected; with --ready, until its Hot Reload is Ready.
		      Exit 1 on timeout, or when Hot Reload is disabled.
		  assert <find parameters> [--state tok,..] [--absent] [--count N]
		      Run find, then check every match: each tok must be in the match's {...} state group, a !tok must not.
		      --absent passes when nothing matched, --count N on an exact count. Prints "assert: pass" or "assert: fail (why)".

		Parameters:
		  --name value              Passed to the tool as "name". JSON values (numbers, true, false, null, {...}, [...]) are
		                            sent typed, anything else as text; a parameter the tool declares as a string gets text.
		  --name                    Alone (last, or followed by another --option): true.
		  --json                    Print the raw MCP CallToolResult as JSON instead of its text.
		  --out <file>              Where an image result is saved (default: uno-app-<timestamp>.png in the current folder).
		                            The saved path is printed.
		  --no-start                Never start a DevServer, and do not wait for tools it does not list yet: exit 2 instead.
		                            For hooks that run on every edit.
		  On an App MCP whose element tools take only --elementRef (from `app tree`), --name <x:Name> is resolved
		  through the visual tree. --automationId cannot be resolved that way.

		Exit codes:
		  0  The tool succeeded.
		  1  The tool reported an error; find matched nothing; assert failed; hot-reload-status: the edit is not applied;
		     wait timed out; the DevServer could not start.
		  2  Bad arguments; no DevServer with --no-start; the tool is not offered.
		  3  hot-reload-status: a rude edit, only `app restart` applies it.

		Examples:
		  uno-devserver app start --projectPath src/MyApp/MyApp.csproj --targetFramework net10.0-desktop
		  uno-devserver app wait --ready
		  uno-devserver app tree
		  uno-devserver app find --automationId SaveButton --waitForMs 5000
		  uno-devserver app assert --automationId Row --state selected --count 2
		  uno-devserver app hot-reload-status --file src/MyApp/MainPage.xaml --waitForMs 10000 --no-start
		  uno-devserver app screenshot --fileType png --out shot.png
		  uno-devserver app close

		When `app` starts the DevServer itself, the DevServer inherits stdout, so `uno-devserver app ... | tail` waits
		until the DevServer exits. Run `uno-devserver start` first, pass --no-start, or redirect stdout to a file.
		""";

	private const string FindTool = "uno_app_find_elements";
	private const string RuntimeInfoTool = "uno_app_get_runtime_info";
	private const string HotReloadStatusTool = "uno_app_hot_reload_status";
	private const string SnapshotTool = "uno_app_visualtree_snapshot";

	private static readonly TimeSpan ToolCallTimeout = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan ToolListTimeout = TimeSpan.FromSeconds(15);

	private readonly ILogger _logger;

	public AppCommandHandler(ILogger logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Parses the arguments after the <c>app</c> verb. <c>--name value</c> becomes a tool argument; a value that
	/// parses as JSON (number, bool, object, array) is sent typed, anything else as a string, and a bare
	/// <c>--flag</c> is <c>true</c>. <c>--json</c>, <c>--out</c> and <c>--no-start</c> are the command's own options.
	/// </summary>
	public static AppCommandArgs Parse(string[] args)
	{
		if (args.Length == 0)
		{
			return new AppCommandArgs("", new Dictionary<string, object?>(), false, null, false, "Missing verb. Try: uno-devserver app --help");
		}

		var verb = args[0];
		var toolName = Verbs.TryGetValue(verb, out var mapped) ? mapped : verb;
		var arguments = new Dictionary<string, object?>();
		var json = false;
		var noStart = false;
		string? outputFile = null;

		for (var i = 1; i < args.Length; i++)
		{
			var arg = args[i];
			if (!arg.StartsWith("--", StringComparison.Ordinal) || arg.Length == 2)
			{
				return new AppCommandArgs(toolName, arguments, json, outputFile, noStart, $"Unexpected argument '{arg}'. Tool parameters are passed as --name value.");
			}

			var name = arg[2..];
			var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);

			if (string.Equals(name, "json", StringComparison.OrdinalIgnoreCase))
			{
				json = true;
			}
			else if (string.Equals(name, "no-start", StringComparison.OrdinalIgnoreCase))
			{
				noStart = true;
			}
			else if (string.Equals(name, "out", StringComparison.OrdinalIgnoreCase))
			{
				if (!hasValue)
				{
					return new AppCommandArgs(toolName, arguments, json, outputFile, noStart, "Missing value for --out");
				}
				outputFile = args[++i];
			}
			else
			{
				arguments[name] = hasValue ? ParseValue(args[++i]) : true;
			}
		}

		return new AppCommandArgs(toolName, arguments, json, outputFile, noStart, null);
	}

	internal static object? ParseValue(string value)
	{
		if (value.Length > 0 && (char.IsDigit(value[0]) || value[0] is '-' or '{' or '[' || value is "true" or "false" or "null"))
		{
			try
			{
				return JsonSerializer.Deserialize<JsonElement>(value);
			}
			catch (JsonException)
			{
				// Not JSON after all (e.g. "-1px", "2024-01-01"): send it as text.
			}
		}

		return value;
	}

	/// <summary>Exit code of <c>app hot-reload-status --file</c> when only a restart can apply the edit (RudeEdit).</summary>
	internal const int RestartNeededExitCode = 3;

	/// <summary>
	/// 0 when the tool succeeded, 1 when it reported an error. A find that matched nothing is also 1, so
	/// <c>app find --automationId X --waitForMs 5000</c> works as an assertion in a script. A hot-reload status
	/// for a file is 0 only when the app runs the edit, 1 when it does not (the text says why), and
	/// <see cref="RestartNeededExitCode"/> when it was a rude edit, so a hook can act on the code alone.
	/// </summary>
	internal static int ExitCodeFor(string toolName, CallToolResult result)
	{
		if (result.IsError == true)
		{
			return 1;
		}

		// App MCP 2.0 hosts put the count and the verdict in structuredContent; the text checks below are for older ones.
		if (result.StructuredContent is { ValueKind: JsonValueKind.Object } structured)
		{
			if (toolName == FindTool && structured.TryGetProperty("matched", out var matched))
			{
				return matched.GetInt32() == 0 ? 1 : 0;
			}

			if (toolName == HotReloadStatusTool && structured.TryGetProperty("verdict", out var verdictValue))
			{
				return verdictValue.GetString() switch
				{
					"applied" => 0,
					"rudeEdit" => RestartNeededExitCode,
					_ => 1,
				};
			}
		}

		var text = TextOf(result);

		// ponytail: the zero-match signal is the "0 matched" prefix of the find_elements text; a structured
		// count in the result would replace this string check.
		if (toolName == FindTool && text.StartsWith("0 matched", StringComparison.Ordinal))
		{
			return 1;
		}

		// ponytail: the file verdict is the text after "edit to '<file>': "; a structured verdict would replace this.
		if (toolName == HotReloadStatusTool
			&& text.Split('\n').FirstOrDefault(l => l.StartsWith("edit to '", StringComparison.Ordinal)) is { } verdict)
		{
			var separator = verdict.IndexOf("': ", StringComparison.Ordinal);
			var outcome = separator < 0 ? verdict : verdict[(separator + 3)..];
			if (outcome.StartsWith("applied", StringComparison.Ordinal))
			{
				return 0;
			}

			return outcome.Contains("RudeEdit", StringComparison.Ordinal) ? RestartNeededExitCode : 1;
		}

		return 0;
	}

	private static string TextOf(CallToolResult result)
		=> string.Concat(result.Content.OfType<TextContentBlock>().Select(t => t.Text)).Replace("\r", "");

	/// <summary>
	/// The verdict of <c>app assert</c> on a find result: null when it passes, otherwise why it failed.
	/// <paramref name="state"/> tokens must all be in every match's <c>{...}</c> group, a <c>!token</c> must be
	/// in none; <paramref name="absent"/> passes on zero matches; <paramref name="count"/> is an exact count.
	/// </summary>
	internal static string? AssertFailure(string findText, string[] state, bool absent, int? count)
	{
		var lines = findText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		var matched = lines.Length > 0 && int.TryParse(lines[0].Split(' ')[0], out var n) ? n : 0;

		if (absent)
		{
			return matched == 0 ? null : $"expected no match, {matched} matched";
		}

		if (count is { } expected && matched != expected)
		{
			return $"expected {expected} matched, got {matched}";
		}

		if (matched == 0)
		{
			return "expected a match, 0 matched";
		}

		foreach (var line in lines.Skip(1))
		{
			// ponytail: the state group is the first "{a b c}" on the match line; tokens are space separated.
			var open = line.IndexOf('{');
			var close = open < 0 ? -1 : line.IndexOf('}', open);
			var tokens = close < 0 ? [] : line[(open + 1)..close].Split(' ', StringSplitOptions.RemoveEmptyEntries);

			foreach (var token in state)
			{
				var negated = token.StartsWith('!');
				var present = tokens.Contains(negated ? token[1..] : token, StringComparer.Ordinal);
				if (present == negated)
				{
					return $"expected {token} on: {line.Trim()}";
				}
			}
		}

		return null;
	}

	/// <summary>
	/// The <c>^ref</c> of the one snapshot line whose <c>#Name</c> is <paramref name="name"/>, for App MCP versions
	/// whose element tools take only <c>elementRef</c>. Null when none or several match (the error says which).
	/// </summary>
	internal static string? ResolveRefByName(string snapshotText, string name, out string? error)
	{
		var refs = new List<string>();
		foreach (var line in snapshotText.Split('\n'))
		{
			// ponytail: "Type ^ref #Name ..." with whitespace-delimited tokens; a name with spaces is not resolved.
			var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var refIndex = Array.FindIndex(tokens, t => t.StartsWith('^'));
			if (refIndex >= 0 && refIndex + 1 < tokens.Length && tokens[refIndex + 1] == "#" + name)
			{
				refs.Add(tokens[refIndex][1..]);
			}
		}

		error = refs.Count switch
		{
			0 => $"No element named '{name}' in the visual tree (names show as #Name in `app tree`).",
			1 => null,
			_ => $"{refs.Count} elements named '{name}'; pass --elementRef with one of: {string.Join(", ", refs)}",
		};
		return refs.Count == 1 ? refs[0] : null;
	}

	/// <summary>
	/// Runs one tool call. <paramref name="resolvePort"/> returns the HTTP port of a ready host for the
	/// workspace, starting one when needed unless told not to, or null when that failed (already logged).
	/// </summary>
	public async Task<int> RunAsync(string[] args, Func<bool, Task<int?>> resolvePort, CancellationToken ct = default)
	{
		if (args.Length == 0 || string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine(HelpText);
			return 0;
		}

		var parsed = Parse(args);
		if (parsed.Error is not null)
		{
			_logger.LogError("{Error}", parsed.Error);
			return 2;
		}

		var port = await resolvePort(parsed.NoStart);
		if (port is null)
		{
			// With --no-start, no host is "nothing to talk to" (2), like a tool that is not offered.
			return parsed.NoStart ? 2 : 1;
		}

		var transport = new HttpClientTransport(new()
		{
			Name = "Devserver",
			Endpoint = new Uri($"http://localhost:{port}/mcp"),
			TransportMode = HttpTransportMode.StreamableHttp,
		});

		await using var client = await McpClient.CreateAsync(
			transport,
			new McpClientOptions { ClientInfo = new Implementation { Name = "uno-devserver-cli", Version = "1.0.0" } },
			cancellationToken: ct);

		if (parsed.ToolName == ToolsVerb)
		{
			// The host has listed each tool twice (host 6.8 with App MCP 1.4); one line per tool here.
			var tools = (await client.ListToolsAsync(cancellationToken: ct)).DistinctBy(t => t.Name).ToList();
			if (parsed.Json)
			{
				Console.WriteLine(JsonSerializer.Serialize(tools.Select(t => t.ProtocolTool), McpJsonUtilities.DefaultOptions));
			}
			else
			{
				foreach (var tool in tools)
				{
					var verb = Verbs.FirstOrDefault(v => v.Value == tool.Name).Key;
					Console.WriteLine($"{(verb is null ? tool.Name : $"{verb} ({tool.Name})")}: {tool.Description}");
				}
			}

			return 0;
		}

		if (parsed.ToolName == WaitVerb)
		{
			return await WaitAsync(client, parsed, ct);
		}

		var toolName = parsed.ToolName == AssertVerb ? FindTool : parsed.ToolName;
		var arguments = new Dictionary<string, object?>(parsed.Arguments);

		// App MCP tools appear once the add-in has checked the licence, shortly after the host is ready.
		if (await WaitForToolAsync(client, toolName, parsed.NoStart, ct) is not { } mcpTool)
		{
			_logger.LogError("Tool '{Tool}' is not available on the DevServer. Run `uno-devserver app tools` to list the tools, and `uno-devserver health` when the list is empty.", toolName);
			return 2;
		}

		CoerceStrings(mcpTool.ProtocolTool.InputSchema, arguments);

		string[] assertState = [];
		var assertAbsent = false;
		int? assertCount = null;
		if (parsed.ToolName == AssertVerb)
		{
			assertState = (TakeString(arguments, "state") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			assertAbsent = TakeBool(arguments, "absent");
			assertCount = TakeInt(arguments, "count");
		}

		// Older App MCP element tools take only elementRef: resolve --name through a snapshot here.
		if (!HasParameter(mcpTool, "name") && HasParameter(mcpTool, "elementRef") && TakeString(arguments, "name") is { } name)
		{
			var snapshot = await CallAsync(client, SnapshotTool, new Dictionary<string, object?> { ["detail"] = "compact" }, ct);
			if (snapshot.IsError == true)
			{
				_logger.LogError("{Error}", TextOf(snapshot));
				return 1;
			}

			if (ResolveRefByName(TextOf(snapshot), name, out var error) is not { } elementRef)
			{
				_logger.LogError("{Error}", error);
				return 1;
			}

			arguments["elementRef"] = elementRef;
		}

		if (!HasParameter(mcpTool, "automationId") && arguments.ContainsKey("automationId"))
		{
			_logger.LogError("This App MCP's {Tool} takes elementRef only and the visual tree does not show automation ids: pass --name (x:Name) or --elementRef (from `app tree`).", toolName);
			return 2;
		}

		var result = await CallAsync(client, toolName, arguments, ct);

		if (parsed.ToolName == AssertVerb && result.IsError != true)
		{
			var failure = AssertFailure(TextOf(result), assertState, assertAbsent, assertCount);
			Console.WriteLine(TextOf(result).TrimEnd());
			Console.WriteLine(failure is null ? "assert: pass" : $"assert: fail ({failure})");
			return failure is null ? 0 : 1;
		}

		if (parsed.Json)
		{
			Console.WriteLine(JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions));
		}
		else
		{
			foreach (var block in result.Content)
			{
				switch (block)
				{
					case TextContentBlock text:
						Console.WriteLine(text.Text);
						break;
					case ImageContentBlock image:
						var file = parsed.OutputFile ?? $"uno-app-{DateTime.Now:yyyyMMdd-HHmmss}.{(image.MimeType.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png")}";
						await File.WriteAllBytesAsync(file, image.DecodedData, ct);
						Console.WriteLine(Path.GetFullPath(file));
						break;
					default:
						Console.WriteLine(JsonSerializer.Serialize(block, McpJsonUtilities.DefaultOptions));
						break;
				}
			}
		}

		return ExitCodeFor(toolName, result);
	}

	/// <summary>
	/// Polls the runtime info until an app is connected, then with <c>--ready</c> until its Hot Reload is Ready
	/// (through the status tool when offered, else by the clock the skill uses). 0 when done, 1 on timeout or
	/// when Hot Reload is disabled, 2 when the runtime info tool is not offered.
	/// </summary>
	private async Task<int> WaitAsync(McpClient client, AppCommandArgs parsed, CancellationToken ct)
	{
		var arguments = new Dictionary<string, object?>(parsed.Arguments);
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(TakeInt(arguments, "timeoutSeconds") ?? 120);
		var ready = TakeBool(arguments, "ready");

		if (await WaitForToolAsync(client, RuntimeInfoTool, parsed.NoStart, ct) is null)
		{
			_logger.LogError("Tool '{Tool}' is not available on the DevServer.", RuntimeInfoTool);
			return 2;
		}

		string info;
		while (true)
		{
			info = TextOf(await CallAsync(client, RuntimeInfoTool, new Dictionary<string, object?>(), ct));
			if (info.Contains("Process Id:", StringComparison.Ordinal))
			{
				break;
			}

			if (DateTime.UtcNow >= deadline)
			{
				Console.WriteLine(info.TrimEnd());
				return 1;
			}

			await Task.Delay(1000, ct);
		}

		if (!ready)
		{
			Console.WriteLine(info.TrimEnd());
			return 0;
		}

		var tools = await client.ListToolsAsync(cancellationToken: ct);
		while (true)
		{
			var state = info.Split('\n').FirstOrDefault(l => l.StartsWith("Hot Reload: ", StringComparison.Ordinal))?["Hot Reload: ".Length..].Split('.', ' ')[0];
			switch (state)
			{
				case "Ready" or "Processing":
					Console.WriteLine(info.TrimEnd());
					return 0;
				case null or "unknown":
					// An add-in that does not report the state: the first edit is safe about 15 s after the connection.
					_logger.LogInformation("The app does not report its Hot Reload state; waiting 15 s after the connection instead.");
					await Task.Delay(TimeSpan.FromSeconds(15), ct);
					Console.WriteLine(info.TrimEnd());
					return 0;
				case "Initializing":
					break;
				default:
					Console.WriteLine(info.TrimEnd());
					return 1; // Disabled and the like: the text says what to do.
			}

			var remaining = deadline - DateTime.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				Console.WriteLine(info.TrimEnd());
				return 1;
			}

			if (tools.Any(t => t.Name == HotReloadStatusTool))
			{
				var waitForMs = (int)Math.Min(remaining.TotalMilliseconds, 20000); // the tool's own cap
				info = TextOf(await CallAsync(client, HotReloadStatusTool, new Dictionary<string, object?> { ["waitForReady"] = true, ["waitForMs"] = waitForMs }, ct));
			}
			else
			{
				await Task.Delay(1000, ct);
				info = TextOf(await CallAsync(client, RuntimeInfoTool, new Dictionary<string, object?>(), ct));
			}
		}
	}

	private static async Task<CallToolResult> CallAsync(McpClient client, string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken ct)
	{
		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(ToolCallTimeout);
		return await client.CallToolAsync(toolName, arguments, cancellationToken: timeoutCts.Token);
	}

	private static bool HasParameter(McpClientTool tool, string name)
		=> tool.ProtocolTool.InputSchema.TryGetProperty("properties", out var properties)
			&& properties.ValueKind == JsonValueKind.Object
			&& properties.TryGetProperty(name, out _);

	/// <summary>
	/// A value that parsed as JSON goes back to its text when the tool declares the parameter as a string:
	/// an elementRef handle is a number on the command line (`--elementRef 1`) and a string to the tool.
	/// </summary>
	internal static void CoerceStrings(JsonElement inputSchema, Dictionary<string, object?> arguments)
	{
		if (!inputSchema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
		{
			return;
		}

		foreach (var (name, value) in arguments.ToArray())
		{
			// A nullable parameter is declared as "type": ["string", "null"].
			if (value is JsonElement { ValueKind: not JsonValueKind.String } json
				&& properties.TryGetProperty(name, out var property)
				&& property.TryGetProperty("type", out var type)
				&& (type.ValueKind == JsonValueKind.String
					? type.GetString() == "string"
					: type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == "string")))
			{
				arguments[name] = json.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? json.GetRawText() : json.ToString();
			}
		}
	}

	/// <summary>Removes a CLI-side option from the tool arguments and returns it; values arrive as string or JsonElement.</summary>
	private static string? TakeString(Dictionary<string, object?> arguments, string name)
	{
		if (!arguments.Remove(name, out var value))
		{
			return null;
		}

		return value is JsonElement json ? json.ToString() : value?.ToString();
	}

	private static int? TakeInt(Dictionary<string, object?> arguments, string name)
		=> int.TryParse(TakeString(arguments, name), out var value) ? value : null;

	private static bool TakeBool(Dictionary<string, object?> arguments, string name)
		=> TakeString(arguments, name) is { } value && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// The tool, waiting for the add-in's licence check after a host start. With <c>--no-start</c> the host was
	/// already running, so a tool it does not list now is not coming: a hook must not pay the wait on every edit.
	/// </summary>
	private async Task<McpClientTool?> WaitForToolAsync(McpClient client, string toolName, bool noStart, CancellationToken ct)
	{
		var deadline = DateTime.UtcNow + (noStart ? TimeSpan.Zero : ToolListTimeout);
		while (true)
		{
			var tools = await client.ListToolsAsync(cancellationToken: ct);
			if (tools.FirstOrDefault(t => t.Name == toolName) is { } tool)
			{
				return tool;
			}

			if (DateTime.UtcNow >= deadline)
			{
				return null;
			}

			_logger.LogDebug("Tool {Tool} not listed yet ({Count} tools); waiting", toolName, tools.Count);
			await Task.Delay(500, ct);
		}
	}
}
