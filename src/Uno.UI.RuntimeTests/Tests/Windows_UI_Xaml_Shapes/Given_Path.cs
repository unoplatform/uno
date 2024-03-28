using System;
using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using SamplesApp.UITests;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	public class Given_Path
	{
		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/6846")]
		public void Should_not_throw_if_Path_Data_is_set_to_null()
		{
			// Set initial Data
			var SUT = new Path { Data = new RectangleGeometry() };

			// Switch back to null.  Should not throw an exception.
			SUT.Data = null;
		}

		[TestMethod]
		[RunsOnUIThread]
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
	}
}
