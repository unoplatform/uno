using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;
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
	}
}
