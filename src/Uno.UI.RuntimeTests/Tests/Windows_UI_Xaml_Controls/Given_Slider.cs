using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Shapes;
#if NETFX_CORE
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
