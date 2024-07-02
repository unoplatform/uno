using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Common;
using Uno.UI.RuntimeTests;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class ToggleSplitButtonTests
	{
		[TestMethod]
		[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[RunsOnUIThread]
		public async Task VerifyFontFamilyForChevron()
		{
			Microsoft/* UWP don't rename */.UI.Xaml.Controls.ToggleSplitButton toggleSplitButton = null;
			using (StyleHelper.UseFluentStyles())
			{
				toggleSplitButton = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.ToggleSplitButton();
				TestServices.WindowHelper.WindowContent = toggleSplitButton;
				await TestServices.WindowHelper.WaitForIdle();

				var secondayButton = toggleSplitButton.GetTemplateChild("SecondaryButton");
				var font = ((secondayButton as Button).Content as TextBlock).FontFamily;
				Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
			}
		}
	}
}
