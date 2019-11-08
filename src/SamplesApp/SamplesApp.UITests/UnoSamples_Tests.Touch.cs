using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Touch_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TouchEventsTests.Touch");

			_app.WaitForElement(_app.Marked("button_inside_grid"));

			var touchLog = _app.Marked("touchLog");

			{
				var button1 = _app.Marked("button_inside_grid");
				var position = button1.FirstResult().Rect;

				_app.TapCoordinates(position.X + 5, position.Y + 5);

				_app.WaitFor(() => "0 - button inside grid" == touchLog.GetDependencyPropertyValue<string>("Text"));

				Assert.AreEqual("0 - button inside grid", touchLog.GetDependencyPropertyValue<string>("Text"));

				TakeScreenshot("0 - button inside grid");
			}

			{
				var button = _app.Marked("button_border_on_top");
				var border = _app.Marked("button_border_on_top_border");

				border.Tap();

				var position = button.FirstResult().Rect;
				_app.TapCoordinates(position.X + 2, position.Y + 2);

				_app.WaitFor(() => "1 - button under grid" == touchLog.GetDependencyPropertyValue<string>("Text"));

				Assert.AreEqual("1 - button under grid", touchLog.GetDependencyPropertyValue<string>("Text"));

				TakeScreenshot("0 - button inside grid");
			}
		}
	}
}
