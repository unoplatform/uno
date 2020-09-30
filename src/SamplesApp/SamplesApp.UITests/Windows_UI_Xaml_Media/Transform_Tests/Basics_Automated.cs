using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests._Utils;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.Transform_Tests
{
	[TestFixture]
	public partial class Basics_Automated : SampleControlUITestBase
	{
		private IAppRect _sut;
		private ScreenshotInfo _result;

		[Test]
		[AutoRetry]
		public void When_Rotate()
		{
			const string colored = "#FF0000";
			const float i = 2; // pixel incertitude

			Arrange("RotateHost");

			Assert(50, 50, colored);

			// Top left
			Assert(19 - i, 0);
			Assert(21 + i, 0, colored);
			Assert(0, 34 - i);
			Assert(0, 38 + i, colored);

			// Bottom left
			Assert(0, 78 - i, colored);
			Assert(0, 80 + i);
			Assert(34 - i, 99);
			Assert(38 + i, 99, colored);

			// Top right
			Assert(62 - i, 0, colored);
			Assert(65 + i, 0);
			Assert(99, 19 - i);
			Assert(99, 21 + i, colored);

			// Bottom right
			Assert(99, 62 - i, colored);
			Assert(99, 65 + i);
			Assert(78 - i, 99, colored);
			Assert(80 + i, 99);
		}

		[Test]
		[AutoRetry]
		public void When_Translate()
		{
			const string colored = "#FF8000";
			const float i = 2; // pixel incertitude

			Arrange("TranslateHost");

			// Top left
			Assert(14 - i, 14 - i);
			Assert(15 + i, 14 + i, colored);

			// Bottom left
			Assert(14 - i, 99);
			Assert(15 + i, 99, colored);

			// Top right
			Assert(99, 14 - i);
			Assert(99, 15 + i, colored);
		}

		[Test]
		[AutoRetry]
		public void When_Skew()
		{
			const string colored = "#FFFF00";
			const float i = 2; // pixel incertitude

			Arrange("SkewHost");

			// Top left
			Assert(0, 0, colored);

			// Bottom left
			Assert(28 - i, 72 + i);
			Assert(30 + i, 70 - i, colored);

			// Top right
			Assert(72 + i, 28 - i);
			Assert(70 - i, 30 + i, colored);

			// Bottom right
			Assert(99, 99, colored);
		}

		[Test]
		[AutoRetry]
		public void When_Scale()
		{
			const string colored = "#008000";
			const float i = 2; // pixel incertitude

			Arrange("ScaleHost");

			// Top left
			Assert(9 - i, 9 - i);
			Assert(10 + i, 10 + i, colored);

			// Bottom left
			Assert(9 - i, 90 + i);
			Assert(10 + i, 89 - i, colored);

			// Top right
			Assert(89 - i, 10 + i, colored);
			Assert(90 + i, 9 - i);

			// Bottom right
			Assert(89 - i, 89 - i, colored);
			Assert(90 + i, 90 + i);
		}

		[Test]
		[AutoRetry]
		public void When_Composite()
		{
			const string colored = "#0000FF";
			const float i = 2; // pixel incertitude

			Arrange("CompositeHost");

			// Center
			Assert(50, 50, colored);

			// Top left
			Assert(41 - i, 0);
			Assert(42 + i, 0, colored);

			// Bottom left
			Assert(41 - i, 70 - i);
			Assert(42 + i, 70 - i, colored);
			Assert(42, 73 + i);

			// Top right
			Assert(87, 56 - i);
			Assert(87 - i, 59 + i, colored);
			Assert(88 + i, 59 + i);

			// Bottom right
			Assert(87 - i, 99, colored);
			Assert(88 + i, 99);
		}

		[Test]
		[AutoRetry]
		public void When_Group()
		{
			const string colored = "#A000C0";
			const float i = 2; // pixel incertitude

			Arrange("GroupHost");

			// Center
			Assert(50, 50, colored);

			// Top left
			Assert(41 - i, 0);
			Assert(42 + i, 0, colored);

			// Bottom left
			Assert(41 - i, 70 - i);
			Assert(42 + i, 70 - i, colored);
			Assert(42, 73 + i);

			// Top right
			Assert(87, 56 - i);
			Assert(87 - i, 59 + i, colored);
			Assert(88 + i, 59 + i);

			// Bottom right
			Assert(87 - i, 99, colored);
			Assert(88 + i, 99);
		}

		private void Arrange(string sutName)
		{
			Run("UITests.Shared.Windows_UI_Xaml_Media.Transform.Basics_Automated");

			_sut = _app.WaitForElement(sutName).Single().Rect;
			_result = TakeScreenshot("Result", ignoreInSnapshotCompare: false);
		}

		private void Assert(float x, float y, string color)
			=> Assert(x, y, ColorCodeParser.Parse(color));

		private void Assert(float x, float y, Color? color = null)
		{
			float b = 3; // border
			var dpiScale = _sut.Width / (b + 100 + b);

			x *= dpiScale;
			y *= dpiScale;
			b *= dpiScale;

			x = x >= 0 ? _sut.X + x + b : _sut.Right + x - (2*b);
			y = y >= 0 ? _sut.Y + y + b : _sut.Bottom + y - (2 * b);

			ImageAssert.HasColorAt(_result, x, y, color ?? Color.White, tolerance: 25);
		}

		[TearDown]
		public void TearDown()
		{
			_result?.Dispose();
			_result = null;
		}
	}
}
