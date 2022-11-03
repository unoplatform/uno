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
			using var screenshot = TakeScreenshot("Spaces typed in PasswordBox.");
			ImageAssert.HasColorInRectangle(screenshot, _app.Query("redPasswordBox").Single().Rect.ToRectangle(), Color.Red);
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
