using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public partial class VisualState_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestButtonReleasedOut() => TestButtonReleasedOutState(
			"MyButton",
			"CommonStates.PointerOver",
			"CommonStates.Pressed",
			"CommonStates.Normal");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestIndeterminateToggleButtonReleasedOut() => TestButtonReleasedOutState(
			"MyIndeterminateToggleButton",
			"CommonStates.IndeterminatePointerOver",
			"CommonStates.IndeterminatePressed",
			"CommonStates.Indeterminate");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestCheckedToggleButtonReleasedOut() => TestButtonReleasedOutState(
			"MyCheckedToggleButton",
			"CommonStates.CheckedPointerOver",
			"CommonStates.CheckedPressed",
			"CommonStates.Checked");

		[Test]
		[AutoRetry]
		[Ignore("We get an invalid 'PointerOver' on release, a fix in pending in another PR")]
		public void TestUncheckedToggleButtonReleasedOut() => TestButtonReleasedOutState(
			"MyUncheckedToggleButton",
			"CommonStates.UncheckedPointerOver",
			"CommonStates.UncheckedPressed",
			"CommonStates.Unchecked");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestRadioButtonReleasedOut() => TestButtonReleasedOutState(
			"MyRadioButton",
			"CommonStates.PointerOver",
			"CommonStates.Pressed",
			"CommonStates.Normal");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestHyperlinkButtonReleasedOut() => TestButtonReleasedOutState(
			"MyHyperlinkButton",
			"CommonStates.PointerOver",
			"CommonStates.Pressed",
			"CommonStates.Normal");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestIndeterminateCheckboxReleasedOut() => TestButtonReleasedOutState(
			"MyIndeterminateCheckbox",
			"CombinedStates.IndeterminatePointerOver",
			"CombinedStates.IndeterminatePressed",
			"CombinedStates.IndeterminateNormal");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestCheckedCheckboxReleasedOut() => TestButtonReleasedOutState(
			"MyCheckedCheckbox",
			"CombinedStates.CheckedPointerOver",
			"CombinedStates.CheckedPressed",
			"CombinedStates.CheckedNormal");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TestUncheckedCheckboxReleasedOut() => TestButtonReleasedOutState(
			"MyUncheckedCheckbox",
			"CombinedStates.UncheckedPointerOver",
			"CombinedStates.UncheckedPressed",
			"CombinedStates.UncheckedNormal");

		[Test]
		[AutoRetry]
		public void TestHyperlinkReleasedOut() => TestButtonReleasedOutState(
			"MyHyperlink"); // There is no "VisualState" for Hyperlink, only a hardcoded opacity of .5 (kind-of like UWP)

		[Test]
		[AutoRetry]
		public void TestListViewReleasedOut()
		{
			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.ListViewItem");

			using var initial = TakeScreenshot("Initial", ignoreInSnapshotCompare: true);
			var rect = _app.WaitForElement("MyListView").Single().Rect;

			// Press over and move out to release
			_app.DragCoordinates(rect.X + 10, rect.Y + 10, rect.X - 30, rect.Y);

			using var final = TakeScreenshot("Final");
			ImageAssert.AreEqual(initial, final, rect);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // WASM-only test, but very flaky #9080
		public void TestTextBoxReleaseOutFocused()
		{
			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.TextBox_VisualStates");

			// Note: We do not validateFinalStateScreenShot as we are expecting to finish "focused" so may have the flashing cursor.
			TestVisualTests("MyTextBox", ReleaseOut, validateFinalStateScreenShot: false, "CommonStates.PointerOver", "CommonStates.Focused");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(/*Platform.Android, */Platform.iOS)] // For touch, focus should be set only when released over the TextBox
		public void TestTextBoxReleaseOutUnfocused()
		{
			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.TextBox_VisualStates");

			TestVisualTests("MyTextBox", ReleaseOut, validateFinalStateScreenShot: true, "CommonStates.PointerOver", "CommonStates.Normal");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android, Platform.Browser)]
		public void TestTextBoxTap()
		{
			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.TextBox_VisualStates");

			// Note: We don not validateFinalStateScreenShot as we are expecting to finish "focused" so may have the flashing cursor.
			TestVisualTests("MyTextBox", Tap, validateFinalStateScreenShot: false, "CommonStates.PointerOver", "CommonStates.Focused");
		}

		private void TestButtonReleasedOutState(string target, params string[] expectedStates)
		{
			Run("UITests.Shared.Windows_UI_Input.VisualStatesTests.Buttons");
			TestVisualTests(target, ReleaseOut, true, expectedStates);
		}

		// Press over and move out to release
		private void ReleaseOut(IAppRect target)
			=> _app.DragCoordinates(target.X + 2, target.Y + 2, target.X, target.Y - 30);

		private void Tap(IAppRect target)
			=> _app.TapCoordinates(target.X + 2, target.Y + 2);

		private void TestVisualTests(string targetName, Action<IAppRect> act, bool validateFinalStateScreenShot, params string[] expectedStates)
		{
			using var initial = TakeScreenshot("Initial", ignoreInSnapshotCompare: true);
			var target = _app.WaitForElement(targetName).Single().Rect;

			act(target);

			using var final = TakeScreenshot("Final", ignoreInSnapshotCompare: true);
			var actualStates = _app
				.Marked("VisualStatesLog")
				.GetDependencyPropertyValue<string>("Text")
				.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
				.Where(line => line.StartsWith(targetName))
				.Select(line => line.Trim().Substring(targetName.Length + 1))
				.ToArray();

			if (expectedStates?.Any() ?? false)
			{
				CollectionAssert.AreEqual(expectedStates, actualStates, StringComparer.OrdinalIgnoreCase);
			}

			if (validateFinalStateScreenShot)
			{
				// For the comparison, we compare only the location of the control (i.e. we provide the rect).
				// This is required to NOT include the visual output ot the states (on the right of the test control)
				ImageAssert.AreEqual(initial, final, target);
			}
		}
	}
}
