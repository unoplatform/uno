using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.HyperlinkButtonTests
{
	[TestClass]
	public class Given_HyperlinkButton
	{
#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		private const string UnderlineVisibleKey = "HyperlinkUnderlineVisible";

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkUnderlineVisible_Is_False_Implicit_Content_Should_Not_Be_Underlined()
		{
			var SUT = new HyperlinkButtonPage();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			// Default Fluent resources set HyperlinkUnderlineVisible to False.
			var implicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
			Assert.IsInstanceOfType(implicitTextBlock, typeof(ImplicitTextBlock));
			Assert.AreEqual(TextDecorations.None, implicitTextBlock.TextDecorations);

			var explicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
			Assert.IsNotInstanceOfType(explicitTextBlock, typeof(ImplicitTextBlock));
			Assert.AreEqual(TextDecorations.None, explicitTextBlock.TextDecorations);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkUnderlineVisible_Is_True_Implicit_Content_Should_Be_Underlined()
		{
			var resources = Application.Current.Resources;
			var hadValue = resources.TryGetValue(UnderlineVisibleKey, out var previous);
			resources[UnderlineVisibleKey] = true;
			try
			{
				var SUT = new HyperlinkButtonPage();
				TestServices.WindowHelper.WindowContent = SUT;
				await TestServices.WindowHelper.WaitForIdle();

				var implicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
				Assert.IsInstanceOfType(implicitTextBlock, typeof(ImplicitTextBlock));
				Assert.AreEqual(TextDecorations.Underline, implicitTextBlock.TextDecorations);

				var explicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
				Assert.AreEqual(TextDecorations.None, explicitTextBlock.TextDecorations);
			}
			finally
			{
				if (hadValue)
				{
					resources[UnderlineVisibleKey] = previous;
				}
				else
				{
					resources.Remove(UnderlineVisibleKey);
				}
			}
		}
#endif
	}
}
