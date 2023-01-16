using System;
using System.Windows;
using Uno.UI.Runtime.Skia.Wpf.Controls;
using Windows.UI.Xaml.Controls;
using WpfElement = System.Windows.UIElement;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal class TextTextBoxView : WpfTextBoxView
{
	private readonly WpfTextViewTextBox _textBox = new();

	public TextTextBoxView()
	{
	}

	public override string Text { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	protected override FrameworkElement RootElement => throw new NotImplementedException();

	protected override System.Windows.Controls.Control[] InputControls => throw new NotImplementedException();

	public override (int start, int end) GetSelectionBounds() => throw new NotImplementedException();
	public override bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox) => throw new NotImplementedException();
	public override IDisposable ObserveTextChanges(EventHandler onChanged) => throw new NotImplementedException();
	public override void SetSelectionBounds(int start, int end) => throw new NotImplementedException();

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
