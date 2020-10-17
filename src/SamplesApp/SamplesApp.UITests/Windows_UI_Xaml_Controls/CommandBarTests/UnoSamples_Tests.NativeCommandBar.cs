using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
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

			myButton.Tap();

			_app.WaitForText(result, "Clicked!");
		}

		[Test]
		[AutoRetry]
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
				if (currentModeIsLandscape)
				{
					_app.SetOrientationPortrait();
				}
				else
				{
					_app.SetOrientationLandscape();
				}

				currentModeIsLandscape = !currentModeIsLandscape;

				_app.WaitFor(()=> GetIsCurrentRotationLandscape(rootElementName) == currentModeIsLandscape);

				await Task.Delay(125); // A delay ia required after rotation for the test to succeed
			}

			try
			{
				var firstScreenShot = TakeScreenshot("FirstOrientation");

				var firstCommandBarRect = _app.GetRect("TheCommandBar");
				var firstYellowBorderRect = _app.GetRect("TheBorder");
				firstCommandBarRect.Bottom.Should().Be(firstYellowBorderRect.Y);

				var firstCommandBarPhysicalRect = ToPhysicalRect(firstCommandBarRect);


				var x1 = firstCommandBarPhysicalRect.X + (firstCommandBarPhysicalRect.Width * 0.75f);
				ImageAssert.HasColorAt(firstScreenShot, x1, firstCommandBarPhysicalRect.Bottom - 1, Color.Red);

				if(!supportsRotation)
				{
					return; // We're on a platform not supporting rotations.
				}

				await ToggleOrientation();

				var secondScreenShot = TakeScreenshot("SecondOrientation");

				var secondCommandBarRect = _app.GetRect("TheCommandBar");
				var secondYellowBorderRect = _app.GetRect("TheBorder");
				secondCommandBarRect.Bottom.Should().Be(secondYellowBorderRect.Y);

				var secondCommandBarPhysicalRect = ToPhysicalRect(secondCommandBarRect);

				var x2 = secondCommandBarPhysicalRect.X + (secondCommandBarPhysicalRect.Width * 0.75f);
				ImageAssert.HasColorAt(secondScreenShot, x2, secondCommandBarPhysicalRect.Bottom - 1, Color.Red);

				await ToggleOrientation();

				var thirdScreenShot = TakeScreenshot("thirdOrientation");

				var thirdCommandBarRect = _app.GetRect("TheCommandBar");
				var thirdYellowBorderRect = _app.GetRect("TheBorder");
				thirdCommandBarRect.Bottom.Should().Be(thirdYellowBorderRect.Y);

				var thirdCommandBarPhysicalRect = ToPhysicalRect(thirdCommandBarRect);

				var x3 = thirdCommandBarPhysicalRect.X + (thirdCommandBarPhysicalRect.Width * 0.75f);
				ImageAssert.HasColorAt(thirdScreenShot, x3, thirdCommandBarPhysicalRect.Bottom - 1, Color.Red);
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
