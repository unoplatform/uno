#nullable enable

using System;
using Uno.UI.Runtime.Skia.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml.Controls;
using GtkWindow = Gtk.Window;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls;

internal class TextBoxViewExtension : OverlayTextBoxViewExtension
{
	private readonly TextBoxView _owner;

	public TextBoxViewExtension(TextBoxView owner) :
		base(owner, GtkTextBoxView.Create)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
	}

	public override bool IsOverlayLayerInitialized =>
		GtkTextBoxView.GetWindowTextInputLayer(_owner.XamlRoot) is not null;	
}
