#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks;

/// <summary>
/// In-process runner used by both the <c>Uno.UI.RuntimeTests</c> MSTest fixture and the
/// SamplesApp <c>BenchmarksPage</c>. Handles per-category BDN config, per-benchmark soft
/// timeouts, suite-level cap, normalized v1-schema JSONL emission, and result zip packaging.
/// </summary>
public static class BenchmarkRunnerHost
{
	private const int SchemaVersion = 1;

	/// <summary>
	/// Runs every <c>[Benchmark]</c>-annotated type discovered in the executing assembly.
	/// </summary>
	/// <param name="filter">Substring matched case-insensitively against the type's full name.</param>
	/// <param name="logger">Optional BDN logger to capture run output.</param>
	/// <param name="cancellationToken">Optional cancellation token.</param>
	public static async Task<RunResult> RunAllAsync(
		string? filter = null,
		ILogger? logger = null,
		CancellationToken cancellationToken = default)
	{
		var benchmarkTypes = DiscoverBenchmarkTypes()
			.Where(t => string.IsNullOrEmpty(filter)
				|| t.FullName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
				|| t.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
			.ToArray();

		var ctx = new RunContext(logger ?? NullLogger.Instance);
		var suiteSw = Stopwatch.StartNew();

		foreach (var type in benchmarkTypes)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				ctx.Logger.WriteLineInfo($"Suite cancelled before {type.Name}");
				break;
			}

			if (suiteSw.Elapsed > CoreConfig.SuiteCap)
			{
				ctx.Logger.WriteLineInfo($"Suite cap of {CoreConfig.SuiteCap} reached, skipping {type.Name}");
				ctx.Rows.Add(BuildErrorRow(type, "skipped: suite cap"));
				continue;
			}

			await RunOneCoreAsync(type, ctx, cancellationToken).ConfigureAwait(false);
		}

