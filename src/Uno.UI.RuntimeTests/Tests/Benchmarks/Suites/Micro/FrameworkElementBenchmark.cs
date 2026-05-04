using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Micro;

/// <summary>
/// Measures the cost of allocating a <see cref="Border"/>, attaching it to a parent, and detaching
/// it. Surfaces GC-allocation regressions through <see cref="MemoryDiagnoserAttribute"/>.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Micro)]
[MemoryDiagnoser]
public class FrameworkElementBenchmark
{
	private StackPanel _parent = null!;

	[GlobalSetup]
	public void Setup() => _parent = new StackPanel();

	[Benchmark]
	public void Create_AttachDetach()
	{
		var child = new Border();
		_parent.Children.Add(child);
		_parent.Children.Remove(child);
	}
}
