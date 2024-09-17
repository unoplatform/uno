using Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class DropDownButtonTests
	{
#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
		[Ignore("Fluent styles V2 use AnimatedIcon instead of FontIcon")]
		[RunsOnUIThread]
		public void VerifyFontFamilyForChevron()
		{
			DropDownButton dropDownButton = null;
			dropDownButton = new DropDownButton();
			TestServices.WindowHelper.WindowContent = dropDownButton;

			var chevronTextBlock = dropDownButton.GetTemplateChild("ChevronTextBlock") as TextBlock;
			var font = chevronTextBlock.FontFamily;
			Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
		}
#endif
	}
}
