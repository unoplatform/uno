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
			btn4.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4.Tap();
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
			btn4R.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.Tap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.Tap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.Tap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn4R.Tap();
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
			btn5.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			close5.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual("1", secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.Tap();
			Assert.AreEqual("1", firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());

			// Click on popup button and verify opacity
			btn5.Tap();
			Assert.AreEqual(null, firstPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, secondPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, thirdPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, fourthPopupTextBlock.GetDependencyPropertyValue("Opacity")?.ToString());
			Assert.AreEqual(null, close5.GetDependencyPropertyValue("Opacity")?.ToString());
		}
	}
}
