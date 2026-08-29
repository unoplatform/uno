using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ScrollBar
	{
		[TestMethod]
		public void When_Value_Changed()
		{
			var sb = new ScrollBar() { Maximum = 30 };
			var timesCalled = 0;
			var newValue = double.NaN;
			sb.ValueChanged += (o, e) =>
			{
				timesCalled++;
				newValue = e.NewValue;
			};

			sb.Value = 22;

			Assert.AreEqual(1, timesCalled);
			Assert.AreEqual(22, newValue);
		}

		[TestMethod]
		[DataRow(Orientation.Vertical)]
		[DataRow(Orientation.Horizontal)]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Touch_Drags_Thumb_Then_Value_Changes(Orientation orientation)
		{
			var isVertical = orientation == Orientation.Vertical;
			var SUT = new ScrollBar
			{
				Orientation = orientation,
				Minimum = 0,
				Maximum = 100,
				ViewportSize = 100,
				Width = isVertical ? 24 : 200,
				Height = isVertical ? 200 : 24,
			};

			SUT.SetIsTouchThumbDragEnabled(true);

			var scrollEvents = new List<ScrollEventType>();
			SUT.Scroll += (_, e) => scrollEvents.Add(e.ScrollEventType);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var thumb = FindTemplateChild<Thumb>(SUT, isVertical ? "VerticalThumb" : "HorizontalThumb");
				Assert.IsNotNull(thumb, "The thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBounds();
				Assert.IsTrue(bounds is { Width: > 0, Height: > 0 }, $"The thumb should be laid out and visible, but its bounds were {bounds}.");

				finger.Press(Center(bounds));
				finger.MoveBy(isVertical ? 0 : 50, isVertical ? 50 : 0, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(SUT.Value > 0, $"A finger drag on the thumb should change the Value, but it stayed at {SUT.Value}.");
				CollectionAssert.Contains(scrollEvents, ScrollEventType.ThumbTrack, "A touch thumb drag should raise ScrollEventType.ThumbTrack.");
				CollectionAssert.Contains(scrollEvents, ScrollEventType.EndScroll, "Releasing a touch thumb drag should raise ScrollEventType.EndScroll.");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Touch_Drags_Thumb_Without_OptIn_Then_Value_Is_Unchanged()
		{
			// WinUI has the ScrollBar parts ignore touch on purpose, so the default must stay that way.
			var SUT = new ScrollBar
			{
				Orientation = Orientation.Vertical,
				Minimum = 0,
				Maximum = 100,
				ViewportSize = 100,
				IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
				Width = 24,
				Height = 200,
			};

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var thumb = FindTemplateChild<Thumb>(SUT, "VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				finger.Press(Center(thumb.GetAbsoluteBounds()));
				finger.MoveBy(0, 50, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(0, SUT.Value, "Without opting in, a touch drag on the thumb must not move the ScrollBar.");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Touch_Drags_Standalone_Thumb_Then_Value_Changes()
		{
			// A ScrollBar not hosted by a ScrollViewer -- the shape DataGrid uses -- has nothing to
			// raise its indicator, so the extension has to hold it interactive on its own.
			var SUT = new ScrollBar
			{
				Orientation = Orientation.Vertical,
				Minimum = 0,
				Maximum = 100,
				ViewportSize = 100,
				Width = 24,
				Height = 200,
			};

			SUT.SetIsTouchThumbDragEnabled(true);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var thumb = FindTemplateChild<Thumb>(SUT, "VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBounds();
				finger.Press(Center(bounds));
				finger.MoveBy(0, 50, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(SUT.Value > 0, $"A finger drag should change the Value of a standalone ScrollBar (IndicatorMode={SUT.IndicatorMode}), but it stayed at {SUT.Value}.");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		// Auto is not just a variation: the template defers the bars (x:Load="False") and only realizes them
		// once an axis overflows, so the opt-in has to hold for a bar which appears after everything else.
		[DataRow(ScrollBarVisibility.Visible)]
		[DataRow(ScrollBarVisibility.Auto)]
#if !HAS_INPUT_INJECTOR || !UNO_HAS_MANAGED_SCROLL_PRESENTER
		[Ignore("This test only applies to the managed scroll presenter and requires the input injector.")]
#endif
		public async Task When_Touch_Drags_ScrollViewer_Thumb_Then_Scrolls(ScrollBarVisibility verticalScrollBarVisibility)
		{
			var SUT = CreateScrollViewer(verticalScrollBarVisibility);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var scrollBar = FindTemplateChild<ScrollBar>(SUT, "VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The ScrollViewer template should contain a VerticalScrollBar.");

				scrollBar.SetIsTouchThumbDragEnabled(true);
				await TestServices.WindowHelper.WaitForIdle();

				var thumb = FindTemplateChild<Thumb>(scrollBar, "VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBounds();
				Assert.IsTrue(bounds is { Width: > 0, Height: > 0 }, $"The thumb should be laid out and hit-testable, but its bounds were {bounds}.");

				finger.Press(Center(bounds));
				finger.MoveBy(0, 60, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(
					SUT.VerticalOffset > 0,
					$"A finger drag on the scrollbar thumb should scroll the ScrollViewer, but VerticalOffset stayed at {SUT.VerticalOffset} "
					+ $"(IndicatorMode={scrollBar.IndicatorMode}, thumb bounds={bounds}).");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		// Auto is not just a variation: the template defers the bars (x:Load="False") and only realizes them
		// once an axis overflows, so the opt-in has to hold for a bar which appears after everything else.
		[DataRow(ScrollBarVisibility.Visible)]
		[DataRow(ScrollBarVisibility.Auto)]
#if !HAS_INPUT_INJECTOR || !UNO_HAS_MANAGED_SCROLL_PRESENTER
		[Ignore("This test only applies to the managed scroll presenter and requires the input injector.")]
#endif
		public async Task When_Touch_Panned_Then_Thumb_Is_Still_Draggable(ScrollBarVisibility verticalScrollBarVisibility)
		{
			// A finger pan drives the ScrollViewer to its TouchIndicator state, where the template collapses
			// the interactive root the thumb lives in. That is the state a tablet is always in, so the opt-in
			// is worthless unless it survives it.
			var SUT = CreateScrollViewer(verticalScrollBarVisibility);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var scrollBar = FindTemplateChild<ScrollBar>(SUT, "VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The ScrollViewer template should contain a VerticalScrollBar.");

				scrollBar.SetIsTouchThumbDragEnabled(true);
				await TestServices.WindowHelper.WaitForIdle();

				var contentBounds = SUT.GetAbsoluteBounds();
				finger.Press(new Point(contentBounds.Left + 40, contentBounds.Top + 200));
				finger.MoveBy(0, -60, steps: 30);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				if (scrollBar.IndicatorMode != ScrollingIndicatorMode.TouchIndicator)
				{
					// The pan is the way a device gets there; drive it explicitly if the injected one did not,
					// so the test cannot pass by never reaching the state it is about.
					VisualStateManager.GoToState(SUT, "TouchIndicator", true);
					await TestServices.WindowHelper.WaitForIdle();
				}

				Assert.AreEqual(
					ScrollingIndicatorMode.TouchIndicator,
					scrollBar.IndicatorMode,
					"Test premise: the ScrollViewer should be showing its touch indicator.");

				var offsetAfterPan = SUT.VerticalOffset;

				var thumb = FindTemplateChild<Thumb>(scrollBar, "VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBounds();
				Assert.IsTrue(
					bounds is { Width: > 0, Height: > 0 },
					$"The thumb should stay laid out and hit-testable while the touch indicator is shown, but its bounds were {bounds}.");

				finger.Press(Center(bounds));
				finger.MoveBy(0, 60, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(
					SUT.VerticalOffset > offsetAfterPan,
					$"A finger drag on the thumb should scroll even after a pan, but VerticalOffset stayed at {SUT.VerticalOffset} "
					+ $"(offset after the pan={offsetAfterPan}, IndicatorMode={scrollBar.IndicatorMode}, thumb bounds={bounds}).");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR || !UNO_HAS_MANAGED_SCROLL_PRESENTER
		[Ignore("This test only applies to the managed scroll presenter and requires the input injector.")]
#endif
		public async Task When_Reloaded_Then_Touch_Thumb_Drag_Survives()
		{
			// The bar (re)applies IgnoreTouchInput to its parts every time it attaches them, so an opt-in which
			// is applied from the outside once would be undone by the next reload.
			var SUT = CreateScrollViewer();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var scrollBar = FindTemplateChild<ScrollBar>(SUT, "VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The ScrollViewer template should contain a VerticalScrollBar.");
				scrollBar.SetIsTouchThumbDragEnabled(true);
				await TestServices.WindowHelper.WaitForIdle();

				TestServices.WindowHelper.WindowContent = null;
				await TestServices.WindowHelper.WaitForIdle();
				await UITestHelper.Load(SUT);

				scrollBar = FindTemplateChild<ScrollBar>(SUT, "VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The VerticalScrollBar should be back after the reload.");

				var thumb = FindTemplateChild<Thumb>(scrollBar, "VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBounds();
				finger.Press(Center(bounds));
				finger.MoveBy(0, 60, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(
					SUT.VerticalOffset > 0,
					$"A finger drag on the thumb should still scroll after a reload, but VerticalOffset stayed at {SUT.VerticalOffset} "
					+ $"(IndicatorMode={scrollBar.IndicatorMode}, thumb bounds={bounds}).");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static ScrollViewer CreateScrollViewer(ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Visible)
			=> new ScrollViewer
			{
				Width = 200,
				Height = 300,
				VerticalScrollBarVisibility = verticalScrollBarVisibility,
				IsScrollInertiaEnabled = false,
				UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
				Content = new Border
				{
					Width = 180,
					Height = 2000,
					Background = new SolidColorBrush(Colors.Blue),
				},
			};

		private static Point Center(Rect rect)
			=> new Point(rect.Left + (rect.Width / 2), rect.Top + (rect.Height / 2));

		private static T FindTemplateChild<T>(DependencyObject root, string name)
			where T : FrameworkElement
		{
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is T match && match.Name == name)
				{
					return match;
				}

				if (FindTemplateChild<T>(child, name) is { } descendant)
				{
					return descendant;
				}
			}

			return null;
		}
	}
}
