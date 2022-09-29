#nullable disable

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
			
			ImageAssert.HasColorInRectangle(initial, icon1.Rect.ToRectangle(), Color.Red);
			ImageAssert.HasColorInRectangle(initial, icon2.Rect.ToRectangle(), Color.Blue);

			_app.FastTap(colorChange);

			using var afterColorChange = TakeScreenshot("Changed");

			ImageAssert.HasColorInRectangle(afterColorChange, icon1.Rect.ToRectangle(), Color.Yellow);
			ImageAssert.HasColorInRectangle(afterColorChange, icon2.Rect.ToRectangle(), Color.Green);

			_app.WaitForDependencyPropertyValue(_app.Marked("icon1"), "Foreground", "[SolidColorBrush #FFFFFF00]");
			_app.WaitForDependencyPropertyValue(_app.Marked("icon2"), "Foreground", "[SolidColorBrush #FF008000]");
		}
	}
}
