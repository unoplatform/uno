using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Micro;

[BenchmarkCategory(BenchmarkCategory.Micro)]
[MemoryDiagnoser]
public class BorderBenchmark
{
	private Border _sut = null!;
	private SolidColorBrush _brush = null!;

	[GlobalSetup]
	public void Setup()
	{
		_sut = new Border();
		_brush = new SolidColorBrush();
	}

	[Benchmark]
	public void Toggle_BorderBrush()
	{
		_sut.BorderBrush = _brush;
		_sut.BorderBrush = null;
	}

	[Benchmark]
	public void Toggle_Style()
	{
		_sut.Style = new Style();
		_sut.ClearValue(Border.StyleProperty);
	}
}
