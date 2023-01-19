using System;
using System.Windows;
using System.Windows.Controls;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.Wpf.Controls;
using Windows.UI.Xaml.Controls;
using PasswordBox = Windows.UI.Xaml.Controls.PasswordBox;
using WpfElement = System.Windows.UIElement;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal class TextTextBoxView : WpfTextBoxView
{
	private readonly WpfTextViewTextBox _textBox = new();

	public TextTextBoxView()
	{
	}

	public override string Text
	{
		get => _textBox.Text;
		set => _textBox.Text = value;
	}

	protected override FrameworkElement RootElement => _textBox;

	public override (int start, int length) Selection
	{
		get => (_textBox.SelectionStart, _textBox.SelectionLength);
		set => (_textBox.SelectionStart, _textBox.SelectionLength) = value;
	}

	public override void SetFocus(bool isFocused) => _textBox.Focus();

	public override bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox) => textBox is not PasswordBox;

	public override IDisposable ObserveTextChanges(EventHandler onChanged)
	{
		void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs args) => onChanged?.Invoke(sender, EventArgs.Empty);
		_textBox.TextChanged += OnTextChanged;
		return Disposable.Create(() => _textBox.TextChanged -= OnTextChanged);
	}

	public override void UpdateProperties(Windows.UI.Xaml.Controls.TextBox winUITextBox)
	{
		SetFont(_textBox, winUITextBox);
		SetForegroundAndHighlightColor(_textBox, winUITextBox);
		//TODO:MZ:
	}
}
