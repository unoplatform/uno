using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;
using StringQuery = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppTypedSelector<string>>;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TimePickerTests
{
	public class DragCoordinates_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // Android is disabled https://github.com/unoplatform/uno/issues/1257
		public void DragBorder01()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Input.Pointers.DragCoordinates_Automated");

			Query rootCanvas = q => q.Marked("rootCanvas");
			Query myBorder = q => q.Marked("myBorder");
			Query topValue = q => q.Marked("borderPositionTop");
			Query leftValue = q => q.Marked("borderPositionLeft");

			_app.WaitForElement(rootCanvas);

			TakeScreenshot("tb01 - Initial");

			_app.WaitForDependencyPropertyValue(topValue, "Text", "0");
			_app.WaitForDependencyPropertyValue(leftValue, "Text", "0");

			TakeScreenshot("DragBorder01 - Step 1");

			var topBorderRect = _app.Query(myBorder).First().Rect;

			_app.DragCoordinates(topBorderRect.CenterX, topBorderRect.CenterY, topBorderRect.CenterX + 50, topBorderRect.CenterY + 50);

			TakeScreenshot("DragBorder01 - Step 2");

			_app.WaitForDependencyPropertyValue(topValue, "Text", "50");
			_app.WaitForDependencyPropertyValue(leftValue, "Text", "50");
		}
	}
}
