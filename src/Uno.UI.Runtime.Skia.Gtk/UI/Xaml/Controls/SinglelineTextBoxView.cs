#nullable enable

using Gtk;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal class SinglelineTextBoxView : GtkTextBoxView
{
	private readonly Entry _entry = new();

	protected override Widget ActualInputWidget => _entry;

	protected override Widget RootWidget => _entry;
}
