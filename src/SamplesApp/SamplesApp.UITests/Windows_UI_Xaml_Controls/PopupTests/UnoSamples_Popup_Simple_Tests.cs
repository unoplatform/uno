using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

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

			_app.Wait(0.15f);

			var toggleButton = _app.Marked("TogglePopup2");

			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", false);

			toggleButton.Tap();

			_app.WaitForDependencyPropertyValue(toggleButton, "IsChecked", true);

			var popupContent = _app.Marked("popupContent2");

			_app.WaitForElement(popupContent);

			popupContent.Tap();

			_app.Wait(0.25f);

			_app.WaitForElement(popupContent); // should remain opened

			var screenRect = _app.Marked("sampleContent").FirstResult().Rect;

			_app.TapCoordinates(10, screenRect.Bottom - 10); // click elsewhere on the page

			_app.Wait(0.25f);

			_app.WaitForElement(popupContent); // should remain opened

			_app.Wait(0.25f);

			toggleButton.Tap(); // should dismiss here

			for (var i = 0; i < 5; i++)
			{
				if (_app.Marked("popupContent2").FirstResult() == null)
				{
					return; // dismissed!
				}

				_app.Wait(i * 0.15f);
			}

			Assert.Fail("Popup not dismissed."); // this feature is known to fail on Android
		}
	}
}
