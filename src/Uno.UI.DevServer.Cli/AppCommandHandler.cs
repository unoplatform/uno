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
		["info"] = "uno_app_get_runtime_info",
		["hot-reload"] = "uno_app_hot_reload_status",
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
	/// <c>--flag</c> is <c>true</c>. <c>--json</c> and <c>--out</c> are the command's own options.
	/// </summary>
	public static AppCommandArgs Parse(string[] args)
	{
		if (args.Length == 0)
		{
			return new AppCommandArgs("", new Dictionary<string, object?>(), false, null, "Missing verb. Try: uno-devserver app tools");
		}

		var verb = args[0];
		var toolName = Verbs.TryGetValue(verb, out var mapped) ? mapped : verb;
		var arguments = new Dictionary<string, object?>();
		var json = false;
		string? outputFile = null;

		for (var i = 1; i < args.Length; i++)
		{
			var arg = args[i];
			if (!arg.StartsWith("--", StringComparison.Ordinal) || arg.Length == 2)
			{
				return new AppCommandArgs(toolName, arguments, json, outputFile, $"Unexpected argument '{arg}'. Tool parameters are passed as --name value.");
			}

			var name = arg[2..];
			var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);

			if (string.Equals(name, "json", StringComparison.OrdinalIgnoreCase))
			{
				json = true;
			}
			else if (string.Equals(name, "out", StringComparison.OrdinalIgnoreCase))
			{
				if (!hasValue)
				{
					return new AppCommandArgs(toolName, arguments, json, outputFile, "Missing value for --out");
				}
				outputFile = args[++i];
			}
			else
			{
				arguments[name] = hasValue ? ParseValue(args[++i]) : true;
			}
		}

		return new AppCommandArgs(toolName, arguments, json, outputFile, null);
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

	/// <summary>
	/// 0 when the tool succeeded, 1 when it reported an error. A find that matched nothing is also 1, so
	/// <c>app find --automationId X --waitForMs 5000</c> works as an assertion in a script.
	/// </summary>
	internal static int ExitCodeFor(string toolName, CallToolResult result)
	{
		if (result.IsError == true)
		{
			return 1;
		}

		// ponytail: the zero-match signal is the "0 matched" prefix of the find_elements text; a structured
		// count in the result would replace this string check.
		if (toolName == "uno_app_find_elements"
			&& result.Content.OfType<TextContentBlock>().Any(t => t.Text.StartsWith("0 matched", StringComparison.Ordinal)))
		{
			return 1;
		}

		return 0;
	}

	/// <summary>
	/// Runs one tool call. <paramref name="resolvePort"/> returns the HTTP port of a ready host for the
	/// workspace, starting one when needed, or null when that failed (already logged).
	/// </summary>
	public async Task<int> RunAsync(string[] args, Func<Task<int?>> resolvePort, CancellationToken ct = default)
	{
		var parsed = Parse(args);
		if (parsed.Error is not null)
		{
			_logger.LogError("{Error}", parsed.Error);
			return 2;
		}

		var port = await resolvePort();
		if (port is null)
		{
			return 1;
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

		// App MCP tools appear once the add-in has checked the licence, shortly after the host is ready.
		if (!await WaitForToolAsync(client, parsed.ToolName, ct))
		{
			_logger.LogError("Tool '{Tool}' is not available on the DevServer. Run `uno-devserver app tools` to list the tools, and `uno-devserver health` when the list is empty.", parsed.ToolName);
			return 2;
		}

		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(ToolCallTimeout);

		var result = await client.CallToolAsync(parsed.ToolName, parsed.Arguments, cancellationToken: timeoutCts.Token);

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

		return ExitCodeFor(parsed.ToolName, result);
	}

	private async Task<bool> WaitForToolAsync(McpClient client, string toolName, CancellationToken ct)
	{
		var deadline = DateTime.UtcNow + ToolListTimeout;
		while (true)
		{
			var tools = await client.ListToolsAsync(cancellationToken: ct);
			if (tools.Any(t => t.Name == toolName))
			{
				return true;
			}

			if (DateTime.UtcNow >= deadline)
			{
				return false;
			}

			_logger.LogDebug("Tool {Tool} not listed yet ({Count} tools); waiting", toolName, tools.Count);
			await Task.Delay(500, ct);
		}
	}
}
