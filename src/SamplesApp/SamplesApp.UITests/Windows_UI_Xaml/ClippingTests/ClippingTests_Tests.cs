using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.ClippingTests
{
	[TestFixture]
	public class ClippingTests_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Clip_Is_Set_On_Container_Element()
		{
			Run("UITests.Windows_UI_Xaml.Clipping.Clipping652");

			var grid1 = _app.Marked("ClippingGrid1");
			var grid2 = _app.Marked("ClippingGrid2");

			_app.WaitForElement(grid1);
			_app.WaitForElement(grid2);

			var rect1 = grid1.FirstResult().Rect;
			var rect2 = grid2.FirstResult().Rect;

			var screenshot = TakeScreenshot("Clipping");

			ImageAssert.HasColorAt(screenshot, rect1.Right + 8, rect1.Y + 75, Color.Blue);
			ImageAssert.HasColorAt(screenshot, rect1.X + 75, rect1.Bottom + 8, Color.Blue);

			ImageAssert.HasColorAt(screenshot, rect2.Right + 8, rect2.Y + 75, Color.Blue);
			ImageAssert.HasColorAt(screenshot, rect2.X + 75, rect2.Bottom + 8, Color.Blue);
		}

		[Test]
		[AutoRetry]
		public void When_Clipped_Rounded_Corners()
		{
			Run("UITests.Windows_UI_Xaml.Clipping.Clipping4273");

			_app.WaitForElement("RoundedGrid");

			var rect = _app.GetRect("RoundedGrid");

			var centre = GetScaledCenter(rect);
			var scale = GetDisplayScreenScaling();
			var offset = 5 * (float)scale;
			var innerCornerX = rect.X + offset;
			var innerCornerY = rect.Y + offset;

			var screenshot = TakeScreenshot("ClippedCorners");
			ImageAssert.HasColorAt(screenshot, centre.X, centre.Y, Color.Blue);
			ImageAssert.HasColorAt(screenshot, innerCornerX, innerCornerY, Color.Red);

		}
	}
}
