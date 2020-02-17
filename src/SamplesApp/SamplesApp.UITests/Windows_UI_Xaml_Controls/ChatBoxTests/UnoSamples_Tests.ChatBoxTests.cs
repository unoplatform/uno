using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ChatBoxTests
{
	[TestFixture]
	public partial class ChatBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void ChatBoxTest_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ChatBox.ChatBox");

			var clicksCountTextBlock = _app.Marked("ClicksCountTextBlock");
			var clickButton = _app.Marked("ClickButton");
			var textBox = _app.Marked("TextBox");

			// Validate number of clicks
			Assert.AreEqual("You clicked 0 times", clicksCountTextBlock.GetDependencyPropertyValue("Text")?.ToString());
			clickButton.Tap();
			Assert.AreEqual("You clicked 1 times", clicksCountTextBlock.GetDependencyPropertyValue("Text")?.ToString());
			clickButton.Tap();
			Assert.AreEqual("You clicked 2 times", clicksCountTextBlock.GetDependencyPropertyValue("Text")?.ToString());

			// Validate text box
			textBox.Tap();
			textBox.ClearText();
			textBox.EnterText("Testing");
			Assert.AreEqual("Testing", textBox.GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
