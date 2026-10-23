#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Accessibility of scrollers. On WASM: the <c>role=region</c> gate (T042, US5, FR-013/FR-014: only a
/// scrollable AND named ScrollViewer is a region, and <c>aria-roledescription</c> is never emitted
/// without a name), and the semantic scroll port mirroring the scroller's geometry (extent, offsets,
/// zoom, child positions) in both directions. On Skia: the scroll pattern automation events.
/// </summary>
[TestClass]
public class Given_AccessibleScrollViewer
{
#if HAS_UNO
	[TestCleanup]
	public void DisableAccessibilityAfterTest() => DisableAccessibility();
#endif

#if HAS_UNO
	/// <summary>
	/// A non-scrollable ScrollViewer (content fits the viewport) must NOT get role=region, even when
	/// it is named — there is nothing to scroll, so it is not a meaningful landmark (FR-013).
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Not_Scrollable_Then_No_Region()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			// Content smaller than the viewport → nothing to scroll.
			Content = new Border { Width = 50, Height = 50 },
		};
		AutomationProperties.SetName(scrollViewer, "Fits Content");

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A non-scrollable ScrollViewer must NOT be exposed as role=region (FR-013).");
	}

	/// <summary>
	/// A scrollable, named ScrollViewer earns role=region and carries its accessible name as aria-label
	/// (FR-013/FR-014). This is the positive case ensuring the gate is not vacuously over-broad.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Scrollable_And_Named_Then_Labeled_Region()
	{
		const string name = "Article Body";
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			// Content much taller than the viewport → vertically scrollable.
			Content = new Border { Width = 50, Height = 2000 },
		};
		AutomationProperties.SetName(scrollViewer, name);

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A scrollable, named ScrollViewer must be exposed as role=region (FR-013).");
		Assert.AreEqual(name, GetSemanticAttribute(scrollViewer, "aria-label"),
			"A region's accessible name must be emitted as aria-label (FR-014).");
	}

	/// <summary>
	/// A scrollable but UNNAMED ScrollViewer must NOT get role=region — an unlabeled region is an axe
	/// "region must have a name" violation (FR-014). It still appears in the tree (as a plain div), but
	/// without the region role.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Scrollable_But_Unnamed_Then_No_Region()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			// Scrollable, but no AutomationProperties.Name / labeled content.
			Content = new Border { Width = 50, Height = 2000 },
		};

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A scrollable but unnamed ScrollViewer must NOT be exposed as an unlabeled role=region (FR-014).");
	}

	/// <summary>
	/// FR-014 invariant: aria-roledescription is not a substitute for an accessible name. An unnamed
	/// ScrollViewer (region candidate) must never emit aria-roledescription.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Unnamed_Then_No_RoleDescription_Without_Name()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new Border { Width = 50, Height = 2000 },
		};

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(string.Empty, GetSemanticAttribute(scrollViewer, "aria-roledescription"),
			"aria-roledescription must never be emitted on an element with no accessible name (FR-014).");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Navigation_ScrollViewer_Resized_Then_Role_Stays_Navigation()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new Border { Width = 50, Height = 2000 },
		};
		AutomationProperties.SetName(scrollViewer, "Site Navigation");
		AutomationProperties.SetLandmarkType(scrollViewer, AutomationLandmarkType.Navigation);

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		// A resize re-runs the post-layout scroll-region check. The first one also raises the peer's first
		// automatic Name change, which re-resolves the role, so only a later one exercises the check alone.
		scrollViewer.Height = 250;
		await UITestHelper.WaitForIdle();
		scrollViewer.Height = 300;
		await UITestHelper.WaitForIdle();

		Assert.AreEqual("navigation", GetSemanticAttribute(scrollViewer, "role"),
			"The authored landmark must win over the scroll region role after layout.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Whitespace_Named_ScrollViewer_Resized_Then_No_Region()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new Border { Width = 50, Height = 2000 },
		};
		AutomationProperties.SetName(scrollViewer, "   ");

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		// The second resize is the one that runs the post-layout check without a Name change (see above).
		scrollViewer.Height = 250;
		await UITestHelper.WaitForIdle();
		scrollViewer.Height = 300;
		await UITestHelper.WaitForIdle();

		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A whitespace-only name does not name the ScrollViewer, so it must not become a region after layout.");
	}

	/// <summary>
	/// Mirroring XAML focus into the semantic DOM must not let the browser scroll the semantic scroller
	/// to reveal the node, as that scroll is fed back into the ScrollViewer. Programmatic focus does not
	/// bring an element into view.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Programmatic_Focus_Outside_Viewport_Then_Not_Scrolled()
	{
		await EnsureAccessibilityEnabledAsync();

		var target = new Button { Content = "Target" };
		var scrollViewer = new ScrollViewer
		{
			Height = 200,
			Content = new StackPanel
			{
				Children =
				{
					new Button { Content = "Top" },
					new Border { Height = 2000 },
					target,
				},
			},
		};

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitFor(() => SemanticElementExists(target), timeoutMS: 5000,
			message: "Timed out waiting for the target's semantic node.");

		target.Focus(FocusState.Programmatic);

		// A browser-driven scroll can land a few frames after Focus(), so keep watching.
		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < 500)
		{
			var semanticScrollTop = InvokeBrowserJs($"(function(){{let e = document.getElementById('{GetSemanticElementId(target)}'); let top = 0; while (e) {{ top = Math.max(top, e.scrollTop); e = e.parentElement; }} return String(top); }})()");
			Assert.AreEqual("0", semanticScrollTop, "The browser scrolled a semantic ancestor to reveal the focused node.");
			await Task.Delay(50);
		}

		await UITestHelper.WaitForIdle();

		Assert.AreEqual(0, scrollViewer.VerticalOffset);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Semantic_Children_Are_Sparse_Then_Native_Scroll_Range_Matches_Extent()
	{
		// Only the top of the content has a semantic node, so the DOM children alone would give
		// the semantic scroll port a much smaller range than the managed ScrollViewer.
		var filler = new Border { Height = 1200 };
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new StackPanel { Children = { new TextBlock { Text = "Top" }, filler } },
		};

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(presenter), timeoutMS: 5000,
			message: "Timed out waiting for the semantic scroll port.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(scrollViewer.ScrollableHeight, GetNativeScrollRange(presenter), 1);

		scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
		await UITestHelper.WaitForIdle();

		await AssertHoldsForFramesAsync(() => Math.Abs(scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset) < 1,
			"The browser must not clamp the managed offset.");
		Assert.AreEqual(scrollViewer.VerticalOffset, GetNativeScrollTop(presenter), 1);

		filler.Height = 2000;
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.ScrollableHeight - GetNativeScrollRange(presenter)) < 1, timeoutMS: 5000,
			message: "Extent changes without an offset change must update the native range.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Zoomed_Then_Native_Scroll_Port_Matches()
	{
		var target = new Button { Content = "Target", Height = 40 };
		AutomationProperties.SetName(target, "Target");
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			Content = new StackPanel { Children = { new Border { Height = 400 }, target, new Border { Height = 560 } } },
		};

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(target), timeoutMS: 5000,
			message: "Timed out waiting for the target's semantic node.");

		scrollViewer.ChangeView(null, null, 2f, disableAnimation: true);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.ZoomFactor - 2) < 0.01, timeoutMS: 5000,
			message: $"ScrollViewer did not zoom (zoom={scrollViewer.ZoomFactor}).");
		await UITestHelper.WaitForIdle();

		// 1000px of content at zoom 2 in a 200px viewport.
		Assert.AreEqual(1800, scrollViewer.ScrollableHeight, 1);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.ScrollableHeight - GetNativeScrollRange(presenter)) < 1, timeoutMS: 5000,
			message: $"The native range must follow the zoomed extent (native={GetNativeScrollRange(presenter)}).");

		scrollViewer.ChangeView(null, 1800, null, disableAnimation: true);
		await UITestHelper.WaitForIdle();

		await AssertHoldsForFramesAsync(() => Math.Abs(scrollViewer.VerticalOffset - 1800) < 1,
			"The browser must not clamp the zoomed managed offset.");
		Assert.AreEqual(1800, GetNativeScrollTop(presenter), 1);
		Assert.AreEqual(800, GetSemanticOffsetTop(target), 1, "Semantic children must be positioned in zoomed content coordinates.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollContentPresenter_Has_No_Owner_Then_Semantic_Port_Created()
	{
		// A presenter whose ScrollOwner was never assigned (no templated ScrollViewer) is still a scroll port.
		var presenter = new ScrollContentPresenter { Width = 200, Height = 200, Content = new Border { Height = 800 } };

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(presenter);

		await UITestHelper.WaitFor(() => SemanticElementExists(presenter), timeoutMS: 5000,
			message: "Timed out waiting for the semantic scroll port.");
		Assert.IsNull(presenter.ScrollOwner);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Axis_Not_Scrollable_Then_Browser_Cannot_Scroll_It()
	{
		// The content fits vertically, but a child drawn below it gives the native scroller a vertical range.
		var outlier = new Border { Width = 20, Height = 20 };
		AutomationProperties.SetName(outlier, "Outlier");
		Canvas.SetTop(outlier, 1000);
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = new Canvas { Width = 600, Height = 150, Children = { outlier } },
		};

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(outlier), timeoutMS: 5000,
			message: "Timed out waiting for the outlier's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual("scroll", GetSemanticStyle(presenter, "overflowX"));
		Assert.AreEqual("hidden", GetSemanticStyle(presenter, "overflowY"));

		SetNativeScrollLeft(presenter, 80);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.HorizontalOffset - 80) < 1, timeoutMS: 5000,
			message: $"ScrollViewer did not follow the browser (offset={scrollViewer.HorizontalOffset}).");

		// overflow: hidden still lets script (and scrollIntoView) move the offset.
		SetNativeScrollTop(presenter, 50);
		await UITestHelper.WaitFor(() => GetNativeScrollTop(presenter) == 0, timeoutMS: 5000,
			message: "The browser must be brought back to the managed offset on a non-scrollable axis.");
		Assert.AreEqual(0, scrollViewer.VerticalOffset);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollView_Scroll_Animated_Then_Animation_Completes()
	{
		var scrollView = new ScrollView { Width = 200, Height = 200, Content = new Border { Height = 2000 } };

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollView);
		var presenter = scrollView.ScrollPresenter;
		await UITestHelper.WaitFor(() => SemanticElementExists(presenter), timeoutMS: 5000,
			message: "Timed out waiting for the semantic scroll port.");
		await UITestHelper.WaitForIdle();

		// Every intermediate offset is mirrored to the browser; its scroll echo must not be fed back
		// as a new (non-animated) scroll, which would stop the animation midway.
		scrollView.ScrollTo(0, 1500, new ScrollingScrollOptions(ScrollingAnimationMode.Enabled));
		await UITestHelper.WaitFor(() => Math.Abs(scrollView.VerticalOffset - 1500) < 1, timeoutMS: 5000,
			message: $"The animated scroll did not complete (offset={scrollView.VerticalOffset}).");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(1500, GetNativeScrollTop(presenter), 1);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Scrolled_Then_Semantic_Children_Stay_In_Content_Coordinates()
	{
		var content = new StackPanel();
		for (var i = 0; i < 20; i++)
		{
			var button = new Button { Content = $"Item {i}", Height = 40 };
			AutomationProperties.SetName(button, $"Item {i}");
			content.Children.Add(button);
		}
		var target = (Button)content.Children[10];
		var scrollViewer = new ScrollViewer { Width = 200, Height = 200, Content = content };

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(target), timeoutMS: 5000,
			message: "Timed out waiting for the target's semantic node.");
		await UITestHelper.WaitForIdle();

		scrollViewer.ChangeView(null, 300, null, disableAnimation: true);
		await UITestHelper.WaitForIdle();

		// The native scroller applies the offset once; the child itself keeps its content position.
		Assert.AreEqual(300, GetNativeScrollTop(presenter), 1);
		Assert.AreEqual(400, GetSemanticOffsetTop(target), 1);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Browser_Scrolls_Semantic_Port_Then_ScrollViewer_Follows()
	{
		var scrollViewer = new ScrollViewer { Width = 200, Height = 200, Content = new Border { Height = 800 } };

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(presenter), timeoutMS: 5000,
			message: "Timed out waiting for the semantic scroll port.");
		await UITestHelper.WaitForIdle();

		SetNativeScrollTop(presenter, 80);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.VerticalOffset - 80) < 1, timeoutMS: 5000,
			message: $"ScrollViewer did not follow the browser (offset={scrollViewer.VerticalOffset}).");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Browser_Scrolls_Back_To_Last_Synced_Offset_Then_ScrollViewer_Follows()
	{
		var scrollViewer = new ScrollViewer { Width = 200, Height = 200, Content = new Border { Height = 800 } };

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(presenter), timeoutMS: 5000,
			message: "Timed out waiting for the semantic scroll port.");
		await UITestHelper.WaitForIdle();

		// The managed sync to 300 raises one echo, which must be ignored...
		scrollViewer.ChangeView(null, 300, null, disableAnimation: true);
		await UITestHelper.WaitFor(() => Math.Abs(GetNativeScrollTop(presenter) - 300) < 1, timeoutMS: 5000,
			message: "The managed offset was not mirrored to the browser.");
		await AssertHoldsForFramesAsync(() => Math.Abs(scrollViewer.VerticalOffset - 300) < 1, "The echo of the managed sync moved the ScrollViewer.");

		SetNativeScrollTop(presenter, 100);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.VerticalOffset - 100) < 1, timeoutMS: 5000,
			message: $"ScrollViewer did not follow the browser (offset={scrollViewer.VerticalOffset}).");

		// ...but a real browser scroll back to that same position must still reach managed code.
		SetNativeScrollTop(presenter, 300);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.VerticalOffset - 300) < 1, timeoutMS: 5000,
			message: $"A browser scroll back to the last synced offset was dropped (offset={scrollViewer.VerticalOffset}).");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Semantic_Child_Overhangs_Content_Then_Browser_Scroll_Is_Clamped_To_ScrollViewer()
	{
		// A Canvas child drawn below the Canvas bounds extends the native range past the managed one.
		var outlier = new Border { Width = 20, Height = 20 };
		AutomationProperties.SetName(outlier, "Outlier");
		Canvas.SetTop(outlier, 1800);
		var scrollViewer = new ScrollViewer { Width = 200, Height = 200, Content = new Canvas { Height = 800, Children = { outlier } } };

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollViewer);
		var presenter = (ScrollContentPresenter)scrollViewer.Presenter!;
		await UITestHelper.WaitFor(() => SemanticElementExists(outlier), timeoutMS: 5000,
			message: "Timed out waiting for the outlier's semantic node.");
		await UITestHelper.WaitForIdle();

		SetNativeScrollTop(presenter, 1500);
		await UITestHelper.WaitFor(() => Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) < 1, timeoutMS: 5000,
			message: $"ScrollViewer did not follow the browser (offset={scrollViewer.VerticalOffset}).");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(scrollViewer.VerticalOffset, GetNativeScrollTop(presenter), 1,
			"The browser must be brought back to the managed (clamped) offset.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollView_Scrolls_Then_Native_Scroll_Port_Matches()
	{
		var filler = new Border { Height = 1200 };
		var scrollView = new ScrollView
		{
			Width = 200,
			Height = 200,
			Content = new StackPanel { Children = { new TextBlock { Text = "Top" }, filler } },
		};

		await EnsureAccessibilityEnabledAsync();
		await UITestHelper.Load(scrollView);
		var presenter = scrollView.ScrollPresenter;
		await UITestHelper.WaitFor(() => SemanticElementExists(presenter), timeoutMS: 5000,
			message: "Timed out waiting for the semantic scroll port.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(presenter.ScrollableHeight, GetNativeScrollRange(presenter), 1);

		var target = presenter.ScrollableHeight;
		scrollView.ScrollTo(0, target, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
		await UITestHelper.WaitFor(() => Math.Abs(scrollView.VerticalOffset - target) < 1, timeoutMS: 5000,
			message: $"ScrollView did not scroll (offset={scrollView.VerticalOffset}, target={target}).");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(scrollView.VerticalOffset, GetNativeScrollTop(presenter), 1);

		SetNativeScrollTop(presenter, 100);
		await UITestHelper.WaitFor(() => Math.Abs(scrollView.VerticalOffset - 100) < 1, timeoutMS: 5000,
			message: $"ScrollView did not follow the browser (offset={scrollView.VerticalOffset}).");
	}

	private static double GetNativeScrollRange(UIElement element)
		=> double.Parse(InvokeBrowserJs($"(function(){{ const e = document.getElementById('{GetSemanticElementId(element)}'); return String(e.scrollHeight - e.clientHeight); }})()"), CultureInfo.InvariantCulture);

	private static double GetNativeScrollTop(UIElement element)
		=> double.Parse(InvokeBrowserJs($"String(document.getElementById('{GetSemanticElementId(element)}').scrollTop)"), CultureInfo.InvariantCulture);

	private static double GetSemanticOffsetTop(UIElement element)
		=> double.Parse(InvokeBrowserJs($"String(document.getElementById('{GetSemanticElementId(element)}').offsetTop)"), CultureInfo.InvariantCulture);

	private static void SetNativeScrollTop(UIElement element, double value)
		=> InvokeBrowserJs($"(function(){{ document.getElementById('{GetSemanticElementId(element)}').scrollTop = {value.ToString(CultureInfo.InvariantCulture)}; return ''; }})()");

	private static void SetNativeScrollLeft(UIElement element, double value)
		=> InvokeBrowserJs($"(function(){{ document.getElementById('{GetSemanticElementId(element)}').scrollLeft = {value.ToString(CultureInfo.InvariantCulture)}; return ''; }})()");

	private static string GetSemanticStyle(UIElement element, string property)
		=> InvokeBrowserJs($"String(document.getElementById('{GetSemanticElementId(element)}').style.{property})");

	// The browser dispatches scroll events once per frame, so an echo shows up within a few frames.
	private static async Task AssertHoldsForFramesAsync(Func<bool> condition, string message)
	{
		for (var i = 0; i < 5; i++)
		{
			await UITestHelper.WaitForRender();
			Assert.IsTrue(condition(), message);
		}
	}
#endif

#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Extent_Changes_Then_ScrollPattern_Events_Raised()
	{
		var filler = new Border { Height = 100 };
		var scrollViewer = new ScrollViewer { Width = 200, Height = 200, Content = filler };
		await UITestHelper.Load(scrollViewer);

		var listener = new ScrollPatternListener();
		try
		{
			AutomationPeer.TestAutomationPeerListener = listener;

			filler.Height = 1000;
			await UITestHelper.WaitForIdle();

			// Only the extent changed, not the offset.
			CollectionAssert.Contains(listener.Properties, ScrollPatternIdentifiers.VerticallyScrollableProperty);
			CollectionAssert.Contains(listener.Properties, ScrollPatternIdentifiers.VerticalViewSizeProperty);
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
		}
	}

	private class ScrollPatternListener : IAutomationPeerListener
	{
		public List<AutomationProperty> Properties { get; } = new();

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
		{
			if (peer is ScrollViewerAutomationPeer)
			{
				Properties.Add(automationProperty);
			}
		}
		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
		public void NotifyInvalidatePeer(AutomationPeer peer) { }
		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
	}
#endif
}
