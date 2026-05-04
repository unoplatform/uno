using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Micro;

/// <summary>
/// Sets an inherited DP on a parent and forces propagation through a 3-deep visual tree.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Micro)]
[MemoryDiagnoser]
public class PropagationDPBenchmark
{
	private Border _root = null!;

	[GlobalSetup]
	public void Setup()
	{
		var leaf = new TextBlock { Text = "leaf" };
		var middle = new Border { Child = leaf };
		_root = new Border { Child = middle };
	}

	[Benchmark]
	public void Set_Foreground()
	{
		_root.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.Red));
		_root.ClearValue(TextBlock.ForegroundProperty);
	}
}
