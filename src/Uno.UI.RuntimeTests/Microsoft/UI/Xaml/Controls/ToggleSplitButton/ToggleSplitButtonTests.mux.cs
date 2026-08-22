using Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class ToggleSplitButtonTests
	{
		[TestMethod]
		[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
		[RunsOnUIThread]
		public async Task VerifyFontFamilyForChevron()
		{
			Microsoft.UI.Xaml.Controls.ToggleSplitButton toggleSplitButton = null;
			toggleSplitButton = new Microsoft.UI.Xaml.Controls.ToggleSplitButton();
			TestServices.WindowHelper.WindowContent = toggleSplitButton;
			await TestServices.WindowHelper.WaitForIdle();

			var secondayButton = toggleSplitButton.GetTemplateChild("SecondaryButton");
			var font = ((secondayButton as Button).Content as TextBlock).FontFamily;
			Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
		}
	}
}
