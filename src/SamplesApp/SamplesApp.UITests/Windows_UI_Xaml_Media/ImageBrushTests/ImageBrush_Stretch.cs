using System.Drawing;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.ImageBrushTests
{
	[TestFixture]
	public partial class ImageBrush_Stretch : SampleControlUITestBase
	{
		private const string Redish = "#FFEB1C24";
		private const string Yellowish = "#FFFEF200";
		private const string Greenish = "#FF0ED145";

		[Test]
		[AutoRetry]

		// Other platforms are tested in RuntimeTests:
		// src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Media/Given_ImageBrushStretch.cs
		// This UI Test can be deleted once we can test Wasm in RuntimeTests.
		// This is currently blocked due to lack of support for RenderTargetBitmap on Wasm.
		[ActivePlatforms(Platform.Browser)]
		public void When_Stretch()
		{
			Run("UITests.Windows_UI_Xaml_Media.ImageBrushTests.ImageBrush_Stretch");
			var fill = _app.Query("FillBorder").Single().Rect;
			var uniformToFill = _app.Query("UniformToFillBorder").Single().Rect;
			var uniform = _app.Query("UniformBorder").Single().Rect;
			var none = _app.Query("NoneBorder").Single().Rect;

			using var screenshot = TakeScreenshot(nameof(When_Stretch));

			// All edges are red-ish
			ImageAssert.HasColorAt(screenshot, fill.CenterX, fill.Y + 6, Redish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, fill.CenterX, fill.Bottom - 6, Redish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, fill.X + 6, fill.CenterY, Redish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, fill.Right - 6, fill.CenterY, Redish, tolerance: 5);

			// Top and bottom are red-ish. Left and right are yellow-ish
			ImageAssert.HasColorAt(screenshot, uniformToFill.CenterX, uniformToFill.Y + 6, Redish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, uniformToFill.CenterX, uniformToFill.Bottom - 6, Redish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, uniformToFill.X + 6, uniformToFill.CenterY, Yellowish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, uniformToFill.Right - 6, uniformToFill.CenterY, Yellowish, tolerance: 5);

			// Top and bottom are same as backround. Left and right are red-ish
			ImageAssert.HasColorAt(screenshot, uniform.CenterX, uniform.Y + 6, Color.FromArgb(255, 243, 243, 243), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, uniform.CenterX, uniform.Bottom - 6, Color.FromArgb(255, 243, 243, 243), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, uniform.X + 6, uniform.CenterY, Redish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, uniform.Right - 6, uniform.CenterY, Redish, tolerance: 5);

			// Everything is green-ish
			ImageAssert.HasColorAt(screenshot, none.CenterX, none.Y + 6, Greenish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, none.CenterX, none.Bottom - 6, Greenish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, none.X + 6, none.CenterY, Greenish, tolerance: 5);
			ImageAssert.HasColorAt(screenshot, none.Right - 6, none.CenterY, Greenish, tolerance: 5);
		}
	}
}
