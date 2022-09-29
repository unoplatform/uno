#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.HotReload.Windows_UI_Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.Tests.HotReload.Windows_UI_Xaml
{
	[TestClass]
	public class Given_StaticResource_HotReload
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
		public void When_Inherited_In_Template_Applied()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Template_XAML_Scope_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			var text1InlineBefore = control.InlineTemplateControl.TextBlock1.Text;
			var text1ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock1.Text;

			var text3InlineBefore = control.InlineTemplateControl.TextBlock3.Text;
			var text3ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock3.Text;

			app.HostView.Children.Add(control);

			var text1InlineAfter = control.InlineTemplateControl.TextBlock1.Text;
			var text1ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock1.Text;

			Assert.AreEqual("LocalVisualTree", text1InlineBefore);
			Assert.AreEqual("OuterVisualTree", text1ResourceTemplateBefore);

			Assert.AreEqual("ApplicationLevel", text3InlineBefore);
			Assert.AreEqual("ApplicationLevel", text3ResourceTemplateBefore);

			Assert.AreEqual("LocalVisualTree", text1InlineAfter);
			Assert.AreEqual("OuterVisualTree", text1ResourceTemplateAfter);
		}

		[TestMethod]
		public void When_Inherited_In_Template_XAML_Scope()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Template_XAML_Scope_Control();

			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var text1ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock1.Text;

			Assert.AreEqual("OuterVisualTree", text1ResourceTemplateAfter);
		}
	}
}
