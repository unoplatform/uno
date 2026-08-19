using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ScrollViewerTests;

/// <summary>
/// Covers <see cref="ScrollViewer.ChainScrollFromDescendant"/>, the entry point used to hand a scroll delta
/// that Uno's pointer pipeline never saw (today: a native HTML element on Skia WebAssembly which exhausted
/// its own scrolling) to the ScrollViewer ancestry of the element it originated from.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ScrollViewer_ScrollChaining
{
#if HAS_UNO && UNO_HAS_MANAGED_SCROLL_PRESENTER
	private const double ViewportSize = 200;
	private const double InnerContentHeight = 500;
	private const double OuterContentHeight = 2000;

	// A chained delta is reported as intermediate, so the presenter commits its own offsets synchronously
	// while the ScrollViewer's public HorizontalOffset/VerticalOffset are refreshed through RequestUpdate.
	// Every test therefore awaits idle before reading the ScrollViewer's offsets.
	private static (ScrollViewer outer, ScrollViewer inner, Border origin) BuildNested()
	{
		var origin = new Border
		{
			Height = InnerContentHeight,
			Width = 100,
			Background = new SolidColorBrush(Colors.LightCoral),
		};

		var inner = new ScrollViewer
		{
			Height = ViewportSize,
			Width = 150,
			Content = origin,
		};

		var outerContent = new StackPanel();
		outerContent.Children.Add(inner);
		outerContent.Children.Add(new Border
		{
			Height = OuterContentHeight,
			Background = new SolidColorBrush(Colors.LightBlue),
		});

		var outer = new ScrollViewer
		{
			Height = ViewportSize,
			Width = 200,
			Content = outerContent,
		};

		return (outer, inner, origin);
	}

	[TestMethod]
	public async Task When_Inner_Can_Scroll_Then_Outer_Is_Untouched()
	{
		var (outer, inner, origin) = BuildNested();
		await UITestHelper.Load(outer);

		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, 50, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(result.DidScroll);
		Assert.AreEqual(0, result.RemainingVerticalDelta, 0.01);
		Assert.AreEqual(50, inner.VerticalOffset, 0.01);
		Assert.AreEqual(0, outer.VerticalOffset, 0.01);
	}

	[TestMethod]
	public async Task When_Inner_Reaches_Boundary_Then_Residual_Chains_To_Outer()
	{
		var (outer, inner, origin) = BuildNested();
		await UITestHelper.Load(outer);

		var scrollableHeight = inner.ScrollableHeight;
		Assert.IsTrue(scrollableHeight > 0, "The inner ScrollViewer must be scrollable for this test to mean anything.");

		// Ask for more than the inner one can possibly consume: it should take its full range and the
		// leftover should end up on the outer one, in a single call.
		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, scrollableHeight + 80, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(result.DidScroll);
		Assert.AreEqual(0, result.RemainingVerticalDelta, 0.01);
		Assert.AreEqual(scrollableHeight, inner.VerticalOffset, 0.01);
		Assert.AreEqual(80, outer.VerticalOffset, 0.01);
	}

	[TestMethod]
	public async Task When_Chaining_Backwards_Then_Residual_Chains_To_Outer()
	{
		var (outer, inner, origin) = BuildNested();
		await UITestHelper.Load(outer);

		ScrollViewer.ChainScrollFromDescendant(origin, 0, inner.ScrollableHeight + 200, isIntermediate: true);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(200, outer.VerticalOffset, 0.01);

		// Dragging the other way empties the inner one first, then pulls the outer one back up.
		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, -(inner.ScrollableHeight + 150), isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(result.DidScroll);
		Assert.AreEqual(0, result.RemainingVerticalDelta, 0.01);
		Assert.AreEqual(0, inner.VerticalOffset, 0.01);
		Assert.AreEqual(50, outer.VerticalOffset, 0.01);
	}

	[TestMethod]
	public async Task When_Inner_Scrolling_Is_Disabled_Then_Outer_Consumes_Everything()
	{
		var (outer, inner, origin) = BuildNested();
		inner.VerticalScrollMode = ScrollMode.Disabled;
		inner.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		await UITestHelper.Load(outer);

		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, 60, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(result.DidScroll);
		Assert.AreEqual(0, inner.VerticalOffset, 0.01);
		Assert.AreEqual(60, outer.VerticalOffset, 0.01);
	}

	[TestMethod]
	public async Task When_Nothing_Can_Consume_Then_Delta_Is_Returned_As_Residual()
	{
		var (outer, inner, origin) = BuildNested();
		await UITestHelper.Load(outer);

		// Already at the top, so a backwards drag has nowhere to go on either ScrollViewer.
		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, -40, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(result.DidScroll);
		Assert.AreEqual(-40, result.RemainingVerticalDelta, 0.01);
	}

	[TestMethod]
	public async Task When_Chained_Past_Boundary_Then_Growing_Extent_Does_Not_Jump()
	{
		// Regression guard: chaining used to drive the ancestry through ChangeView, which arms the
		// ScrollViewer's offset intent with the *requested* offset. Overshooting at a boundary - which a
		// chained drag or fling does constantly - therefore left an intent parked past the end of the
		// content, and the next extent growth (virtualization realizing more items, an image loading, ...)
		// made RecomputeOffsetsFromIntent re-apply it and jump the view away from where the user let go.
		var origin = new Border { Height = 100, Background = new SolidColorBrush(Colors.LightCoral) };
		var spacer = new Border { Height = 600, Background = new SolidColorBrush(Colors.LightBlue) };
		var content = new StackPanel();
		content.Children.Add(origin);
		content.Children.Add(spacer);

		var outer = new ScrollViewer { Height = ViewportSize, Width = 200, Content = content };
		await UITestHelper.Load(outer);

		var scrollableHeight = outer.ScrollableHeight;
		Assert.IsTrue(scrollableHeight > 0, "The ScrollViewer must be scrollable for this test to mean anything.");

		// Overshoot the bottom boundary by a wide margin, as a fling does.
		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, scrollableHeight + 300, isIntermediate: true);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(scrollableHeight, outer.VerticalOffset, 1);
		Assert.AreEqual(300, result.RemainingVerticalDelta, 1, "The unconsumed overshoot must be reported back as residual.");

		// Grow the content so the scrollable range now covers the offset that was overshot to.
		spacer.Height = 1000;
		outer.UpdateLayout();
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(outer.ScrollableHeight > scrollableHeight + 300, "The extent must grow past the overshoot for this test to mean anything.");
		Assert.AreEqual(scrollableHeight, outer.VerticalOffset, 1, "The view must stay where the chained scroll left it, not jump to the overshot offset.");
	}


	[TestMethod]
	public async Task When_Chaining_Is_Disabled_Then_Residual_Is_Absorbed()
	{
		// IsVerticalScrollChainingEnabled=false means this ScrollViewer is the end of the line: it takes what
		// it can and the rest must not reach its ancestors. FlipView relies on this for the horizontal axis.
		var (outer, inner, origin) = BuildNested();
		inner.IsVerticalScrollChainingEnabled = false;
		await UITestHelper.Load(outer);

		var scrollableHeight = inner.ScrollableHeight;
		var result = ScrollViewer.ChainScrollFromDescendant(origin, 0, scrollableHeight + 120, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(scrollableHeight, inner.VerticalOffset, 0.01);
		Assert.AreEqual(0, outer.VerticalOffset, 0.01, "The outer ScrollViewer must not be chained into when the inner one opts out.");
		Assert.AreEqual(0, result.RemainingVerticalDelta, 0.01, "The opted-out ScrollViewer absorbs the residual.");
	}

	[TestMethod]
	public async Task When_Horizontal_Then_Residual_Chains_To_Outer()
	{
		var origin = new Border { Width = 500, Height = 100, Background = new SolidColorBrush(Colors.LightCoral) };
		var inner = new ScrollViewer
		{
			Width = ViewportSize,
			Height = 150,
			HorizontalScrollMode = ScrollMode.Enabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollMode = ScrollMode.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = origin,
		};

		var outerContent = new StackPanel { Orientation = Orientation.Horizontal };
		outerContent.Children.Add(inner);
		outerContent.Children.Add(new Border { Width = 2000, Background = new SolidColorBrush(Colors.LightBlue) });

		var outer = new ScrollViewer
		{
			Width = ViewportSize,
			Height = 200,
			HorizontalScrollMode = ScrollMode.Enabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = outerContent,
		};

		await UITestHelper.Load(outer);

		var scrollableWidth = inner.ScrollableWidth;
		Assert.IsTrue(scrollableWidth > 0, "The inner ScrollViewer must be horizontally scrollable for this test to mean anything.");

		var result = ScrollViewer.ChainScrollFromDescendant(origin, scrollableWidth + 70, 0, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(result.DidScroll);
		Assert.AreEqual(scrollableWidth, inner.HorizontalOffset, 0.01);
		Assert.AreEqual(70, outer.HorizontalOffset, 0.01);
	}

	[TestMethod]
	[DataRow(double.NaN, 0d, DisplayName = "NaN horizontal")]
	[DataRow(0d, double.NaN, DisplayName = "NaN vertical")]
	[DataRow(double.PositiveInfinity, 0d, DisplayName = "Infinite horizontal")]
	[DataRow(0d, double.NegativeInfinity, DisplayName = "Infinite vertical")]
	public async Task When_Delta_Is_Not_Finite_Then_It_Is_Rejected(double horizontalDelta, double verticalDelta)
	{
		// The deltas come from JS, where nothing guarantees they are finite. A NaN reaching
		// ScrollContentPresenter.ValidateInputOffset throws.
		var (outer, inner, origin) = BuildNested();
		await UITestHelper.Load(outer);

		var result = ScrollViewer.ChainScrollFromDescendant(origin, horizontalDelta, verticalDelta, isIntermediate: true);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(result.DidScroll);
		Assert.AreEqual(0, inner.VerticalOffset, 0.01);
		Assert.AreEqual(0, outer.VerticalOffset, 0.01);
	}

	[TestMethod]
	public async Task When_Gesture_Is_Intermediate_Then_Final_ViewChanged_Is_Not_Intermediate()
	{
		// Consumers treat ViewChanged(IsIntermediate: false) as "the scroll ended", so a drag must report
		// intermediate deltas and exactly one terminal change when it completes.
		var (outer, _, origin) = BuildNested();
		await UITestHelper.Load(outer);

		var intermediateStates = new List<bool>();
		void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs args) => intermediateStates.Add(args.IsIntermediate);

		outer.ViewChanged += OnViewChanged;
		try
		{
			ScrollViewer.ChainScrollFromDescendant(origin, 0, 4000, isIntermediate: true);
			await WindowHelper.WaitForIdle();

			CollectionAssert.DoesNotContain(intermediateStates, false, "Every in-flight delta must be reported as intermediate.");

			ScrollViewer.CompleteChainedScrollFromDescendant(origin);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(intermediateStates.Contains(false), "Completing the gesture must raise a non-intermediate ViewChanged.");
		}
		finally
		{
			outer.ViewChanged -= OnViewChanged;
		}
	}
#endif
}
