#nullable enable

using System;
using System.Windows;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.Wpf.Controls;
using WpfElement = System.Windows.UIElement;
using WpfFrameworkElement = System.Windows.FrameworkElement;
using WpfGrid = System.Windows.Controls.Grid;
using WpfPasswordBox = System.Windows.Controls.PasswordBox;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal class PasswordTextBoxView : WpfTextBoxView
{
	private readonly WpfTextViewTextBox _textBox = new();
	private readonly WpfPasswordBox _passwordBox = new();
	private readonly WpfGrid _grid = new();
	private EventHandler? _textChangedWatcher;

	public PasswordTextBoxView()
	{
		_passwordBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
		_passwordBox.Background = System.Windows.Media.Brushes.Transparent;
		_passwordBox.BorderThickness = new Thickness(0);
		_textBox.Visibility = Visibility.Collapsed;
		_passwordBox.Visibility = Visibility.Collapsed;
		_grid.Children.Add(_passwordBox);
		_grid.Children.Add(_textBox);
	}

	public override string Text
	{
		get => _textBox.Visibility == Visibility.Visible ? _textBox.Text : _passwordBox.Password;
		set
		{
			_textBox.Text = value;
			_passwordBox.Password = value;
		}
	}

	protected override WpfFrameworkElement RootElement => _grid;

	public override (int start, int length) Selection
	{
		get => (_textBox.SelectionStart, _textBox.SelectionLength);
		set => (_textBox.SelectionStart, _textBox.SelectionLength) = value;
	}

	public override bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox) => textBox is Windows.UI.Xaml.Controls.PasswordBox;

	public override void SetFocus() => GetDisplayedElement()?.Focus();

	//public void SetIsPassword(bool isPassword)
	//{
	//	_isPasswordRevealed = !isPassword;
	//	if (_owner.TextBox is { } textBox)
	//	{
	//		if (_currentTextBoxInputWidget is not null)
	//		{
	//			_currentTextBoxInputWidget.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
	//			if (_currentPasswordBoxInputWidget is not null)
	//			{
	//				_currentPasswordBoxInputWidget.Visibility = isPassword ? Visibility.Visible : Visibility.Collapsed;
	//			}

	//		}
	//	}



	//	// Display

	//	if (_isPasswordBox)
	//	{
	//		textInputLayer.Children.Add(_currentPasswordBoxInputWidget!);
	//		_currentPasswordBoxInputWidget!.Visibility = _isPasswordRevealed ? Visibility.Collapsed : Visibility.Visible;
	//		_currentTextBoxInputWidget!.Visibility = _isPasswordRevealed ? Visibility.Visible : Visibility.Collapsed;
	//	}
	//}

	public override IDisposable ObserveTextChanges(EventHandler onChanged)
	{
		_textChangedWatcher = onChanged;
		return Disposable.Create(() => _textChangedWatcher = null);
	}

	public override void UpdateProperties(Windows.UI.Xaml.Controls.TextBox textBox)
	{
		SetControlProperties(_textBox, textBox);
		SetControlProperties(_passwordBox, textBox);
		SetTextBoxProperties(_textBox, textBox);
		SetPasswordBoxProperties(_passwordBox, textBox);
	}

	public override void SetPasswordRevealState(Windows.UI.Xaml.Controls.PasswordRevealState passwordRevealState)
	{
		// Sync current text between controls.
		if (_textBox.Visibility == Visibility.Visible)
		{
			_passwordBox.Password = _textBox.Text;
		}
		else
		{
			_textBox.Text = _passwordBox.Password;
		}

		if (passwordRevealState == Windows.UI.Xaml.Controls.PasswordRevealState.Revealed)
		{
			_textBox.Visibility = Visibility.Visible;
			_passwordBox.Visibility = Visibility.Collapsed;
		}
		else
		{
			_textBox.Visibility = Visibility.Collapsed;
			_passwordBox.Visibility = Visibility.Visible;
		}

		// Reset text change observer, to ensure we are watching the correct control.
		ObserveVisibleControlTextChanges();
	}

	private void ObserveVisibleControlTextChanges()
	{
		void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs args) => OnCommonTextChanged();
		void OnPasswordChanged(object sender, EventArgs args) => OnCommonTextChanged();
		IDisposable disposable;
		if (_textBox.Visibility == Visibility.Visible)
		{
			_textBox.TextChanged += OnTextChanged;
			disposable = Disposable.Create(() => _textBox.TextChanged -= OnTextChanged);
		}
		else
		{
			_passwordBox.PasswordChanged += OnPasswordChanged;
			disposable = Disposable.Create(() => _passwordBox.PasswordChanged -= OnPasswordChanged);
		}
	}

	private void OnCommonTextChanged() => _textChangedWatcher?.Invoke(this, EventArgs.Empty);

	private WpfElement? GetDisplayedElement()
	{
		if (_textBox.Visibility == Visibility.Visible)
		{
			return _textBox;
		}
		else if (_passwordBox.Visibility == Visibility.Visible)
		{
			return _passwordBox;
		}
		else
		{
			return null;
		}
	}
}
