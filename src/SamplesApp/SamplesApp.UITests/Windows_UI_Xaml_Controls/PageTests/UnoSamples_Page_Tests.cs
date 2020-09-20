using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.PivotTests
{
	public class UnoSamples_Page_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry()]
		public void Pivot_Non_PivotItem_Items()
		{
			Run("UITests.Windows_UI_Xaml_Controls.PageTests.Page_Automated");

			_app.WaitForElement(_app.Marked("_empty"));

			var content = TakeScreenshot("afterColor");

			var scale = (float)GetDisplayScreenScaling();
			var emptyRect = _app.GetRect("_empty");
			var transparentRect = _app.GetRect("_transparent");
			var solidRect = _app.GetRect("_colored");

			const int offset = 30;
			ImageAssert.HasColorAt(content, (emptyRect.X + offset) * scale, (emptyRect.Y + offset) * scale, Color.Red);
			ImageAssert.HasColorAt(content, (transparentRect.X + offset) * scale, (transparentRect.Y + offset) * scale, Color.Red);
			ImageAssert.HasColorAt(content, (solidRect.X + offset) * scale, (solidRect.Y + offset) * scale, Color.Blue);
		}
	}
}
