using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.BitmapIconTests
{
	[TestFixture]
	public partial class BitmapIcon_Tests : SampleControlUITestBase
	{
		private bool ContainsColor(Bitmap bitmap, Rectangle rect, Color color)
		{
			for (var x = rect.Left; x < rect.Right; x++)
			{
				for (var y = rect.Top; y < rect.Bottom; y++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if ((pixel.A, pixel.R, pixel.G, pixel.B) == (color.A, color.R, color.G, color.B))
					{
						return true;
					}
				}
			}

			return false;
		}

		[Test]
		[AutoRetry]
		public void When_BitmapIcon_Generic()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.BitmapIconTests.BitmapIcon_Generic");

			var colorChange = _app.Marked("colorChange");
			var icon1 = _app.Query("icon1").Single();
			var icon2 = _app.Query("icon2").Single();
			_app.WaitForElement(colorChange);

			using var initial = TakeScreenshot("Initial");
			
			var bitmap = initial.GetBitmap();
			Assert.IsTrue(ContainsColor(bitmap, icon1.Rect.ToRectangle(), Color.Red));
			Assert.IsTrue(ContainsColor(bitmap, icon2.Rect.ToRectangle(), Color.Blue));

			_app.FastTap(colorChange);

			using var afterColorChange = TakeScreenshot("Changed");
			bitmap = afterColorChange.GetBitmap();
			Assert.IsTrue(ContainsColor(bitmap, icon1.Rect.ToRectangle(), Color.Yellow));
			Assert.IsTrue(ContainsColor(bitmap, icon2.Rect.ToRectangle(), Color.Green));

			_app.WaitForDependencyPropertyValue(_app.Marked("icon1"), "Foreground", "[SolidColorBrush #FFFFFF00]");
			_app.WaitForDependencyPropertyValue(_app.Marked("icon2"), "Foreground", "[SolidColorBrush #FF008000]");
		}
	}
}
