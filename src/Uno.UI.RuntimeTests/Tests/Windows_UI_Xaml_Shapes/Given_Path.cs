using System;
using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	public class Given_Path
	{
		[TestMethod]
		[RunsOnUIThread]
		public void Should_not_throw_if_Path_Data_is_set_to_null()
		{
			// This bug is an illustration of issue
			// https://github.com/unoplatform/uno/issues/6846

			// Set initial Data
			var SUT = new Path { Data = new RectangleGeometry() };

			// Switch back to null.  Should not throw an exception.
			SUT.Data = null;
		}

		[TestMethod]
		[RunsOnUIThread]
		public void Should_Not_Include_Control_Points_Bounds()
		{
#if WINDOWS_UWP
			var SUT = new Path { Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0 0 C 0 0 25 25 0 50") };
#else
			var SUT = new Path { Data = "M 0 0 C 0 0 25 25 0 50" };
#endif

			SUT.Measure(new Size(300, 300));
			// Windows produces DesiredSize as 11x50
#if __ANDROID__
			Assert.IsTrue(Math.Abs(12 - SUT.DesiredSize.Width) <= 0.02, $"Actual size: {SUT.DesiredSize}");
			Assert.IsTrue(Math.Abs(51 - SUT.DesiredSize.Height) <= 0.02, $"Actual size: {SUT.DesiredSize}");
#elif __IOS__
			// The Width on iOS is 12.111 which is not very accurate.
			Assert.IsTrue(Math.Abs(12 - SUT.DesiredSize.Width) <= 0.2, $"Actual size: {SUT.DesiredSize}");
			Assert.IsTrue(Math.Abs(51 - SUT.DesiredSize.Height) <= 0.02, $"Actual size: {SUT.DesiredSize}");
#elif __WASM__
			Assert.IsTrue(Math.Abs(11 - SUT.DesiredSize.Width) <= 0.2, $"Actual size: {SUT.DesiredSize}");
			Assert.IsTrue(Math.Abs(50 - SUT.DesiredSize.Height) <= 0.02, $"Actual size: {SUT.DesiredSize}");
#else
			Assert.AreEqual(new Size(11, 50), SUT.DesiredSize);
#endif
		}
	}
}
