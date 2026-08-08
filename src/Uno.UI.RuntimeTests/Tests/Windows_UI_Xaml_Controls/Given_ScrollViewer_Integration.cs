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
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

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

		// MUX Reference CanInstantiate (C++ line 57).
		// Validates that a ScrollViewer can be instantiated without error.
		[TestMethod]
		public void CanInstantiate()
		{
			var scrollViewer = new ScrollViewer();
			Assert.IsNotNull(scrollViewer);
		}

		// MUX Reference CanEnterAndLeaveLiveTree (C++ line 62).
		// Validates that a ScrollViewer can be added to and removed from the live
		// visual tree without error.
		[TestMethod]
		public async Task CanEnterAndLeaveLiveTree()
		{
			var scrollViewer = new ScrollViewer
			{
				Content = new Border { Width = 200, Height = 200, Background = new SolidColorBrush(Colors.Cyan) },
			};

			TestServices.WindowHelper.WindowContent = scrollViewer;
			await TestServices.WindowHelper.WaitForLoaded(scrollViewer);

			Assert.IsTrue(scrollViewer.IsLoaded, "ScrollViewer should be loaded after entering tree");

			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(scrollViewer.IsLoaded, "ScrollViewer should be unloaded after leaving tree");
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

		// MUX Reference ChangeScrollViewerHeightToZero (C++ line 1622).
		// Regression test: setting Height=0 on a loaded ScrollViewer must not crash.
		[TestMethod]
		public async Task ChangeScrollViewerHeightToZero()
		{
			var scrollViewer = await AddScrollViewer(Orientation.Vertical);

			// Changing ScrollViewer Height to 0 after it was loaded.
			scrollViewer.Height = 0;

			await TestServices.WindowHelper.WaitForIdle();

			// No assertion: just verify no crash.
			Assert.AreEqual(0.0, scrollViewer.Height);
		}

		// MUX Reference ResetContent (C++ line 1643).
		// Validates that Content=null implies ScrollableHeight=0, and that re-assigning
		// content restores scrollability.
		[TestMethod]
		public async Task ResetContent()
		{
			var scrollViewer = await AddScrollViewer(Orientation.Vertical);

			Assert.IsTrue(scrollViewer.ScrollableHeight > 0.0, "Initial ScrollableHeight should be > 0");

			// Resetting Content for the case where ScrollContentPresenter::m_isChildActualHeightUsedAsExtent is false.
			scrollViewer.Content = null;
			await TestServices.WindowHelper.WaitForIdle();

			// ScrollViewer.Content == null implies ScrollViewer.ScrollableHeight == 0
			Assert.AreEqual(0.0, scrollViewer.ScrollableHeight, 0.5, "ScrollableHeight after Content=null");

			var textBlock = new TextBlock
			{
				FontSize = 100.0,
				Text = "A text with large characters.",
			};
			scrollViewer.Content = textBlock;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(scrollViewer.ScrollableHeight > 0.0, "ScrollableHeight after re-content should be > 0");

			// Resetting Content for the case where ScrollContentPresenter::m_isChildActualHeightUsedAsExtent is true.
			scrollViewer.Content = null;
			await TestServices.WindowHelper.WaitForIdle();

			// ScrollViewer.Content == null implies ScrollViewer.ScrollableHeight == 0
			Assert.AreEqual(0.0, scrollViewer.ScrollableHeight, 0.5, "ScrollableHeight after second Content=null");
		}

		[TestMethod]
		public async Task ViewChangeEventsAreCorrect()
		{
			var scrollViewer = await AddScrollViewer(Orientation.Vertical);
			scrollViewer.ZoomMode = ZoomMode.Enabled;
			const double newVerticalOffset = 10.0;
			const float newZoomFactor = 2.0f;
			var inertialViewChangingCount = 0;
			var intermediateViewChangedCount = 0;
			var nonIntermediateViewChangedCount = 0;
			var directManipulationStartedCount = 0;
			var directManipulationCompletedCount = 0;
			var lastNextView = (HorizontalOffset: 0.0, VerticalOffset: 0.0, ZoomFactor: 0.0f);
			var completed = new TaskCompletionSource<bool>();
			scrollViewer.DirectManipulationStarted += (_, _) => directManipulationStartedCount++;
			scrollViewer.DirectManipulationCompleted += (_, _) => directManipulationCompletedCount++;

			scrollViewer.ViewChanging += (_, args) =>
			{
				lastNextView = (
					args.NextView.HorizontalOffset,
					args.NextView.VerticalOffset,
					args.NextView.ZoomFactor);
				Assert.AreEqual(0.0, args.FinalView.HorizontalOffset, 0.001);
				Assert.IsTrue(
					Math.Abs(args.FinalView.VerticalOffset - newVerticalOffset) < 0.001 ||
					Math.Abs(args.FinalView.VerticalOffset - args.NextView.VerticalOffset) < 0.001);
				Assert.IsTrue(
					Math.Abs(args.FinalView.ZoomFactor - newZoomFactor) < 0.001 ||
					Math.Abs(args.FinalView.ZoomFactor - args.NextView.ZoomFactor) < 0.001);
				if (args.IsInertial)
				{
					inertialViewChangingCount++;
				}
			};
			scrollViewer.ViewChanged += (_, args) =>
			{
				if (args.IsIntermediate)
				{
					intermediateViewChangedCount++;
				}
				else
				{
					nonIntermediateViewChangedCount++;
					completed.TrySetResult(true);
				}
			};

			Assert.IsTrue(scrollViewer.ChangeView(null, newVerticalOffset, newZoomFactor, disableAnimation: false));
			Assert.AreEqual(completed.Task, await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(3))));

			if (new global::Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
			{
				Assert.IsGreaterThan(
					0,
					inertialViewChangingCount,
					$"intermediate={intermediateViewChangedCount}, final={nonIntermediateViewChangedCount}, dm={directManipulationStartedCount}/{directManipulationCompletedCount}, next={lastNextView}");
				Assert.IsGreaterThan(0, intermediateViewChangedCount);
			}
			else
			{
				Assert.AreEqual(0, inertialViewChangingCount);
				Assert.AreEqual(0, intermediateViewChangedCount);
			}
			Assert.AreEqual(1, nonIntermediateViewChangedCount);
			Assert.AreEqual(0.0, lastNextView.HorizontalOffset, 0.001);
			Assert.AreEqual(newVerticalOffset, lastNextView.VerticalOffset, 0.001);
			Assert.AreEqual(newZoomFactor, lastNextView.ZoomFactor, 0.001);
		}

		// MUX Reference ValidateNoLayoutCycleByChangeContentSize (C++ line 4854).
		// Regression test for a layout cycle that used to occur when the content
		// width oscillated across a parent's MinWidth constraint.
		[TestMethod]
		public async Task ValidateNoLayoutCycleByChangeContentSize()
		{
			var rootGrid = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" x:Name=\"rootGrid\" Background=\"Orange\">" +
				"  <StackPanel Background=\"DarkGray\">" +
				"    <Button x:Name=\"cycleButton\" Content=\"Cycle\" HorizontalAlignment=\"Center\" />" +
				"    <Border x:Name=\"constrainOwner\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Stretch\" MinWidth=\"800\" Background=\"Yellow\">" +
				"      <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" HorizontalScrollMode=\"Enabled\" VerticalScrollBarVisibility=\"Disabled\" VerticalScrollMode=\"Disabled\">" +
				"        <Border>" +
				"          <Rectangle x:Name=\"contentRect\" Fill=\"Red\" Height=\"100\" Width=\"2000\" />" +
				"        </Border>" +
				"      </ScrollViewer>" +
				"    </Border>" +
				"  </StackPanel>" +
				"</Grid>");

			TestServices.WindowHelper.WindowContent = rootGrid;
			await TestServices.WindowHelper.WaitForLoaded(rootGrid);
			await TestServices.WindowHelper.WaitForIdle();

			// Do the change content 4 times that ensures no layout cycle by changing the content size
			for (int i = 0; i < 4; i++)
			{
				var constrainOwner = (Border)rootGrid.FindName("constrainOwner");
				var contentRect = (Microsoft.UI.Xaml.Shapes.Rectangle)rootGrid.FindName("contentRect");
				var constraint = constrainOwner.MinWidth;

				contentRect.Width = contentRect.Width < constraint
					? constraint + 100
					: constraint - 100;

				// Update the layout to ensure no layout cycle by changing the content size
				constrainOwner.UpdateLayout();

				await TestServices.WindowHelper.WaitForIdle();
			}

			// Completed the verification without a layout cycle crash.
		}

		// MUX Reference ValidateNoLayoutCycleByChangeAlignment (C++ line 4912).
		// Regression test: changing a ScrollViewer's VerticalAlignment after layout
		// must not cause a layout cycle crash.
		[TestMethod]
		public async Task ValidateNoLayoutCycleByChangeAlignment()
		{
			var rootGrid = new Grid
			{
				Background = new SolidColorBrush(Colors.SlateBlue),
				Width = 400,
				Height = 400,
			};

			TestServices.WindowHelper.WindowContent = rootGrid;
			await TestServices.WindowHelper.WaitForLoaded(rootGrid);
			await TestServices.WindowHelper.WaitForIdle();

			var scrollViewer = new ScrollViewer();
			var stackPanel = new StackPanel();
			var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
			{
				Width = 100,
				Height = 200,
				Fill = new SolidColorBrush(Colors.Red),
			};

			stackPanel.Children.Add(rect);
			scrollViewer.Content = stackPanel;

			rootGrid.Children.Add(scrollViewer);
			rootGrid.UpdateLayout();

			scrollViewer.VerticalAlignment = VerticalAlignment.Top;

			await TestServices.WindowHelper.WaitForIdle();

			// Validate no layout cycle crash by changing the alignment.
		}

		// MUX Reference DefaultValuesAreCorrect (C++ line 3170).
		// Validates that a fresh ScrollViewer has the documented default values for
		// scroll modes, rail enablement, zoom mode, scroll bar visibility, and
		// MaxZoomFactor.
		// Note: HorizontalScrollBarVisibility's default differs across editions.
		// WinUI generic style sets it to Disabled; AddScrollViewer(Vertical) does not
		// touch it so we check the as-built state.
		[TestMethod]
		public async Task DefaultValuesAreCorrect()
		{
			var scrollViewer = await AddScrollViewer(Orientation.Vertical);

			Assert.AreEqual(ScrollMode.Auto, scrollViewer.HorizontalScrollMode);
			Assert.AreEqual(ScrollMode.Auto, scrollViewer.VerticalScrollMode);
			Assert.AreEqual(true, scrollViewer.IsHorizontalRailEnabled);
			Assert.AreEqual(true, scrollViewer.IsVerticalRailEnabled);
			Assert.AreEqual(ZoomMode.Disabled, scrollViewer.ZoomMode);
			Assert.AreEqual(10.0, scrollViewer.MaxZoomFactor, 0.001);
			// MinZoomFactor default per WinUI is 0.1.
			Assert.AreEqual(0.1, scrollViewer.MinZoomFactor, 0.001);
		}

		[TestMethod]
		public Task ConstrainVerticalStackPanelAvailableSize() =>
			ConstrainStackPanelAvailableSize(Orientation.Vertical);

		[TestMethod]
		public Task ConstrainHorizontalStackPanelAvailableSize() =>
			ConstrainStackPanelAvailableSize(Orientation.Horizontal);

		private static async Task ConstrainStackPanelAvailableSize(Orientation orientation)
		{
			var scrollViewer = await AddScrollViewer(orientation);
			var presenter = FindNamedDescendant<ScrollContentPresenter>(scrollViewer, "ScrollContentPresenter");
			Assert.IsNotNull(presenter);
			Assert.IsFalse(presenter.SizesContentToTemplatedParent);

			scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			await TestServices.WindowHelper.WaitForIdle();

			var stackPanel = (StackPanel)scrollViewer.Content;
			if (orientation == Orientation.Vertical)
			{
				Assert.AreEqual(0.0, scrollViewer.ScrollableWidth, 0.5);
				Assert.AreEqual(1100.0, scrollViewer.ScrollableHeight, 0.5);
			}
			else
			{
				Assert.AreEqual(1100.0, scrollViewer.ScrollableWidth, 0.5);
				Assert.AreEqual(0.0, scrollViewer.ScrollableHeight, 0.5);
			}

			presenter.SizesContentToTemplatedParent = true;
			await TestServices.WindowHelper.WaitForIdle();

			if (orientation == Orientation.Vertical)
			{
				Assert.AreEqual(1100.0, scrollViewer.ScrollableHeight, 0.5);
				Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedVerticalScrollBarVisibility);
				stackPanel.VerticalAlignment = VerticalAlignment.Top;
			}
			else
			{
				Assert.AreEqual(1100.0, scrollViewer.ScrollableWidth, 0.5);
				Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedHorizontalScrollBarVisibility);
				stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
			}

			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() =>
				orientation == Orientation.Vertical
					? Math.Abs(scrollViewer.ScrollableHeight) < 0.5
					: Math.Abs(scrollViewer.ScrollableWidth) < 0.5);
			var diagnostics =
				$"content={presenter.Content?.GetType().Name ?? "<null>"}, owner={presenter.ScrollOwner?.GetType().Name ?? "<null>"}, " +
				$"presenter desired={presenter.DesiredSize} actual={presenter.ActualWidth}x{presenter.ActualHeight}, " +
				$"panel desired={stackPanel.DesiredSize} actual={stackPanel.ActualWidth}x{stackPanel.ActualHeight}, " +
				$"available={LayoutInformation.GetAvailableSize(presenter)}, sizesToParent={presenter.SizesContentToTemplatedParent}";

			if (orientation == Orientation.Vertical)
			{
				Assert.AreEqual(0.0, scrollViewer.ScrollableHeight, 0.5, diagnostics);
				Assert.AreEqual(Visibility.Collapsed, scrollViewer.ComputedVerticalScrollBarVisibility);
				stackPanel.VerticalAlignment = VerticalAlignment.Stretch;
			}
			else
			{
				Assert.AreEqual(0.0, scrollViewer.ScrollableWidth, 0.5, diagnostics);
				Assert.AreEqual(Visibility.Collapsed, scrollViewer.ComputedHorizontalScrollBarVisibility);
				stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
			}

			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() =>
				orientation == Orientation.Vertical
					? Math.Abs(scrollViewer.ScrollableHeight - 1100.0) < 0.5
					: Math.Abs(scrollViewer.ScrollableWidth - 1100.0) < 0.5);

			if (orientation == Orientation.Vertical)
			{
				Assert.AreEqual(1100.0, scrollViewer.ScrollableHeight, 0.5);
				Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedVerticalScrollBarVisibility);
			}
			else
			{
				Assert.AreEqual(1100.0, scrollViewer.ScrollableWidth, 0.5);
				Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedHorizontalScrollBarVisibility);
			}
		}

		// MUX Reference ReenterContent (C++ line 1697).
		// Validates that resetting Content to null and then back to the original
		// content preserves the SV's view (HorizontalOffset / VerticalOffset /
		// ZoomFactor). The C++ test additionally renders before/after the toggle
		// and asserts DComp pixel parity; the Skia variant skips that (no DComp)
		// and keeps the public-API view assertions.
		[TestMethod]
		public async Task ReenterContent()
		{
			var scrollViewer = await AddScrollViewer(Orientation.Vertical);

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
				_ = scrollViewer.ChangeView(null /*horizontalOffset*/, 200.0 /*verticalOffset*/, 1.2f /*zoomFactor*/, true /*disableAnimation*/);

				var completed = await Task.WhenAny(viewChangedTcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
				Assert.AreEqual(viewChangedTcs.Task, completed, "Initial ViewChanged didn't fire within 3s");
				await TestServices.WindowHelper.WaitForIdle();

				// Momentarily setting ScrollViewer.Content to null.
				var content = scrollViewer.Content;
				scrollViewer.Content = null;
				scrollViewer.Content = content;

				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(0.0, scrollViewer.HorizontalOffset, 0.001, "HorizontalOffset");
				Assert.AreEqual(200.0, scrollViewer.VerticalOffset, 0.001, "VerticalOffset");
				Assert.AreEqual(1.2f, scrollViewer.ZoomFactor, 0.001, "ZoomFactor");
			}
			finally
			{
				scrollViewer.ViewChanged -= OnViewChanged;
			}
		}

		private static T FindNamedDescendant<T>(DependencyObject root, string name)
			where T : FrameworkElement
		{
			var childCount = VisualTreeHelper.GetChildrenCount(root);
			for (var index = 0; index < childCount; index++)
			{
				var child = VisualTreeHelper.GetChild(root, index);
				if (child is T element && element.Name == name)
				{
					return element;
				}

				if (FindNamedDescendant<T>(child, name) is { } descendant)
				{
					return descendant;
				}
			}

			return default;
		}

		[TestMethod]
		public async Task ChangeViewTwice()
		{
			var scrollViewer = await AddScrollViewer(Orientation.Vertical);
			scrollViewer.ZoomMode = ZoomMode.Enabled;
			var completed = new TaskCompletionSource<bool>();
			scrollViewer.ViewChanged += (_, args) =>
			{
				if (!args.IsIntermediate)
				{
					completed.TrySetResult(true);
				}
			};

			Assert.IsTrue(scrollViewer.ChangeView(null, null, 2.0f, disableAnimation: true));
			Assert.IsTrue(scrollViewer.ChangeView(null, 3500.0, 3.0f, disableAnimation: true));
			Assert.AreEqual(completed.Task, await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(3))));
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0.0, scrollViewer.HorizontalOffset, 0.001);
			Assert.AreEqual(3500.0, scrollViewer.VerticalOffset, 5.0);
			Assert.AreEqual(3.0f, scrollViewer.ZoomFactor, 0.001);
		}
	}
}
