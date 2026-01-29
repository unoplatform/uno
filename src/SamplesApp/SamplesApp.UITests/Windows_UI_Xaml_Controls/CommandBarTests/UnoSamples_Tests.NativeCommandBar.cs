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

			myButton.FastTap();

			_app.WaitForText(result, "Clicked!");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Disabled on Wasm as there is no native command bar. Enabling for iOS tracked by https://github.com/unoplatform/uno/issues/6732
		public void When_Foreground_Is_Set()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.CommandBar_Native_With_AppBarButton_With_Foreground");
			using var screenshot = TakeScreenshot("CommandBar_Native_With_AppBarButton_With_Foreground.When_Foreground_Is_Set");
			var rect = _app.Query("MyCommandBar").Single().Rect;

			// Couldn't find a way to get the pixel at the center of the icon. Using this hacky approach:
			var bitmap = screenshot.GetBitmap();
			for (int x = (int)rect.Right; x > rect.CenterX; x--)
			{
				if (bitmap.GetPixel(x, (int)rect.CenterY) is { A: 255, R: 255, G: 0, B: 0 })
				{
					return;
				}
			}

			Assert.Fail("Expected a red pixel.");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Test is flaky on iOS: https://github.com/unoplatform/uno/issues/9080
		public async Task NativeCommandBar_Size()
		{
			Run("Uno.UI.Samples.Content.UITests.CommandBar.CommandBar_Dynamic");

			const string rootElementName = "RootPanel";
			_app.WaitForElement(rootElementName);

			var supportsRotation = GetSupportsRotation();

			var isLandscape = GetIsCurrentRotationLandscape(rootElementName);
			var currentModeIsLandscape = isLandscape;


			async Task ToggleOrientation()
			{
				currentModeIsLandscape = !currentModeIsLandscape;

				_app.WaitFor(() =>
				{
					if (currentModeIsLandscape)
					{
						_app.SetOrientationLandscape();
					}
					else
					{
						_app.SetOrientationPortrait();
					}

					return GetIsCurrentRotationLandscape(rootElementName) == currentModeIsLandscape;
				}, timeout: TimeSpan.FromSeconds(60));

				await Task.Delay(125); // A delay ia required after rotation for the test to succeed
			}

			try
			{
				var firstScreenShot = TakeScreenshot("FirstOrientation");

				var firstCommandBarRect = _app.GetPhysicalRect("TheCommandBar");
				var firstYellowBorderRect = _app.GetPhysicalRect("TheBorder");
				firstCommandBarRect.Bottom.Should().Be(firstYellowBorderRect.Y);

				var x1 = firstCommandBarRect.X + (firstCommandBarRect.Width * 0.75f);
				ImageAssert.HasColorAt(firstScreenShot, x1, firstCommandBarRect.Bottom - 1, Color.Red);

				if (!supportsRotation)
				{
					return; // We're on a platform not supporting rotations.
				}

				await ToggleOrientation();

				var secondScreenShot = TakeScreenshot("SecondOrientation");

				var secondCommandBarRect = _app.GetPhysicalRect("TheCommandBar");
				var secondYellowBorderRect = _app.GetPhysicalRect("TheBorder");
				secondCommandBarRect.Bottom.Should().Be(secondYellowBorderRect.Y);

				var x2 = secondCommandBarRect.X + (secondCommandBarRect.Width * 0.75f);
				ImageAssert.HasColorAt(secondScreenShot, x2, secondCommandBarRect.Bottom - 1, Color.Red);

				await ToggleOrientation();

				var thirdScreenShot = TakeScreenshot("thirdOrientation");

				var thirdCommandBarRect = _app.GetPhysicalRect("TheCommandBar");
				var thirdYellowBorderRect = _app.GetPhysicalRect("TheBorder");
				thirdCommandBarRect.Bottom.Should().Be(thirdYellowBorderRect.Y);

				var x3 = thirdCommandBarRect.X + (thirdCommandBarRect.Width * 0.75f);
				ImageAssert.HasColorAt(thirdScreenShot, x3, thirdCommandBarRect.Bottom - 1, Color.Red);
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

				_app.WaitFor(() => GetIsCurrentRotationLandscape(rootElementName) == isLandscape);
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
			Assert.IsLessThanOrEqualTo(myCommandBarResult.Rect.Width / 2, innerTextBoxResult.Rect.Width, "TextBox Width is too large");

			horizontalValue.SetDependencyPropertyValue("SelectedItem", "Stretch");

			TakeScreenshot("Stretch");

			innerTextBoxResult = _app.Query(innerTextBox).First();
			Assert.IsGreaterThan(myCommandBarResult.Rect.Width * .75, innerTextBoxResult.Rect.Width, "TextBox Width is not large enough");

			horizontalValue.SetDependencyPropertyValue("SelectedItem", "Left");

			innerTextBoxResult = _app.Query(innerTextBox).First();
			Assert.IsLessThanOrEqualTo(myCommandBarResult.Rect.Width / 2, innerTextBoxResult.Rect.Width, "TextBox Width is too large");

			TakeScreenshot("Left");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
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
			var rect = _app.GetLogicalRect("CommandBarTitleText");

			Assert.Greater(rect.Height, 1);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_Navigated_CommandBarShouldKeepColor_Native_Frame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.Background.CommandBar_Background_Frame");

			_app.WaitForElement("NavigateInitialButton");
			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("NavigateToPage2Button");
			_app.FastTap("NavigateToPage2Button");

			_app.WaitForElement("Page2CommandBar");

			var initial = TakeScreenshot("initial", ignoreInSnapshotCompare: true);

			_app.Wait(TimeSpan.FromMilliseconds(500));

			var final = TakeScreenshot("final", ignoreInSnapshotCompare: true);

			ImageAssert.AreEqual(initial, final);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_Navigated_CommandBarShouldHideBackButtonTitle_NativeFrame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.BackButtonTitle.CommandBar_Frame");

			_app.WaitForElement("NavigateInitialButton");

			// Will set the global style for the CommandBar to remove the Back Button Title
			_app.FastTap("SetGlobalStyleButton");

			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("NavigateToPage2Button");
			_app.FastTap("NavigateToPage2Button");

			_app.WaitForElement("BackButtonTitleButton");

			_app.FastTap("BackButtonTitleButton");

			var textblock = _app.Marked("InfoTextBlock");

			_app.WaitForDependencyPropertyValue(textblock, "Text", "PASSED");

			// Removing the global style we added for the CommandBar preventing other UITest to fail
			_app.FastTap("UnsetGlobalStyleButton");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_Navigated_CommandBarDisplayCustomBackButtonIcon_NativeFrame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.BackButtonImage.CommandBar_Frame");

			_app.WaitForElement("NavigateInitialButton");

			// Will set the global style for the CommandBar to remove the Back Button Title
			_app.FastTap("SetGlobalStyleButton");

			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("NavigateToPage2Button");
			_app.FastTap("NavigateToPage2Button");

			_app.WaitForElement("BackButtonImageLoaderButton");

			_app.FastTap("BackButtonImageLoaderButton");

			_app.Wait(TimeSpan.FromMilliseconds(500));

			using var bmp = TakeScreenshot("Source set");

			var borderThickness = LogicalToPhysical(3);

			var expectedRect = _app.GetPhysicalRect("RefImage").DeflateBy(borderThickness);
			var lateRect = _app.GetPhysicalRect("ExpectedImage").DeflateBy(borderThickness);

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, lateRect, permittedPixelError: 10);

			// Removing the global style we added for the CommandBar preventing other UITest to fail
			_app.FastTap("UnsetGlobalStyleButton");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_CustomContent_CommandBarTitleShouldBeVisible_NativeFrame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.CustomContent.CommandBar_Frame");

			_app.WaitForElement("NavigateInitialButton");

			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("Result");

			_app.WaitForDependencyPropertyValue(_app.Marked("Result"), "Text", "PASSED");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_CustomContentAndLongTitle_TitleShouldNotOverlapBarButtons_OnLoad_NativeFrame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.LongTitle.CommandBar_Frame");

			_app.WaitForElement("NavigateInitialButton");
			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("CalculateSize");
			_app.FastTap("CalculateSize");

			_app.WaitForDependencyPropertyValue(_app.Marked("Result"), "Text", "PASSED");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_CustomContentAndLongTitle_TitleShouldNotOverlapBarButtons_OnNavigateBack_NativeFrame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.LongTitle.CommandBar_Frame");

			_app.WaitForElement("NavigateInitialButton");
			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("NavigateToPage2Button");
			_app.FastTap("NavigateToPage2Button");

			_app.WaitForElement("GoBackButton");
			_app.FastTap("GoBackButton");

			_app.WaitForElement("CalculateSize");
			_app.FastTap("CalculateSize");

			_app.WaitForDependencyPropertyValue(_app.Marked("Result"), "Text", "PASSED");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_CustomContentAndLongTitleAndDoubleNavigation_TitleShouldNotOverlapBarButtons_OnNavigateBack_NativeFrame()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.LongTitle.CommandBar_Frame");

			_app.WaitForElement("NavigateInitialButton");
			_app.FastTap("NavigateInitialButton");

			_app.WaitForElement("NavigateToPage2Button");
			_app.FastTap("NavigateToPage2Button");

			_app.WaitForElement("NavigateToPage3Button");
			_app.FastTap("NavigateToPage3Button");

			_app.WaitForElement("GoBackButton");
			_app.FastTap("GoBackButton");

			_app.WaitForElement("CalculateSize");
			_app.FastTap("CalculateSize");

			_app.WaitForDependencyPropertyValue(_app.Marked("Result"), "Text", "PASSED");
		}
	}
}
