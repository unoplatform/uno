using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_PasswordBox
{
	[TestMethod]
	public async Task When_Display_Text_Changes_Selection_Survives()
	{
		var SUT = new PasswordBox { Password = "0123456789", Width = 200 };
		await UITestHelper.Load(SUT);

		SUT.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		SUT.Core.Select(2, 5);
		await TestServices.WindowHelper.WaitForIdle();

		var displayBlock = SUT.Core.TextBoxView.DisplayBlock;
		var expected = new TextBlock.Range(2, 7);
		Assert.AreEqual(expected, displayBlock.Selection, "the engine must push its selection onto the display block");

		// Rewriting the mask must not clear the selection. The display block resets its own selection on a
		// text change only when no engine owns it — so this fails if it cannot see the PasswordBox's engine.
		SUT.PasswordChar = "#";
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(expected, displayBlock.Selection, "a display-text update must not clear the engine's selection");
	}
}
