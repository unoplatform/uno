using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.UI;

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

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24882")]
		public void When_DataTemplate_Resources_Text_Content()
		{
			var template = (DataTemplate)XamlReader.Load("""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<Grid>
						<Grid.Resources>
							<Color x:Key="c">#FF0000</Color>
							<Thickness x:Key="t">1,2,3,4</Thickness>
						</Grid.Resources>
					</Grid>
				</DataTemplate>
				""");

			var grid = (Grid)template.LoadContent();

			Assert.AreEqual(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), grid.Resources["c"]);
			Assert.AreEqual(new Thickness(1, 2, 3, 4), grid.Resources["t"]);
		}
	}
}
