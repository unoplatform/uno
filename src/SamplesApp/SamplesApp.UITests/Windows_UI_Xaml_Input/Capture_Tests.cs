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
	public class Capture_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)] // This fails with unit test
		public void TestSimple()
			=> RunTest("Simple", TouchAndMoveOut);

		[Test]
		[AutoRetry]
		public void TestVisibility()
			=> RunTest("Visibility");

		[Test]
		[AutoRetry]
		public void TestNestedVisibility()
			=> RunTest("NestedVisibility");

		[Test]
		[AutoRetry]
		[Ignore("Inconsistent behavior between manual and unit test")]
		public void TestIsEnabled()
			=> RunTest("IsEnabled");

		[Test]
		[AutoRetry]
		[Ignore("Inconsistent behavior between manual and unit test")]
		[ActivePlatforms(Platform.Browser)] // The IsEnabled property is not inherited on other platforms yet.
		public void TestNestedIsEnabled()
			=> RunTest("NestedIsEnabled");


		private readonly Action<QueryEx> TouchAndHold = element => /*element.TouchAndHold() not implemented ... we can use tap instead */ element.Tap();
		private void TouchAndMoveOut(QueryEx element)
		{
			var rect = _app.WaitForElement(element).Single().Rect;
			_app.DragCoordinates(rect.X + 10, rect.Y + 10, rect.Right + 10, rect.Y + 10);
		}

		private void RunTest(string testName, Action<QueryEx> act = null)
		{
			act = act ?? TouchAndHold;

			Run("UITests.Shared.Windows_UI_Input.PointersTests.Capture");

			var target = _app.Marked($"{testName}Target");
			var result = _app.Marked($"{testName}Result");

			_app.WaitForElement(target);
			act(target);

			TakeScreenshot("Result");

			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}
	}
}
