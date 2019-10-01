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
	public class EventSequence_Tests : SampleControlUITestBase
	{
		[Test]
		public void TestTap()
		{
			Run("UITests.Shared.Windows_UI_Input.PointersTests.EventsSequences");

			var target = _app.Marked("TestTapTarget");
			var validate = _app.Marked("TestTapValidate");
			var result = _app.Marked("TestTapResult");

			_app.WaitForElement(target);
			target.Tap();

			validate.Tap();
			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}
	}
}
