using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public partial class Focus_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Test_FocusState_Interactive()
		{
			Run("UITests.Windows_UI_Xaml.FocusManager.Focus_FocusState");

			_app.WaitForElement("YeButton");

			string GetButtonFocusState() => _app.GetText("ButtonFocusStatus");
			string GetContentControlFocusState() => _app.GetText("ContentControlFocusStatus");
			string GetTextBoxFocusState() => _app.GetText("TextBoxFocusStatus");

			Assert.AreEqual("Unfocused", GetButtonFocusState());
			Assert.AreEqual("Unfocused", GetContentControlFocusState());
			Assert.AreEqual("Unfocused", GetTextBoxFocusState());

			_app.FastTap("YeButton");

			_app.WaitForText("ButtonFocusStatus", "Pointer");
			Assert.AreEqual("Unfocused", GetContentControlFocusState());
			Assert.AreEqual("Unfocused", GetTextBoxFocusState());

			_app.FastTap("YeTextBox");
			_app.WaitForText("TextBoxFocusStatus", "Pointer");
			Assert.AreEqual("Unfocused", GetButtonFocusState());
			Assert.AreEqual("Unfocused", GetContentControlFocusState());
		}
	}
}
