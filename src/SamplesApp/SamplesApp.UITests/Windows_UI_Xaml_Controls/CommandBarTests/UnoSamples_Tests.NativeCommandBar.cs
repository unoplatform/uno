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
