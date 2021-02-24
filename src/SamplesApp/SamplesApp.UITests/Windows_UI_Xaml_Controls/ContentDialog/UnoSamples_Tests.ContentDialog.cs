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
		private ScreenshotInfo CurrentTestTakeScreenShot(string name) =>
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

			_app.FastTap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.FastTap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Primary");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_06_Reuse()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog6 = _app.Marked("showDialog6");

			_app.WaitForElement(showDialog6);

			var dialogResult = _app.Marked("dialogResult");

			// Assert initial state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.FastTap(showDialog6);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.FastTap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Primary");

			_app.FastTap(showDialog6);

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.FastTap(secondaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Secondary");
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

			_app.FastTap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.FastTap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Undefined");

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.FastTap(secondaryButton);

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

			_app.FastTap(showDialog1);

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.FastTap(secondaryButton);

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

			_app.FastTap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			CurrentTestTakeScreenShot("Primary Button");

			_app.FastTap(primaryButton);

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

			_app.FastTap(showDialog1);

			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			CurrentTestTakeScreenShot("Close Button");

			_app.FastTap(closeButton);

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

			_app.FastTap(showDialog1);

			var dialogInnerButton = _app.Marked("dialogInnerButton");
			_app.WaitForElement(dialogInnerButton);

			CurrentTestTakeScreenShot("Secondary Button");

			_app.FastTap(dialogInnerButton);

			var buttonClickResult = _app.Marked("buttonClickResult");
			_app.WaitForDependencyPropertyValue(buttonClickResult, "Text", "OnDialogInnerButtonClick");

			var dialogTb = new QueryEx(q => q.All().Marked("dialogTb"));
			// _app.FastTap(dialogTb);
			_app.EnterText(dialogTb, "This is some text");

			var dialogTextBinding = new QueryEx(q => q.All().Marked("dialogTextBinding"));
			_app.WaitForDependencyPropertyValue(dialogTextBinding, "Text", "This is some text");

			CurrentTestTakeScreenShot("Secondary Button");

			var primaryButton = _app.Marked("SecondaryButton");
			_app.FastTap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Secondary");
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Auto_Closing()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Closing");

			var showDialog = _app.Marked("AutoCloseDialog");

			_app.WaitForElement(showDialog);

			_app.FastTap(showDialog);

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

			_app.FastTap(showDialog);

			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			_app.FastTap(closeButton);

			var resultText = _app.Marked("ResultTextBlock");
			var closedText = _app.Marked("DidCloseTextBlock");

			var defer1 = _app.Marked("Complete1Button");
			_app.FastTap(defer1);
			_app.WaitForDependencyPropertyValue(resultText, "Text", "First complete called");

			var defer2 = _app.Marked("Complete2Button");
			_app.FastTap(defer2);
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

			_app.FastTap(showDialog);

			var closeButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(closeButton);

			_app.FastTap(closeButton);

			var resultText = _app.Marked("ResultTextBlock");
			var closedText = _app.Marked("DidCloseTextBlock");

			_app.WaitForDependencyPropertyValue(resultText, "Text", "Primary");
			_app.WaitForDependencyPropertyValue(closedText, "Text", "Closed");
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Closing_PrimaryDialogCancelClosing()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Closing");

			var showDialog = _app.Marked("PrimaryDialogCancelClosing");
			var resultText = _app.Marked("ResultTextBlock");
			var closedText = _app.Marked("DidCloseTextBlock");
			var closeButton = _app.Marked("CloseButton");
			var primaryButton = _app.Marked("PrimaryButton");

			_app.WaitForElement(showDialog);
			_app.FastTap(showDialog);

			_app.WaitForElement(primaryButton);
			_app.FastTap(primaryButton);

			_app.WaitForDependencyPropertyValue(resultText, "Text", "Primary");
			_app.WaitForDependencyPropertyValue(closedText, "Text", "Not closed");

			_app.FastTap(closeButton);

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
			_app.FastTap(showDialog);

			var comboBox = _app.Marked("InnerComboBox");
			_app.WaitForElement(comboBox);
			_app.FastTap(comboBox);

			var item = _app.Marked("ComboElement4");
			_app.WaitForElement(item);
			_app.FastTap(item);

			var resultsText = _app.Marked("ResultsTextBlock");
			_app.WaitForDependencyPropertyValue(resultsText, "Text", "Item 4");

			// Close the dialog, otherwise subsequent tests may fail
			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			_app.FastTap(closeButton);
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Simple_NotLightDismissible()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialogButton = _app.Marked("showDialog1");
			var statusBarBackground = _app.Marked("statusBarBackground");
			var dialogSpace = _app.Marked("DialogSpace"); // from ContentDialog default ControlTemplate
			var primaryButton = _app.Marked("PrimaryButton");

			// initial state
			_app.WaitForElement(showDialogButton);
			var initialScreenshot = CurrentTestTakeScreenShot("0 Initial State");

			// open dialog
			_app.FastTap(showDialogButton);
			_app.WaitForElement(primaryButton);
			var dialogOpenedScreenshot = CurrentTestTakeScreenShot("1 ContentDialog Opened");

			// tapping outside of dialog
			var dialogRect = _app.GetRect(dialogSpace);
			_app.TapCoordinates(dialogRect.CenterX, dialogRect.Bottom + 50);
			var dialogStillOpenedScreenshot = CurrentTestTakeScreenShot("2 ContentDialog Still Opened");

			// close dialog
			_app.FastTap(primaryButton);
			_app.Wait(seconds: 1);
			var dialogClosedScreenshot = CurrentTestTakeScreenShot("3 ContentDialog Closed");

			// compare
			var comparableRect = GetOsComparableRect();
			ImageAssert.AreNotEqual(initialScreenshot, dialogOpenedScreenshot, comparableRect);
			ImageAssert.AreEqual(dialogOpenedScreenshot, dialogStillOpenedScreenshot, comparableRect);
			ImageAssert.AreNotEqual(dialogStillOpenedScreenshot, dialogClosedScreenshot, comparableRect);

			Rectangle? GetOsComparableRect()
			{
				if (AppInitializer.GetLocalPlatform() == Platform.Android)
				{
					try
					{
						// the status bar area needs to be excluded for image comparison
						var screen = _app.GetScreenDimensions();
						var statusBarRect = _app.GetRect(statusBarBackground);

						return new Rectangle(
							0,
							(int)statusBarRect.Height,
							(int)screen.Width,
							(int)screen.Height - (int)statusBarRect.Height
						);
					}
					catch (TimeoutException e)
					{
						// The status bar is not present in Android 11+
						return default;
					}
					catch(Exception e)
					{
						return default;
					}
				}
				else
				{
					return default;
				}
			}
		}

		[Test]
		[AutoRetry]
		public void ContentDialog_Async()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Async");

			var showDialogButton = _app.Marked("AsyncDialogButton");
			var hideDialogButton = _app.Marked("HideButton");
			var statusTextblock = _app.Marked("DidShowAsyncReturnTextBlock");

			// open dialog
			_app.WaitForElement(showDialogButton);
			_app.FastTap(showDialogButton);

			// hide dialog
			_app.WaitForElement(hideDialogButton);
			_app.WaitForDependencyPropertyValue(statusTextblock, "Text", "Not Returned"); // verify that the dialog didn't return yet
			_app.FastTap(hideDialogButton);

			// verify showAsync() returned
			_app.WaitForDependencyPropertyValue(statusTextblock, "Text", "Returned");
		}
	}
}
