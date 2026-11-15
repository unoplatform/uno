#nullable enable

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class SymbolIcon
{
	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
	{
		if (GetChildElementNoRef() is TextBlock textBlock)
		{
			textBlock.Foreground = (Brush)e.NewValue;
		}
	}
}
