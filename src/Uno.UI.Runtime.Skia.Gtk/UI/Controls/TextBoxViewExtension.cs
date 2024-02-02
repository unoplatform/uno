#nullable enable

using Uno.UI.Runtime.Skia.Gtk.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Gtk.Extensions.UI.Xaml.Controls;

internal class TextBoxViewExtension : OverlayTextBoxViewExtension
{
	public TextBoxViewExtension(TextBoxView owner) :
		base(owner, GtkTextBoxView.Create)
	{
	}

	public override bool IsOverlayLayerInitialized(XamlRoot xamlRoot) =>
		GtkNativeElementHostingExtension.GetOverlayLayer(xamlRoot) is not null;
}
