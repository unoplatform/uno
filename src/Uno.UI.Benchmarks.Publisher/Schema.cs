using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Uno.UI.Benchmarks.Publisher;

/// <summary>One v1 schema row. Mirrors <c>Uno.UI.RuntimeTests.Tests.Benchmarks.ResultRow</c>.</summary>
public sealed class ResultRow
{
	[JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = 1;
	[JsonPropertyName("runId")] public string? RunId { get; set; }
	[JsonPropertyName("commit")] public string? Commit { get; set; }
	[JsonPropertyName("branch")] public string? Branch { get; set; }
	[JsonPropertyName("timestamp")] public string? Timestamp { get; set; }
	[JsonPropertyName("platform")] public string? Platform { get; set; }
	[JsonPropertyName("category")] public string? Category { get; set; }
	[JsonPropertyName("benchmark")] public string? Benchmark { get; set; }
	[JsonPropertyName("params")] public Dictionary<string, object?>? Params { get; set; }
	[JsonPropertyName("stats")] public StatsBlock? Stats { get; set; }
	[JsonPropertyName("memory")] public MemoryBlock? Memory { get; set; }
	[JsonPropertyName("host")] public HostBlock? Host { get; set; }
	[JsonPropertyName("error")] public string? Error { get; set; }
	[JsonPropertyName("extra")] public Dictionary<string, object?>? Extra { get; set; }
}

public sealed class StatsBlock
{
	[JsonPropertyName("meanNs")] public double MeanNs { get; set; }
	[JsonPropertyName("stddevNs")] public double StddevNs { get; set; }
	[JsonPropertyName("minNs")] public double MinNs { get; set; }
	[JsonPropertyName("maxNs")] public double MaxNs { get; set; }
	[JsonPropertyName("p50Ns")] public double P50Ns { get; set; }
	[JsonPropertyName("p95Ns")] public double P95Ns { get; set; }
	[JsonPropertyName("n")] public int N { get; set; }
}

public sealed class MemoryBlock
{
	[JsonPropertyName("allocatedBytesPerOp")] public long AllocatedBytesPerOp { get; set; }
	[JsonPropertyName("gen0Collections")] public int Gen0Collections { get; set; }
	[JsonPropertyName("gen1Collections")] public int Gen1Collections { get; set; }
	[JsonPropertyName("gen2Collections")] public int Gen2Collections { get; set; }
}

public sealed class HostBlock
{
	[JsonPropertyName("os")] public string? Os { get; set; }
	[JsonPropertyName("arch")] public string? Arch { get; set; }
	[JsonPropertyName("cpu")] public string? Cpu { get; set; }
	[JsonPropertyName("logicalCores")] public int LogicalCores { get; set; }
	[JsonPropertyName("dotnetRuntime")] public string? DotnetRuntime { get; set; }
	[JsonPropertyName("bdnVersion")] public string? BdnVersion { get; set; }
	[JsonPropertyName("agentName")] public string? AgentName { get; set; }
}
