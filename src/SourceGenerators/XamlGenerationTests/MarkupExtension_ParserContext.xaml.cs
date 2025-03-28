using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{
	public partial class MarkupExtension_ParserContext : UserControl
	{
		public MarkupExtension_ParserContext()
		{
			this.InitializeComponent();
		}
	}
}

namespace XamlGenerationTests.Shared.MarkupExtensions
{
	public class MyParserContextExtension : MarkupExtension
	{
		protected override object ProvideValue() => "qweasd";

		protected override object ProvideValue(IXamlServiceProvider serviceProvider) => ProvideValue();
	}
}
