using System;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests;

/// <summary>
/// A custom markup extension for testing XamlReader.Load with custom markup extensions
/// </summary>
internal class CustomMarkupExtension : MarkupExtension
{
	public string Value1 { get; set; }
	public string Value2 { get; set; }

	protected override object ProvideValue()
	{
		return $"{Value1},{Value2}";
	}
}

/// <summary>
/// A simplified version similar to Uno.Toolkit.UI.Responsive markup extension
/// </summary>
internal class ResponsiveExtension : MarkupExtension
{
	public int Narrow { get; set; }
	public int Wide { get; set; }

	protected override object ProvideValue()
	{
		// For testing purposes, just return the Wide value
		return Wide;
	}
}
