using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Uno.UI.DevServer.Cli.Mcp;

internal static class ToolCacheFile
{
	private const int CacheVersion = 1;
	private const int MaxCachedTools = 128;
	private const int MinToolNameLength = 3;
	private const int MaxToolNameLength = 64;

	public static bool TryRead(
		string json,
		string cachePath,
		ILogger logger,
		out Tool[] tools,
		string? expectedWorkspaceHash = null,
		string? expectedUnoSdkVersion = null)
	{
		tools = [];
		var trimmed = json.AsSpan().TrimStart();

		try
		{
			if (trimmed.Length == 0)
			{
				return false;
			}

			if (trimmed[0] == '[')
			{
				var legacy = JsonSerializer.Deserialize<Tool[]>(json, McpJsonUtilities.DefaultOptions) ?? [];
				if (TryValidateCachedTools(legacy, out _))
				{
					tools = legacy;
					return true;
				}

				return false;
			}

			var entry = JsonSerializer.Deserialize<ToolCacheEntry>(json, McpJsonUtilities.DefaultOptions);
			if (entry?.Tools is null)
			{
				return false;
			}

			var normalized = JsonSerializer.Serialize(entry.Tools, McpJsonUtilities.DefaultOptions);
			var expectedChecksum = ComputeChecksum(normalized);
			if (!string.Equals(expectedChecksum, entry.Checksum, StringComparison.OrdinalIgnoreCase))
			{
				logger.LogWarning(
					"Tool cache checksum mismatch for {Path}. Expected {Expected}, found {Actual}",
					cachePath,
					expectedChecksum,
					entry.Checksum);
				return false;
			}

			if (entry.Version != CacheVersion)
			{
				logger.LogDebug(
					"Tool cache version mismatch for {Path} (found {Found}, expected {Expected}). Cache will be re-written when refreshed.",
					cachePath,
					entry.Version,
					CacheVersion);
			}

			// Validate metadata if both expected and stored values are present
			if (expectedWorkspaceHash is not null
				&& entry.WorkspaceHash is not null
				&& !string.Equals(expectedWorkspaceHash, entry.WorkspaceHash, StringComparison.OrdinalIgnoreCase))
			{
				logger.LogDebug(
					"Tool cache workspace hash mismatch for {Path} (found {Found}, expected {Expected})",
					cachePath,
					entry.WorkspaceHash,
					expectedWorkspaceHash);
				return false;
			}

			if (expectedUnoSdkVersion is not null
				&& entry.UnoSdkVersion is not null
				&& !string.Equals(expectedUnoSdkVersion, entry.UnoSdkVersion, StringComparison.Ordinal))
			{
				logger.LogDebug(
					"Tool cache SDK version mismatch for {Path} (found {Found}, expected {Expected})",
					cachePath,
					entry.UnoSdkVersion,
					expectedUnoSdkVersion);
				return false;
			}

			if (!TryValidateCachedTools(entry.Tools, out var reason))
			{
				logger.LogWarning(
					"Tool cache validation failed for {Path}: {Reason}",
					cachePath,
					reason ?? "Unknown reason");
				return false;
			}

			tools = entry.Tools;
			return true;
		}
		catch (JsonException ex)
		{
			logger.LogWarning(ex, "Malformed tool cache file {Path}", cachePath);
			return false;
		}
	}

	public static bool TryValidateCachedTools(Tool[] tools, out string? reason)
	{
		if (tools.Length > MaxCachedTools)
		{
			reason = $"Tool cache contains too many entries ({tools.Length} > {MaxCachedTools})";
			return false;
		}

		for (var i = 0; i < tools.Length; i++)
		{
			var tool = tools[i];
			if (tool is null)
			{
				reason = $"Tool cache entry at index {i} is null";
				return false;
			}

			if (!IsValidToolName(tool.Name))
			{
				reason = $"Tool cache entry '{tool.Name}' has an invalid name";
				return false;
			}
		}

		reason = null;
		return true;
	}

	public static ToolCacheEntry CreateEntry(Tool[] tools, string? workspaceHash = null, string? unoSdkVersion = null)
	{
		var cloned = tools.ToArray();
		var normalizedJson = JsonSerializer.Serialize(cloned, McpJsonUtilities.DefaultOptions);
		var checksum = ComputeChecksum(normalizedJson);

		return new ToolCacheEntry
		{
			Version = CacheVersion,
			Tools = cloned,
			Checksum = checksum,
			WorkspaceHash = workspaceHash,
			UnoSdkVersion = unoSdkVersion,
			Timestamp = DateTimeOffset.UtcNow,
		};
	}

	internal static string ComputeWorkspaceHash(string? solutionDirectory)
	{
		if (string.IsNullOrWhiteSpace(solutionDirectory))
		{
			return string.Empty;
		}

		var normalized = solutionDirectory.Replace('\\', '/').ToLowerInvariant();
		var bytes = Encoding.UTF8.GetBytes(normalized);
		var hash = SHA256.HashData(bytes);
		return Convert.ToHexString(hash)[..16];
	}

	private static string ComputeChecksum(string payload)
	{
		var bytes = Encoding.UTF8.GetBytes(payload);
		var hash = SHA256.HashData(bytes);
		return Convert.ToHexString(hash);
	}

	private static bool IsValidToolName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}

		if (name.Length < MinToolNameLength || name.Length > MaxToolNameLength)
		{
			return false;
		}

		foreach (var c in name)
		{
			if (!(char.IsLetterOrDigit(c) || c is '_' or '-' or '.'))
			{
				return false;
			}
		}

		return true;
	}

	internal sealed class ToolCacheEntry
	{
		public int Version { get; set; }
		public Tool[] Tools { get; set; } = [];
		public string Checksum { get; set; } = string.Empty;
		public string? WorkspaceHash { get; set; }
		public string? UnoSdkVersion { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
	}
}