		return PackageResults(ctx);
	}

	/// <summary>
	/// Runs a single benchmark type. Used by the MSTest fixture (one MSTest row per benchmark class).
	/// </summary>
	public static async Task<RunResult> RunOneAsync(
		Type benchmarkType,
		ILogger? logger = null,
		CancellationToken cancellationToken = default)
	{
		var ctx = new RunContext(logger ?? NullLogger.Instance);
		await RunOneCoreAsync(benchmarkType, ctx, cancellationToken).ConfigureAwait(false);
		return PackageResults(ctx);
	}

	/// <summary>
	/// Discovers every type in the calling assembly that has at least one <c>[Benchmark]</c> method
	/// and is non-generic + has a parameterless constructor.
	/// </summary>
	public static IEnumerable<Type> DiscoverBenchmarkTypes(Assembly? assembly = null)
	{
		assembly ??= typeof(BenchmarkRunnerHost).Assembly;
		return assembly.GetTypes()
			.Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
			.Where(t => t.GetConstructor(Type.EmptyTypes) != null)
			.Where(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Any(m => m.IsDefined(typeof(BenchmarkAttribute), false)))
			.OrderBy(t => t.FullName, StringComparer.Ordinal);
	}

	private static async Task RunOneCoreAsync(Type type, RunContext ctx, CancellationToken cancellationToken)
	{
		var category = type.GetCustomAttribute<BenchmarkCategoryAttribute>()?.Category
			?? BenchmarkCategory.Micro;
		var timeout = CoreConfig.PerBenchmarkTimeout(category);

		ctx.Logger.WriteLineHeader($"--- Running {type.FullName} (category={category}, timeout={timeout}) ---");

		var artifactsDir = Path.Combine(ctx.ArtifactsRoot, type.Name);
		Directory.CreateDirectory(artifactsDir);

		var config = CoreConfig.For(category, ctx.Logger, artifactsDir);
		var sw = Stopwatch.StartNew();

		Summary? summary = null;
		string? errorMessage = null;

		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(timeout);

			var runTask = Task.Run(() => BenchmarkRunner.Run(type, config), cts.Token);
			var winner = await Task.WhenAny(runTask, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
			if (winner == runTask)
			{
				summary = await runTask.ConfigureAwait(false);
			}
			else
			{
				errorMessage = $"timeout after {timeout.TotalSeconds:F0}s";
				ctx.Logger.WriteLineError(errorMessage);
				// We deliberately do not Abort() the benchmark task. BDN's worker thread
				// completes naturally on most platforms. Worst case the suite cap will catch a
				// fully-stuck thread.
			}
		}
		catch (Exception ex)
		{
			errorMessage = $"{ex.GetType().Name}: {ex.Message}";
			ctx.Logger.WriteLineError(ex.ToString());
		}

		sw.Stop();

		if (summary != null && summary.Reports.Length > 0)
		{
			foreach (var report in summary.Reports)
			{
				ctx.Rows.Add(BuildSuccessRow(type, category, report));
			}
		}
		else
		{
			ctx.Rows.Add(BuildErrorRow(type, errorMessage ?? "no reports produced", category, sw.Elapsed));
		}
	}

	private static ResultRow BuildSuccessRow(Type type, BenchmarkCategory category, BenchmarkReport report)
	{
		var stats = report.ResultStatistics;
		var name = $"{type.Name}.{report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo}";
		var allocBytes = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);
		return new ResultRow
		{
			SchemaVersion = SchemaVersion,
			Category = category.ToString().ToLowerInvariant(),
			Benchmark = name,
			Params = ExtractParams(report.BenchmarkCase),
			Stats = new StatsBlock
			{
				MeanNs = stats?.Mean ?? 0,
				StddevNs = stats?.StandardDeviation ?? 0,
				MinNs = stats?.Min ?? 0,
				MaxNs = stats?.Max ?? 0,
				P50Ns = stats?.Percentiles?.P50 ?? 0,
				P95Ns = stats?.Percentiles?.P95 ?? 0,
				N = stats?.N ?? 0,
			},
			Memory = new MemoryBlock
			{
				AllocatedBytesPerOp = allocBytes ?? 0,
				Gen0Collections = report.GcStats.Gen0Collections,
				Gen1Collections = report.GcStats.Gen1Collections,
				Gen2Collections = report.GcStats.Gen2Collections,
			},
		};
	}

	private static ResultRow BuildErrorRow(Type type, string error, BenchmarkCategory? category = null, TimeSpan? elapsed = null)
		=> new()
		{
			SchemaVersion = SchemaVersion,
			Category = (category ?? BenchmarkCategory.Micro).ToString().ToLowerInvariant(),
			Benchmark = type.Name,
			Error = error,
			Stats = null,
			Memory = null,
		};

	private static Dictionary<string, object?>? ExtractParams(BenchmarkCase benchmarkCase)
	{
		var pairs = benchmarkCase.Parameters.Items;
		if (pairs.Count == 0)
		{
			return null;
		}
		var dict = new Dictionary<string, object?>(pairs.Count);
		foreach (var p in pairs)
		{
			dict[p.Name] = p.Value;
		}
		return dict;
	}

	private static RunResult PackageResults(RunContext ctx)
	{
		var jsonlPath = Path.Combine(ctx.ArtifactsRoot, "results.jsonl");
		using (var writer = new StreamWriter(jsonlPath, append: false, Encoding.UTF8))
		{
			var host = HostInfo();
			foreach (var row in ctx.Rows)
			{
				row.Host = host;
				row.Timestamp = DateTime.UtcNow.ToString("O");
				writer.WriteLine(JsonSerializer.Serialize(row, JsonOpts));
			}
		}

		var meta = new
		{
			schemaVersion = SchemaVersion,
			generatedAt = DateTime.UtcNow.ToString("O"),
			host = HostInfo(),
		};
		File.WriteAllText(
			Path.Combine(ctx.ArtifactsRoot, "meta.json"),
			JsonSerializer.Serialize(meta, JsonOpts));

		var zipPath = Path.Combine(Path.GetTempPath(), $"benchmarks-{Guid.NewGuid():N}.zip");
		if (File.Exists(zipPath))
		{
			File.Delete(zipPath);
		}
		ZipFile.CreateFromDirectory(ctx.ArtifactsRoot, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

		return new RunResult(ctx.Rows, jsonlPath, zipPath, ctx.ArtifactsRoot);
	}

	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
	};

	private static HostBlock HostInfo() => new()
	{
		Os = RuntimeInformation.OSDescription,
		Arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
		Cpu = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
		LogicalCores = Environment.ProcessorCount,
		DotnetRuntime = RuntimeInformation.FrameworkDescription,
		BdnVersion = typeof(BenchmarkRunner).Assembly.GetName().Version?.ToString() ?? "unknown",
		AgentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "local",
	};

	private sealed class RunContext
	{
		public RunContext(ILogger logger)
		{
			Logger = logger;
			ArtifactsRoot = Path.Combine(Path.GetTempPath(), $"uno-bench-{Guid.NewGuid():N}");
			Directory.CreateDirectory(ArtifactsRoot);
		}

		public ILogger Logger { get; }
		public string ArtifactsRoot { get; }
		public List<ResultRow> Rows { get; } = new();
	}
}

/// <summary>One serialized JSONL row, matching the v1 schema in doc/benchmarks.</summary>
public sealed class ResultRow
{
	public int SchemaVersion { get; set; }
	public string? RunId { get; set; }
	public string? Commit { get; set; }
	public string? Branch { get; set; }
	public string? Timestamp { get; set; }
	public string? Platform { get; set; }
	public string? Category { get; set; }
	public string? Benchmark { get; set; }
	public Dictionary<string, object?>? Params { get; set; }
	public StatsBlock? Stats { get; set; }
	public MemoryBlock? Memory { get; set; }
	public HostBlock? Host { get; set; }
	public string? Error { get; set; }
	public Dictionary<string, object?>? Extra { get; set; }
}

public sealed class StatsBlock
{
	public double MeanNs { get; set; }
	public double StddevNs { get; set; }
	public double MinNs { get; set; }
	public double MaxNs { get; set; }
	public double P50Ns { get; set; }
	public double P95Ns { get; set; }
	public int N { get; set; }
}

public sealed class MemoryBlock
{
	public long AllocatedBytesPerOp { get; set; }
	public int Gen0Collections { get; set; }
	public int Gen1Collections { get; set; }
	public int Gen2Collections { get; set; }
}

public sealed class HostBlock
{
	public string? Os { get; set; }
	public string? Arch { get; set; }
	public string? Cpu { get; set; }
	public int LogicalCores { get; set; }
	public string? DotnetRuntime { get; set; }
	public string? BdnVersion { get; set; }
	public string? AgentName { get; set; }
}

public readonly record struct RunResult(IReadOnlyList<ResultRow> Rows, string JsonlPath, string ZipPath, string ArtifactsRoot);
