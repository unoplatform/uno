#nullable enable

using Uno.UI.Xaml.Controls.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal class TextBoxViewExtension : OverlayTextBoxViewExtension
{
	public TextBoxViewExtension(TextBoxView owner) :
		base(owner, WpfTextBoxView.Create)
	{
	}

	public override bool IsOverlayLayerInitialized(XamlRoot xamlRoot) =>
		WpfTextBoxView.GetOverlayLayer(xamlRoot) is not null;
}
