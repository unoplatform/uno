#nullable enable

using System;
using Gtk;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;

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

	protected override Widget InputWidget => _textView;

	public override string Text
	{
		get => _textView.Buffer.Text;
		set => _textView.Buffer.Text = value;
	}

	public override bool IsCompatible(TextBox textBox) => textBox.AcceptsReturn && textBox is not PasswordBox;

	public override (int start, int end) GetSelectionBounds() => _textView.Buffer.GetSelectionBounds(out var start, out var end) ? (start.Offset, end.Offset) : (0, 0); // TODO: Confirm this implementation is correct.

	public override void SetSelectionBounds(int start, int end)
	{
		var startIterator = _textView.Buffer.GetIterAtOffset(start);
		var endIterator = _textView.Buffer.GetIterAtOffset(end);
		_textView.Buffer.SelectRange(startIterator, endIterator);
	}

	public override void UpdateProperties(TextBox textBox)
	{
		base.UpdateProperties(textBox);

		_textView.Editable = !textBox.IsReadOnly;
		_textView.WrapMode = textBox.TextWrapping switch
		{
			Windows.UI.Xaml.TextWrapping.Wrap => Gtk.WrapMode.WordChar,
			Windows.UI.Xaml.TextWrapping.WrapWholeWords => Gtk.WrapMode.Word,
			_ => Gtk.WrapMode.None,
		};
	}

	public override IDisposable ObserveTextChanges(EventHandler onChanged)
	{
		_textView.Buffer.Changed += onChanged;
		return Disposable.Create(() => _textView.Buffer.Changed -= onChanged);
	}
}
