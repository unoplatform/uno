using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{

	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{

		[Test]
		[AutoRetry]
		[Ignore("TODO Popups are not removed properly")]
		public void Popup_Dismissable_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.Popup.Popup_Automated");

			_app.WaitForElement(_app.Marked("CloseDismissablePopupButton"));

			var OpenDismissablePopupButton = _app.Marked("OpenDismissablePopupButton");
			var CloseDismissablePopupButton = _app.Marked("CloseDismissablePopupButton");
			var DismissablePopup = _app.Marked("DismissablePopup");
			var PopupText = _app.Marked("DismissablePopupText");

			// Assert initial state 
			TakeScreenshot("Popup - Dismissable - 1 - InitialState");
			Assert.AreEqual(null, PopupText.GetDependencyPropertyValue("Text")?.ToString());

			OpenDismissablePopupButton.Tap();

			_app.WaitForElement(PopupText, timeout: TimeSpan.FromSeconds(5));

			// Assert after opening dismissable 
			TakeScreenshot("Popup - Dismissable - 2 - Open");
			Assert.AreEqual("Test", PopupText.GetDependencyPropertyValue("Text")?.ToString());

			_app.TapCoordinates(10,100);

			// Assert after dismiss
			TakeScreenshot("Popup - Dismissable - 3 - Dismissable");
			Assert.AreEqual(null, PopupText.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[Ignore("TODO Popups are not removed properly")]
		public void Popup_NonDismissable_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.Popup.Popup_Automated");

			_app.WaitForElement(_app.Marked("CloseNonDismissablePopupButton"));

			var OpenNonDismissablePopupButton = _app.Marked("OpenNonDismissablePopupButton");
			var CloseNonDismissablePopupButton = _app.Marked("CloseNonDismissablePopupButton");
			var NonDismissablePopup = _app.Marked("NonDismissablePopup");
			var PopupText = _app.Marked("NonDismissablePopupText");

			// Assert initial state 
			TakeScreenshot("Popup - Non Dismissable - 1 - InitialState");
			Assert.AreEqual(null, PopupText.GetDependencyPropertyValue("Text")?.ToString());

			OpenNonDismissablePopupButton.Tap();
			OpenNonDismissablePopupButton.Tap();

			_app.WaitForElement(PopupText, timeout: TimeSpan.FromSeconds(5));

			// Assert after opening dismissable 
			TakeScreenshot("Popup - Non Dismissable - 2 - Open");
			Assert.AreEqual("Test", PopupText.GetDependencyPropertyValue("Text")?.ToString());

			_app.TapCoordinates(10, 100);

			// Assert after trying to dismiss
			TakeScreenshot("Popup - Non Dismissable - 3 - Try Dismiss");
			Assert.AreEqual("Test", PopupText.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[Ignore("TODO Popups are not removed properly")]
		public void Popup_NoFixedHeight_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.Popup.Popup_Automated");

			_app.WaitForElement(_app.Marked("CloseNoFixedHeightPopupButton"));

			var OpenNoFixedHeightPopupButton = _app.Marked("OpenNoFixedHeightPopupButton");
			var CloseNoFixedHeightPopupButton = _app.Marked("CloseNoFixedHeightPopupButton");
			var NoFixedHeightPopup = _app.Marked("NoFixedHeightPopup");
			var PopupText = _app.Marked("NoFixedHeightPopupText");

			// Assert initial state 
			TakeScreenshot("Popup - No Fixed Height - 1 - InitialState");
			Assert.AreEqual(null, PopupText.GetDependencyPropertyValue("Text")?.ToString());

			OpenNoFixedHeightPopupButton.Tap();

			_app.WaitForElement(PopupText, timeout: TimeSpan.FromSeconds(5));

			// Assert after opening dismissable 
			TakeScreenshot("Popup - No Fixed Height - 2 - Open");
			Assert.AreEqual("Test", PopupText.GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
