using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
#if HAS_UNO
using Uno.UI.Toolkit.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
#endif

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

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Touch_Drags_Vertical_Thumb_Then_Value_Changes()
		{
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

			var scrollEvents = new List<ScrollEventType>();
			SUT.Scroll += (_, e) => scrollEvents.Add(e.ScrollEventType);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var thumb = SUT.FindFirstDescendant<Thumb>("VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBoundsRect();
				finger.Press(new Point(bounds.GetMidX(), bounds.GetMidY()));
				await TestServices.WindowHelper.WaitForIdle();
				finger.MoveBy(0, 50);
				await TestServices.WindowHelper.WaitForIdle();
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(SUT.Value > 0, $"A finger drag on the thumb should change the Value, but it stayed at {SUT.Value}.");
				CollectionAssert.Contains(scrollEvents, ScrollEventType.ThumbTrack, "A touch thumb drag should raise ScrollEventType.ThumbTrack.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Touch_Drags_ScrollViewer_Vertical_Thumb_Then_Scrolls()
		{
			var SUT = new ScrollViewer
			{
				Width = 200,
				Height = 300,
				VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = new Border
				{
					Width = 180,
					Height = 2000,
					Background = new SolidColorBrush(Colors.Blue),
				},
			};

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				await UITestHelper.Load(SUT);

				var scrollBar = SUT.FindFirstDescendant<ScrollBar>("VerticalScrollBar");
				Assert.IsNotNull(scrollBar, "The ScrollViewer template should contain a VerticalScrollBar.");
				var thumb = scrollBar.FindFirstDescendant<Thumb>("VerticalThumb");
				Assert.IsNotNull(thumb, "The vertical thumb should be part of the ScrollBar template.");

				var bounds = thumb.GetAbsoluteBoundsRect();
				finger.Press(new Point(bounds.GetMidX(), bounds.GetMidY()));
				await TestServices.WindowHelper.WaitForIdle();
				finger.MoveBy(0, 60);
				await TestServices.WindowHelper.WaitForIdle();
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(
					SUT.VerticalOffset > 0,
					$"A finger drag on the scrollbar thumb should scroll the ScrollViewer, but VerticalOffset stayed at {SUT.VerticalOffset} "
					+ $"(ScrollBar.IndicatorMode={scrollBar.IndicatorMode}, thumb visibility={thumb.Visibility}).");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Touch_Pans_Content_Then_ScrollViewer_Still_Scrolls()
		{
			var SUT = new ScrollViewer
			{
				Width = 200,
				Height = 300,
				VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = new Border
				{
					Width = 180,
					Height = 2000,
					Background = new SolidColorBrush(Colors.Blue),
				},
			};

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				var bounds = await UITestHelper.Load(SUT);

				// Well away from the scrollbar strip on the trailing edge.
				finger.Press(new Point(bounds.Left + 40, bounds.GetMidY()));
				await TestServices.WindowHelper.WaitForIdle();
				finger.MoveBy(0, -80);
				await TestServices.WindowHelper.WaitForIdle();
				finger.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(SUT.VerticalOffset > 0, $"Panning the content with a finger should still scroll, but VerticalOffset stayed at {SUT.VerticalOffset}.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}
#endif
	}
}
