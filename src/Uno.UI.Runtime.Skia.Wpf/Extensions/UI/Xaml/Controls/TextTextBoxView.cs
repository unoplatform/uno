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

	public override void UpdateProperties(Windows.UI.Xaml.Controls.TextBox textBox)
	{
		//TODO:MZ:
	}

	//	public override string Text { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	//	protected override WpfElement RootElement => _textBox;

	//	protected override WpfElement InputElement => _textBox;


	//	public override (int start, int end) GetSelectionBounds() => throw new NotImplementedException();
	//	public override bool IsCompatible(TextBox textBox) => throw new NotImplementedException();
	//	public override IDisposable ObserveTextChanges(EventHandler onChanged)
	//	{
	//		InputElement.TextChanged += WpfTextViewTextChanged;
	//		disposable.Add(Disposable.Create(() => _currentTextBoxInputWidget.TextChanged -= WpfTextViewTextChanged));
	//	}

	//			if (_currentPasswordBoxInputWidget is not null)
	//			{
	//				_currentPasswordBoxInputWidget.PasswordChanged += PasswordBoxViewPasswordChanged;
	//				disposable.Add(Disposable.Create(() => _currentPasswordBoxInputWidget.PasswordChanged -= PasswordBoxViewPasswordChanged));
	//			}
	//_textChangedDisposable.Disposable = disposable;
	//		}
	//		public override void SetSelectionBounds(int start, int end) => throw new NotImplementedException();

	//private WpfTextViewTextBox CreateInputControl()
	//{
	//	var textView = new WpfTextViewTextBox();
	//	return textView;
	//}
}
