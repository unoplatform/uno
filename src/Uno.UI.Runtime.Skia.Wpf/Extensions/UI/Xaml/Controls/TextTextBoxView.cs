using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
using Windows.UI.Xaml.Controls;
using static Uno.UI.FeatureConfiguration;
using PasswordBox = Windows.UI.Xaml.Controls.PasswordBox;
using WpfElement = System.Windows.UIElement;
using WpfFrameworkElement = System.Windows.FrameworkElement;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal class TextTextBoxView : WpfTextBoxView
{
	private readonly WpfTextViewTextBox _textBox = new();
	private (int start, int length) _selectionBeforeKeyDown;

	public TextTextBoxView()
	{
		_textBox.PreviewKeyDown += OnPreviewKeyDown;
	}

	public override string Text
	{
		get => _textBox.Text;
		set => _textBox.Text = value;
	}

	protected override WpfFrameworkElement RootElement => _textBox;

	public override (int start, int length) Selection
	{
		get => (_textBox.SelectionStart, _textBox.SelectionLength);
		set
		{
			(_textBox.SelectionStart, _textBox.SelectionLength) = value;
			(_selectionBeforeKeyDown.start, _selectionBeforeKeyDown.length) = value;
		}
	}

	public override (int start, int length) SelectionBeforeKeyDown
	{
		get => (_selectionBeforeKeyDown.start, _selectionBeforeKeyDown.length);
		protected set => (_selectionBeforeKeyDown.start, _selectionBeforeKeyDown.length) = value;
	}

	public override void SetFocus() => _textBox.Focus();

	public override bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox) => textBox is not PasswordBox;

	public override IDisposable ObserveTextChanges(EventHandler onChanged)
	{
		void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs args) => onChanged?.Invoke(sender, EventArgs.Empty);
		_textBox.TextChanged += OnTextChanged;
		return Disposable.Create(() => _textBox.TextChanged -= OnTextChanged);
	}

	public override void UpdateProperties(Windows.UI.Xaml.Controls.TextBox winUITextBox)
	{
		SetControlProperties(_textBox, winUITextBox);
		SetTextBoxProperties(_textBox, winUITextBox);
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		// On WPF, KeyDown is fired AFTER Selection is already changed to the new value
		SelectionBeforeKeyDown = Selection;
	}
}
