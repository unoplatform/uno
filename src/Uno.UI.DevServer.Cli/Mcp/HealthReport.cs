using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <seealso href="../health-diagnostics.md"/>
internal sealed record HealthReport
{
	public required HealthStatus Status { get; init; }
	public string? DevServerVersion { get; init; }
	public int? HostProcessId { get; init; }
	public string? HostEndpoint { get; init; }
	public bool UpstreamConnected { get; init; }
	public int ToolCount { get; init; }
	public string? UnoSdkVersion { get; init; }
	public long DiscoveryDurationMs { get; init; }
	public ConnectionState? ConnectionState { get; init; }
	public IReadOnlyList<string>? DiscoveredSolutions { get; init; }
	public string? EffectiveWorkspaceDirectory { get; init; }
	public string? SelectedSolutionPath { get; init; }
	public WorkspaceResolutionKind? ResolutionKind { get; init; }
	public WorkspaceSelectionSource? SelectionSource { get; init; }
	public IReadOnlyList<string>? CandidateSolutions { get; init; }
	public required IReadOnlyList<ValidationIssue> Issues { get; init; }
	public DiscoverySummary? Discovery { get; init; }
}

internal sealed record WorkspaceSelectionSnapshot
{
	public string? EffectiveWorkspaceDirectory { get; init; }
	public string? SelectedSolutionPath { get; init; }
	public WorkspaceResolutionKind? ResolutionKind { get; init; }
	public WorkspaceSelectionSource? SelectionSource { get; init; }
	public IReadOnlyList<string>? CandidateSolutions { get; init; }
}

internal sealed record DiscoverySummary
{
	public string? RequestedWorkingDirectory { get; init; }
	public string? WorkingDirectory { get; init; }
	public string? EffectiveWorkspaceDirectory { get; init; }
	public string? SelectedSolutionPath { get; init; }
	public string? SelectedGlobalJsonPath { get; init; }
	public WorkspaceResolutionKind? ResolutionKind { get; init; }
	public WorkspaceSelectionSource? SelectionSource { get; init; }
	public IReadOnlyList<string>? CandidateSolutions { get; init; }
	public string? DotNetVersion { get; init; }
	public string? UnoSdkVersion { get; init; }
	public string? UnoSdkPath { get; init; }
	public string? HostPath { get; init; }
	public string? SettingsPath { get; init; }
	public IReadOnlyList<AddInSummary>? AddIns { get; init; }
	public IReadOnlyList<ActiveServerSummary>? ActiveServers { get; init; }
}

internal sealed record ActiveServerSummary
{
	public int ProcessId { get; init; }
	public int Port { get; init; }
	public string McpEndpoint { get; init; } = "";
	public int ParentProcessId { get; init; }
	public DateTime StartTime { get; init; }
	public string? IdeChannelId { get; init; }
	public string? SolutionPath { get; init; }
	public bool IsInWorkspace { get; init; }
	public IReadOnlyList<ProcessChainEntry>? ProcessChain { get; init; }
}

internal sealed record AddInSummary
{
	public required string PackageName { get; init; }
	public required string PackageVersion { get; init; }
	public required string EntryPointDll { get; init; }
	public required string DiscoverySource { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum HealthStatus
{
	Healthy,
	Degraded,
	Unhealthy,
}

internal sealed record ValidationIssue
{
	public required IssueCode Code { get; init; }
	public required ValidationSeverity Severity { get; init; }
	public required string Message { get; init; }
	public string? Remediation { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ValidationSeverity
{
	Fatal,
	Warning,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum IssueCode
{
	GlobalJsonNotFound,
	UnoSdkNotInGlobalJson,
	SdkNotInCache,
	PackagesJsonNotFound,
	DevServerPackageNotCached,
	HostBinaryNotFound,
	HostNotStarted,
	HostCrashed,
	HostUnreachable,
	DotNetNotFound,
	DotNetVersionUnsupported,
	AddInPackageNotCached,
	AddInBinaryNotFound,
	AddInLoadFailed,
	AddInDiscoveryFallback,
	UpstreamError,
	NoSolutionFound,
	WorkspaceAmbiguous,
	WorkspaceNotResolved,
	HostMcpEndpointNotAvailable,
}
