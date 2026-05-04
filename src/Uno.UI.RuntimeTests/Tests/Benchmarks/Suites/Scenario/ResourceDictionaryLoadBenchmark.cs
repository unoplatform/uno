#nullable enable

using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Scenario;

/// <summary>
/// Allocates an instance of <see cref="XamlControlsResources"/>. Lazy theme-resource init
/// regression canary: a regression here typically signals that lazy resource paths have
/// reverted to eager evaluation.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Scenario)]
[MemoryDiagnoser]
public class ResourceDictionaryLoadBenchmark
{
	[Benchmark]
	public XamlControlsResources Load() => new();
}
