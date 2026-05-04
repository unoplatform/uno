using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Scenario;

/// <summary>
/// Cold and warm lookup of 50 distinct theme resource keys against
/// <see cref="XamlControlsResources"/>. The cold variant rebuilds the resource
/// dictionary every iteration (pays full lazy-init each time); the warm variant
/// reuses a pre-built dictionary across iterations (cache-resolution path).
/// </summary>
[BenchmarkCategory(BenchmarkCategory.Scenario)]
[MemoryDiagnoser]
public class ResourceDictionaryLookupBenchmark
{
	private static readonly string[] Keys =
	{
		"AccentButtonBackground", "AccentButtonForeground", "ButtonBackground",
		"ButtonForeground", "ButtonBorderBrush", "TextControlBackground",
		"TextControlForeground", "TextControlBorderBrush", "ToggleSwitchFillOff",
		"ToggleSwitchFillOn", "SystemControlAcrylicElementBrush",
		"SystemControlAcrylicWindowBrush", "DefaultTextForegroundThemeBrush",
		"ApplicationPageBackgroundThemeBrush", "SystemAccentColor",
		"ListViewItemBackground", "ListViewItemForeground", "FlyoutBackground",
		"ToolTipBackground", "ToolTipForeground",
	};

	private XamlControlsResources _warm = null!;

	[GlobalSetup]
	public void Setup() => _warm = new XamlControlsResources();

	[Benchmark]
	public int LookupCold()
	{
		var dict = new XamlControlsResources();
		var hits = 0;
		foreach (var k in Keys)
		{
			if (dict.TryGetValue(k, out _))
			{
				hits++;
			}
		}
		return hits;
	}

	[Benchmark]
	public int LookupWarm()
	{
		var hits = 0;
		foreach (var k in Keys)
		{
			if (_warm.TryGetValue(k, out _))
			{
				hits++;
			}
		}
		return hits;
	}
}
