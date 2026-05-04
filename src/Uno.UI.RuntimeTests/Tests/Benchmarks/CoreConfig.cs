using System;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks;

internal static class CoreConfig
{
	public static IConfig For(BenchmarkCategory category, ILogger logger, string artifactsPath) =>
		ManualConfig.CreateEmpty()
			.AddLogger(logger)
			.AddJob(BuildJob(category))
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddExporter(JsonExporter.Full)
			.AddExporter(CsvExporter.Default)
			.AddExporter(MarkdownExporter.GitHub)
			.WithArtifactsPath(artifactsPath)
			.WithOption(ConfigOptions.DisableLogFile, true);

	private static Job BuildJob(BenchmarkCategory category)
	{
		// InProcessNoEmit works on every Skia target including iOS/Android NativeAOT/Wasm AOT
		// because it does not require Reflection.Emit.
		var job = Job.Default
			.WithToolchain(InProcessNoEmitToolchain.DontLogOutput)
			.WithLaunchCount(1);

		return category switch
		{
			BenchmarkCategory.Micro => job
				.WithWarmupCount(3)
				.WithIterationCount(10)
				.WithIterationTime(TimeInterval.FromMilliseconds(250))
				.WithId("Micro"),
			BenchmarkCategory.Scenario => job
				.WithWarmupCount(2)
				.WithIterationCount(7)
				.WithId("Scenario"),
			BenchmarkCategory.Render => job
				.WithWarmupCount(2)
				.WithIterationCount(5)
				.WithId("Render"),
			_ => throw new ArgumentOutOfRangeException(nameof(category)),
		};
	}

	public static TimeSpan PerBenchmarkTimeout(BenchmarkCategory category) => category switch
	{
		BenchmarkCategory.Micro => TimeSpan.FromSeconds(90),
		BenchmarkCategory.Scenario => TimeSpan.FromSeconds(120),
		BenchmarkCategory.Render => TimeSpan.FromSeconds(180),
		_ => TimeSpan.FromSeconds(120),
	};

	public static readonly TimeSpan SuiteCap = TimeSpan.FromMinutes(25);
}
