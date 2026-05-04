using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Scenario;

/// <summary>
/// Virtualized ListView with 1000 items. Measure pass only — no scroll. Catches
/// itemsrepeater/recycling pipeline regressions.
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Scenario)]
[MemoryDiagnoser]
public class ListViewMeasure1000ItemsBenchmark
{
	private ListView _sut = null!;

	[GlobalSetup]
	public async Task Setup()
	{
		await RenderBenchmarkHost.SetupSceneAsync(() =>
		{
			_sut = new ListView
			{
				Width = 400,
				Height = 600,
				ItemsSource = Enumerable.Range(0, 1000).Select(i => $"Item {i}").ToList(),
			};
			return _sut;
		});
	}

	[Benchmark]
	public Task Measure() => TestServices.WindowHelper.RootElementDispatcher.RunAsyncWithBypass(() =>
	{
		_sut.InvalidateMeasure();
		_sut.Measure(new Size(400, 600));
		_sut.Arrange(new Rect(0, 0, 400, 600));
	});

	[GlobalCleanup]
	public Task Cleanup() => RenderBenchmarkHost.ResetSceneAsync();
}
