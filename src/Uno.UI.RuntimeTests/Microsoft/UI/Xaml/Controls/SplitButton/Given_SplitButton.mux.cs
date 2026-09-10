using Common;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
public class Given_SplitButton
{
	[TestMethod]
	[RunsOnUIThread]
	[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]

	public async Task VerifyFontFamilyForChevron()
	{
		var splitButton = new SplitButton();
		TestServices.WindowHelper.WindowContent = splitButton;
		await TestServices.WindowHelper.WaitForIdle();

		var secondayButton = splitButton.GetTemplateChild("SecondaryButton");
		var font = ((secondayButton as Button).Content as TextBlock).FontFamily;
		Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
	}
}
