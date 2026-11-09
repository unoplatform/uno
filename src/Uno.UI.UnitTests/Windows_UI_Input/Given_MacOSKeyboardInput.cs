#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.MacOS;

namespace Uno.UI.Tests.Windows_UI_Input;

[UITestClass]
public class Given_MacOSKeyboardInput
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22818")]
	[DataRow('\0')]
	[DataRow('\u0001')]
	[DataRow('\b')]
	[DataRow('\t')]
	[DataRow('\v')]
	[DataRow('\f')]
	[DataRow('\u0019')]
	[DataRow('\u001b')]
	[DataRow('\u001f')]
	[DataRow('\u007f')]
	[DataRow('\u0085')]
	[DataRow('\u009f')]
	public void When_Control_Character_Is_Not_Text(char unicode)
	{
		Assert.IsNull(MacOSKeyboardInput.FilterTextInputCharacter(unicode));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22818")]
	[DataRow('\r')]
	[DataRow('\n')]
	[DataRow(' ')]
	[DataRow('A')]
	[DataRow('é')]
	[DataRow('Ж')]
	[DataRow('中')]
	[DataRow('\u0301')]
	[DataRow('\u200d')]
	public void When_Text_Character_Is_Preserved(char unicode)
	{
		Assert.AreEqual((char?)unicode, MacOSKeyboardInput.FilterTextInputCharacter(unicode));
	}
}
