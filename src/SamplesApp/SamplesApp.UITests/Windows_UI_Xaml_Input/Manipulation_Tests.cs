using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public class Manipulation_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TestManipulation()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.ManipulationEvents");

			var rect = _app.WaitForElement("_thumb").Single().Rect;
			_app.DragCoordinates(rect.X + 10, rect.Y + 10, rect.X - 100, rect.Y - 100);

			TakeScreenshot("Result");
		}
	}
}
