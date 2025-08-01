using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	//Web Assembly does not have a helper that take screenshots yet
	//MacOs interprets colors differently
#if __WASM__
	[Ignore]
#endif
	[TestClass]
	[RunsOnUIThread]
	public class Basics_AutomatedTransformation
	{
		private const string White = "#FFFFFF";
		private const float PixelIncertitude = 2;

		[TestMethod]
		public async Task When_Rotate()
		{
			const string colored = "#FF0000";
			var grid = new Basics_Automated_Transformation();
			var SUT = grid.RotateHost;
			var result = await Arrange(SUT);

			Assert(SUT, result, 50, 50, colored);

			// Top left
			Assert(SUT, result, 19 - PixelIncertitude, 0, White);
			Assert(SUT, result, 21 + PixelIncertitude, 0, colored);
			Assert(SUT, result, 0, 34 - PixelIncertitude, White);
			Assert(SUT, result, 0, 38 + PixelIncertitude, colored);

			// Bottom left
			Assert(SUT, result, 0, 78 - PixelIncertitude, colored);
			Assert(SUT, result, 0, 80 + PixelIncertitude, White);
			Assert(SUT, result, 34 - PixelIncertitude, 99, White);
			Assert(SUT, result, 38 + PixelIncertitude, 99, colored);

			// Top right
			Assert(SUT, result, 62 - PixelIncertitude, 0, colored);
			Assert(SUT, result, 65 + PixelIncertitude, 0, White);
			Assert(SUT, result, 99, 19 - PixelIncertitude, White);
			Assert(SUT, result, 99, 21 + PixelIncertitude, colored);

			// Bottom right
			Assert(SUT, result, 99, 62 - PixelIncertitude, colored);
			Assert(SUT, result, 99, 65 + PixelIncertitude, White);
			Assert(SUT, result, 78 - PixelIncertitude, 99, colored);
			Assert(SUT, result, 80 + PixelIncertitude, 99, White);
		}

		[TestMethod]
		public async Task When_Translate()
		{
			const string colored = "#FF8000";

			var grid = new Basics_Automated_Transformation();
			var SUT = grid.TranslateHost;
			var result = await Arrange(SUT);

			// Top left
			Assert(SUT, result, 14 - PixelIncertitude, 14 - PixelIncertitude, White);
			Assert(SUT, result, 15 + PixelIncertitude, 14 + PixelIncertitude, colored);

			// Bottom left
			Assert(SUT, result, 14 - PixelIncertitude, 99, White);
			Assert(SUT, result, 15 + PixelIncertitude, 99, colored);

			// Top right
			Assert(SUT, result, 99, 14 - PixelIncertitude, White);
			Assert(SUT, result, 99, 15 + PixelIncertitude, colored);
		}

		[TestMethod]
		public async Task When_Skew()
		{
			const string colored = "#FFFF00";

			var grid = new Basics_Automated_Transformation();
			var SUT = grid.SkewHost;
			var result = await Arrange(SUT);

			// Top left
			Assert(SUT, result, 0, 0, colored);

			// Bottom left
			Assert(SUT, result, 28 - PixelIncertitude, 72 + PixelIncertitude, White);
			Assert(SUT, result, 30 + PixelIncertitude, 70 - PixelIncertitude, colored);

			// Top right
			Assert(SUT, result, 72 + PixelIncertitude, 28 - PixelIncertitude, White);
			Assert(SUT, result, 70 - PixelIncertitude, 30 + PixelIncertitude, colored);

			// Bottom right
			Assert(SUT, result, 99, 99, colored);
		}

		[TestMethod]
		public async Task When_Scale()
		{
			const string colored = "#008000";

			var grid = new Basics_Automated_Transformation();
			var SUT = grid.ScaleHost;
			var result = await Arrange(SUT);

			// Top left
			Assert(SUT, result, 9 - PixelIncertitude, 9 - PixelIncertitude, White);
			Assert(SUT, result, 10 + PixelIncertitude, 10 + PixelIncertitude, colored);

			// Bottom left
			Assert(SUT, result, 9 - PixelIncertitude, 90 + PixelIncertitude, White);
			Assert(SUT, result, 10 + PixelIncertitude, 89 - PixelIncertitude, colored);

			// Top right
			Assert(SUT, result, 89 - PixelIncertitude, 10 + PixelIncertitude, colored);
			Assert(SUT, result, 90 + PixelIncertitude, 9 - PixelIncertitude, White);

			// Bottom right
			Assert(SUT, result, 89 - PixelIncertitude, 89 - PixelIncertitude, colored);
			Assert(SUT, result, 90 + PixelIncertitude, 90 + PixelIncertitude, White);
		}

		[TestMethod]
		public async Task When_Composite()
		{
			const string colored = "#0000FF";

			var grid = new Basics_Automated_Transformation();
			var SUT = grid.CompositeHost;
			var result = await Arrange(SUT);

			// Center
			Assert(SUT, result, 50, 50, colored);

			// Top left
			Assert(SUT, result, 41 - PixelIncertitude, 0, White);
			Assert(SUT, result, 42 + PixelIncertitude, 0, colored);

			// Bottom left
			Assert(SUT, result, 41 - PixelIncertitude, 70 - PixelIncertitude, White);
			Assert(SUT, result, 42 + PixelIncertitude, 70 - PixelIncertitude, colored);
			Assert(SUT, result, 42, 73 + PixelIncertitude, White);

			// Top right
			Assert(SUT, result, 87, 56 - PixelIncertitude, White);
			Assert(SUT, result, 87 - PixelIncertitude, 59 + PixelIncertitude, colored);
			Assert(SUT, result, 88 + PixelIncertitude, 59 + PixelIncertitude, White);

			// Bottom right
			Assert(SUT, result, 87 - PixelIncertitude, 99, colored);
			Assert(SUT, result, 88 + PixelIncertitude, 99, White);
		}

		[Test]
		public async Task When_Group()
		{
			const string colored = "#A000C0";

			var grid = new Basics_Automated_Transformation();
			FrameworkElement SUT = grid.GroupHost;
			var result = await Arrange(SUT);

			// Center
			Assert(SUT, result, 50, 50, colored);

			// Top left
			Assert(SUT, result, 41 - PixelIncertitude, 0, White);
			Assert(SUT, result, 42 + PixelIncertitude, 0, colored);

			// Bottom left
			Assert(SUT, result, 41 - PixelIncertitude, 70 - PixelIncertitude, White);
			Assert(SUT, result, 42 + PixelIncertitude, 70 - PixelIncertitude, colored);
			Assert(SUT, result, 42, 73 + PixelIncertitude, White);

			//Topp right
			Assert(SUT, result, 87, 56 - PixelIncertitude, White);
			Assert(SUT, result, 87 - PixelIncertitude, 59 + PixelIncertitude, colored);
			Assert(SUT, result, 88 + PixelIncertitude, 59 + PixelIncertitude, White);

			// Bottom right
			Assert(SUT, result, 87 - PixelIncertitude, 99, colored);
			Assert(SUT, result, 88 + PixelIncertitude, 99, White);
		}

		private async Task<RawBitmap> Arrange(FrameworkElement SUT)
		{
#if __ANDROID__
			if (SUT is Android.Views.View view && view.Parent is Controls.BindableView bindableView)
			{
				bindableView.RemoveView(view);
			}
#endif
			await UITestHelper.Load(SUT);
			var result = await UITestHelper.ScreenShot(SUT);
			await WindowHelper.WaitForIdle();
			return result;
		}

		private void Assert(FrameworkElement SUT, RawBitmap result, float x, float y, string color)
		{
			float border = 3;
			float width = (float)SUT.ActualWidth;
			float height = (float)SUT.ActualHeight;
			float dpiScale = width / (border + 100 + border);

			x *= dpiScale;
			y *= dpiScale;
			border *= dpiScale;

			x = x >= 0 ? x + border : width + x - (2 * border);
			y = y >= 0 ? y + border : height + y - (2 * border);

			ImageAssert.HasColorAt(result, x, y, color, tolerance: 25);
		}
	}
}

