using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_MarkupExtension
	{
#if HAS_UNO || !WINAPPSDK // the signatures are present from winui, uno\uwp and uno\winui, just not uwp
		[TestMethod]
		public void When_MarkupExtension_Default()
		{
			var page = new MarkupExtension_ParserContext();

			var sut = (page.SimpleMarkupExtension as TextBlock);
			var context = (IXamlServiceProvider)sut.Tag;
			var pvt = (IProvideValueTarget)context.GetService(typeof(IProvideValueTarget));
			var rop = (IRootObjectProvider)context.GetService(typeof(IRootObjectProvider));
			var property = (ProvideValueTargetProperty)pvt.TargetProperty;

			Assert.AreEqual(pvt.TargetObject, sut);
			Assert.AreEqual(nameof(TextBlock.Tag), property.Name);
			Assert.AreEqual(rop.RootObject, page);
		}

		[TestMethod]
		public void When_MarkupExtension_Nested()
		{
			var page = new MarkupExtension_ParserContext();

			var sut = (page.NestedMarkupExtension as TextBlock);
			var context = (IXamlServiceProvider)sut.Tag;
			var pvt = (IProvideValueTarget)context.GetService(typeof(IProvideValueTarget));
			var rop = (IRootObjectProvider)context.GetService(typeof(IRootObjectProvider));
			var property = (ProvideValueTargetProperty)pvt.TargetProperty;

			Assert.IsInstanceOfType(pvt.TargetObject, typeof(Binding));
			Assert.AreEqual(nameof(Binding.Source), property.Name);
			Assert.AreEqual(rop.RootObject, page);
		}

		[TestMethod]
		public async Task When_MarkupExtension_ResourceDictionary1()
		{
			var page = new MarkupExtension_ParserContext();
			await UITestHelper.Load(page, isLoaded: x => x.IsLoaded); // waiting for control-template to materialize

			var sut = (Grid)(page.ButtonMarkupExtension_Style as Button).GetTemplateRoot();
			var context = (IXamlServiceProvider)sut.Tag;
			var pvt = (IProvideValueTarget)context.GetService(typeof(IProvideValueTarget));
			var rop = (IRootObjectProvider)context.GetService(typeof(IRootObjectProvider));
			var property = (ProvideValueTargetProperty)pvt.TargetProperty;

			Assert.AreEqual(pvt.TargetObject, sut);
			Assert.AreEqual(nameof(TextBlock.Tag), property.Name);
			Assert.IsInstanceOfType(rop.RootObject, typeof(ResourceDictionary));
		}

		[TestMethod]
		public async Task When_MarkupExtension_ResourceDictionary2()
		{
			var page = new MarkupExtension_ParserContext();
			await UITestHelper.Load(page, isLoaded: x => x.IsLoaded); // waiting for control-template to materialize

			var sut = (Grid)(page.ButtonMarkupExtension_Template as Button).GetTemplateRoot();
			var context = (IXamlServiceProvider)sut.Tag;
			var pvt = (IProvideValueTarget)context.GetService(typeof(IProvideValueTarget));
			var rop = (IRootObjectProvider)context.GetService(typeof(IRootObjectProvider));
			var property = (ProvideValueTargetProperty)pvt.TargetProperty;

			Assert.AreEqual(pvt.TargetObject, sut);
			Assert.AreEqual(nameof(TextBlock.Tag), property.Name);
			Assert.IsInstanceOfType(rop.RootObject, typeof(ResourceDictionary));
		}

		[TestMethod]
		public void When_MarkupExtension_Enum()
		{
			var page = new MarkupExtension_ParserContext();

			Assert.AreEqual(Orientation.Horizontal, page.EnumMarkupExtension_Horizontal.Orientation);
			Assert.AreEqual(Orientation.Vertical, page.EnumMarkupExtension_Vertical.Orientation);
		}

		[TestMethod]
		public void When_MarkupExtension_ReturnNullForNullableStructType()
		{
			// it also shouldn't throw here
			var page = new MarkupExtension_CodegenTypeCast();

			Assert.IsTrue(page.Control.IsChecked, "sanity check failed: Control.IsChecked is not true");
			Assert.IsNull(page.SUT.IsChecked, "Property value should be set to null by the markup-extension");
		}

		[TestMethod]
		public void When_MarkupExtension_FullNameFirst()
		{
			var page = new MarkupExtension_FullNameFirst();

			Assert.AreEqual(nameof(FullNameFirstMarkupExtension), (page.FullName as TextBlock).Tag);
			Assert.AreEqual(nameof(ShortNameMarkup), (page.ShortName as TextBlock).Tag);
		}

		[TestMethod]
		public void When_MarkupExtension_MultiLevelNesting()
		{
			var page = new MarkupExtension_MultiLevelNesting();

			Assert.AreEqual(nameof(TargetValueExtension), (page.Nested as TextBlock).Tag);
		}

		[TestMethod]
		public async Task When_MarkupExtension_SiblingControlExtension()
		{
			// A control element (SiblingExtensionControl) must not be misidentified as a markup extension just
			// because a sibling SiblingExtensionControlExtension exists in the same namespace. The element must be
			// instantiated and its bindings must be emitted. See https://github.com/unoplatform/uno/issues/21992.
			var page = new MarkupExtension_SiblingExtension();
			await UITestHelper.Load(page, isLoaded: x => x.IsLoaded);

			Assert.AreEqual("literal", (page.Literal as SiblingExtensionControl).Value, "Literal value was not set (control was likely dropped).");
			Assert.AreEqual("bound", (page.XBind as SiblingExtensionControl).Value, "x:Bind binding code was not emitted.");
			Assert.AreEqual("bound", (page.Bound as SiblingExtensionControl).Value, "Binding code was not emitted.");
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_MarkupExtension_SiblingControlExtension_XamlReader()
		{
			// Same scenario as When_MarkupExtension_SiblingControlExtension, but exercising the runtime XamlReader.
			var panel = (StackPanel)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				            xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<local:SiblingExtensionControl x:Name="Literal" Value="literal" />
					<local:SiblingExtensionControl x:Name="Bound" Value="{Binding}" />
				</StackPanel>
				""");

			panel.DataContext = "bound";
			await UITestHelper.Load(panel, isLoaded: x => x.IsLoaded); // bare Control has no template => 0 size, so bypass the size-based load check

			var literal = (SiblingExtensionControl)panel.FindName("Literal");
			var bound = (SiblingExtensionControl)panel.FindName("Bound");

			Assert.AreEqual("literal", literal.Value, "Literal value was not set (control was likely dropped).");
			Assert.AreEqual("bound", bound.Value, "Binding was not processed for the control.");
		}
#endif
	}
}
