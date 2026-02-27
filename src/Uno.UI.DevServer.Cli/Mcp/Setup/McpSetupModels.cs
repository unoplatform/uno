using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

// ──────────────────────────────────────────────
// Definition records (deserialized from embedded JSON)
// ──────────────────────────────────────────────

internal sealed record IdeProfile(
	string[] ConfigPaths,
	string WriteTarget,
	string JsonRootKey);

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
	IReadOnlyList<ServerStatusEntry> Servers);

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

internal sealed record LocationEntry(string Path, string Variant);

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
	string? Reason);

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
	IReadOnlyList<string> Warnings);

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
