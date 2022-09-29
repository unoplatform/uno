#nullable disable

using System.Threading;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.MessageDialogTests
{
	[TestFixture]
	public partial class MessageDialogTest : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		// iOS tracked by: https://github.com/unoplatform/uno/issues/6936
		// Skipping Wasm due to: OpenQA.Selenium.UnhandledAlertException : unexpected alert open: {Alert text : Content}
		[ActivePlatforms(Platform.Android)]
		public void When_Click_Outside_Dialog_Expect_No_Dismiss()
		{
			Run("UITests.Shared.MessageDialogTests.MessageDialogTest");
			var button = _app.Marked("showDialogBtn");
			button.FastTap();

			// A little animation happens. Sleep to reliably compare screenshots
			Thread.Sleep(1000);

			using var screenshot = TakeScreenshot("BeforeClicking");

			var label = _app.Marked("labelOutside");
			_app.WaitForElement(label);
			label.FastTap();
			using var screenshot2 = TakeScreenshot("AfterClicking");

			ImageAssert.AreEqual(screenshot, screenshot2);

			// Close the dialog.
			var chkBox = _app.Marked("chkBox");
			chkBox.SetDependencyPropertyValue("IsChecked", "True");
		}
	}
}
