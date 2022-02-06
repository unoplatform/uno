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
	[TestFixture]
	public partial class UnoSamples_Popup_LightDismiss_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void DismissablaPopups_ForwardOrder_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Popup.Popup_LightDismiss");

			var btn4 = _app.Marked("btn4");

			var firstPopupTextBlock = _app.Marked("FirstPopupTextBlock");
			var secondPopupTextBlock = _app.Marked("SecondPopupTextBlock");
			var thirdPopupTextBlock = _app.Marked("ThirdPopupTextBlock");
			var fourthPopupTextBlock = _app.Marked("FourthPopupTextBlock");

			// Verify opacity of all popups initially
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.FastTap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void DismissablaPopups_ReverseOrder_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Popup.Popup_LightDismiss");

			var btn4R = _app.Marked("btn4R");

			var firstPopupTextBlock = _app.Marked("FirstPopupTextBlock");
			var secondPopupTextBlock = _app.Marked("SecondPopupTextBlock");
			var thirdPopupTextBlock = _app.Marked("ThirdPopupTextBlock");
			var fourthPopupTextBlock = _app.Marked("FourthPopupTextBlock");

			// Verify opacity of all popups initially
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.FastTap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.FastTap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.FastTap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.FastTap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void DismissablaPopups_WithOneNonDismissablePopup_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Popup.Popup_LightDismiss");

			var btn5 = _app.Marked("btn5");

			var firstPopupTextBlock = _app.Marked("FirstPopupTextBlock");
			var secondPopupTextBlock = _app.Marked("SecondPopupTextBlock");
			var thirdPopupTextBlock = _app.Marked("ThirdPopupTextBlock");
			var fourthPopupTextBlock = _app.Marked("FourthPopupTextBlock");
			var close5 = _app.Marked("close5");

			// Click on popup button and verify opacity
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			close5.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.FastTap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.FastTap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void When_Dismissible_Popup()
		{
			Run("UITests.Windows_UI_Input.PointersTests.HitTest_LightDismiss");

			_app.FastTap("ResetButton");

			_app.FastTap("LaunchDismissiblePopupButton");

			_app.WaitForElement("TargetPopupContent");

			Assert.AreEqual("True", _app.GetText("PopupStatusTextBlock"));

			_app.FastTap("TargetPopupContent");

			_app.WaitForText("ResultTextBlock", "Popup content pressed");
			Assert.AreEqual("True", _app.GetText("PopupStatusTextBlock"));

			_app.FastTap("ActionButton"); // Should dismiss popup, without executing button click event

			_app.WaitForText("PopupStatusTextBlock", "False");
			Assert.AreEqual("Popup content pressed", _app.GetText("ResultTextBlock"));

			_app.FastTap("ResetButton");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser, Platform.iOS)] // On Android when using native popups (as in SamplesApp), the native popup intercepts pointer events
		public void When_Undismissible_Popup()
		{
			Run("UITests.Windows_UI_Input.PointersTests.HitTest_LightDismiss");

			_app.FastTap("ResetButton");

			_app.FastTap("LaunchUndismissiblePopupButton");

			_app.WaitForElement("TargetPopupContent");

			Assert.AreEqual("True", _app.GetText("PopupStatusTextBlock"));

			_app.FastTap("TargetPopupContent");

			_app.WaitForText("ResultTextBlock", "Popup content pressed");
			Assert.AreEqual("True", _app.GetText("PopupStatusTextBlock"));

			_app.FastTap("ActionButton"); // Should execute button click event, without dismissing popup

			_app.WaitForText("ResultTextBlock", "Button pressed");
			Assert.AreEqual("True", _app.GetText("PopupStatusTextBlock"));

			_app.FastTap("ResetButton"); // Dismiss popup programmatically
			_app.WaitForText("PopupStatusTextBlock", "False");
		}

		[Test]
		[AutoRetry]
		public void When_Flyout()
		{
			Run("UITests.Windows_UI_Input.PointersTests.HitTest_LightDismiss");

			_app.FastTap("ResetButton");

			_app.FastTap("FlyoutButton");

			_app.WaitForElement("FlyoutContentGrid");
			Assert.AreEqual("True", _app.GetText("FlyoutStatusTextBlock"));

			_app.FastTap("FlyoutContentBorderNoPressed");
			_app.FastTap("FlyoutContentBorderWithPressed");

			_app.WaitForText("ResultTextBlock", "Flyout content pressed");
			Assert.AreEqual("True", _app.GetText("FlyoutStatusTextBlock")); // Tapping flyout content should not have dismissed flyout

			_app.FastTap("FlyoutContentGrid");
			_app.WaitForText("FlyoutStatusTextBlock", "False"); // FlyoutContentGrid is hit-transparent at its centre, tapping it should dismiss the flyout

			_app.FastTap("ResetButton");
		}

		[Test]
		[AutoRetry]
		public void When_ComboBox()
		{
			Run("UITests.Windows_UI_Input.PointersTests.HitTest_LightDismiss");

			_app.FastTap("ResetButton");

			_app.FastTap("TargetComboBox");
			_app.WaitForText("ComboBoxStatusTextBlock", "True");

			Assert.AreEqual("None", _app.GetText("ResultTextBlock"));

			_app.FastTap("ActionButton"); // Should dismiss ComboBox dropdown, without executing button click event

			_app.WaitForText("ComboBoxStatusTextBlock", "False");
			Assert.AreEqual("None", _app.GetText("ResultTextBlock"));

			_app.FastTap("ResetButton");

			_app.FastTap("TargetComboBox");
			_app.WaitForText("ComboBoxStatusTextBlock", "True");

			_app.FastTap("TargetComboBox"); // Should select an item, dismissing the ComboBox
			_app.WaitForText("ComboBoxStatusTextBlock", "False");
			Assert.AreEqual("Item selected", _app.GetText("ResultTextBlock"));

			_app.FastTap("ResetButton");
		}
	}
}
