#nullable enable

using Gtk;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;
internal class UnoGtkTextView : TextView
{
	protected override void OnPasteClipboard()
	{
		var args = new TextControlPasteEventArgs();
		Paste?.Invoke(this, args);
		if (!args.Handled)
		{
			base.OnPasteClipboard();
		}
	}

	public event TextControlPasteEventHandler? Paste;
}
