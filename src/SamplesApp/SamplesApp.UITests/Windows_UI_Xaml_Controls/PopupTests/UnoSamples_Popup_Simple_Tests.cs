using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.PopupTests
{
	public class UnoSamples_Popup_Simple_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry()]
		public void DismissiblePopup()
		{
			Run("Uno.UI.Samples.Content.UITests.Popup.Popup_Simple");

			_app.WaitForElement(_app.Marked("sampleContent"));

			_app.Wait(0.15f);

			var toggleButton = _app.Marked("TogglePopup");

			var v = toggleButton.GetDependencyPropertyValue("IsChecked");



			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", false);

			toggleButton.Tap();

			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", true);

			var popupContent = _app.Marked("popupContent");

			_app.WaitForElement(popupContent);

			popupContent.Tap();

			_app.Wait(0.25f);

			_app.WaitForElement(popupContent); // should remain opened

			var screenRect = _app.Marked("sampleContent").FirstResult().Rect;

			_app.TapCoordinates(10, screenRect.Bottom - 10); // click elsewhere on the page

			for (var i = 0; i < 5; i++)
			{
				if (_app.Marked("popupContent").FirstResult() == null)
				{
					return; // dismissed!
				}

				_app.Wait(i * 0.15f);
			}

			Assert.Fail("Popup not dismissed.");
		}

		[Test]
		[AutoRetry()]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // Android is disabled https://github.com/unoplatform/uno/issues/1631

		public void NonDismissiblePopup()
		{
			Run("Uno.UI.Samples.Content.UITests.Popup.Popup_Simple");

			_app.WaitForElement(_app.Marked("sampleContent"));
			var toggleButton = _app.Marked("TogglePopup2");
			var popupContent = _app.Marked("popupContent2");

			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", false);

			// 1. Open the popup
			toggleButton.Tap();
			_app.Wait(0.25f);
			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", true);
			_app.WaitForElement(popupContent);
			TakeScreenshot("Popup_Simple - NonDismissiblePopup - Popup opened");

			// 2. Tap on content should keep the popup opened
			popupContent.Tap();
			_app.Wait(0.25f);
			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", true);
			TakeScreenshot("Popup_Simple - NonDismissiblePopup - Popup stays open when tap on content");

			// 2. Tap out of the popup should keep the pop opened
			var screenRect = _app.Marked("sampleContent").FirstResult().Rect;
			_app.TapCoordinates(10, screenRect.Bottom - 10); // click elsewhere on the page
			_app.Wait(0.25f);
			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", true);
			TakeScreenshot("Popup_Simple - NonDismissiblePopup - Popup stays open when tap out of popup");

			// 3. Close the popup (to make sure to not pollute other tests)
			toggleButton.Tap(); // should dismiss here
			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", false);
			TakeScreenshot("Popup_Simple - NonDismissiblePopup - Popup closed"); // We add a screen shot in order to make sure that the check of the bool IsChecked is valid!
		}

		[Test]
		[AutoRetry]
		public void PopupWithOverlay()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Popup.Popup_Overlay_On");

			var before = TakeScreenshot("Before");
			var rect = _app.GetRect("LocatorRectangle");
			ImageAssert.HasColorAt(before, rect.CenterX, rect.CenterY, Color.Blue);

			_app.Tap("PopupCheckBox");

			_app.WaitForElement("PopupChild");

			var during = TakeScreenshot("During", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Status bar appears with clock*/);

			ImageAssert.AssertDoesNotHaveColorAt(during, rect.CenterX, rect.CenterY, Color.Blue);

			// Dismiss popup
			var screenRect = _app.Marked("sampleContent").FirstResult().Rect;
			_app.TapCoordinates(10, screenRect.Bottom - 10);

			_app.WaitForNoElement("PopupChild");

			var after = TakeScreenshot("After");

			ImageAssert.HasColorAt(after, rect.CenterX, rect.CenterY, Color.Blue);
		}
	}
}
