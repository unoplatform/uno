using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Pure static methods for detecting existing MCP server entries in config files
/// and determining whether they are up-to-date.
/// </summary>
internal static class DuplicateDetector
{
	private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(100);

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
			var url = GetUrl(entryJson);
			if (url is not null && serverDef.Detection.UrlPatterns is { } urlPatterns
				&& MatchesAnyPattern(url, urlPatterns))
			{
				return "legacy-http";
			}
		}

		var args = entryJson["args"]?.AsArray();

		// OpenCode format: extract args from command array (skip first element = command)
		if (args is null && entryJson["command"] is JsonArray cmdArray && cmdArray.Count > 1)
		{
			args = new JsonArray();
			for (int i = 1; i < cmdArray.Count; i++)
			{
				args.Add(cmdArray[i]?.DeepClone());
			}
		}

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
		var existingUrl = GetUrl(existingEntry);
		var expectedUrl = GetUrl(expectedDefinition);
		return string.Equals(existingUrl, expectedUrl, StringComparison.Ordinal);
	}

	private static bool CommandEquals(JsonObject existing, JsonObject expected)
	{
		// Normalize both sides to (command, args) for comparison
		var (existingCmd, existingArgs) = NormalizeCommandArgs(existing);
		var (expectedCmd, expectedArgs) = NormalizeCommandArgs(expected);

		if (!string.Equals(existingCmd, expectedCmd, StringComparison.Ordinal))
		{
			return false;
		}

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

	/// <summary>
	/// Normalizes command+args from either standard format (command string + args array)
	/// or OpenCode format (command array) into a consistent (command, args) tuple.
	/// </summary>
	private static (string? Command, JsonArray? Args) NormalizeCommandArgs(JsonObject entry)
	{
		var commandNode = entry["command"];
		if (commandNode is JsonArray cmdArray)
		{
			var cmd = cmdArray.Count > 0 ? cmdArray[0]?.GetValue<string>() : null;
			JsonArray? args = null;
			if (cmdArray.Count > 1)
			{
				args = new JsonArray();
				for (int i = 1; i < cmdArray.Count; i++)
				{
					args.Add(cmdArray[i]?.DeepClone());
				}
			}

			return (cmd, args);
		}

		return (commandNode?.GetValue<string>(), entry["args"]?.AsArray());
	}

	/// <summary>
	/// Gets the URL from an entry, checking both <c>url</c> and <c>serverUrl</c> (Gemini/Antigravity).
	/// </summary>
	internal static string? GetUrl(JsonObject entryJson) =>
		entryJson["url"]?.GetValue<string>() ?? entryJson["serverUrl"]?.GetValue<string>();

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
			var url = GetUrl(entryJson);
			if (url is not null && MatchesAnyPattern(url, urlPatterns))
			{
				return true;
			}
		}

		return false;
	}

	private static string? BuildCommandLine(JsonObject entryJson)
	{
		var commandNode = entryJson["command"];
		if (commandNode is null)
		{
			return null;
		}

		var parts = new List<string>();

		if (commandNode is JsonArray cmdArray)
		{
			// OpenCode format: command is an array combining command + args
			foreach (var item in cmdArray)
			{
				if (item?.GetValue<string>() is { } val)
				{
					parts.Add(val);
				}
			}
		}
		else
		{
			// Standard format: command is a string, args is a separate array
			if (commandNode.GetValue<string>() is { } cmd)
			{
				parts.Add(cmd);
			}

			if (entryJson["args"]?.AsArray() is { } args)
			{
				foreach (var arg in args)
				{
					if (arg?.GetValue<string>() is { } val)
					{
						parts.Add(val);
					}
				}
			}
		}

		return parts.Count > 0 ? string.Join(" ", parts) : null;
	}

	private static bool MatchesAnyPattern(string input, string[] patterns)
	{
		foreach (var pattern in patterns)
		{
			try
			{
				if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, _regexTimeout))
				{
					return true;
				}
			}
			catch (RegexMatchTimeoutException ex)
			{
				throw new InvalidOperationException($"Regex pattern match timed out for pattern '{pattern}'.", ex);
			}
		}

		return false;
	}
}
