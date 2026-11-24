#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class FontIcon
{
	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e) =>
		UpdateLinkedChildProperty(ForegroundProperty, TextBlock.ForegroundProperty);
}
