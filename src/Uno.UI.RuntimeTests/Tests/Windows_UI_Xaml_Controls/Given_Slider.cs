using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;
using Windows.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;

#if WINAPPSDK
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Slider
	{
		[TestMethod]
		public async Task When_Value_At_Maximum()
		{
			var slider = new Slider { Minimum = 0, Maximum = 100, Value = 100, Orientation = Orientation.Horizontal, Width = 320, VerticalAlignment = VerticalAlignment.Stretch };
			WindowHelper.WindowContent = slider;

			await WindowHelper.WaitForIdle();

			var thumb = slider.FindFirstChild<Thumb>();

			Assert.IsNotNull(thumb);

			Assert.IsTrue(thumb.ActualWidth > 0);
			Assert.IsTrue(thumb.ActualHeight > 0);
		}

		[TestMethod]
		public async Task When_Slider_Constrained_Horizontal()
		{
			var slider = new MySlider { Minimum = 0, Maximum = 100, Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var container = new Grid { Width = 0, Height = 80 };
			container.Children.Add(slider);
			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, slider.ActualWidth);
			Assert.AreEqual(0, slider.HorizontalDecreaseRect.Width);
			Assert.AreEqual(0, slider.HorizontalDecreaseRect.ActualWidth);
		}

		[TestMethod]
		public async Task When_Slider_Constrained_Vertical()
		{
			var slider = new MySlider { Minimum = 0, Maximum = 100, Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var container = new Grid { Width = 80, Height = 0 };
			container.Children.Add(slider);
			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, slider.ActualHeight);
			Assert.AreEqual(0, slider.VerticalDecreaseRect.Height);
			Assert.AreEqual(0, slider.VerticalDecreaseRect.ActualHeight);
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_Value_Decimal()
		{
			var slider = new Slider()
			{
				Value = 0.5,
				Minimum = 0,
				Maximum = 1,
				StepFrequency = 0.01,
				Orientation = Orientation.Horizontal,
			};
			WindowHelper.WindowContent = slider;
			await WindowHelper.WaitForLoaded(slider);

			var thumb = VisualTreeUtils.FindVisualChildByName(slider, "HorizontalThumb");
			var toolTip = ToolTipService.GetToolTipReference(thumb);
			var tb = (TextBlock)toolTip.Content;
			var text = tb.Text;
			Assert.AreEqual("0.50", text);
		}
#endif

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_Slider_Dragged()
		{
			var slider = new MySlider
			{
				Minimum = 0,
				Maximum = 100,
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			var container = new Grid
			{
				Width = 80,
				Height = 80
			};
			container.Children.Add(slider);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			mouse.Press(slider.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(Math.Abs(50 - slider.Value) < 1);

			var clickableLength = slider.ActualWidth - slider.FindVisualChildByType<Thumb>().ActualWidth;

			mouse.MoveBy(clickableLength / 4, 0);

			Assert.IsTrue(Math.Abs(75 - slider.Value) < 1);

			mouse.Release();
		}
#endif
	}

	public partial class MySlider : Slider
	{
		public Rectangle HorizontalDecreaseRect { get; private set; }
		public Rectangle VerticalDecreaseRect { get; private set; }
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			HorizontalDecreaseRect = GetTemplateChild("HorizontalDecreaseRect") as Rectangle;
			VerticalDecreaseRect = GetTemplateChild("VerticalDecreaseRect") as Rectangle;
		}
	}
}
