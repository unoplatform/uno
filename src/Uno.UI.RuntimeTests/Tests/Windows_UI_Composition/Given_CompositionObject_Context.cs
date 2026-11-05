#if __SKIA__
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// A composition brush keeps a weak "context" entry per consumer (visual/shape) so it can propagate its changes.
/// Consumers that are collected without reassigning their brush property must not leave their entry behind
/// forever on a long-lived shared brush (e.g. a theme SolidColorBrush).
/// </summary>
[TestClass]
public class Given_CompositionObject_Context
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Consumers_Collected_Then_ContextEntries_Do_Not_Accumulate()
	{
		var compositor = Compositor.GetSharedCompositor();
		var brush = compositor.CreateColorBrush(Colors.Red);

		for (var batch = 0; batch < 100; batch++)
		{
			AddDeadConsumers(compositor, brush, 10);
			Collect();
		}

		// Before the fix: 1000 entries, all of them dead.
		Assert.IsTrue(brush.ContextEntriesCount <= 64, $"Expected a bounded number of context entries, got {brush.ContextEntriesCount}");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Consumers_Collected_Then_Live_Consumers_Keep_Their_Entry()
	{
		var compositor = Compositor.GetSharedCompositor();
		var brush = compositor.CreateColorBrush(Colors.Red);

		var live = new List<SpriteVisual>();
		for (var i = 0; i < 50; i++)
		{
			live.Add(new SpriteVisual(compositor) { Brush = brush });
		}

		for (var batch = 0; batch < 100; batch++)
		{
			AddDeadConsumers(compositor, brush, 10);
			Collect();
		}

		Assert.IsTrue(brush.ContextEntriesCount <= 200, $"Expected a bounded number of context entries, got {brush.ContextEntriesCount}");

		// A change of the brush walks the list and drops whatever is dead: what remains must be exactly the live consumers.
		Collect();
		brush.Color = Colors.Blue;
		Assert.AreEqual(live.Count, brush.ContextEntriesCount);

		// Reassigning the property still removes the entry.
		live[0].Brush = null;
		Assert.AreEqual(live.Count - 1, brush.ContextEntriesCount);

		GC.KeepAlive(live);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Shared_SolidColorBrush_Consumers_Unloaded_Then_ContextEntries_Do_Not_Accumulate()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var host = new Grid { Width = 200, Height = 50 };
		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(host);

		for (var i = 0; i < 100; i++)
		{
			AddXamlConsumers(host, brush);
			await TestServices.WindowHelper.WaitForIdle();
			host.Children.Clear();

			if (i % 10 == 9)
			{
				await TestServices.WindowHelper.WaitForIdle();
				Collect();
			}
		}

		// One more consumer, so that AddContext gets a chance to sweep what was collected last.
		AddXamlConsumers(host, brush);
		await TestServices.WindowHelper.WaitForIdle();

		var compositionBrush = brush.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor());

		// Each Border with a Background adds 2 entries (BorderVisual.BackgroundBrush and its background shape's FillBrush)
		// and each Rectangle adds 1. Before the fix: ~300 entries, all but the last 3 dead.
		Assert.IsTrue(compositionBrush.ContextEntriesCount <= 100, $"Expected a bounded number of context entries, got {compositionBrush.ContextEntriesCount}");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void AddDeadConsumers(Compositor compositor, CompositionBrush brush, int count)
	{
		for (var i = 0; i < count; i++)
		{
			_ = new SpriteVisual(compositor) { Brush = brush };
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void AddXamlConsumers(Panel host, Brush brush)
	{
		host.Children.Add(new Border { Width = 20, Height = 20, Background = brush });
		host.Children.Add(new Rectangle { Width = 20, Height = 20, Fill = brush });
	}

	private static void Collect()
	{
		for (var i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}
}
#endif
