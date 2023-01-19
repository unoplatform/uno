#nullable enable

using System;
using Gtk;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal class MultilineTextBoxView : GtkTextBoxView
{
	private const string MultilineHostCssClass = "textbox_multiline_host";

	private readonly ScrolledWindow _scrolledWindow = new();
	private readonly TextView _textView = new();

	public MultilineTextBoxView()
	{
		_scrolledWindow.Add(_textView);
		_scrolledWindow.TouchEvent += _scrolledWindow_TouchEvent;
		_scrolledWindow.StyleContext.AddClass(MultilineHostCssClass);
	}

	private void _scrolledWindow_TouchEvent(object o, TouchEventArgs args)
	{
	}

	protected override Widget RootWidget => _scrolledWindow;

	protected override Widget InputWidget => _textView;

	public override string Text
	{
		get => _textView.Buffer.Text;
		set => _textView.Buffer.Text = value;
	}

	public override bool IsCompatible(TextBox textBox) => textBox.AcceptsReturn && textBox is not PasswordBox;


	public override (int start, int length) Selection
	{
		get
		{
			_textView.Buffer.GetSelectionBounds(out var start, out var end);
			return (start.Offset, end.Offset - start.Offset);
		}
		set
		{
			var startIterator = _textView.Buffer.GetIterAtOffset(value.start);
			var endIterator = _textView.Buffer.GetIterAtOffset(value.start + value.length);
			_textView.Buffer.SelectRange(startIterator, endIterator);
		}
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
