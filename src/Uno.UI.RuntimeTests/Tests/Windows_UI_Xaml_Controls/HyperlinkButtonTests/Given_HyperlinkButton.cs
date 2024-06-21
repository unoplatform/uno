using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Text;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.HyperlinkButtonTests
{
	[TestClass]
	public class Given_HyperlinkButton
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkButton_With_Implicit_Content_Should_Be_Underlined()
		{
			var SUT = new HyperlinkButtonPage();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			var underlinedContentPresenter = (ContentPresenter)SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter");
			Assert.IsNotNull(underlinedContentPresenter);
#if HAS_UNO
			Assert.IsTrue(underlinedContentPresenter.IsUsingDefaultTemplate);
#endif
			var underlinedImplicitTextBlock = (TextBlock)VisualTreeHelper.GetChild(underlinedContentPresenter, 0);
			Assert.IsNotNull(underlinedImplicitTextBlock);
			Assert.AreEqual(TextDecorations.Underline, underlinedImplicitTextBlock.TextDecorations);

			var notUnderlinedContentPresenter = (ContentPresenter)SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter");
			Assert.IsNotNull(notUnderlinedContentPresenter);
#if HAS_UNO
			Assert.IsFalse(notUnderlinedContentPresenter.IsUsingDefaultTemplate);
#endif
			var notUnderlinedTextBlock = (TextBlock)VisualTreeHelper.GetChild(notUnderlinedContentPresenter, 0);
			Assert.IsNotNull(notUnderlinedTextBlock);
			Assert.AreEqual(TextDecorations.None, notUnderlinedTextBlock.TextDecorations);
		}
	}
}
