using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

internal sealed class LoggingContentTemplateSelector : DataTemplateSelector
{
	public List<string> Logs { get; } = new();

	protected override DataTemplate SelectTemplateCore(object item)
	{
		Logs.Add(item?.ToString() ?? "null");
		return base.SelectTemplateCore(item);
	}

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		=> SelectTemplateCore(item);
}
