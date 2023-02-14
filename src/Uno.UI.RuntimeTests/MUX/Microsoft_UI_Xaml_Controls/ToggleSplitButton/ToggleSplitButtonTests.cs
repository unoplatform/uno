using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Common;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class ToggleSplitButtonTests
	{
		[TestMethod]
		[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
		public void VerifyFontFamilyForChevron()
		{
			Microsoft.UI.Xaml.Controls.ToggleSplitButton toggleSplitButton = null;
			using (StyleHelper.UseFluentStyles())
			{
				RunOnUIThread.Execute(() =>
				{
					toggleSplitButton = new Microsoft.UI.Xaml.Controls.ToggleSplitButton();
					TestServices.WindowHelper.WindowContent = toggleSplitButton;

					var secondayButton = toggleSplitButton.GetTemplateChild("SecondaryButton");
					var font = ((secondayButton as Button).Content as TextBlock).FontFamily;
					Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
				});
			}
		}
	}
}
