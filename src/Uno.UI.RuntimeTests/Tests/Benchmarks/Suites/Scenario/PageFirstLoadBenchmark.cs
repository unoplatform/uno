#nullable enable

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Scenario;

/// <summary>
/// Instantiate a fixed sample page (consistent shape across iterations), attach to the
/// visual tree, and measure time-to-Loaded.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Scenario)]
[MemoryDiagnoser]
public class PageFirstLoadBenchmark
{
	[Benchmark]
	public async Task FirstLoad()
	{
		var tcs = new TaskCompletionSource();
		await TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
		{
			var page = BuildPage();
			page.Loaded += (_, _) => tcs.TrySetResult();
			TestServices.WindowHelper.WindowContent = page;
		});
		await tcs.Task;
		await TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
		{
			TestServices.WindowHelper.WindowContent = null;
		});
	}

	private static Page BuildPage()
	{
		var stack = new StackPanel();
		for (int i = 0; i < 20; i++)
		{
			stack.Children.Add(new TextBlock
			{
				Text = $"Row {i}",
				Foreground = new SolidColorBrush(Colors.Black),
			});
		}
		return new Page { Content = stack };
	}
}
