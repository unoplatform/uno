using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public partial class HitTest_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Control_Partially_Hit_Transparent()
		{
			Run("UITests.Windows_UI_Input.PointersTests.HitTest_Control");

			_app.WaitForElement("FrontControl");

			Assert.AreEqual("None", _app.GetText("ResultTextBlock"));

			var controlRect = _app.GetPhysicalRect("FrontControl");

			_app.TapCoordinates(controlRect.X + 50, controlRect.Y + 50); // Tap hit-test-opaque border

			_app.WaitForText("ResultTextBlock", "Front control pressed");

			_app.FastTap("ResetButton");
			_app.WaitForText("ResultTextBlock", "None");

			_app.TapCoordinates(controlRect.X + 150, controlRect.Y + 50); // Tap IsHitTestVisible=false border

			_app.WaitForText("ResultTextBlock", "Behind control pressed");

			_app.FastTap("ResetButton");
			_app.WaitForText("ResultTextBlock", "None");

			_app.TapCoordinates(controlRect.X + 50, controlRect.Y + 150); // Tap Background=null Grid

			_app.WaitForText("ResultTextBlock", "Behind control pressed");
		}
	}
}
