using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Scenario;

/// <summary>
/// Measure + Arrange of a 5x5 grid of Borders. Layout fast path on the dispatcher thread.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Scenario)]
[MemoryDiagnoser]
public class GridMeasure5x5Benchmark
{
	private Grid _sut = null!;

	[GlobalSetup]
	public Task Setup() => TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
	{
		_sut = new Grid();
		for (int r = 0; r < 5; r++)
		{
			_sut.RowDefinitions.Add(new RowDefinition());
		}
		for (int c = 0; c < 5; c++)
		{
			_sut.ColumnDefinitions.Add(new ColumnDefinition());
		}
		for (int r = 0; r < 5; r++)
		{
			for (int c = 0; c < 5; c++)
			{
				var b = new Border();
				Grid.SetRow(b, r);
				Grid.SetColumn(b, c);
				_sut.Children.Add(b);
			}
		}
	});

	[Benchmark]
	public Task Measure_Arrange() => TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
	{
		_sut.InvalidateMeasure();
		_sut.Measure(new Size(500, 500));
		_sut.Arrange(new Rect(0, 0, 500, 500));
	});
}
