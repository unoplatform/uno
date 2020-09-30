using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ComboBoxTests
{
	[TestFixture]
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled for iOS: https://github.com/unoplatform/uno/issues/1955
	public partial class ComboxBox_DropDownPlacement : SampleControlUITestBase
	{
		[Test] [AutoRetry] public void NoSelectionPreferAbove() => TestAbove();
		[Test] [AutoRetry] public void NoSelectionPreferCentered() => TestCentered();
		[Test] [AutoRetry] public void NoSelectionPreferBelow() => TestBelow();
		[Test] [AutoRetry] public void NoSelectionPreferAuto() => TestCentered();

		[Test] [AutoRetry] public void FirstSelectedPreferAbove() => TestAbove();
		[Test] [AutoRetry] public void FirstSelectedPreferCentered() => TestCentered();
		[Test] [AutoRetry] public void FirstSelectedPreferBelow() => TestBelow();
		[Test] [AutoRetry] public void FirstSelectedPreferAuto() => TestBelow();

		[Test] [AutoRetry] public void MiddleSelectedPreferAbove() => TestAbove();
		[Test] [AutoRetry] public void MiddleSelectedPreferCentered() => TestCentered();
		[Test] [AutoRetry] public void MiddleSelectedPreferBelow() => TestBelow();
		[Test] [AutoRetry] public void MiddleSelectedPreferAuto() => TestCentered();

		[Test] [AutoRetry] public void LastSelectedPreferAbove() => TestAbove();
		[Test] [AutoRetry] public void LastSelectedPreferCentered() => TestCentered();
		[Test] [AutoRetry] public void LastSelectedPreferBelow() => TestBelow();
		[Test] [AutoRetry] public void LastSelectedPreferAuto() => TestAbove();

		private void TestAbove([CallerMemberName] string test = null) => Test(test: test, aboveEquals: false, belowEquals: true);
		private void TestCentered([CallerMemberName] string test = null) => Test(test: test, aboveEquals: false, belowEquals: false);
		private void TestBelow([CallerMemberName] string test = null) => Test(test: test, aboveEquals: true, belowEquals: false);

		private void Test(bool aboveEquals, bool belowEquals, [CallerMemberName] string test = null)
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ComboBox.ComboBox_DropDownPlacement", skipInitialScreenshot: true);

			_app.WaitForElement(test);

			var rect = _app.GetPhysicalRect(test);

			using var notOpened = TakeScreenshot("not_opened", ignoreInSnapshotCompare: true);

			// Open the combo
			_app.TapCoordinates(rect.Right - 10, rect.CenterY);

			// Wait for popup to open
			_app.WaitForElement("PopupBorder");

			using var opened = TakeScreenshot("opened", ignoreInSnapshotCompare: true);

			// Make sure to close the combo
			_app.TapCoordinates(rect.X - 10, rect.Y - 10);

			// Assertions
			const int testHeight = 50;
			const int tolerance = 10; // Margins, etc
			var above = new Rectangle((int)rect.X, (int)rect.Y - testHeight - tolerance, (int)rect.Width, testHeight);
			var below = new Rectangle((int)rect.X, (int)rect.Bottom + tolerance, (int)rect.Width, testHeight);

			if (aboveEquals)
			{
				ImageAssert.AreEqual(notOpened, opened, above);
			}
			else
			{
				ImageAssert.AreNotEqual(notOpened, opened, above);
			}

			if (belowEquals)
			{
				// On Android the StatusBar may flicker when we open a ComboBox, so validate the 
				ImageAssert.AreEqual(notOpened, opened, below);
			}
			else
			{
				ImageAssert.AreNotEqual(notOpened, opened, below);
			}
		}
	}
}
