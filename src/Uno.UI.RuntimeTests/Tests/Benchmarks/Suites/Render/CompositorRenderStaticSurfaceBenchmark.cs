using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Render;

/// <summary>
/// Renders 60 frames of a fixed visual tree (~100 elements) and measures total compositor
/// time. Skia rendering hot path. Numbers are platform-trend only — vsync semantics differ
/// across Skia targets so absolute values are not comparable platform-to-platform.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Render)]
[MemoryDiagnoser]
public class CompositorRenderStaticSurfaceBenchmark
{
	[GlobalSetup]
	public Task Setup() => RenderBenchmarkHost.SetupSceneAsync(BuildScene);

	[Benchmark]
	public async Task Render60Frames()
	{
		for (int i = 0; i < 60; i++)
		{
			await RenderBenchmarkHost.RenderFrameAsync();
		}
	}

	[GlobalCleanup]
	public Task Cleanup() => RenderBenchmarkHost.ResetSceneAsync();

	private static UIElement BuildScene()
	{
		var grid = new Grid { Width = 800, Height = 600 };
		for (int r = 0; r < 10; r++)
		{
			grid.RowDefinitions.Add(new RowDefinition());
		}
		for (int c = 0; c < 10; c++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
		}
		for (int r = 0; r < 10; r++)
		{
			for (int c = 0; c < 10; c++)
			{
				var border = new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(255, (byte)(r * 25), (byte)(c * 25), 128)),
					BorderBrush = new SolidColorBrush(Colors.Black),
					BorderThickness = new Thickness(1),
				};
				Grid.SetRow(border, r);
				Grid.SetColumn(border, c);
				grid.Children.Add(border);
			}
		}
		return grid;
	}
}
