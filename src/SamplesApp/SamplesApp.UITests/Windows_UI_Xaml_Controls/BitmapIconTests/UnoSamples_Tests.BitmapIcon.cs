using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			var icon1 = _app.Marked("icon1");
			var icon2 = _app.Marked("icon2");
			_app.WaitForElement(colorChange);

			TakeScreenshot("Initial");

			_app.Tap(colorChange);

			var color1 = icon1.GetDependencyPropertyValue<string>("Foreground");
			var color2 = icon2.GetDependencyPropertyValue<string>("Foreground");

			_app.WaitForDependencyPropertyValue(icon1, "Foreground", "[SolidColorBrush [Color: 000000FF;000000FF;000000FF;00000000]]");
			_app.WaitForDependencyPropertyValue(icon2, "Foreground", "[SolidColorBrush [Color: 000000FF;00000000;00000080;00000000]]");

			TakeScreenshot("Changed");
		}
	}
}
