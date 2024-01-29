using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

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
