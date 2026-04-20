#pragma warning disable IDE0055 // Fix formatting
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation.Metadata;
using Windows.UI;

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ScrollViewerTests;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollViewer_Anchoring
{
	private const double ItemHeight = 100;
	private const double ItemWidth = 100;
	private const int ItemCount = 20;
	private const double ViewportSize = 300;

	private static (ScrollViewer sv, StackPanel content, List<Border> items) BuildVertical(double itemHeight = ItemHeight, int count = ItemCount)
	{
		var items = new List<Border>();
		var panel = new StackPanel { Orientation = Orientation.Vertical };
		for (int i = 0; i < count; i++)
		{
			var b = new Border
			{
				Width = 200,
				Height = itemHeight,
				Background = new SolidColorBrush(i % 2 == 0 ? Colors.LightBlue : Colors.LightCoral),
				Tag = i,
			};
			panel.Children.Add(b);
			items.Add(b);
		}

		var sv = new ScrollViewer
		{
			Width = 250,
			Height = ViewportSize,
			Content = panel,
		};

		return (sv, panel, items);
	}

	private static (ScrollViewer sv, StackPanel content, List<Border> items) BuildHorizontal(double itemWidth = ItemWidth, int count = ItemCount)
	{
		var items = new List<Border>();
		var panel = new StackPanel { Orientation = Orientation.Horizontal };
		for (int i = 0; i < count; i++)
		{
			var b = new Border
			{
				Width = itemWidth,
				Height = 200,
				Background = new SolidColorBrush(i % 2 == 0 ? Colors.LightBlue : Colors.LightCoral),
				Tag = i,
			};
			panel.Children.Add(b);
			items.Add(b);
		}

		var sv = new ScrollViewer
		{
			Width = ViewportSize,
			Height = 250,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollMode = ScrollMode.Disabled,
			Content = panel,
		};

		return (sv, panel, items);
	}

	private static async Task ScrollToVerticalAsync(ScrollViewer sv, double offset)
	{
		// ChangeView to the current offset is a no-op and doesn't raise ViewChanged, so skip the wait.
		if (Math.Abs(sv.VerticalOffset - offset) < 0.5)
		{
			await WindowHelper.WaitForIdle();
			return;
		}

		var tcs = new TaskCompletionSource<bool>();
		void OnChanged(object s, ScrollViewerViewChangedEventArgs e)
		{
			if (!e.IsIntermediate)
			{
				tcs.TrySetResult(true);
			}
		}
		sv.ViewChanged += OnChanged;
		try
		{
			sv.ChangeView(null, offset, null, disableAnimation: true);
			var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
			if (completed != tcs.Task)
			{
				Assert.Fail($"Timed out waiting for vertical scroll to complete. Requested offset: {offset}, current VerticalOffset: {sv.VerticalOffset}.");
			}
		}
		finally
		{
			sv.ViewChanged -= OnChanged;
		}
		await WindowHelper.WaitForIdle();
	}

	private static async Task ScrollToHorizontalAsync(ScrollViewer sv, double offset)
	{
		// ChangeView to the current offset is a no-op and doesn't raise ViewChanged, so skip the wait.
		if (Math.Abs(sv.HorizontalOffset - offset) < 0.5)
		{
			await WindowHelper.WaitForIdle();
			return;
		}

		var tcs = new TaskCompletionSource<bool>();
		void OnChanged(object s, ScrollViewerViewChangedEventArgs e)
		{
			if (!e.IsIntermediate)
			{
				tcs.TrySetResult(true);
			}
		}
		sv.ViewChanged += OnChanged;
		try
		{
			sv.ChangeView(offset, null, null, disableAnimation: true);
			var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
			if (completed != tcs.Task)
			{
				Assert.Fail($"Timed out waiting for horizontal scroll to complete. Requested offset: {offset}, current HorizontalOffset: {sv.HorizontalOffset}.");
			}
		}
		finally
		{
			sv.ViewChanged -= OnChanged;
		}
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CurrentAnchor_Null_WhenNoRatios()
	{
		var (sv, panel, items) = BuildVertical();

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		await WindowHelper.WaitForIdle();

		Assert.IsNull(sv.CurrentAnchor, "CurrentAnchor should be null when both ratios are NaN (default).");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CurrentAnchor_Null_WhenNoCandidates()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);
		await WindowHelper.WaitForIdle();

		Assert.IsNull(sv.CurrentAnchor, "CurrentAnchor should be null when no candidates are registered.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CurrentAnchor_SelectsClosestCandidate_Vertical()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		// Scroll so items[5] is centered: item top = 5*100=500, center=550. Viewport center at offset+150.
		// offset = 400 → viewport [400, 700], center 550 matches item 5 (500..600).
		await ScrollToVerticalAsync(sv, 400);

		Assert.AreSame(items[5], sv.CurrentAnchor, $"Expected items[5] as anchor, got Tag={((sv.CurrentAnchor as Border)?.Tag)}");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CurrentAnchor_SelectsClosestCandidate_Horizontal()
	{
		var (sv, panel, items) = BuildHorizontal();
		sv.HorizontalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		// Items are 100 wide. Offset 400 → viewport [400, 700], center 550 matches item 5 (500..600).
		await ScrollToHorizontalAsync(sv, 400);

		Assert.AreSame(items[5], sv.CurrentAnchor);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CurrentAnchor_UpdatesOnScroll()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		await ScrollToVerticalAsync(sv, 400);
		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();
		var first = sv.CurrentAnchor;

		await ScrollToVerticalAsync(sv, 1000);
		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();
		var second = sv.CurrentAnchor;

		Assert.AreNotSame(first, second, "CurrentAnchor should change as the viewport moves.");
		Assert.AreSame(items[11], second, "At offset 1000, center 1150 matches items[11] (1100..1200).");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task Register_Unregister_Roundtrip()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		// Unregister items[5] which would be the nearest at offset 400.
		sv.UnregisterAnchorCandidate(items[5]);

		await ScrollToVerticalAsync(sv, 400);

		Assert.AreNotSame(items[5], sv.CurrentAnchor, "Unregistered item should not be chosen.");
		// Either items[4] or items[6] depending on tie-break.
		Assert.IsTrue(sv.CurrentAnchor == items[4] || sv.CurrentAnchor == items[6]);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task Register_Duplicate_Allowed_Unregister_Removes_One()
	{
		// WinUI allows duplicate registrations (see ScrollViewer_Partial.cpp:9373 —
		// Release builds do NOT dedup; Unregister removes only the first match).
		// After triple register + single unregister, the candidate should STILL be chosen.
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		sv.RegisterAnchorCandidate(items[5]);
		sv.RegisterAnchorCandidate(items[5]);
		sv.RegisterAnchorCandidate(items[5]);

		sv.UnregisterAnchorCandidate(items[5]);

		await ScrollToVerticalAsync(sv, 400);
		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();

		Assert.AreSame(items[5], sv.CurrentAnchor,
			"Register does not dedup; a single Unregister after triple-register still leaves the item as a candidate.");

		// Two more unregisters drain the remaining copies.
		sv.UnregisterAnchorCandidate(items[5]);
		sv.UnregisterAnchorCandidate(items[5]);
		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();

		Assert.IsNull(sv.CurrentAnchor, "After as many Unregisters as Registers, no candidate remains.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task AnchorRequested_Raised_WithCandidates()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		sv.RegisterAnchorCandidate(items[3]);
		sv.RegisterAnchorCandidate(items[7]);

		int raised = 0;
		IList<UIElement> seenCandidates = null;
		sv.AnchorRequested += (s, e) =>
		{
			raised++;
			seenCandidates = e.AnchorCandidates;
		};

		await ScrollToVerticalAsync(sv, 400);
		_ = sv.CurrentAnchor; // force evaluation

		Assert.IsTrue(raised > 0, "AnchorRequested should have fired.");
		Assert.IsNotNull(seenCandidates);
		CollectionAssert.Contains(seenCandidates.ToList(), items[3]);
		CollectionAssert.Contains(seenCandidates.ToList(), items[7]);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task AnchorRequested_Handler_OverrideAnchor()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		sv.AnchorRequested += (s, e) =>
		{
			e.Anchor = items[0]; // force the far-away item
		};

		await ScrollToVerticalAsync(sv, 400);

		Assert.AreSame(items[0], sv.CurrentAnchor, "Handler override via e.Anchor should take precedence.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task AnchorRequested_Handler_CanModifyCandidates()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		sv.AnchorRequested += (s, e) =>
		{
			// Remove items[5] (and neighbors) so they cannot be chosen.
			e.AnchorCandidates.Remove(items[5]);
			e.AnchorCandidates.Remove(items[4]);
			e.AnchorCandidates.Remove(items[6]);
		};

		await ScrollToVerticalAsync(sv, 400);

		Assert.AreNotSame(items[5], sv.CurrentAnchor);
		Assert.AreNotSame(items[4], sv.CurrentAnchor);
		Assert.AreNotSame(items[6], sv.CurrentAnchor);
		Assert.IsTrue(sv.CurrentAnchor == items[3] || sv.CurrentAnchor == items[7]);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task Anchoring_PreservesPosition_OnContentGrowAbove()
	{
		// Semantic guarantee: after inserting content above the anchor, the anchor element
		// stays at approximately the same visual position within the viewport.
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		await ScrollToVerticalAsync(sv, 400);
		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();
		var anchor = sv.CurrentAnchor as Border;
		Assert.IsNotNull(anchor);
		var anchorYBefore = anchor.TransformToVisual(sv).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;

		var inserted = new Border { Width = 200, Height = 100, Background = new SolidColorBrush(Colors.LightGreen) };
		panel.Children.Insert(0, inserted);

		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();

		var anchorYAfter = anchor.TransformToVisual(sv).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
		Assert.AreEqual(anchorYBefore, anchorYAfter, 2.0, "Anchor's Y position within the viewport should be preserved.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task Anchoring_PreservesPosition_OnContentShrinkAbove()
	{
		// Semantic guarantee: removing content above the anchor keeps the anchor at the
		// same visual position (offset decreases to compensate).
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		foreach (var item in items)
		{
			sv.RegisterAnchorCandidate(item);
		}

		await ScrollToVerticalAsync(sv, 400);
		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();
		var anchor = sv.CurrentAnchor as Border;
		Assert.IsNotNull(anchor);
		var anchorYBefore = anchor.TransformToVisual(sv).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;

		panel.Children.RemoveAt(0);
		sv.UnregisterAnchorCandidate(items[0]);

		sv.InvalidateArrange();
		await WindowHelper.WaitForIdle();

		var anchorYAfter = anchor.TransformToVisual(sv).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
		Assert.AreEqual(anchorYBefore, anchorYAfter, 2.0, "Anchor's Y position within the viewport should be preserved after content removal above.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task Anchoring_Ignored_CollapsedCandidate()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		sv.RegisterAnchorCandidate(items[5]);
		items[5].Visibility = Visibility.Collapsed;

		await ScrollToVerticalAsync(sv, 400);

		Assert.IsNull(sv.CurrentAnchor, "A collapsed candidate must not be chosen.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task Anchoring_Ignored_NonDescendantCandidate()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		// An element NOT inside the ScrollViewer's content subtree.
		var outsider = new Border { Width = 50, Height = 50 };
		sv.RegisterAnchorCandidate(outsider);

		await ScrollToVerticalAsync(sv, 400);

		Assert.IsNull(sv.CurrentAnchor, "A non-descendant candidate must not be chosen.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task FarEdge_Anchoring_VerticalRatio1()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 1.0;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		// Scroll to bottom.
		await ScrollToVerticalAsync(sv, sv.ScrollableHeight);
		var offsetBefore = sv.VerticalOffset;
		var extentBefore = sv.ExtentHeight;

		// Add content (which grows extent by 100). Since we are at the far edge with ratio 1.0,
		// the scroll offset should shift by the same amount so we remain pinned to the bottom.
		var inserted = new Border { Width = 200, Height = 100, Background = new SolidColorBrush(Colors.LightGreen) };
		panel.Children.Insert(0, inserted);

		await WindowHelper.WaitForIdle();

		Assert.AreEqual(extentBefore + 100, sv.ExtentHeight, 1.5, "Extent should have grown.");
		Assert.AreEqual(offsetBefore + 100, sv.VerticalOffset, 1.5, "Offset should follow the bottom edge.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task NearEdge_NoAdjustment_At_Ratio0()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.0;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		// At offset 0 (near edge). Add content below - should not shift.
		var added = new Border { Width = 200, Height = 100, Background = new SolidColorBrush(Colors.LightGreen) };
		panel.Children.Add(added);

		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, sv.VerticalOffset, 0.5, "At near edge with ratio 0, offset should remain at 0.");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task IScrollAnchorProvider_Cast_Works()
	{
		var (sv, panel, items) = BuildVertical();
		sv.VerticalAnchorRatio = 0.5;

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);

		var provider = (IScrollAnchorProvider)sv;

		foreach (var item in items)
		{
			provider.RegisterAnchorCandidate(item);
		}

		await ScrollToVerticalAsync(sv, 400);

		Assert.AreSame(items[5], provider.CurrentAnchor);

		provider.UnregisterAnchorCandidate(items[5]);
		await WindowHelper.WaitForIdle();

		Assert.AreNotSame(items[5], provider.CurrentAnchor);
	}
}
