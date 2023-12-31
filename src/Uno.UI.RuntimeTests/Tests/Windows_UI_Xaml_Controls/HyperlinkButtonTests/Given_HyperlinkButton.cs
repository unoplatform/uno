using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.HyperlinkButtonTests
{
	[TestClass]
	public class Given_HyperlinkButton
	{
#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkButton_With_Implicit_Content_Should_Be_Underlined()
		{
			var SUT = new HyperlinkButtonPage();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			var underlinedImplicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
			Assert.IsInstanceOfType(underlinedImplicitTextBlock, typeof(ImplicitTextBlock));
			Assert.AreEqual(TextDecorations.Underline, underlinedImplicitTextBlock.TextDecorations);

			var notUnderlinedTextBlock = VisualTreeHelper.GetChild(SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
			Assert.IsNotInstanceOfType(notUnderlinedTextBlock, typeof(ImplicitTextBlock));
			Assert.AreEqual(TextDecorations.None, notUnderlinedTextBlock.TextDecorations);
		}
#endif
	}
}
