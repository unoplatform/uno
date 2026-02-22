using System.Collections.Generic;
using System.Text.Json.Serialization;

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
	public required IReadOnlyList<ValidationIssue> Issues { get; init; }
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
}
