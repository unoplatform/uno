using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Micro;

/// <summary>
/// Exercises the specialized DP fast-path that the source generator emits for built-in DPs
/// such as <see cref="Control.BackgroundProperty"/>. Catches regressions in the
/// generator's specialized accessor codegen.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Micro)]
[MemoryDiagnoser]
public class SpecializedDPBenchmark
{
	private TextBlock _sut = null!;

	[GlobalSetup]
	public void Setup() => _sut = new TextBlock();

	[Benchmark]
	public string Get_Set()
	{
		_sut.Text = "value";
		return _sut.Text;
	}
}
