#nullable enable

using Gtk;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal class MultilineTextBoxView : GtkTextBoxView
{
	private readonly ScrolledWindow _scrolledWindow = new();
	private readonly TextView _textView = new();

	public MultilineTextBoxView()
	{
		_scrolledWindow.Add(_textView);
	}

	protected override Widget RootWidget => _scrolledWindow;

	protected override Widget ActualInputWidget => _textView;
}
