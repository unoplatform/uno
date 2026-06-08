using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Uno.UI.RemoteControl.Host.Mcp;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum HostHealthStatus
{
	Healthy,
	Degraded,
	Unhealthy,
}

internal sealed record HostHealthReport
{
	public required HostHealthStatus Status { get; init; }
	public string? HostVersion { get; init; }
	public IReadOnlyList<HostAddInEntry> AddIns { get; init; } = [];
	public bool IdeChannelConnected { get; init; }
}

internal sealed record HostAddInEntry
{
	public required string Name { get; init; }
	public string? Version { get; init; }
	public required string AssemblyPath { get; init; }
	public bool Loaded { get; init; }
	public string? Error { get; init; }
}
