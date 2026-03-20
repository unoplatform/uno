using System.Text.Json.Nodes;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Resolves the expected variant and concrete <see cref="JsonObject"/> definition
/// for a given server based on the running tool version and CLI flags.
/// </summary>
internal static class ServerDefinitionResolver
{
	public static string ResolveExpectedVariant(
		string toolVersion,
		string? channel,
		string? toolVersionOverride)
	{
		if (toolVersionOverride is not null)
		{
			return $"pinned:{toolVersionOverride}";
		}

		if (string.Equals(channel, "prerelease", StringComparison.OrdinalIgnoreCase))
		{
			return "prerelease";
		}

		if (string.Equals(channel, "stable", StringComparison.OrdinalIgnoreCase))
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
	/// Delegates to <see cref="AssemblyVersionHelper.GetAssemblyVersion"/>.
	/// </summary>
	public static string GetToolVersion() => AssemblyVersionHelper.GetAssemblyVersion(typeof(ServerDefinitionResolver).Assembly);

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

	private static JsonObject CloneJsonObject(JsonObject source) =>
		source.DeepClone().AsObject();

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
