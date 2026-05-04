using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks;

/// <summary>
/// Auto-discovers every <c>[Benchmark]</c>-annotated class under
/// <c>Uno.UI.RuntimeTests.Tests.Benchmarks.Suites</c> and produces one MSTest row per type.
/// Filtered out of normal runtime-tests runs via <c>--filter TestCategory!=Benchmark</c>.
/// </summary>
[TestClass]
public class BenchmarkRunner_Fixture
{
	public static IEnumerable<object[]> AllBenchmarks =>
		BenchmarkRunnerHost.DiscoverBenchmarkTypes()
			.Where(t => t.Namespace?.StartsWith("Uno.UI.RuntimeTests.Tests.Benchmarks.Suites", StringComparison.Ordinal) == true)
			.Select(t => new object[] { t.AssemblyQualifiedName! });

	public static string GetBenchmarkDisplayName(System.Reflection.MethodInfo _, object[] data)
		=> Type.GetType((string)data[0])?.Name ?? "(unknown)";

	[TestMethod]
	[TestCategory("Benchmark")]
	[DynamicData(nameof(AllBenchmarks), DynamicDataDisplayName = nameof(GetBenchmarkDisplayName))]
	public async Task Run(string benchmarkTypeName)
	{
		var type = Type.GetType(benchmarkTypeName)
			?? throw new InvalidOperationException($"Benchmark type {benchmarkTypeName} could not be loaded");

		var result = await BenchmarkRunnerHost.RunOneAsync(type);

		// Always-green collector: the test passes if BDN produced any rows, including error rows.
		// A genuine harness crash propagates as an exception above and fails the test.
		Assert.IsTrue(result.Rows.Count > 0, "BenchmarkRunnerHost produced no result rows");
	}
}
