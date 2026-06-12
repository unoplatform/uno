#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO && !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

[TestClass]
[RunsOnUIThread]
public class Given_ItemsRepeater_FastScroll
{
	private const double OffsetTolerance = 0.5;
	private const double OverlapTolerance = 0.5;

	private const string ItemTemplateXaml = """
		<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<Border Height="{Binding Height}" Background="{Binding Background}" />
		</DataTemplate>
		""";

	// Horizontal variant reuses the model's Height property as the major-axis size (Width) so
	// vertical/horizontal tests can share the same ItemModel and helpers.
	private const string ItemTemplateHorizontalXaml = """
		<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<Border Width="{Binding Height}" Background="{Binding Background}" />
		</DataTemplate>
		""";

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_FastScrollWithHighVarianceItems_Then_NoOverlap()
	{
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		var offsets = new[] { 5000.0, 0.0, 12000.0, 300.0 };
		foreach (var offset in offsets)
		{
			sut.Scroller.ChangeView(null, offset, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();

			AssertNoOverlap(sut);
			AssertAllMaterializedChildrenHaveFiniteOffsets(sut);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_ScrollIncrementallyToBottomAndBack_Then_FirstItemFlushAtZero()
	{
		// Regression for a chat-style scenario: after navigating up/down through a list of items
		// with high-variance heights, the top of the list becomes cropped / inaccessible.
		// Incremental ChangeView calls preserve realization-window state across scrolls, which is
		// what drives StackLayout's average-size estimation off-course (the path that exercises
		// the Uno workaround clamp in StackLayout.GetExtent).
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		// Walk down to the bottom, then back up to the top, in small increments. Each ChangeView is
		// followed by WaitForIdle so the realization window tracks the scroll progression.
		const int Steps = 30;
		var bottomTarget = sut.Scroller.ScrollableHeight;
		for (var i = 1; i <= Steps; i++)
		{
			sut.Scroller.ChangeView(null, bottomTarget * i / Steps, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}
		for (var i = Steps - 1; i >= 1; i--)
		{
			sut.Scroller.ChangeView(null, bottomTarget * i / Steps, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}
		// Final explicit scroll-to-top.
		sut.Scroller.ChangeView(null, 0, null, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		sut.Scroller.VerticalOffset.Should().Be(0);

		var firstItem = FindMaterializedElementForIndex(sut, 0);
		firstItem.Should().NotBeNull("Source[0] must be materialized after scrolling back to top");

		var firstItemY = firstItem!.TransformToVisual(sut.Scroller).TransformPoint(new Point(0, 0)).Y;
		firstItemY.Should().BeApproximately(0, OffsetTolerance,
			"Source[0] must be flush at VerticalOffset=0 after an incremental scroll down and back up — content must not be cropped off the top.");
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_TallItemReRealizedAfterScrollCycle_Then_PositionIsStable()
	{
		// Regression for the ItemsRepeater rendering corruption observed during fast scrolling
		// through high-variance lists: when a much-taller-than-average item is realized, then
		// virtualized out, then re-realized after a scroll cycle, its reported position must not
		// drift. StackLayout.GetExtent estimates first-realized-item position using average size;
		// after a tall item inflates the average, re-realizing that tall item should still
		// resolve to its true cumulative offset. The Uno clamp in GetExtent
		// (Math.Max(0, firstRealizedMajor)) breaks this because it prevents the compensating
		// negative origin shift WinUI applies.
		// Uses the same 200-item high-variance layout as Test A: every 10th item is 1200px tall,
		// others are 40px. Item 3 is 1200px tall at Y=120 (covers Y=[120,1320]).
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		await ScrollInStepsAsync(sut, 400);

		var item3Element = FindMaterializedElementForIndex(sut, 3);
		item3Element.Should().NotBeNull("Item 3 (tall, spans Y=120..1320) must be materialized at viewport offset 400");
		var item3YBefore = item3Element!.TransformToVisual(sut.Repeater).TransformPoint(new Point(0, 0)).Y;

		// Scroll far enough that item 3 becomes dematerialized (well past its end at Y=1320).
		await ScrollInStepsAsync(sut, 6000);

		// Scroll back so item 3 is in the realization cache again.
		await ScrollInStepsAsync(sut, 400);

		var item3ElementAfter = FindMaterializedElementForIndex(sut, 3);
		item3ElementAfter.Should().NotBeNull("Item 3 must be re-materialized after scrolling back to viewport offset 400");
		var item3YAfter = item3ElementAfter!.TransformToVisual(sut.Repeater).TransformPoint(new Point(0, 0)).Y;

		item3YAfter.Should().BeApproximately(item3YBefore, 1,
			"Item 3 must not drift vertically across a dematerialize/re-materialize cycle.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_RealizedItemGrows_Then_LayoutRemainsConsistent()
	{
		const double OriginalHeight = 60;
		var items = Enumerable.Range(0, 10)
			.Select(i => new ItemModel(i, OriginalHeight, ColorForIndex(i)))
			.ToArray();
		// Viewport 800 comfortably contains all 10 items (10 * 60 = 600) with 200px of headroom.
		// The exact-fit case (viewport == content) is implementation-defined: the realization rule
		// (elementMajorStart < rectMajorEnd) is identical on Uno and native WinUI, but the boundary
		// outcome depends on cache-buffer growth timing — native WinUI realizes 9/10 at the exact
		// boundary while Uno realizes all 10. A margin sidesteps that ambiguity so the test exercises
		// the layout-consistency invariant regardless of platform virtualization timing.
		var sut = CreateSut(items, new Size(300, 800));
		await LoadAsync(sut);

		// Items 5 and 6 must be realized so we can assert the post-growth shift on them.
		FindMaterializedElementForIndex(sut, 5).Should().NotBeNull(
			"Item 5 must be realized — it is the item being grown");
		FindMaterializedElementForIndex(sut, 6).Should().NotBeNull(
			"Item 6 must be realized — its post-growth offset is the test invariant");

		const double GrownHeight = 1500;
		sut.Source[5].Height = GrownHeight;
		sut.Repeater.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var grownItem5 = FindMaterializedElementForIndex(sut, 5);
		grownItem5.Should().NotBeNull("Item 5 must remain materialized after growing");
		grownItem5!.ActualHeight.Should().BeApproximately(GrownHeight, 0.5,
			"Item 5 must reflect its new height after the layout pass");

		var expectedGrowth = GrownHeight - OriginalHeight;

		AssertNoOverlap(sut);

		// Items after index 5 must have shifted down by the growth delta.
		var item6 = FindMaterializedElementForIndex(sut, 6);
		item6.Should().NotBeNull("Item 6 must remain materialized after item 5 grows");
		((double)item6!.ActualOffset.Y).Should().BeApproximately(6 * OriginalHeight + expectedGrowth, 1,
			"Item 6 must shift down by the growth delta after item 5 grows");
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_FastScrollHorizontal_WithHighVarianceItems_Then_NoOverlap()
	{
		// Horizontal orientation counterpart of When_FastScrollWithHighVarianceItems_Then_NoOverlap.
		// The StackLayout.GetExtent clamp applied to the major axis regardless of orientation, so the
		// bug symptoms are equally reproducible horizontally. This guards against orientation-specific
		// regressions in the anchor-shift pipeline.
		var sut = CreateHighVarianceHorizontalSut(itemCount: 200, viewport: new Size(600, 300));
		await LoadAsync(sut);

		var offsets = new[] { 5000.0, 0.0, 12000.0, 300.0 };
		foreach (var offset in offsets)
		{
			sut.Scroller.ChangeView(offset, null, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();

			AssertNoOverlapHorizontal(sut);
			AssertAllMaterializedChildrenHaveFiniteOffsets(sut);
		}
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_ScrolledThroughList_Then_ExtentAccommodatesRealContent()
	{
		// The pre-fix symptom: the clamp caused StackLayout to report an ExtentHeight smaller than
		// the real cumulative content size, making the last items unreachable via scroll. Without
		// asserting exact convergence (which varies across platforms — native WinUI can hold an
		// over-estimated extent during rapid scrolling), this test enforces the floor invariant:
		// after walking through the full list, ExtentHeight must be at least the real content size.
		// The test items are 20 × 1200 + 180 × 40 = 31 200 px cumulative.
		const double RealContentHeight = 31200;

		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		// Walk through the entire list so every item is measured at least once.
		const int WalkSteps = 250;
		for (var i = 1; i <= WalkSteps; i++)
		{
			sut.Scroller.ChangeView(null, sut.Scroller.ScrollableHeight * i / WalkSteps, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}

		sut.Scroller.ExtentHeight.Should().BeGreaterThanOrEqualTo(RealContentHeight - 1,
			"ExtentHeight must accommodate the real cumulative content size after every item is measured; "
			+ "the pre-fix clamp caused an under-estimated extent that left late items unreachable.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_BottomClickedFromInitialState_Then_LastItemFlushAtViewportBottom()
	{
		// Reproduces issue #23041 / studio.live#1333: clicking "Bottom" once on a freshly-loaded
		// chat-style ItemsRepeater (200 items, every 10th 1200px tall, others 40px, IR
		// VerticalAlignment=Bottom) must leave Source[199] flush against the bottom of the viewport.
		// The observed Uno bug: the first ChangeView(ScrollableHeight) lands the offset in blank
		// territory (UI appears empty) because StackLayout's average-size estimate is biased toward
		// the few short items realized at the trailing edge after the jump, which shrinks
		// ExtentHeight below the offset that was just requested. WinUI keeps the extent monotonic
		// enough through this transition to land Source[199] at the viewport bottom on the first
		// click.
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		var initialExtent = sut.Scroller.ExtentHeight;
		var initialScrollableHeight = sut.Scroller.ScrollableHeight;

		// First (and only) "Bottom" click — explicitly NOT preceded by ChangeView(0) so the SUT is
		// in the same starting state as the manual repro: never scrolled, only the leading items
		// have ever been measured.
		sut.Scroller.ChangeView(null, sut.Scroller.ScrollableHeight, null, disableAnimation: true);
		// Wait for the offset to actually take effect — on WinUI ChangeView is async and a single
		// WaitForIdle may return before the SCV has settled. Poll for VO != 0 (or extent change)
		// up to ~3s, then run a final WaitForIdle so any post-change layout cascade settles.
		for (var attempt = 0; attempt < 30; attempt++)
		{
			if (sut.Scroller.VerticalOffset > 0)
			{
				break;
			}
			await Task.Delay(100);
		}
		await TestServices.WindowHelper.WaitForIdle();

		var diagnostics = $"Initial: ExtentHeight={initialExtent:F2}, ScrollableHeight={initialScrollableHeight:F2}; "
			+ $"After Bottom click: VerticalOffset={sut.Scroller.VerticalOffset:F2}, ExtentHeight={sut.Scroller.ExtentHeight:F2}, "
			+ $"ScrollableHeight={sut.Scroller.ScrollableHeight:F2}, ViewportHeight={sut.Scroller.ViewportHeight:F2}";

		var lastIndex = sut.Source.Count - 1;
		var lastItem = FindMaterializedElementForIndex(sut, lastIndex);
		lastItem.Should().NotBeNull(
			$"Source[{lastIndex}] must be materialized after a single ChangeView(ScrollableHeight) from the initial state — "
			+ "if it isn't, the user sees blank UI because the offset jumped past the actual content. " + diagnostics);

		var lastTop = lastItem!.TransformToVisual(sut.Scroller).TransformPoint(new Point(0, 0)).Y;
		var lastBottom = lastTop + lastItem.ActualHeight;

		lastBottom.Should().BeApproximately(sut.Scroller.ViewportHeight, 1,
			$"Source[{lastIndex}] bottom must be flush with the viewport bottom on the FIRST 'Bottom' click — "
			+ "users should not need to click twice or scroll manually to recover from a blank viewport. "
			+ $"lastTop={lastTop:F2}, lastBottom={lastBottom:F2}. " + diagnostics);
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#elif __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_WheelScrollDownThroughVarianceList_Then_OffsetMonotonicallyAdvances()
	{
		// Reproduces issue #23041 / studio.live#816: mouse-wheel scrolling down through a
		// high-variance ItemsRepeater visibly snaps the offset backward (the user has to "fight"
		// the scroller) when realization-driven extent shrinkage triggers TrimOverscroll mid-input.
		// Records EVERY ViewChanged offset (intermediate and final) so we catch the visible
		// "snap-back" frames the user perceives even when the final settled position eventually
		// moves forward. On WinUI, the composition-thread scroll position is decoupled from the
		// layout-thread extent estimation, so the user's wheel input is never reversed by mid-scroll
		// layout adjustments.
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		var bounds = sut.Scroller.GetAbsoluteBounds();
		mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		// Record EVERY scroll position the SV passes through, including animation intermediates.
		// We tolerate small backward fluctuations from animation easing/quantization but a real
		// "snap back" caused by extent shrinkage shows up as a many-pixel drop within the same
		// wheel-down sequence.
		var offsets = new List<double>();
		void OnViewChanged(object? s, ScrollViewerViewChangedEventArgs e) => offsets.Add(sut.Scroller.VerticalOffset);
		sut.Scroller.ViewChanged += OnViewChanged;
		try
		{
			offsets.Add(sut.Scroller.VerticalOffset);

			// Multiple wheel-down ticks in rapid succession — typical user behavior. Each tick should
			// nudge the offset forward; the realization rect chases the new visible region.
			const int Ticks = 12;
			for (var i = 0; i < Ticks; i++)
			{
				mouse.WheelDown();
				await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
			}
		}
		finally
		{
			sut.Scroller.ViewChanged -= OnViewChanged;
		}

		// Forward-progress invariant: in any monotonically-advancing forward-scroll, no recorded
		// offset should be more than this tolerance below the cumulative max so far. A "fight"
		// shows up as a sample 100s of pixels below the running max.
		const double BackwardJumpTolerance = 5.0;
		var runningMax = offsets[0];
		for (var i = 1; i < offsets.Count; i++)
		{
			(offsets[i] - runningMax).Should().BeGreaterThan(-BackwardJumpTolerance,
				$"Sample #{i} (offset {offsets[i]:F2}) jumped backward from the running max ({runningMax:F2}) "
				+ $"during a forward wheel-down sequence — the user perceives this as the scroller fighting input. "
				+ $"All recorded offsets: [{string.Join(", ", offsets.Select(o => o.ToString("F2")))}].");
			if (offsets[i] > runningMax)
			{
				runningMax = offsets[i];
			}
		}

		// Sanity: the wheel sequence must have advanced *somewhere* — if every tick stayed at 0,
		// the monotonic check has nothing to validate. With 200-item variance content and 12
		// wheel ticks, expect at least 200px of forward progress.
		(offsets[^1] - offsets[0]).Should().BeGreaterThan(200,
			$"The wheel sequence must produce non-trivial forward movement (got {offsets[^1] - offsets[0]:F2}px after {offsets.Count} samples); "
			+ "otherwise the monotonic-advance check has nothing to validate.");
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#elif __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_SmallWheelFlicksOnMultiTemplateContent_Then_EachAdvancesOffset()
	{
		// Reproduces the "small flick of the scrollwheel often snap back to the previous scroll
		// position instead of moving" symptom reported on the studio.live multi-template subagent
		// markdown UI. The scenario: chat-style ItemsRepeater with mixed-height templates (32-400
		// px). A single small wheel tick — typical when reading content carefully — must produce a
		// visible forward advance. The user-reported regression was that the offset would jump
		// somewhere, snap back to the prior position, and need a fresh wheel to register progress.
		var sut = CreateMixedTemplateSut(itemCount: 150, viewport: new Size(360, 600));
		await LoadAsync(sut);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		var bounds = sut.Scroller.GetAbsoluteBounds();
		mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		// Each iteration: one wheel tick, wait for full settle, capture offset. Assert each tick
		// produced forward motion. We allow tiny floating-point noise (≤ 1 px) but anything bigger
		// is the visible "snap-back" the user reports.
		const int Ticks = 6;
		var offsetsAfterEachTick = new List<double> { sut.Scroller.VerticalOffset };
		for (var i = 0; i < Ticks; i++)
		{
			var before = sut.Scroller.VerticalOffset;
			mouse.WheelDown();
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
			var after = sut.Scroller.VerticalOffset;
			offsetsAfterEachTick.Add(after);

			(after - before).Should().BeGreaterThan(0.5,
				$"Single wheel-down tick #{i + 1} on multi-template content must advance the offset, "
				+ $"but went from {before:F2} to {after:F2}. "
				+ $"All recorded offsets: [{string.Join(", ", offsetsAfterEachTick.Select(o => o.ToString("F2")))}].");
		}
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#elif __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_SlowWheelOnMultiTemplate_Then_ItemsDoNotJumpInIRLocalSpace()
	{
		// Stronger reproducer for the "jumps while scrolling slowly" symptom: tracks each
		// realized item's IR-local position across a sequence of wheel ticks. The user-reported
		// flicker is *items repositioning within the IR even though the user's VerticalOffset
		// advances monotonically* — caused by StackLayout.GetExtent recomputing extent.X
		// (layout origin Y) as the running-average element size shifts when items enter/leave
		// the 100-slot estimation buffer. The relevant invariant: a single item's IR-local Y
		// must not change between consecutive measures unless the user actually scrolled past
		// the item entirely.
		var sut = CreateMixedTemplateSut(itemCount: 150, viewport: new Size(360, 600));
		await LoadAsync(sut);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		var bounds = sut.Scroller.GetAbsoluteBounds();
		mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		// For each item index we've ever seen, record its first-observed IR-local Y. Subsequent
		// observations must match (within 0.5 px) or we have a layout-origin shift between
		// measures, which the user observes as visible item jumping.
		var firstObservedY = new Dictionary<int, double>();
		var report = new List<string>();

		void Snapshot(int tickIndex)
		{
			for (var i = 0; i < sut.Source.Count; i++)
			{
				if (sut.Repeater.TryGetElement(i) is not FrameworkElement fe || fe.ActualHeight <= 0)
				{
					continue;
				}
				// IR-local Y, which is what the layout origin produces; converting via TransformToVisual(Repeater)
				// keeps the comparison platform-agnostic.
				var y = fe.TransformToVisual(sut.Repeater).TransformPoint(new Point(0, 0)).Y;
				if (firstObservedY.TryGetValue(i, out var prevY))
				{
					if (Math.Abs(prevY - y) > 0.5)
					{
						report.Add($"tick #{tickIndex}: item[{i}] IR-local Y changed from {prevY:F2} to {y:F2}");
					}
				}
				else
				{
					firstObservedY[i] = y;
				}
			}
		}

		Snapshot(tickIndex: 0);
		const int Ticks = 8;
		for (var i = 0; i < Ticks; i++)
		{
			mouse.WheelDown();
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
			Snapshot(tickIndex: i + 1);
		}

		// Report-driven assertion: build a single error message containing every observed item
		// jump so a failure points at the exact items + values that moved.
		report.Should().BeEmpty(
			"No item should change its IR-local Y between successive wheel ticks (the user perceives that as the list jumping). "
			+ $"Captured discrepancies:{Environment.NewLine}{string.Join(Environment.NewLine, report)}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_ScrollBarThumbDragged_Then_OffsetTracksRequestMonotonically()
	{
		// Reproduces the "scrollbar drag becomes unresponsive" symptom reported on the studio.live
		// multi-template subagent markdown UI. The scrollbar drag fires Scroll events with the
		// requested offset; the ScrollViewer must apply each in order without snapping back.
		// We simulate the drag at the SCV level by firing the same ChangeViewCore the
		// scrollbar drag handler uses (line 1254 of ScrollViewer.cs), incrementing the offset by
		// small steps the way the user moves the thumb pixel-by-pixel.
		var sut = CreateMixedTemplateSut(itemCount: 150, viewport: new Size(360, 600));
		await LoadAsync(sut);

		// Simulate progressive scrollbar-thumb drag down by 20 px each step (typical thumb tick).
		// The scrollbar drag handler in ScrollViewer.OnVerticalScrollBarScrolled calls
		// ChangeViewCore with `immediate=true` (matching the e.NewValue branch). Each request
		// must land at the requested offset — not snap back to the previous one or skip ahead.
		const int Steps = 20;
		const double Step = 20.0;
		var observed = new List<double> { sut.Scroller.VerticalOffset };
		for (var i = 1; i <= Steps; i++)
		{
			var target = i * Step;
			sut.Scroller.ChangeView(null, target, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
			observed.Add(sut.Scroller.VerticalOffset);
			(observed[i] - observed[i - 1]).Should().BeGreaterThan(0.5,
				$"Scrollbar drag step #{i} (requested {target:F2}) must advance the offset from "
				+ $"{observed[i - 1]:F2}, but landed at {observed[i]:F2} — the user perceives this as "
				+ $"the scrollbar 'becoming unresponsive'. "
				+ $"All recorded offsets: [{string.Join(", ", observed.Select(o => o.ToString("F2")))}].");
		}
	}

	// ----- helpers -----

	// Mixed-template sample SUT: mimics the studio.live multi-template subagent markdown UI's
	// shape — 5 template types of varying heights cycling through 13 indices. Used by the
	// small-wheel and scrollbar-drag regression tests.
	private static SutHandle CreateMixedTemplateSut(int itemCount, Size viewport)
	{
		var items = Enumerable.Range(0, itemCount)
			.Select(i => new ItemModel(i, MixedTemplateHeight(i), ColorForIndex(i)))
			.ToArray();
		return CreateSut(items, viewport);
	}

	private static double MixedTemplateHeight(int index) => (index % 13) switch
	{
		0 or 1 or 3 or 4 or 6 or 8 or 10 or 11 => 32,  // StatusRow
		2 or 9 => 120,                                  // ContentCard
		5 => 200,                                       // DetailSection
		7 => 80,                                        // InputBubble
		12 => 60,                                       // Banner
		_ => 32,
	};

	private static SutHandle CreateHighVarianceSut(int itemCount, Size viewport)
	{
		var items = Enumerable.Range(0, itemCount)
			.Select(i => new ItemModel(i, (i % 10 == 3) ? 1200 : 40, ColorForIndex(i)))
			.ToArray();
		return CreateSut(items, viewport);
	}

	private static SutHandle CreateHighVarianceHorizontalSut(int itemCount, Size viewport)
	{
		var items = Enumerable.Range(0, itemCount)
			.Select(i => new ItemModel(i, (i % 10 == 3) ? 1200 : 40, ColorForIndex(i)))
			.ToArray();

		var source = new ObservableCollection<ItemModel>(items);
		var template = (DataTemplate)XamlReader.Load(ItemTemplateHorizontalXaml);

		ItemsRepeater repeater = new()
		{
			ItemsSource = source,
			Layout = new StackLayout { Orientation = Orientation.Horizontal },
			ItemTemplate = template,
			// Horizontal mirror of the chat-style layout anchored to the trailing edge.
			HorizontalAlignment = HorizontalAlignment.Right,
		};

		ScrollViewer scroller = new()
		{
			Width = viewport.Width,
			Height = viewport.Height,
			Content = repeater,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
		};

		return new SutHandle(scroller, repeater, source);
	}

	private static SutHandle CreateSut(IReadOnlyList<ItemModel> items, Size viewport)
	{
		var source = new ObservableCollection<ItemModel>(items);

		var template = (DataTemplate)XamlReader.Load(ItemTemplateXaml);

		ItemsRepeater repeater = new()
		{
			ItemsSource = source,
			Layout = new StackLayout { Orientation = Orientation.Vertical },
			ItemTemplate = template,
			// Matches the chat-style layout that triggers the real-world bug: when short content
			// is anchored to the bottom, the realization window evolution during scroll-up is what
			// exercises the StackLayout.GetExtent clamp path.
			VerticalAlignment = VerticalAlignment.Bottom,
		};

		ScrollViewer scroller = new()
		{
			Width = viewport.Width,
			Height = viewport.Height,
			Content = repeater,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
		};

		return new SutHandle(scroller, repeater, source);
	}

	private static async Task LoadAsync(SutHandle sut)
	{
		TestServices.WindowHelper.WindowContent = sut.Scroller;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(sut.Repeater);
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static async Task ScrollInStepsAsync(SutHandle sut, double targetOffset, int steps = 6)
	{
		var current = sut.Scroller.VerticalOffset;
		for (var i = 1; i <= steps; i++)
		{
			var next = current + (targetOffset - current) * i / steps;
			sut.Scroller.ChangeView(null, next, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
			sut.Repeater.UpdateLayout();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	private static IEnumerable<FrameworkElement> EnumerateRepeaterChildren(ItemsRepeater repeater)
	{
		// Enumerate via VisualTreeHelper (captures all rendered children) AND TryGetElement
		// (captures materialized-but-possibly-recycled elements). Deduplicate by reference.
		var seen = new HashSet<FrameworkElement>();
		var vtCount = VisualTreeHelper.GetChildrenCount(repeater);
		for (var i = 0; i < vtCount; i++)
		{
			if (VisualTreeHelper.GetChild(repeater, i) is FrameworkElement fe && seen.Add(fe))
			{
				yield return fe;
			}
		}
		if (repeater.ItemsSource is System.Collections.IList list)
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (repeater.TryGetElement(i) is FrameworkElement fe && seen.Add(fe))
				{
					yield return fe;
				}
			}
		}
	}

	private static FrameworkElement? FindMaterializedElementForIndex(SutHandle sut, int index)
	{
		var targetId = sut.Source[index].Id;
		return EnumerateRepeaterChildren(sut.Repeater)
			.FirstOrDefault(c => c.DataContext is ItemModel m && m.Id == targetId);
	}

	private static void AssertNoOverlap(SutHandle sut)
	{
		// ItemsRepeater parks recycled/unrealized elements at large negative offsets (Y ≈ -10000)
		// as a hide trick. Filter those out before checking overlap.
		var laidOut = EnumerateRepeaterChildren(sut.Repeater)
			.Where(c => c.ActualHeight > 0 && c.ActualOffset.Y > -1000)
			.Select(c => (Top: c.ActualOffset.Y, Bottom: c.ActualOffset.Y + c.ActualHeight))
			.OrderBy(t => t.Top)
			.ToArray();

		for (var i = 1; i < laidOut.Length; i++)
		{
			var prev = laidOut[i - 1];
			var curr = laidOut[i];
			(curr.Top - prev.Bottom).Should().BeGreaterThanOrEqualTo(-OverlapTolerance,
				$"Items must not overlap, but item@{curr.Top:F2} overlaps previous item bottom@{prev.Bottom:F2}");
		}
	}

	private static void AssertNoOverlapHorizontal(SutHandle sut)
	{
		var laidOut = EnumerateRepeaterChildren(sut.Repeater)
			.Where(c => c.ActualWidth > 0 && c.ActualOffset.X > -1000)
			.Select(c => (Left: c.ActualOffset.X, Right: c.ActualOffset.X + c.ActualWidth))
			.OrderBy(t => t.Left)
			.ToArray();

		for (var i = 1; i < laidOut.Length; i++)
		{
			var prev = laidOut[i - 1];
			var curr = laidOut[i];
			(curr.Left - prev.Right).Should().BeGreaterThanOrEqualTo(-OverlapTolerance,
				$"Items must not overlap, but item@{curr.Left:F2} overlaps previous item right@{prev.Right:F2}");
		}
	}

	private static void AssertAllMaterializedChildrenHaveFiniteOffsets(SutHandle sut)
	{
		foreach (var child in EnumerateRepeaterChildren(sut.Repeater))
		{
			if (child.ActualHeight <= 0)
			{
				continue;
			}

			double.IsFinite(child.ActualOffset.Y).Should().BeTrue(
				"Materialized child must have a finite Y offset");
			double.IsNaN(child.ActualHeight).Should().BeFalse(
				"Materialized child must have a finite height");
		}
	}

	private static Color ColorForIndex(int i)
	{
		var palette = new[]
		{
			Color.FromArgb(0xFF, 0xE5, 0x39, 0x35),
			Color.FromArgb(0xFF, 0xFB, 0x8C, 0x00),
			Color.FromArgb(0xFF, 0xFD, 0xD8, 0x35),
			Color.FromArgb(0xFF, 0x43, 0xA0, 0x47),
			Color.FromArgb(0xFF, 0x1E, 0x88, 0xE5),
			Color.FromArgb(0xFF, 0x5E, 0x35, 0xB1),
			Color.FromArgb(0xFF, 0xD8, 0x1B, 0x60),
		};
		return palette[i % palette.Length];
	}

	private sealed class ItemModel : INotifyPropertyChanged
	{
		private double _height;

		public ItemModel(int id, double height, Color background)
		{
			Id = id;
			_height = height;
			Background = new SolidColorBrush(background);
		}

		public int Id { get; }

		public Brush Background { get; }

		public double Height
		{
			get => _height;
			set
			{
				if (_height != value)
				{
					_height = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}

	private sealed record SutHandle(ScrollViewer Scroller, ItemsRepeater Repeater, ObservableCollection<ItemModel> Source);
}
