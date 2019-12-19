using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ContentDialogTests
{
	[TestFixture]
	public partial class ContentDialog_Tests : SampleControlUITestBase
	{
		private void CurrentTestTakeScreenShot(string name) =>
			// Screenshot taking for this fixture is disabled on Android because of the
			// presence of the status bar when native popups are opened, adding the clock
			// (that is always changing :)).
			TakeScreenshot(name, ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android);

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Primary()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog1");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Primary");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Primary_Disabled()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog3");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Undefined");

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.Tap(secondaryButton);

			CurrentTestTakeScreenShot("Closed");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Secondary()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog1");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.Tap(secondaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Secondary");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_PrimaryCommand()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog4");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");
			var dialogCommand = _app.Marked("commandResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Primary");
			_app.WaitForDependencyPropertyValue(dialogCommand, "Text", "primaryCommand");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Close()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog2");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			CurrentTestTakeScreenShot("Close Button");

			_app.Tap(closeButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "None");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_TypeInner()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog1");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var dialogInnerButton = _app.Marked("dialogInnerButton");
			_app.WaitForElement(dialogInnerButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.Tap(dialogInnerButton);

			var buttonClickResult = _app.Marked("buttonClickResult");
			_app.WaitForDependencyPropertyValue(buttonClickResult, "Text", "OnDialogInnerButtonClick");

			var dialogTb = _app.Marked("dialogTb");
			_app.Tap(dialogTb);
			_app.EnterText("This is some text");

			var dialogTextBinding = _app.Marked("dialogTextBinding");
			_app.WaitForDependencyPropertyValue(dialogTextBinding, "Text", "This is some text");

			CurrentTestTakeScreenShot("Secondary Button");

			var primaryButton = _app.Marked("SecondaryButton");
			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Secondary");
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Auto_Closing()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Closing");

			var showDialog = _app.Marked("AutoCloseDialog");

			_app.WaitForElement(showDialog);

			_app.Tap(showDialog);

			var resultText = _app.Marked("ResultTextBlock");
			var closedText = _app.Marked("DidCloseTextBlock");

			_app.WaitForDependencyPropertyValue(resultText, "Text", "Closing event was raised!");

			_app.WaitForDependencyPropertyValue(closedText, "Text", "Closed");
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Closing_Deferred()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Closing");

			var showDialog = _app.Marked("DeferredDialog");

			_app.WaitForElement(showDialog);

			_app.Tap(showDialog);

			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			_app.Tap(closeButton);

			var resultText = _app.Marked("ResultTextBlock");
			var closedText = _app.Marked("DidCloseTextBlock");

			var defer1 = _app.Marked("Complete1Button");
			_app.Tap(defer1);
			_app.WaitForDependencyPropertyValue(resultText, "Text", "First complete called");

			var defer2 = _app.Marked("Complete2Button");
			_app.Tap(defer2);
			_app.WaitForDependencyPropertyValue(resultText, "Text", "Second complete called");

			_app.WaitForDependencyPropertyValue(closedText, "Text", "Closed");
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Closing_Result()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Closing");

			var showDialog = _app.Marked("PrimaryDialog");

			_app.WaitForElement(showDialog);

			_app.Tap(showDialog);

			var closeButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(closeButton);

			_app.Tap(closeButton);

			var resultText = _app.Marked("ResultTextBlock");
			var closedText = _app.Marked("DidCloseTextBlock");

			_app.WaitForDependencyPropertyValue(resultText, "Text", "Primary");
			_app.WaitForDependencyPropertyValue(closedText, "Text", "Closed");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] //TODO: https://github.com/unoplatform/uno/issues/1583
		public void ContentDialog_ComboBox()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_ComboBox");

			var showDialog = _app.Marked("ShowComboBoxDialog");
			_app.WaitForElement(showDialog);
			_app.Tap(showDialog);

			var comboBox = _app.Marked("InnerComboBox");
			_app.WaitForElement(comboBox);
			_app.Tap(comboBox);

			var item = _app.Marked("ComboElement4");
			_app.WaitForElement(item);
			_app.Tap(item);

			var resultsText = _app.Marked("ResultsTextBlock");
			_app.WaitForDependencyPropertyValue(resultsText, "Text", "Item 4");

			// Close the dialog, otherwise subsequent tests may fail
			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			_app.Tap(closeButton);
		}
	}
}
