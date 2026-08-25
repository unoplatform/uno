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
				IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
				Width = isVertical ? 24 : 200,
				Height = isVertical ? 200 : 24,
			};

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
		[Ignore("A ScrollBar not hosted by a ScrollViewer keeps IndicatorMode at None, so no interactive indicator is ever raised for it and the thumb stays out of reach - the DataGrid case.")]
		public async Task When_Touch_Drags_Thumb_With_Default_IndicatorMode_Then_Value_Changes()
		{
			// A ScrollBar not hosted by a ScrollViewer -- the shape used by DataGrid -- keeps
			// IndicatorMode at its default, so nothing raises the interactive indicator for it.
			var SUT = new ScrollBar
			{
				Orientation = Orientation.Vertical,
				Minimum = 0,
				Maximum = 100,
				ViewportSize = 100,
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

				var bounds = thumb.GetAbsoluteBounds();
				finger.Press(Center(bounds));
				finger.MoveBy(0, 50, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(SUT.Value > 0, $"A finger drag should change the Value with the default IndicatorMode ({SUT.IndicatorMode}), but it stayed at {SUT.Value}.");
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
		public async Task When_Touch_Drags_ScrollViewer_Thumb_Then_Scrolls()
		{
			var SUT = CreateScrollViewer();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var scrollBar = FindTemplateChild<ScrollBar>(SUT, "VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The ScrollViewer template should contain a VerticalScrollBar.");

				// Touch has no hover, so raise the indicator explicitly instead of racing its auto-hide.
				scrollBar.IndicatorMode = ScrollingIndicatorMode.MouseIndicator;
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
		[Ignore("While the indicator is shown the interactive root takes the touch and the track ignores it, so a pan started on the strip does nothing.")]
		public async Task When_Touch_Presses_Indicator_Strip_Then_Content_Still_Pans()
		{
			var SUT = CreateScrollViewer();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var scrollBar = FindTemplateChild<ScrollBar>(SUT, "VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The ScrollViewer template should contain a VerticalScrollBar.");

				scrollBar.IndicatorMode = ScrollingIndicatorMode.MouseIndicator;
				await TestServices.WindowHelper.WaitForIdle();

				// Inside the scrollbar strip but clear of the thumb, which sits at the top while the offset is 0.
				var bounds = SUT.GetAbsoluteBounds();
				finger.Press(new Point(bounds.Right - 4, bounds.Bottom - 30));
				finger.MoveBy(0, -60, steps: 50);
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(
					SUT.VerticalOffset > 0,
					$"A finger pan starting on the scrollbar strip should still scroll the content, but VerticalOffset stayed at {SUT.VerticalOffset}.");
			}
			finally
			{
				finger.Release();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static ScrollViewer CreateScrollViewer()
			=> new ScrollViewer
			{
				Width = 200,
				Height = 300,
				VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
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
