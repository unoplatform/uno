using System;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.PasswordBoxTests
{
	public partial class PasswordBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void PasswordShouldBeObscured()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.PasswordBoxTests.PasswordBoxPage");
			var passwordBox = _app.Marked("redPasswordBox");
			passwordBox.EnterText("         ");

			// PasswordBox has to be unfocused for Foreground to be red.
			// Otherwise, animations from template would take precedence and set the Foreground to black.
			var rect = _app.Query("redPasswordBox").Single().Rect;
			_app.TapCoordinates(rect.CenterX, rect.Bottom + 5);
			using var screenshot = TakeScreenshot("Spaces typed in PasswordBox.");
			ImageAssert.HasColorInRectangle(screenshot, rect.ToRectangle(), Color.Red);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void PasswordShouldNotDuplicateWhenRegainingFocus()
		{
			Run("UITests.Windows_UI_Xaml_Controls.PasswordBoxTests.PasswordBox_iOS_DuplicatingText");
			var passwordBox = _app.Marked("MyPasswordBox");

			passwordBox.EnterText("123");
			using var screenshot = TakeScreenshot("PasswordBox_Initial_123.");

			var button = _app.Marked("MyButton");
			button.FastTap();

			passwordBox.EnterText("123");

			using var screenshot2 = TakeScreenshot("PasswordBox_Second_123.");

			var pwd = passwordBox.GetDependencyPropertyValue<string>("Text");
			Assert.AreEqual("123123", pwd);
		}
	}
}
