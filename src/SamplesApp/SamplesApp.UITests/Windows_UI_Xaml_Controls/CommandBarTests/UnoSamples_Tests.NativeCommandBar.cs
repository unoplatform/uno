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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.CommandBarTests
{
	[TestFixture]
	public partial class NativeCommandBar_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void NativeCommandBar_Automated()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.CommandBar_Native_With_Content");

			var myButton = _app.Marked("myButton");
			var result = _app.Marked("result");

			myButton.Tap();

			_app.WaitForText(result, "Clicked!");
		}

		[Test]
		[AutoRetry]
		public async Task NativeCommandBar_Size()
		{
			Run("Uno.UI.Samples.Content.UITests.CommandBar.CommandBar_Dynamic");

			var sampleRect = _app.Marked("RootPanel").FirstResult().Rect;
			var isLandscape = sampleRect.Width > sampleRect.Height;
			var currentModeIsLandscape = isLandscape;

			async Task ToggleOrientation()
			{
				if (currentModeIsLandscape)
				{
					_app.SetOrientationPortrait();
				}
				else
				{
					_app.SetOrientationLandscape();
				}

				currentModeIsLandscape = !currentModeIsLandscape;

				await Task.Delay(1200); // give time to device to rotate
			}

			try
			{
				var firstScreenShot = this.TakeScreenshot("FirstOrientation");

				var firstCommandBarRect = _app.Marked("TheCommandBar").FirstResult().Rect;

				var x1 = firstCommandBarRect.X + (firstCommandBarRect.Width * 0.75f);
				ImageAssert.HasColorAt(firstScreenShot, x1, firstCommandBarRect.Bottom - 1, Color.Red);
				ImageAssert.HasColorAt(firstScreenShot, x1, firstCommandBarRect.Bottom + 1, Color.Yellow);

				await ToggleOrientation();

				var secondScreenShot = this.TakeScreenshot("SecondOrientation");

				var secondCommandBarRect = _app.Marked("TheCommandBar").FirstResult().Rect;

				var x2 = secondCommandBarRect.X + (secondCommandBarRect.Width * 0.75f);
				ImageAssert.HasColorAt(secondScreenShot, x2, secondCommandBarRect.Bottom - 1, Color.Red);
				ImageAssert.HasColorAt(secondScreenShot, x2, secondCommandBarRect.Bottom + 1, Color.Yellow);

				await ToggleOrientation();

				var thirdScreenShot = this.TakeScreenshot("thirdOrientation");

				var thirdCommandBarRect = _app.Marked("TheCommandBar").FirstResult().Rect;

				var x3 = thirdCommandBarRect.X + (thirdCommandBarRect.Width * 0.75f);
				ImageAssert.HasColorAt(thirdScreenShot, x3, thirdCommandBarRect.Bottom - 1, Color.Red);
				ImageAssert.HasColorAt(thirdScreenShot, x3, thirdCommandBarRect.Bottom + 1, Color.Yellow);
			}
			finally
			{
				// Reset orientation to original value
				if (isLandscape)
				{
					_app.SetOrientationLandscape();
				}
				else
				{
					_app.SetOrientationPortrait();
				}

				await Task.Delay(500); // give time to device to rotate before ending test
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void NativeCommandBar_Content_Alignment_Automated()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.CommandBar_Native_With_TextBox");

			var verticalValue = _app.Marked("verticalValue");
			var horizontalValue = _app.Marked("horizontalValue");
			var innerTextBox = _app.Marked("InnerTextBox");
			var innerTextBlock = _app.Marked("InnerTextBlock");
			var myCommandBar = _app.Marked("MyCommandBar");

			var myCommandBarResult = _app.Query(myCommandBar).First();

			TakeScreenshot("Default");

			var innerTextBoxResult = _app.Query(innerTextBox).First();
			Assert.IsTrue(innerTextBoxResult.Rect.Width <= myCommandBarResult.Rect.Width / 2, "TextBox Width is too large");

			horizontalValue.SetDependencyPropertyValue("SelectedItem", "Stretch");

			TakeScreenshot("Stretch");

			innerTextBoxResult = _app.Query(innerTextBox).First();
			Assert.IsTrue(innerTextBoxResult.Rect.Width > myCommandBarResult.Rect.Width * .75, "TextBox Width is not large enough");

			horizontalValue.SetDependencyPropertyValue("SelectedItem", "Left");

			innerTextBoxResult = _app.Query(innerTextBox).First();
			Assert.IsTrue(innerTextBoxResult.Rect.Width <= myCommandBarResult.Rect.Width / 2, "TextBox Width is too large");

			TakeScreenshot("Left");
		}

		[Test]
		[AutoRetry]
		public void When_TextBlock_Centred_Native_Frame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.CommandBar_Native_Frame");

			_app.WaitForElement("NavigateInitialButton");
			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("NavigateDetailButton");
			_app.FastTap("NavigateDetailButton");

			_app.WaitForElement("NavigateBackButton");
			_app.FastTap("NavigateBackButton");

			_app.WaitForElement("CommandBarTitleText");
			var rect = _app.GetRect("CommandBarTitleText");

			Assert.Greater(rect.Height, 1);

		}
	}
}
