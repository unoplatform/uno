using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public class VisualState_Tests : SampleControlUITestBase
	{
		[Test] public void TestButtonReleasedOut() => TestReleasedOutState();

		[Test] public void TestIndeterminateToggleButtonReleasedOut() => TestReleasedOutState();
		[Test] public void TestCheckedToggleButtonReleasedOut() => TestReleasedOutState();
		[Test] public void TestUncheckedToggleButtonReleasedOut() => TestReleasedOutState();

		[Test] public void TestRadioButtonReleasedOut() => TestReleasedOutState();

		[Test] public void TestHyperlinkButtonReleasedOut() => TestReleasedOutState();

		[Test] public void TestIndeterminateCheckboxReleasedOut() => TestReleasedOutState();
		[Test] public void TestCheckedCheckboxReleasedOut() => TestReleasedOutState();
		[Test] public void TestUncheckedCheckboxReleasedOut() => TestReleasedOutState();

		[Test] public void TestHyperlinkReleasedOut() => TestReleasedOutState();

		private void TestReleasedOutState([CallerMemberName] string target = null)
		{
			target = "My" + target.Substring("Test".Length, target.Length - "TestReleasedOut".Length);

			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.Buttons");

			var rect = _app.WaitForElement(target).Single().Rect;

			// Press over and move out to release
			_app.DragCoordinates(rect.X + 2, rect.Y + 2, rect.X, rect.Y - 30);

			// Validate the state was restored to default
			TakeScreenshot("Result");
		}
	}
}
