#if __SKIA__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// A composition brush keeps a weak "context" entry per consumer (visual/shape) so it can propagate its changes.
/// Consumers that are collected without reassigning their brush property must not leave their entry behind
/// forever on a long-lived shared brush (e.g. a theme SolidColorBrush).
/// </summary>
/// <remarks>
/// A consumer the GC fails to collect (conservative stack scanning on WASM / mobile) legitimately keeps its entry,
/// so the expected counts include the measured survivors instead of assuming a precise GC.
/// </remarks>
[TestClass]
public class Given_CompositionObject_Context
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Consumers_Collected_Then_ContextEntries_Do_Not_Accumulate()
	{
		var compositor = Compositor.GetSharedCompositor();
		var brush = compositor.CreateColorBrush(Colors.Red);
		var dead = new List<WeakReference>();
		var maxSurvivors = 0;

		for (var batch = 0; batch < 100; batch++)
		{
			dead.AddRange(AddDeadConsumers(compositor, brush, 10));
			maxSurvivors = Math.Max(maxSurvivors, await CollectAsync(dead));
		}

		var survivors = await CollectUntilCollectedAsync(dead);

		// Before the fix: 1000 entries, all of them dead. The sweep threshold is twice the surviving entries.
		AssertBounded(brush.ContextEntriesCount, 64 + 2 * maxSurvivors, maxSurvivors, survivors, dead.Count);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Consumers_Collected_Then_Live_Consumers_Keep_Their_Entry()
	{
		var compositor = Compositor.GetSharedCompositor();
		var brush = compositor.CreateColorBrush(Colors.Red);

		var live = new List<SpriteVisual>();
		for (var i = 0; i < 50; i++)
		{
			live.Add(new SpriteVisual(compositor) { Brush = brush });
		}

		var dead = new List<WeakReference>();
		var maxSurvivors = 0;
		for (var batch = 0; batch < 100; batch++)
		{
			dead.AddRange(AddDeadConsumers(compositor, brush, 10));
			maxSurvivors = Math.Max(maxSurvivors, await CollectAsync(dead));
		}

		var survivorCount = await CollectUntilCollectedAsync(dead);
		AssertBounded(brush.ContextEntriesCount, 200 + 2 * maxSurvivors, maxSurvivors, survivorCount, dead.Count);

		// Hold the uncollected consumers so the expected count can't change under the asserts below.
		var survivors = dead.Select(r => r.Target).Where(t => t is not null).ToList();

		// A change of the brush walks the list and drops whatever is dead: what remains must be exactly the live consumers.
		brush.Color = Colors.Blue;
		Assert.AreEqual(live.Count + survivors.Count, brush.ContextEntriesCount, $"live={live.Count}, uncollected={survivors.Count}");

		// Reassigning the property still removes the entry.
		live[0].Brush = null;
		Assert.AreEqual(live.Count + survivors.Count - 1, brush.ContextEntriesCount, $"live={live.Count - 1}, uncollected={survivors.Count}");

		GC.KeepAlive(live);
		GC.KeepAlive(survivors);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Shared_SolidColorBrush_Consumers_Unloaded_Then_ContextEntries_Do_Not_Accumulate()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var host = new StackPanel { Orientation = Orientation.Horizontal, Width = 200, Height = 50 };
		TestServices.WindowHelper.WindowContent = host;
		try
		{
			await TestServices.WindowHelper.WaitForLoaded(host);

			var dead = new List<WeakReference>();
			var maxSurvivors = 0;
			for (var i = 0; i < 100; i++)
			{
				dead.AddRange(AddXamlConsumers(host, brush));
				await TestServices.WindowHelper.WaitForIdle();
				host.Children.Clear();

				if (i % 10 == 9)
				{
					await TestServices.WindowHelper.WaitForIdle();
					maxSurvivors = Math.Max(maxSurvivors, await CollectAsync(dead));
				}
			}

			var survivors = await CollectUntilCollectedAsync(dead);

			// One more consumer, so that AddContext gets a chance to sweep what was collected last.
			AddXamlConsumers(host, brush);
			await TestServices.WindowHelper.WaitForIdle();

			var compositionBrush = brush.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor());

			// Each Border with a Background adds 2 entries (BorderVisual.BackgroundBrush and its background shape's FillBrush)
			// and each Rectangle adds 1. Before the fix: ~300 entries, all but the last 3 dead.
			AssertBounded(compositionBrush.ContextEntriesCount, 100 + 4 * maxSurvivors, maxSurvivors, survivors, dead.Count);
			Assert.IsTrue(compositionBrush.ContextEntriesCount >= 3, $"The live Border and Rectangle lost their entries: {compositionBrush.ContextEntriesCount}");

			// The live consumers must still pick up a change of the brush after the sweep.
			brush.Color = Colors.Blue;
			await TestServices.WindowHelper.WaitForIdle();

			var screenshot = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(screenshot, 10, 25, Colors.Blue, tolerance: 5); // Border
			ImageAssert.HasColorAt(screenshot, 30, 25, Colors.Blue, tolerance: 5); // Rectangle
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference[] AddDeadConsumers(Compositor compositor, CompositionBrush brush, int count)
	{
		var refs = new WeakReference[count];
		for (var i = 0; i < count; i++)
		{
			refs[i] = new WeakReference(new SpriteVisual(compositor) { Brush = brush });
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference[] AddXamlConsumers(Panel host, Brush brush)
	{
		var border = new Border { Width = 20, Height = 20, Background = brush };
		var rectangle = new Rectangle { Width = 20, Height = 20, Fill = brush };
		host.Children.Add(border);
		host.Children.Add(rectangle);

		return new[] { new WeakReference(border), new WeakReference(rectangle) };
	}

	private static void AssertBounded(int entries, int bound, int maxSurvivors, int survivors, int total)
	{
		// Keeps the survivor-adjusted bound below the entry count the leak itself produces.
		Assert.IsTrue(maxSurvivors <= total / 10, $"Up to {maxSurvivors} of {total} consumers were not collected ({survivors} at the end): something still references them.");
		Assert.IsTrue(entries <= bound, $"Expected at most {bound} context entries, got {entries} ({survivors} of {total} consumers uncollected, peak {maxSurvivors}).");
	}

	/// <returns>The number of consumers still alive.</returns>
	private static async Task<int> CollectAsync(List<WeakReference> refs)
	{
		for (var i = 0; i < 3; i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();

			// Some platforms need a yield before weak references are cleared, as in Given_BindingMemoryLeak.
			if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
			{
				await Task.Yield();
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
			}
		}

		return refs.Count(r => r.IsAlive);
	}

	/// <returns>The number of consumers still alive after retrying for a while.</returns>
	private static async Task<int> CollectUntilCollectedAsync(List<WeakReference> refs)
	{
		var sw = Stopwatch.StartNew();
		int survivors;
		while ((survivors = await CollectAsync(refs)) > 0 && sw.Elapsed < TimeSpan.FromSeconds(5))
		{
			await Task.Delay(10);
		}

		return survivors;
	}
}
#endif
