using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Size = Windows.Foundation.Size;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Path
	{
		[TestMethod]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/6846")]
		public void Should_not_throw_if_Path_Data_is_set_to_null()
		{
			// Set initial Data
			var SUT = new Path { Data = new RectangleGeometry() };

			// Switch back to null.  Should not throw an exception.
			SUT.Data = null;
		}

		[TestMethod]
		public void Should_Not_Include_Control_Points_Bounds()
		{
#if WINAPPSDK
			var SUT = new Path { Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0 0 C 0 0 25 25 0 50") };
#else
			var SUT = new Path { Data = "M 0 0 C 0 0 25 25 0 50" };
#endif

			SUT.Measure(new Size(300, 300));

#if WINAPPSDK
			Assert.AreEqual(new Size(11, 50), SUT.DesiredSize);
#else
			Assert.IsTrue(Math.Abs(11 - SUT.DesiredSize.Width) <= 1, $"Actual size: {SUT.DesiredSize}");
			Assert.IsTrue(Math.Abs(50 - SUT.DesiredSize.Height) <= 1, $"Actual size: {SUT.DesiredSize}");
#endif
		}

		// This tests a workaround for a Linux-specific skia bug.
		// This doesn't actually repro for some reason, so it's here for documentation purposes.
		[TestMethod]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/18473")]
		public async Task When_Path_Clipped_Inside_ScrollViewer()
		{
			var root = (FrameworkElement)XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<Border Height="100" HorizontalAlignment="Stretch" Background="Blue" />
					<ScrollViewer Height="30">
						<Path Fill="Red" Data="m 0,0 c -0.639,0 -1.276,0.243 -1.765,0.729 -0.977,0.974 -0.98,2.557 -0.006,3.535 L 16.925,23.032 -1.768,41.725 c -0.976,0.976 -0.976,2.559 0,3.535 0.977,0.976 2.559,0.976 3.536,0 L 22.225,24.803 c 0.975,-0.975 0.976,-2.555 0.004,-3.532 L 1.771,0.736 C 1.283,0.245 0.642,0 0,0" />
					</ScrollViewer>
				</StackPanel>
				""");
			await UITestHelper.Load(root);

			root.FindVisualChildByType<ScrollViewer>().ScrollToVerticalOffset(9999);
			await UITestHelper.WaitForIdle();

			var screenshot = await UITestHelper.ScreenShot(root);
			ImageAssert.DoesNotHaveColorInRectangle(screenshot, new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height * 60 / 100), Microsoft.UI.Colors.Red);
		}
	}
}
