#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ScrollViewerTests;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollViewer_OffsetIntent
{
	private const double ViewportHeight = 200;
	private const double ViewportWidth = 200;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23695")]
	public async Task When_ChangeView_BeyondExtent_Then_ExtentGrowth_Does_Not_Move_Offset()
	{
		// WinUI stores the requested offset already clamped to the scrollable height known at
		// request time (ScrollData::m_Offset, written through ValidateInputOffset in
		// ScrollContentPresenter::SetVerticalOffsetPrivate / SetOffsetsWithExtents). Later extent
		// growth therefore never pulls the offset further down.
		var content = new Border { Width = 180, Height = 400 };
		var sut = new ScrollViewer
		{
			Width = ViewportWidth,
			Height = ViewportHeight,
			Content = content,
		};

		await UITestHelper.Load(sut);

		var scrollableAtRequest = sut.ScrollableHeight;
		scrollableAtRequest.Should().BeApproximately(200, 1, "the 400px content in a 200px viewport must be 200px scrollable");

		sut.ChangeView(null, 10_000, null, disableAnimation: true);
		await WaitForOffsetAsync(sut, scrollableAtRequest);

		sut.VerticalOffset.Should().BeApproximately(scrollableAtRequest, 1,
			"a beyond-extent ChangeView must clamp to the live extent");

		// Content grows asynchronously, the way a virtualized ItemsRepeater's extent does while it
		// realizes more items.
		content.Height = 2000;
		sut.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		sut.ScrollableHeight.Should().BeApproximately(1800, 1, "the grown content must widen the scrollable range");
		sut.VerticalOffset.Should().BeApproximately(scrollableAtRequest, 1,
			$"the offset must stay where the beyond-extent ChangeView clamped it ({scrollableAtRequest:F2}) instead of "
			+ $"chasing the grown extent. Observed VerticalOffset={sut.VerticalOffset:F2}, ScrollableHeight={sut.ScrollableHeight:F2}.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23695")]
	public async Task When_ChangeView_BeyondExtent_Horizontal_Then_ExtentGrowth_Does_Not_Move_Offset()
	{
		var content = new Border { Width = 400, Height = 180 };
		var sut = new ScrollViewer
		{
			Width = ViewportWidth,
			Height = ViewportHeight,
			Content = content,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
			HorizontalScrollMode = ScrollMode.Enabled,
		};

		await UITestHelper.Load(sut);

		var scrollableAtRequest = sut.ScrollableWidth;
		scrollableAtRequest.Should().BeApproximately(200, 1);

		sut.ChangeView(10_000, null, null, disableAnimation: true);
		await WaitForOffsetAsync(sut, scrollableAtRequest, horizontal: true);

		sut.HorizontalOffset.Should().BeApproximately(scrollableAtRequest, 1);

		content.Width = 2000;
		sut.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		sut.HorizontalOffset.Should().BeApproximately(scrollableAtRequest, 1,
			$"the horizontal offset must not chase the grown extent. Observed HorizontalOffset={sut.HorizontalOffset:F2}, "
			+ $"ScrollableWidth={sut.ScrollableWidth:F2}.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23695")]
	public async Task When_ChangeView_BeforeFirstLayout_Then_Offset_Stays_At_Top()
	{
		// WinUI clamps the requested offset against the extent it knows *at request time*
		// (ScrollViewer::AdjustTargetVerticalOffset feeds ComputePixelExtentHeight straight into the
		// clamp, with no intervening UpdateLayout), so a ChangeView issued before the first layout pass
		// resolves against a zero extent and the later extent never re-drives it.
		var content = new Border { Width = 180, Height = 2000 };
		var sut = new ScrollViewer
		{
			Width = ViewportWidth,
			Height = ViewportHeight,
			Content = content,
		};

		sut.ScrollableHeight.Should().Be(0, "the ScrollViewer has not been laid out yet");
		sut.ChangeView(null, double.MaxValue, null, disableAnimation: true);

		await UITestHelper.Load(sut);
		await TestServices.WindowHelper.WaitForIdle();

		sut.ScrollableHeight.Should().BeApproximately(1800, 1, "the content is laid out by now");
		sut.VerticalOffset.Should().Be(0,
			$"a ChangeView resolved against a zero extent must not be re-driven once the content arrives. "
			+ $"Observed VerticalOffset={sut.VerticalOffset:F2}.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23041")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_ExtentShrinksThenGrowsBack_Then_RequestedOffset_Is_Restored()
	{
		// Companion invariant to the test above, and the reason the offset intent exists at all
		// (#23041): a transient extent shrink clamps the *displayed* offset, but the requested offset
		// survives, so growing the extent back restores it.
		// Excluded from native WinUI, where this was measured to land at 100 — its DirectManipulation
		// pipeline drops the request permanently. Uno restores instead, because its virtualized
		// StackLayout extent estimate is non-monotonic and a mid-realization dip would otherwise strand
		// a ChangeView(ScrollableHeight) in blank territory.
		var content = new Border { Width = 180, Height = 400 };
		var sut = new ScrollViewer
		{
			Width = ViewportWidth,
			Height = ViewportHeight,
			Content = content,
		};

		await UITestHelper.Load(sut);

		sut.ChangeView(null, 200, null, disableAnimation: true);
		await WaitForOffsetAsync(sut, 200);
		sut.VerticalOffset.Should().BeApproximately(200, 1);

		content.Height = 300;
		sut.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();
		sut.VerticalOffset.Should().BeApproximately(100, 1,
			"a shrinking extent must clamp the displayed offset down to the new scrollable height");

		content.Height = 400;
		sut.UpdateLayout();
		await WaitForOffsetAsync(sut, 200);
		sut.VerticalOffset.Should().BeApproximately(200, 1,
			$"restoring the extent must restore the requested offset. Observed VerticalOffset={sut.VerticalOffset:F2}.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23695")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)] // async native scrolling
	public async Task When_ChangeView_BeyondExtent_On_IncrementallyLoadingRepeater_Then_Loading_Is_Bounded()
	{
		// End-to-end shape of the reported regression: an ItemsRepeater whose source appends a batch
		// whenever the viewport reaches the end. A single beyond-extent ChangeView must not keep
		// pulling the viewport onto the moving bottom edge, which would load batches until the content
		// height reaches the requested offset.
		var source = new ObservableCollection<string>();
		var repeater = new ItemsRepeater
		{
			ItemsSource = source,
			Layout = new StackLayout { Orientation = Orientation.Vertical },
			ItemTemplate = (DataTemplate)XamlReader.Load("""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<Border Height="29" Background="SkyBlue" />
				</DataTemplate>
				"""),
		};

		var sut = new ScrollViewer
		{
			Width = 210,
			Height = 210,
			Content = repeater,
		};

		var batches = 0;
		var isLoading = false;
		void LoadBatch()
		{
			batches++;
			for (var i = 0; i < 25; i++)
			{
				source.Add($"Item {source.Count}");
			}
		}

		// The canonical "load more once the viewport reaches the end" trigger. Incremental loading is
		// inherently async, so the batch is enqueued rather than appended inline — which is exactly what
		// lets a chased offset re-arrive at the new bottom edge and request yet another batch.
		sut.ViewChanged += (_, _) =>
		{
			if (isLoading || batches >= 40 || sut.VerticalOffset < sut.ScrollableHeight - 1)
			{
				return;
			}

			isLoading = true;
			sut.DispatcherQueue.TryEnqueue(() =>
			{
				LoadBatch();
				isLoading = false;
			});
		};

		LoadBatch();
		await UITestHelper.Load(sut);
		await TestServices.WindowHelper.WaitForIdle();

		sut.ChangeView(null, 10_000, null, disableAnimation: true);

		// Settle: keep pumping until the batch count stops moving for several consecutive idles.
		var stable = 0;
		var last = batches;
		for (var i = 0; i < 200 && stable < 5; i++)
		{
			await TestServices.WindowHelper.WaitForIdle();
			if (batches == last)
			{
				stable++;
			}
			else
			{
				stable = 0;
				last = batches;
			}
		}

		batches.Should().BeLessThan(6,
			$"a single beyond-extent ChangeView must clamp to the live extent and stop triggering incremental loads. "
			+ $"Loaded {batches} batches ({source.Count} items); VerticalOffset={sut.VerticalOffset:F2}, "
			+ $"ExtentHeight={sut.ExtentHeight:F2}.");
	}

	private static async Task WaitForOffsetAsync(ScrollViewer sut, double expected, bool horizontal = false, int attempts = 30)
	{
		// ChangeView is asynchronous on native WinUI; a single WaitForIdle can return before it settles.
		for (var i = 0; i < attempts; i++)
		{
			var current = horizontal ? sut.HorizontalOffset : sut.VerticalOffset;
			if (Math.Abs(current - expected) <= 1)
			{
				break;
			}

			await Task.Delay(50);
		}

		await TestServices.WindowHelper.WaitForIdle();
	}
}
