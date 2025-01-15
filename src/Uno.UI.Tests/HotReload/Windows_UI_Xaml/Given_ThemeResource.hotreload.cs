using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Helpers;
using Uno.UI.Tests.HotReload.Windows_UI_Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.Tests.HotReload.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ThemeResource_HotReload
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
			FeatureConfiguration.Xaml.ForceHotReloadDisabled = false;
		}

		[TestCleanup]
		public void Cleanup()
		{
			FeatureConfiguration.Xaml.ForceHotReloadDisabled = true;
		}

		[TestMethod]
		public void When_Inherited_In_Template_Applied_XAML_Scope()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Template_XAML_Scope_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			var text2InlineBefore = control.InlineTemplateControl.TextBlock2.Text;
			var text2ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock2.Text;

			var text4InlineBefore = control.InlineTemplateControl.TextBlock4.Text;
			var text4ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock4.Text;

			app.HostView.Children.Add(control);

			var text2InlineAfter = control.InlineTemplateControl.TextBlock2.Text;
			var text2ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock2.Text;

			Assert.AreEqual("LocalVisualTree", text2InlineBefore);
			Assert.AreEqual("OuterVisualTree", text2ResourceTemplateBefore);

			Assert.AreEqual("ApplicationLevel", text4InlineBefore);
			Assert.AreEqual("ApplicationLevel", text4ResourceTemplateBefore);

			Assert.AreEqual("LocalVisualTree", text2InlineAfter);
			Assert.AreEqual("LocalVisualTree", text2ResourceTemplateAfter);
		}

		[TestMethod]
		public void When_Theme_Changed()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Theme_Changed_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;

			Assert.AreEqual("LocalVisualTreeLight", textLightThemeMarkup);

			using var _ = ThemeHelper.SwapSystemTheme();

			if (control.Parent == null)
			{
				app.HostView.Children.Add(control); // On UWP the control may have been removed by another test after the async swap
			}
			var textDarkThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;
			Assert.AreEqual("LocalVisualTreeDark", textDarkThemeMarkup); //ThemeResource markup change lookup uses the visual tree (rather than original XAML namescope)
		}

		[TestMethod]
		public void When_Theme_Changed_Static()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Theme_Changed_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;

			Assert.AreEqual("ApplicationLevelLight", textLightStaticMarkup);

			using var _ = ThemeHelper.SwapSystemTheme();
			var textDarkStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;
			Assert.AreEqual("ApplicationLevelLight", textDarkStaticMarkup); //StaticResource markup doesn't change
		}
	}
}
