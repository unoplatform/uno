#if HAS_UNO
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_XamlReader_CustomMarkupExtension
	{
		[TestMethod]
		public void When_CustomMarkupExtension_WithNamedParameters()
		{
			// This test reproduces the issue from GitHub issue
			// where custom markup extensions with named parameters fail to parse
			var xaml = """
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<Grid.RowDefinitions>
						<RowDefinition Height="*" />
						<RowDefinition Height="*" />
					</Grid.RowDefinitions>
					<Border Grid.RowSpan="{local:TestResponsive Narrow=1, Wide=2}" Background="Red" />
				</Grid>
				""";

			var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as Grid;
			
			Assert.IsNotNull(element);
			var border = element.Children[0] as Border;
			Assert.IsNotNull(border);
			
			// The markup extension should have evaluated and returned the row span value
			var rowSpan = Grid.GetRowSpan(border);
			Assert.AreEqual(2, rowSpan); // Should return Wide value
		}

		[TestMethod]
		public void When_CustomMarkupExtension_Simple()
		{
			var xaml = """
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<TextBlock Tag="{local:TestSimpleMarkup Value=42}" />
				</Grid>
				""";

			var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as Grid;
			
			Assert.IsNotNull(element);
			var textBlock = element.Children[0] as TextBlock;
			Assert.IsNotNull(textBlock);
			Assert.AreEqual(42, textBlock.Tag);
		}

		[TestMethod]
		public void When_CustomMarkupExtension_InvalidTypeConversion_ThrowsException()
		{
			// Test that invalid type conversions throw clear error messages
			var xaml = """
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<TextBlock Tag="{local:TestSimpleMarkup Value=NotANumber}" />
				</Grid>
				""";

			Assert.ThrowsException<Uno.Xaml.XamlParseException>(() =>
			{
				var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
			});
		}

		[TestMethod]
		public void When_CustomMarkupExtension_UnknownProperty_ThrowsWhenConfigured()
		{
			// Save current configuration
			var originalConfig = Uno.UI.FeatureConfiguration.XamlReader.FailOnUnknownProperties;
			
			try
			{
				// Enable FailOnUnknownProperties
				Uno.UI.FeatureConfiguration.XamlReader.FailOnUnknownProperties = true;

				var xaml = """
					<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						  xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
						<TextBlock Tag="{local:TestSimpleMarkup Value=42, NonExistentProperty=99}" />
					</Grid>
					""";

				Assert.ThrowsException<Uno.Xaml.XamlParseException>(() =>
				{
					var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
				});
			}
			finally
			{
				// Restore original configuration
				Uno.UI.FeatureConfiguration.XamlReader.FailOnUnknownProperties = originalConfig;
			}
		}

		[TestMethod]
		public void When_CustomMarkupExtension_NestedMarkupExtension()
		{
			// Test nested markup extensions
			var xaml = """
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup">
					<TextBlock Tag="{local:TestNested Inner={local:TestSimpleMarkup Value=99}}" />
				</Grid>
				""";

			var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as Grid;
			
			Assert.IsNotNull(element);
			var textBlock = element.Children[0] as TextBlock;
			Assert.IsNotNull(textBlock);
			// The nested markup extension should have been evaluated
			Assert.AreEqual(99, textBlock.Tag);
		}
	}

	// Test markup extensions
	// Note: WinUI/Uno automatically resolves 'TestResponsive' to 'TestResponsiveExtension'
	// by appending 'Extension' suffix when used in XAML markup
	public class TestResponsiveExtension : MarkupExtension
	{
		public int Narrow { get; set; }
		public int Wide { get; set; }

		protected override object ProvideValue(IXamlServiceProvider serviceProvider)
		{
			// For testing purposes, just return Wide value
			return Wide;
		}
	}

	public class TestSimpleMarkupExtension : MarkupExtension
	{
		public int Value { get; set; }

		protected override object ProvideValue(IXamlServiceProvider serviceProvider)
		{
			return Value;
		}
	}

	// Markup extension for testing nested scenarios
	public class TestNestedExtension : MarkupExtension
	{
		public object Inner { get; set; }

		protected override object ProvideValue(IXamlServiceProvider serviceProvider)
		{
			// Return the inner value
			return Inner;
		}
	}
}
#endif
