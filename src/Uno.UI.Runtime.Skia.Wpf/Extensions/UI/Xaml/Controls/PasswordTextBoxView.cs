#nullable enable

using System;
using System.Windows;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
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
	private readonly SerialDisposable _visibleControlTextChangedSubscription = new();
	private EventHandler? _textChangedWatcher;
	private (int start, int length) _selectionBeforeKeyDown;

	public PasswordTextBoxView()
	{
		_passwordBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
		_passwordBox.Background = System.Windows.Media.Brushes.Transparent;
		_passwordBox.BorderThickness = new Thickness(0);
		_textBox.Visibility = Visibility.Collapsed;
		_passwordBox.Visibility = Visibility.Collapsed;
		_grid.Children.Add(_passwordBox);
		_grid.Children.Add(_textBox);

		_passwordBox.PreviewKeyDown += OnPreviewKeyDown;
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

	public override (int start, int length) SelectionBeforeKeyDown
	{
		get => (_selectionBeforeKeyDown.start, _selectionBeforeKeyDown.length);
		protected set => (_selectionBeforeKeyDown.start, _selectionBeforeKeyDown.length) = value;
	}

	public override bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox) => textBox is Windows.UI.Xaml.Controls.PasswordBox;

	public override void SetFocus() => GetDisplayedElement().Focus();

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
		_visibleControlTextChangedSubscription.Disposable = null;

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

		_visibleControlTextChangedSubscription.Disposable = disposable;
	}

	private void OnCommonTextChanged() => _textChangedWatcher?.Invoke(this, EventArgs.Empty);

	private WpfElement GetDisplayedElement() => _textBox.Visibility == Visibility.Visible ? _textBox : _passwordBox;

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		// WpfTextBox's cursor/selection position isn't accessible even with reflection
		// for security reasons. This prevents TextBox keyboard navigation from working
		// correctly. So at least we just set SelectionBeforeKeyDown to any fake value
		// that prevents keyboard navigation from triggering all together.
		if (e.Key == Key.Left || e.Key == Key.LeftShift || e.Key == Key.LeftCtrl || e.Key == Key.LeftAlt)
		{
			SelectionBeforeKeyDown = (_passwordBox.Password.Length, 0);
		}
		else
		{
			SelectionBeforeKeyDown = (0, 0);
		}
	}
}
