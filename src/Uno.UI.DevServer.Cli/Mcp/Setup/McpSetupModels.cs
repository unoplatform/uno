using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

internal static class McpSetupProtocol
{
	public const string Version = "1.0";
}

// ──────────────────────────────────────────────
// Definition records (deserialized from embedded JSON)
// ──────────────────────────────────────────────

internal sealed record IdeProfile(
	string[] ConfigPaths,
	string WriteTarget,
	string JsonRootKey,
	bool IncludeType = false,
	string? UrlKey = null,
	IReadOnlyDictionary<string, string>? TypeMap = null,
	bool MergeCommandArgs = false,
	string Strategy = "file",
	string? ManualRegistrationMessage = null,
	bool ExcludeFromDetection = false,
	CliProfile? Cli = null,
	string[]? SupportedTransports = null,
	string[]? ExtraArgs = null);

/// <summary>
/// Defines how to drive a native CLI for MCP registration.
/// When present and the executable is found in PATH, install/uninstall
/// delegate to the agent's own CLI instead of writing config files directly.
/// Status always scans on-disk config as the source of truth.
/// </summary>
internal sealed record CliProfile(
	string Executable,
	string[] Detect,
	string[]? AddStdio,
	string[]? AddHttp,
	string[]? List,
	string[]? Remove);

internal sealed record ServerDefinition(
	string Transport,
	Dictionary<string, JsonObject> Variants,
	DetectionPatterns Detection);

internal sealed record DetectionPatterns(
	string[] KeyPatterns,
	string[]? CommandPatterns,
	string[]? UrlPatterns);

internal sealed record Definitions(
	IReadOnlyDictionary<string, IdeProfile> Ides,
	IReadOnlyDictionary<string, ServerDefinition> Servers);

// ──────────────────────────────────────────────
// Status response
// ──────────────────────────────────────────────

internal sealed record StatusResponse(
	string Version,
	string? CallerIde,
	string ToolVersion,
	string ExpectedVariant,
	IReadOnlyList<string> DetectedIdes,
	IReadOnlyList<SupportedIdeEntry> SupportedIdes,
	IReadOnlyList<ServerStatusEntry> Servers);

internal sealed record SupportedIdeEntry(
	string Ide,
	string Strategy,
	bool Detected);

internal sealed record ServerStatusEntry(
	string Name,
	string Transport,
	JsonObject Definition,
	IReadOnlyList<ServerIdeStatus> Ides);

internal sealed record ServerIdeStatus(
	string Ide,
	string Status, // "registered", "missing", "outdated"
	IReadOnlyList<LocationEntry>? Locations,
	IReadOnlyList<string>? Warnings);

internal sealed record LocationEntry(string Path, string Variant, string Transport);

// ──────────────────────────────────────────────
// Install / Uninstall response
// ──────────────────────────────────────────────

internal sealed record OperationResponse(
	string Version,
	IReadOnlyList<OperationEntry> Operations);

internal sealed record OperationEntry(
	string Server,
	string Ide,
	string Action, // "created", "updated", "skipped", "removed", "not_found", "error"
	string? Path,
	string? Reason,
	string? Note = null);

// ──────────────────────────────────────────────
// Scan result (internal, used by ConfigScanner)
// ──────────────────────────────────────────────

internal sealed record ScanResult(
	IReadOnlyList<string> ResolvedConfigPaths,
	string ResolvedWriteTarget,
	bool Detected,
	IReadOnlyDictionary<string, ServerScanResult> ServerResults);

internal sealed record ServerScanResult(
	string Status, // "registered", "missing", "outdated"
	IReadOnlyList<LocationEntry> Locations,
	IReadOnlyList<string> Warnings,
	JsonObject? EffectiveEntry);

// ──────────────────────────────────────────────
// JSON output options
// ──────────────────────────────────────────────

internal static class McpSetupJson
{
	public static JsonSerializerOptions OutputOptions { get; } = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};
}
