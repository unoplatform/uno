#nullable enable

using Gtk;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class UnoGtkEntry : Entry
{
	protected override void OnClipboardPasted()
	{
		var args = new TextControlPasteEventArgs();
		Paste?.Invoke(this, args);
		if (!args.Handled)
		{
			base.OnClipboardPasted();
		}
	}

	public event TextControlPasteEventHandler? Paste;
}
