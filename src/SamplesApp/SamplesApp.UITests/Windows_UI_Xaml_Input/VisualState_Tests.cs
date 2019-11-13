using System;
using System.Collections.Generic;
using System.Drawing;
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
	public class VisualState_Tests : SampleControlUITestBase
	{
		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestButtonReleasedOut() => TestReleasedOutState(
			"MyButton",
			"CommonStates.PointerOver",
			"CommonStates.Pressed",
			"CommonStates.Normal");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestIndeterminateToggleButtonReleasedOut() => TestReleasedOutState(
			"MyIndeterminateToggleButton",
			"CommonStates.IndeterminatePointerOver",
			"CommonStates.IndeterminatePressed",
			"CommonStates.Indeterminate");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestCheckedToggleButtonReleasedOut() => TestReleasedOutState(
			"MyCheckedToggleButton",
			"CommonStates.CheckedPointerOver",
			"CommonStates.CheckedPressed",
			"CommonStates.Checked");

		[Test]
		[Ignore("We get an invalid 'PointerOver' on release, a fix in pending in another PR")]
		public void TestUncheckedToggleButtonReleasedOut() => TestReleasedOutState(
			"MyUncheckedToggleButton",
			"CommonStates.UncheckedPointerOver",
			"CommonStates.UncheckedPressed",
			"CommonStates.Unchecked");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestRadioButtonReleasedOut() => TestReleasedOutState(
			"MyRadioButton",
			"CommonStates.PointerOver",
			"CommonStates.Pressed",
			"CommonStates.Normal");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestHyperlinkButtonReleasedOut() => TestReleasedOutState(
			"MyHyperlinkButton",
			"CommonStates.PointerOver",
			"CommonStates.Pressed",
			"CommonStates.Normal");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestIndeterminateCheckboxReleasedOut() => TestReleasedOutState(
			"MyIndeterminateCheckbox",
			"CombinedStates.IndeterminatePointerOver",
			"CombinedStates.IndeterminatePressed",
			"CombinedStates.IndeterminateNormal");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestCheckedCheckboxReleasedOut() => TestReleasedOutState(
			"MyCheckedCheckbox",
			"CombinedStates.CheckedPointerOver",
			"CombinedStates.CheckedPressed",
			"CombinedStates.CheckedNormal");

		[Test]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestUncheckedCheckboxReleasedOut() => TestReleasedOutState(
			"MyUncheckedCheckbox",
			"CombinedStates.UncheckedPointerOver",
			"CombinedStates.UncheckedPressed",
			"CombinedStates.UncheckedNormal");

		[Test]
		public void TestHyperlinkReleasedOut() => TestReleasedOutState(
			"MyHyperlink"); // There is no "VisualState" for Hyperlink, only a hardcoded opacity of .5 (kind-of like UWP)

		private void TestReleasedOutState(string target, params string[] expectedStates)
		{
			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.Buttons");

			var initial = TakeScreenshot("Initial");
			var rect = _app.WaitForElement(target).Single().Rect;

			// Press over and move out to release
			_app.DragCoordinates(rect.X + 2, rect.Y + 2, rect.X, rect.Y - 30);

			var final = TakeScreenshot("Final");
			var actualStates = _app
				.Marked("VisualStatesLog")
				.GetDependencyPropertyValue<string>("Text")
				.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
				.Where(line => line.StartsWith(target))
				.Select(line => line.Trim().Substring(target.Length + 1))
				.ToArray();

			if (expectedStates?.Any() ?? false)
			{
				CollectionAssert.AreEqual(expectedStates, actualStates, StringComparer.OrdinalIgnoreCase);
			}

			// For the comparison, we compare only the location of the control (i.e. we provide the rect).
			// This is required to NOT include the visual output ot the states (on the right of the test control)
			AssertScreenshotsAreEqual(initial, final, rect);
		}
	}
}
