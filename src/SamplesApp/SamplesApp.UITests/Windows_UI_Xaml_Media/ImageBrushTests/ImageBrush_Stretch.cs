using System.Drawing;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.ImageBrushTests
{
	[TestFixture]
	public class ImageBrush_Stretch : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Stretch()
		{
			Run("UITests.Windows_UI_Xaml_Media.ImageBrushTests.ImageBrush_Stretch");
			var fill = _app.Query("FillBorder").Single().Rect;
			var uniformToFill = _app.Query("UniformToFillBorder").Single().Rect;
			var uniform = _app.Query("UniformBorder").Single().Rect;
			var none = _app.Query("NoneBorder").Single().Rect;

			using var screenshot = TakeScreenshot(nameof(When_Stretch));

			// All edges are red-ish
			ImageAssert.HasColorAt(screenshot, fill.CenterX, fill.Y + 6, "#FFEB1C24");
			ImageAssert.HasColorAt(screenshot, fill.CenterX, fill.Bottom - 6, "#FFEB1C24");
			ImageAssert.HasColorAt(screenshot, fill.X + 6, fill.CenterY, "#FFEB1C24");
			ImageAssert.HasColorAt(screenshot, fill.Right - 6, fill.CenterY, "#FFEB1C24");

			// Top and bottom are red-ish. Left and right are yellow-ish
			ImageAssert.HasColorAt(screenshot, uniformToFill.CenterX, uniformToFill.Y + 6, "#FFEB1C24");
			ImageAssert.HasColorAt(screenshot, uniformToFill.CenterX, uniformToFill.Bottom - 6, "#FFEB1C24");
			ImageAssert.HasColorAt(screenshot, uniformToFill.X + 6, uniformToFill.CenterY, "#FFFEF200");
			ImageAssert.HasColorAt(screenshot, uniformToFill.Right - 6, uniformToFill.CenterY, "#FFFEF200");

			// Top and bottom are same as backround. Left and right are red-ish
			ImageAssert.HasColorAt(screenshot, uniform.CenterX, uniform.Y + 6, Color.White);
			ImageAssert.HasColorAt(screenshot, uniform.CenterX, uniform.Bottom - 6, Color.White);
			ImageAssert.HasColorAt(screenshot, uniform.X + 6, uniform.CenterY, "#FFEB1C24");
			ImageAssert.HasColorAt(screenshot, uniform.Right - 6, uniform.CenterY, "#FFEB1C24");

			// Everything is green-ish
			ImageAssert.HasColorAt(screenshot, none.CenterX, none.Y + 6, "#FF0ED145");
			ImageAssert.HasColorAt(screenshot, none.CenterX, none.Bottom - 6, "#FF0ED145");
			ImageAssert.HasColorAt(screenshot, none.X + 6, none.CenterY, "#FF0ED145");
			ImageAssert.HasColorAt(screenshot, none.Right - 6, none.CenterY, "#FF0ED145");
		}
	}
}
