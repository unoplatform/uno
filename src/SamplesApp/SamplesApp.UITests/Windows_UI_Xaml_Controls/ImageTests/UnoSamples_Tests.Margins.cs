using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests
{
	public partial class UnoSamples_Tests
	{
		[Test]
		[AutoRetry]
		public void Image_Margins_Identical()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.Image_Margins");

			_app.WaitForElement("rect4");

			var imageRects = new[] { "img0", "img1", "img2", "img3", "img4" }
				.Select(x => _app.GetPhysicalRect(x))
				.ToArray();

			var rects = new[] { "rect0", "rect1", "rect2", "rect3", "rect4" }
				.Select(x => _app.GetPhysicalRect(x))
				.ToArray();

			var logicalRect4 = _app.GetLogicalRect("rect4");

			using (new AssertionScope())
			{
				var y = imageRects[0].Y;
				var w = imageRects[0].Width;
				var h = imageRects[0].Height;

				imageRects[1].Y.Should().Be(y);
				imageRects[2].Y.Should().Be(y);
				imageRects[3].Y.Should().Be(y);
				imageRects[4].Y.Should().Be(y);
				imageRects[1].Width.Should().Be(w);
				imageRects[2].Width.Should().Be(w);
				imageRects[3].Width.Should().Be(w);
				imageRects[4].Width.Should().Be(w);
				imageRects[1].Height.Should().Be(h);
				imageRects[2].Height.Should().Be(h);
				imageRects[3].Height.Should().Be(h);
				imageRects[4].Height.Should().Be(h);
			}

			using (new AssertionScope())
			{
				var y = rects[0].Y;
				var h = rects[0].Height;

				rects[1].Y.Should().Be(y);
				rects[2].Y.Should().Be(y);
				rects[3].Y.Should().Be(y);
				rects[4].Y.Should().Be(y);
				rects[1].Height.Should().Be(h);
				rects[2].Height.Should().Be(h);
				rects[3].Height.Should().Be(h);
				rects[4].Height.Should().Be(h);
			}

			// Take screenshot
			TakeScreenshot("WriteableBitmap_Invalidate - Result");
		}

	}
}
