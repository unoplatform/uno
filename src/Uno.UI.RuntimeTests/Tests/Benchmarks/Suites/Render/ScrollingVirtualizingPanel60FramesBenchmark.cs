using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Render;

/// <summary>
/// Drives a virtualizing ListView through 60 frames of programmatic scrolling and measures
/// total compositor + virtualization work. End-to-end "is scrolling slow?" canary.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Render)]
[MemoryDiagnoser]
public class ScrollingVirtualizingPanel60FramesBenchmark
{
	private const int ItemCount = 1000;
	private const double ScrollIncrement = 50;

	private ListView _list = null!;
	private ScrollViewer? _scrollViewer;

	[GlobalSetup]
	public async Task Setup()
	{
		await RenderBenchmarkHost.SetupSceneAsync(() =>
		{
			_list = new ListView
			{
				Width = 400,
				Height = 600,
				ItemsSource = Enumerable.Range(0, ItemCount).Select(i => $"Item {i}").ToList(),
			};
			return _list;
		});

		await TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
		{
			_scrollViewer = FindScrollViewer(_list);
		});
	}

	[Benchmark]
	public async Task Scroll60Frames()
	{
		double offset = 0;
		for (int i = 0; i < 60; i++)
		{
			offset += ScrollIncrement;
			await TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
			{
				_scrollViewer?.ChangeView(null, offset, null, disableAnimation: true);
			});
			await RenderBenchmarkHost.RenderFrameAsync();
		}
	}

	[GlobalCleanup]
	public Task Cleanup() => RenderBenchmarkHost.ResetSceneAsync();

	private static ScrollViewer? FindScrollViewer(DependencyObject root)
	{
		if (root is ScrollViewer sv)
		{
			return sv;
		}
		var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
			var found = FindScrollViewer(child);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}
}
