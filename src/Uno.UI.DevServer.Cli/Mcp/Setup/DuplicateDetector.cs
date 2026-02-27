using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Pure static methods for detecting existing MCP server entries in config files
/// and determining whether they are up-to-date.
/// </summary>
internal static class DuplicateDetector
{
	/// <summary>
	/// Finds which server definition (if any) an existing config entry matches.
	/// Checks key name first, then content (command/url patterns).
	/// Returns the matched server name, or <c>null</c>.
	/// </summary>
	public static string? FindMatchingServer(
		string keyName,
		JsonObject entryJson,
		IReadOnlyDictionary<string, ServerDefinition> servers)
	{
		// Pass 1: key name match
		foreach (var (serverName, serverDef) in servers)
		{
			if (MatchesAnyPattern(keyName, serverDef.Detection.KeyPatterns))
			{
				return serverName;
			}
		}

		// Pass 2: content match
		foreach (var (serverName, serverDef) in servers)
		{
			if (MatchesByContent(entryJson, serverDef))
			{
				return serverName;
			}
		}

		return null;
	}

	/// <summary>
	/// Detects the variant of an existing entry by examining command/args/url content.
	/// Returns <c>"stable"</c>, <c>"prerelease"</c>, <c>"pinned:&lt;ver&gt;"</c>, or <c>"legacy-http"</c>.
	/// </summary>
	public static string DetectVariant(JsonObject entryJson, ServerDefinition serverDef)
	{
		// Check for legacy HTTP (localhost URL for a stdio server)
		if (serverDef.Transport == "stdio")
		{
			var url = entryJson["url"]?.GetValue<string>();
			if (url is not null && serverDef.Detection.UrlPatterns is { } urlPatterns
				&& MatchesAnyPattern(url, urlPatterns))
			{
				return "legacy-http";
			}
		}

		var args = entryJson["args"]?.AsArray();
		if (args is not null)
		{
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
		}

		return "stable";
	}

	/// <summary>
	/// Checks whether the semantic fields of an existing entry match an expected definition.
	/// Ignores non-definition keys (env, disabled, alwaysAllow, inputs).
	/// </summary>
	public static bool IsUpToDate(
		JsonObject existingEntry,
		JsonObject expectedDefinition,
		ServerDefinition serverDef)
	{
		if (serverDef.Transport == "stdio")
		{
			return CommandEquals(existingEntry, expectedDefinition);
		}

		// HTTP transport: compare url
		var existingUrl = existingEntry["url"]?.GetValue<string>();
		var expectedUrl = expectedDefinition["url"]?.GetValue<string>();
		return string.Equals(existingUrl, expectedUrl, StringComparison.Ordinal);
	}

	private static bool CommandEquals(JsonObject existing, JsonObject expected)
	{
		var existingCmd = existing["command"]?.GetValue<string>();
		var expectedCmd = expected["command"]?.GetValue<string>();
		if (!string.Equals(existingCmd, expectedCmd, StringComparison.Ordinal))
		{
			return false;
		}

		var existingArgs = existing["args"]?.AsArray();
		var expectedArgs = expected["args"]?.AsArray();

		if (existingArgs is null && expectedArgs is null)
		{
			return true;
		}

		if (existingArgs is null || expectedArgs is null || existingArgs.Count != expectedArgs.Count)
		{
			return false;
		}

		for (int i = 0; i < existingArgs.Count; i++)
		{
			var a = existingArgs[i]?.GetValue<string>();
			var b = expectedArgs[i]?.GetValue<string>();
			if (!string.Equals(a, b, StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	private static bool MatchesByContent(JsonObject entryJson, ServerDefinition serverDef)
	{
		// Check command patterns for stdio
		if (serverDef.Detection.CommandPatterns is { } cmdPatterns)
		{
			var commandLine = BuildCommandLine(entryJson);
			if (commandLine is not null && MatchesAnyPattern(commandLine, cmdPatterns))
			{
				return true;
			}
		}

		// Check url patterns
		if (serverDef.Detection.UrlPatterns is { } urlPatterns)
		{
			var url = entryJson["url"]?.GetValue<string>();
			if (url is not null && MatchesAnyPattern(url, urlPatterns))
			{
				return true;
			}
		}

		return false;
	}

	private static string? BuildCommandLine(JsonObject entryJson)
	{
		var command = entryJson["command"]?.GetValue<string>();
		if (command is null)
		{
			return null;
		}

		var args = entryJson["args"]?.AsArray();
		if (args is null || args.Count == 0)
		{
			return command;
		}

		var parts = new List<string> { command };
		foreach (var arg in args)
		{
			var val = arg?.GetValue<string>();
			if (val is not null)
			{
				parts.Add(val);
			}
		}

		return string.Join(" ", parts);
	}

	private static bool MatchesAnyPattern(string input, string[] patterns)
	{
		foreach (var pattern in patterns)
		{
			if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
