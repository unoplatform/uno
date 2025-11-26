using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno;
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
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkButton_With_Implicit_Content_Should_Not_Be_Underlined_By_Default()
		{
			// Ensure HighContrast is disabled and HyperlinkUnderlineVisible is false (default)
			WinRTFeatureConfiguration.Accessibility.HighContrast = false;
			if (Application.Current.Resources.ContainsKey("HyperlinkUnderlineVisible"))
			{
				Application.Current.Resources["HyperlinkUnderlineVisible"] = false;
			}

			var SUT = new HyperlinkButtonPage();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			var implicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
			Assert.IsInstanceOfType(implicitTextBlock, typeof(ImplicitTextBlock));
			// With the fix, underline should NOT be present by default
			Assert.AreEqual(TextDecorations.None, implicitTextBlock.TextDecorations);

			var explicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
			Assert.IsNotInstanceOfType(explicitTextBlock, typeof(ImplicitTextBlock));
			Assert.AreEqual(TextDecorations.None, explicitTextBlock.TextDecorations);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkButton_With_HighContrast_Should_Be_Underlined()
		{
			// Enable HighContrast
			WinRTFeatureConfiguration.Accessibility.HighContrast = true;
			try
			{
				var SUT = new HyperlinkButtonPage();
				TestServices.WindowHelper.WindowContent = SUT;
				await TestServices.WindowHelper.WaitForIdle();

				var implicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
				Assert.IsInstanceOfType(implicitTextBlock, typeof(ImplicitTextBlock));
				// With HighContrast enabled, underline should be present
				Assert.AreEqual(TextDecorations.Underline, implicitTextBlock.TextDecorations);

				var explicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
				Assert.IsNotInstanceOfType(explicitTextBlock, typeof(ImplicitTextBlock));
				// Explicit TextBlock should not be underlined
				Assert.AreEqual(TextDecorations.None, explicitTextBlock.TextDecorations);
			}
			finally
			{
				// Reset HighContrast
				WinRTFeatureConfiguration.Accessibility.HighContrast = false;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HyperlinkButton_With_HyperlinkUnderlineVisible_True_Should_Be_Underlined()
		{
			// Ensure HighContrast is disabled
			WinRTFeatureConfiguration.Accessibility.HighContrast = false;

			// Set HyperlinkUnderlineVisible to true
			var originalValue = Application.Current.Resources.ContainsKey("HyperlinkUnderlineVisible")
				? Application.Current.Resources["HyperlinkUnderlineVisible"]
				: null;
			Application.Current.Resources["HyperlinkUnderlineVisible"] = true;

			try
			{
				var SUT = new HyperlinkButtonPage();
				TestServices.WindowHelper.WindowContent = SUT;
				await TestServices.WindowHelper.WaitForIdle();

				var implicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
				Assert.IsInstanceOfType(implicitTextBlock, typeof(ImplicitTextBlock));
				// With HyperlinkUnderlineVisible=true, underline should be present
				Assert.AreEqual(TextDecorations.Underline, implicitTextBlock.TextDecorations);

				var explicitTextBlock = VisualTreeHelper.GetChild(SUT.ShouldNotBeUnderlinedHyperlinkButton.GetTemplateChild("ContentPresenter"), 0) as TextBlock;
				Assert.IsNotInstanceOfType(explicitTextBlock, typeof(ImplicitTextBlock));
				// Explicit TextBlock should not be underlined
				Assert.AreEqual(TextDecorations.None, explicitTextBlock.TextDecorations);
			}
			finally
			{
				// Reset HyperlinkUnderlineVisible
				if (originalValue != null)
				{
					Application.Current.Resources["HyperlinkUnderlineVisible"] = originalValue;
				}
				else
				{
					Application.Current.Resources.Remove("HyperlinkUnderlineVisible");
				}
			}
		}
#endif
	}
}
