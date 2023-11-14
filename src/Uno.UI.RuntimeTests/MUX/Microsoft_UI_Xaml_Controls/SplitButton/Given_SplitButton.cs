using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
public class Given_SplitButton
{
	[TestMethod]
	[RunsOnUIThread]
	[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
	public void VerifyFontFamilyForChevron()
	{
		using (StyleHelper.UseFluentStyles())
		{
			var splitButton = new SplitButton();
			TestServices.WindowHelper.WindowContent = splitButton;

			var secondayButton = splitButton.GetTemplateChild("SecondaryButton");
			var font = ((secondayButton as Button).Content as TextBlock).FontFamily;
			Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
		}
	}
}
