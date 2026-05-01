// MUX Reference test/native/external/controls/scrollviewer/ScrollViewerIntegrationTests.cpp,
// commit 5f9e85113. Tests ported from the native WinUI integration test suite to validate
// public API behavior (ScrollToHorizontalOffset / ScrollToVerticalOffset / ChangeView /
// extents from sized children).

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ScrollViewer_Integration
	{
		// (C++ source: AddScrollViewer at line 3739 — simplified Skia port.)
		// Builds the standard 100x100 ScrollViewer with 12 stacked 100x100 rectangles
		// used by most of the C++ integration tests.
		private static async Task<ScrollViewer> AddScrollViewer(Orientation orientation)
		{
			var scrollViewer = new ScrollViewer
			{
				Width = 100,
				Height = 100,
			};

			if (orientation == Orientation.Horizontal)
			{
				scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
			}

			var stackPanel = new StackPanel { Orientation = orientation };
			scrollViewer.Content = stackPanel;

			for (int i = 0; i < 12; i++)
			{
				stackPanel.Children.Add(new Rectangle
				{
					Fill = new SolidColorBrush(i % 2 == 0 ? Colors.Red : Colors.Blue),
					Width = 100,
					Height = 100,
				});
			}

			TestServices.WindowHelper.WindowContent = scrollViewer;
			await TestServices.WindowHelper.WaitForLoaded(scrollViewer);
			await TestServices.WindowHelper.WaitForIdle();

			return scrollViewer;
		}

		// MUX Reference DoScrollToOffset at C++ line 3915.
		// Validates that ScrollToHorizontalOffset / ScrollToVerticalOffset move the
		// view by the expected delta only when the corresponding scrollbar is enabled.
		private static async Task DoScrollToOffset(Orientation direction, bool canScroll)
		{
			var scrollViewer = await AddScrollViewer(direction);

			if (!canScroll)
			{
				if (direction == Orientation.Horizontal)
				{
					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				}
				else
				{
					scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
				}

				await TestServices.WindowHelper.WaitForIdle();
			}

			var oldHorizontalOffset = scrollViewer.HorizontalOffset;
			var oldVerticalOffset = scrollViewer.VerticalOffset;
			var oldZoomFactor = scrollViewer.ZoomFactor;

			var expectedNewHorizontalOffset = oldHorizontalOffset;
			var expectedNewVerticalOffset = oldVerticalOffset;

			if (canScroll)
			{
				if (direction == Orientation.Horizontal)
				{
					expectedNewHorizontalOffset += 1;
				}
				else
				{
					expectedNewVerticalOffset += 1;
				}
			}

			if (direction == Orientation.Horizontal)
			{
				scrollViewer.ScrollToHorizontalOffset(expectedNewHorizontalOffset);
			}
			else
			{
				scrollViewer.ScrollToVerticalOffset(expectedNewVerticalOffset);
			}

			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedNewHorizontalOffset, scrollViewer.HorizontalOffset, 0.001, "HorizontalOffset");
			Assert.AreEqual(expectedNewVerticalOffset, scrollViewer.VerticalOffset, 0.001, "VerticalOffset");
			Assert.AreEqual(oldZoomFactor, scrollViewer.ZoomFactor, "ZoomFactor unchanged");
		}

		// MUX Reference CanScrollToHorizontalOffset (C++ line 73).
		[TestMethod]
		public Task CanScrollToHorizontalOffset() => DoScrollToOffset(Orientation.Horizontal, canScroll: true);

		// MUX Reference CanScrollToVerticalOffset (C++ line 78).
		[TestMethod]
		public Task CanScrollToVerticalOffset() => DoScrollToOffset(Orientation.Vertical, canScroll: true);

		// MUX Reference CannotScrollToHorizontalOffset (C++ line 83).
		[TestMethod]
		public Task CannotScrollToHorizontalOffset() => DoScrollToOffset(Orientation.Horizontal, canScroll: false);

		// MUX Reference CannotScrollToVerticalOffset (C++ line 88).
		[TestMethod]
		public Task CannotScrollToVerticalOffset() => DoScrollToOffset(Orientation.Vertical, canScroll: false);

		// MUX Reference DoChangeView at C++ line 4014.
		// Validates that ScrollViewer.ChangeView changes the view by the expected delta
		// and raises ViewChanged with IsIntermediate=false at the end.
		private static async Task DoChangeView(bool horizontal, bool vertical, bool zoom)
		{
			var orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
			var scrollViewer = await AddScrollViewer(orientation);

			var oldHorizontalOffset = scrollViewer.HorizontalOffset;
			var oldVerticalOffset = scrollViewer.VerticalOffset;
			var oldZoomFactor = scrollViewer.ZoomFactor;

			var expectedNewHorizontalOffset = oldHorizontalOffset + (horizontal ? 1 : 0);
			var expectedNewVerticalOffset = oldVerticalOffset + (vertical ? 1 : 0);
			var expectedNewZoomFactor = oldZoomFactor + (zoom ? 0.01f : 0.0f);

			var viewChangedTcs = new TaskCompletionSource<bool>();
			void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs args)
			{
				if (!args.IsIntermediate)
				{
					viewChangedTcs.TrySetResult(true);
				}
			}
			scrollViewer.ViewChanged += OnViewChanged;
			try
			{
				bool couldChangeView = scrollViewer.ChangeView(
					expectedNewHorizontalOffset,
					expectedNewVerticalOffset,
					expectedNewZoomFactor,
					true /*disableAnimation*/);

				Assert.IsTrue(couldChangeView, "ChangeView returned false");

				var completed = await Task.WhenAny(viewChangedTcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
				Assert.AreEqual(viewChangedTcs.Task, completed, "ViewChanged with IsIntermediate=false didn't fire within 3s");

				Assert.AreEqual(expectedNewHorizontalOffset, scrollViewer.HorizontalOffset, 0.001, "HorizontalOffset");
				Assert.AreEqual(expectedNewVerticalOffset, scrollViewer.VerticalOffset, 0.001, "VerticalOffset");
				Assert.AreEqual(expectedNewZoomFactor, scrollViewer.ZoomFactor, 0.001, "ZoomFactor");
			}
			finally
			{
				scrollViewer.ViewChanged -= OnViewChanged;
			}
		}

		// MUX Reference CanChangeViewHorizontally (C++ line 93).
		[TestMethod]
		public Task CanChangeViewHorizontally() => DoChangeView(horizontal: true, vertical: false, zoom: false);

		// MUX Reference CanChangeViewVertically (C++ line 98).
		[TestMethod]
		public Task CanChangeViewVertically() => DoChangeView(horizontal: false, vertical: true, zoom: false);

		// MUX Reference SizedTextBlock (C++ line 3802).
		// Validates that a short text in a TextBlock with a large MinWidth pushes
		// the large extent to the owning ScrollViewer; lifting MinWidth re-shrinks.
		[TestMethod]
		public async Task SizedTextBlock()
		{
			var scrollViewer = new ScrollViewer
			{
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Width = 100,
				Height = 50,
			};

			var textBlock = new TextBlock
			{
				MinWidth = 500,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Text = "Short",
			};

			scrollViewer.Content = textBlock;
			TestServices.WindowHelper.WindowContent = scrollViewer;
			await TestServices.WindowHelper.WaitForLoaded(scrollViewer);
			await TestServices.WindowHelper.WaitForIdle();

			// Even though the TextBlock's Text is short, the TextBlock::MinWidth value forces its actual width to be 500px.
			Assert.AreEqual(400.0, scrollViewer.ScrollableWidth, 0.5, "ScrollableWidth with MinWidth=500 on 100-wide SV");

			// Eliminate the min width requirement. The TextBlock is expected to shrink.
			textBlock.MinWidth = 0;

			await TestServices.WindowHelper.WaitForIdle();

			// The ScrollViewer is no longer expected to be scrollable horizontally.
			Assert.AreEqual(0.0, scrollViewer.ScrollableWidth, 0.5, "ScrollableWidth after MinWidth lifted");
		}
	}
}
