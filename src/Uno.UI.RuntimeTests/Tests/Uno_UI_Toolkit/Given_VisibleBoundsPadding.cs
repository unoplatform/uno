using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Toolkit
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_VisibleBoundsPadding
	{
		[TestMethod]
		[RequiresFullWindow]
#if __SKIA__ || __WASM__
		[Ignore("VisibleBoundsPadding is not working correctly on these platforms - see https://github.com/unoplatform/uno/issues/7978")]
#elif __ANDROID__
		[Ignore("Flaky test on Android")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Mask_All()
		{
			using (ScreenHelper.OverrideVisibleBounds(new Thickness(0, 34, 0, 65)))
			{
				var inner = new Border
				{
					Background = new SolidColorBrush(Colors.AliceBlue),
					Child = new Ellipse
					{
						Fill = new SolidColorBrush(Colors.DarkOrange)
					}
				};
				var container = new Grid
				{
					Children =
					{
						inner
					}
				};

				VisibleBoundsPadding.SetPaddingMask(container, VisibleBoundsPadding.PaddingMask.All);

				WindowHelper.WindowContent = container;
				await WindowHelper.WaitForLoaded(inner);

				var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
				var windowBounds = Window.Current.Bounds;
				RectAssert.AreNotEqual(windowBounds, visibleBounds);

				var containerBounds = container.GetOnScreenBounds();
				var childBounds = inner.GetOnScreenBounds();
				RectAssert.AreEqual(windowBounds, containerBounds);
				RectAssert.AreEqual(visibleBounds, childBounds);
			}
		}
	}
}
