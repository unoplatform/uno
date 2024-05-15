using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml
{
	[TestFixture]
	public partial class KeyboardTests : SampleControlUITestBase
	{
		[Test]
		[ActivePlatforms(Platform.Browser)]
		[AutoRetry]
		public void When_Keyboard_Press_Down_And_Up()
		{
			Run("UITests.Windows_UI_Xaml_Input.Keyboard.Keyboard_Events");
			string text = "";
			_app.WaitForElement("_btt1");
			_app.Tap("_btt1");
			_app.EnterText("_btt1", "A");
			text += $"_root - [PREVIEWKEYDOWN] A\r\n";
			text += $"_btt1 - [PREVIEWKEYDOWN] A\r\n";
			text += $"_btt1 - [KEYDOWN] A\r\n";
			text += $"_root - [KEYDOWN] A\r\n";

			text += $"_root - [PREVIEWKEYUP] A\r\n";
			text += $"_btt1 - [PREVIEWKEYUP] A\r\n";
			text += $"_btt1 - [KEYUP] A\r\n";
			text += $"_root - [KEYUP] A\r\n";

			text = text.Replace("\r\n", "\n");
			string output = _app.Marked("_output").GetText().Replace("\r\n", "\n");

			Assert.AreEqual(text, output);
		}

	}
}
