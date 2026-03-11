using System.Text.Json.Nodes;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Resolves the expected variant and concrete <see cref="JsonObject"/> definition
/// for a given server based on the running tool version and CLI flags.
/// </summary>
internal static class ServerDefinitionResolver
{
	/// <summary>
	/// Determines the expected variant based on the running tool version and CLI flags.
	/// </summary>
	/// <returns>
	/// <c>"stable"</c>, <c>"prerelease"</c>, or <c>"pinned:&lt;ver&gt;"</c>.
	/// </returns>
	public static string ResolveExpectedVariant(
		string toolVersion,
		bool releaseFlag,
		bool prereleaseFlag,
		string? versionFlag)
	{
		if (versionFlag is not null)
		{
			return $"pinned:{versionFlag}";
		}

		if (prereleaseFlag)
		{
			return "prerelease";
		}

		if (releaseFlag)
		{
			return "stable";
		}

		// Auto-detect from tool version
		return IsPrerelease(toolVersion) ? "prerelease" : "stable";
	}

	/// <summary>
	/// Builds the concrete <see cref="JsonObject"/> definition for a server and variant.
	/// For pinned variants, replaces <c>{version}</c> placeholder in args.
	/// </summary>
	public static JsonObject ResolveDefinition(ServerDefinition serverDef, string expectedVariant)
	{
		if (expectedVariant.StartsWith("pinned:", StringComparison.Ordinal))
		{
			var version = expectedVariant["pinned:".Length..];
			if (!serverDef.Variants.TryGetValue("pinned", out var pinnedTemplate))
			{
				// Fall back to stable if no pinned template
				return CloneJsonObject(GetStableVariant(serverDef, expectedVariant));
			}

			var cloned = CloneJsonObject(pinnedTemplate);
			ReplaceVersionPlaceholder(cloned, version);
			return cloned;
		}

		var variantKey = expectedVariant;
		if (!serverDef.Variants.TryGetValue(variantKey, out var template))
		{
			// Fall back to stable
			template = GetStableVariant(serverDef, expectedVariant);
		}

		return CloneJsonObject(template);
	}

	/// <summary>
	/// Returns the tool's assembly version (without commit hash).
	/// Delegates to <see cref="McpStdioServer.GetAssemblyVersion"/>.
	/// </summary>
	public static string GetToolVersion() => McpStdioServer.GetAssemblyVersion();

	/// <summary>
	/// Determines whether a version string is prerelease (contains <c>-</c>).
	/// </summary>
	public static bool IsPrerelease(string version) => version.Contains('-');

	private static JsonObject GetStableVariant(ServerDefinition serverDef, string expectedVariant)
	{
		if (serverDef.Variants.TryGetValue("stable", out var stableTemplate))
		{
			return stableTemplate;
		}

		throw new InvalidOperationException(
			$"Server definition does not contain variant '{expectedVariant}' or fallback variant 'stable'.");
	}

	private static JsonObject CloneJsonObject(JsonObject source)
	{
		var json = source.ToJsonString();
		return JsonNode.Parse(json)!.AsObject();
	}

	private static void ReplaceVersionPlaceholder(JsonObject obj, string version)
	{
		if (obj["args"] is JsonArray args)
		{
			for (int i = 0; i < args.Count; i++)
			{
				var val = args[i]?.GetValue<string>();
				if (val == "{version}")
				{
					args[i] = version;
				}
			}
		}
	}
}
