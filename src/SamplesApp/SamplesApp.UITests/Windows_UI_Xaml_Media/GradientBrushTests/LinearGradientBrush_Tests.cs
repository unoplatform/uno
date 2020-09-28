using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.GradientBrushTests
{
	[TestFixture]
	public class LinearGradientBrush_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_GradientStops_Changed()
		{
			Run("UITests.Windows_UI_Xaml_Media.GradientBrushTests.LinearGradientBrush_Change_Stops");

			var rectangle = _app.Marked("GradientBrushRectangle");

			_app.WaitForElement(rectangle);

			var screenRect = _app.GetRect(rectangle);

			var before = TakeScreenshot("Before");

			_app.FastTap("ChangeBrushButton");

			_app.WaitForText("StatusTextBlock", "Changed");

			var after = TakeScreenshot("After");

			ImageAssert.AreNotEqual(before, after, screenRect);
		}
	}
}
