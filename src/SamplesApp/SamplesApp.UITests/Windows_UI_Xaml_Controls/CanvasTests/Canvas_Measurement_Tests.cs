using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.CanvasTests
{
	public class Canvas_Measurement_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Measur_CanvasChildren()
		{
			Run("SamplesApp.Windows_UI_Xaml_Controls.Canvas.Measure_Children_In_Canvas");

			_app.WaitForElement("BBorderInCanvas");
			_app.WaitForElement("BBorderOutCanvas");

			var BorderInWidth = _app.Query(_app.Marked("BBorderInCanvas")).First().Rect.Width;
			var BorderInHeight = _app.Query(_app.Marked("BBorderInCanvas")).First().Rect.Height;

			var BorderOutWidth = _app.Query(_app.Marked("BBorderOutCanvas")).First().Rect.Width;
			var BorderOutHeight = _app.Query(_app.Marked("BBorderOutCanvas")).First().Rect.Height;

			if (BorderInWidth != BorderOutWidth || BorderInHeight != BorderOutHeight)
				Assert.Fail("Border in canvas measurement failed");

			TakeScreenshot($"Measure_Children_In_Canvas - Measure Border");
		}
	}
}
