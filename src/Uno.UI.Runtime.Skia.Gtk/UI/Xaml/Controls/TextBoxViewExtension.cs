#nullable enable

using Uno.UI.Runtime.Skia.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls;

internal class TextBoxViewExtension : OverlayTextBoxViewExtension
{
	public TextBoxViewExtension(TextBoxView owner) :
		base(owner, GtkTextBoxView.Create)
	{		
	}

	public override bool IsOverlayLayerInitialized(XamlRoot xamlRoot) =>
		GtkTextBoxView.GetOverlayLayer(xamlRoot) is not null;	
}
