#nullable enable

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Render;

/// <summary>
/// Animates a TranslateTransform on a single element and measures 60 frames of compositor
/// work. Targets the TimeManager / animation pipeline.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Render)]
[MemoryDiagnoser]
public class CompositorAnimatedTransformBenchmark
{
	private TranslateTransform _transform = null!;
	private Storyboard _storyboard = null!;

	[GlobalSetup]
	public Task Setup() => RenderBenchmarkHost.SetupSceneAsync(() =>
	{
		_transform = new TranslateTransform();
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.SteelBlue),
			RenderTransform = _transform,
		};

		var animation = new DoubleAnimation
		{
			From = 0,
			To = 200,
			Duration = new Duration(System.TimeSpan.FromSeconds(1)),
			RepeatBehavior = RepeatBehavior.Forever,
			AutoReverse = true,
		};
		Storyboard.SetTarget(animation, _transform);
		Storyboard.SetTargetProperty(animation, "X");

		_storyboard = new Storyboard();
		_storyboard.Children.Add(animation);

		return new Grid { Children = { border } };
	});

	[Benchmark]
	public async Task Animate60Frames()
	{
		await TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() => _storyboard.Begin());

		for (int i = 0; i < 60; i++)
		{
			await RenderBenchmarkHost.RenderFrameAsync();
		}

		await TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() => _storyboard.Stop());
	}

	[GlobalCleanup]
	public Task Cleanup() => RenderBenchmarkHost.ResetSceneAsync();
}
