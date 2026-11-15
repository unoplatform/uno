using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	public class Given_XamlReader_FrameworkTemplate
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_ControlTemplate_TargetType()
		{
			var template = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								 TargetType="ContentControl">
					<Border Background="Red" />
				</ControlTemplate>
				""");

			Assert.IsNotNull(template);
			Assert.AreEqual(typeof(ContentControl), template.TargetType);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_ControlTemplate_TargetType_Nested_In_Style()
		{
			var style = (Style)XamlReader.Load("""
				<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					   TargetType="ContentControl">
					<Setter Property="Template">
						<Setter.Value>
							<ControlTemplate TargetType="ContentControl">
								<Border Background="Red" />
							</ControlTemplate>
						</Setter.Value>
					</Setter>
				</Style>
				""");

			Assert.IsNotNull(style);

			var templateSetter = style.Setters
				.OfType<Setter>()
				.Single(s => s.Property == ContentControl.TemplateProperty);
			var template = (ControlTemplate)templateSetter.Value;

			Assert.IsNotNull(template);
			Assert.AreEqual(typeof(ContentControl), template.TargetType);
		}
	}
}
