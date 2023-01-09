#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Windows.UI.Xaml.Shapes.BorderLayerRenderer;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal abstract class WpfTextBoxView : ITextBoxView
{
	public WpfTextBoxView()
	{
		
	}

	/// <summary>
	/// Represents the root widget of the input layout.
	/// </summary>
	protected abstract Widget RootWidget { get; }

	/// <summary>
	/// Represents the actual input widget.
	/// </summary>
	protected abstract Widget InputWidget { get; }

	public abstract string Text { get; set; }

	public bool IsDisplayed => RootWidget.Parent is not null;

	public static ITextBoxView Create(TextBox textBox) =>
		textBox is not PasswordBox ?
			new TextTextBoxView() :
			new PasswordTextBoxView();

	public void AddToTextInputLayer(Fixed layer)
	{
		if (textInputLayer.Children.Count == 0)
		{
			textInputLayer.Children.Add(_currentTextBoxInputWidget!);

			if (_isPasswordBox)
			{
				textInputLayer.Children.Add(_currentPasswordBoxInputWidget!);
				_currentPasswordBoxInputWidget!.Visibility = _isPasswordRevealed ? Visibility.Collapsed : Visibility.Visible;
				_currentTextBoxInputWidget!.Visibility = _isPasswordRevealed ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}

	public void RemoveFromTextInputLayer()
	{
		if (RootWidget.Parent is Fixed layer)
		{
			layer.Remove(RootWidget);
		}

		RemoveForegroundCssProvider();
	}

	public abstract (int start, int end) GetSelectionBounds();

	public abstract void SetSelectionBounds(int start, int end);

	public abstract bool IsCompatible(TextBox textBox);

	public abstract IDisposable ObserveTextChanges(EventHandler onChanged);

	public virtual void UpdateProperties(TextBox textBox)
	{

		if ((_isPasswordBox && _currentPasswordBoxInputWidget == null) || _currentTextBoxInputWidget == null)
		{
			// If the input widget does not exist, we don't need to update it.
			return;
		}

		var textBox = _owner.TextBox;
		if (textBox == null)
		{
			// The parent TextBox must exist as source of properties.
			return;
		}

		updateCommon(_currentTextBoxInputWidget);
		updateCommon(_currentPasswordBoxInputWidget);
		SetForeground(textBox.Foreground);
		SetSelectionHighlightColor(textBox.SelectionHighlightColor);

		if (_currentTextBoxInputWidget is not null)
		{
			_currentTextBoxInputWidget.AcceptsReturn = textBox.AcceptsReturn;
			_currentTextBoxInputWidget.TextWrapping = textBox.TextWrapping switch
			{
				Windows.UI.Xaml.TextWrapping.Wrap => TextWrapping.WrapWithOverflow,
				Windows.UI.Xaml.TextWrapping.WrapWholeWords => TextWrapping.Wrap,
				_ => TextWrapping.NoWrap,
			};
			_currentTextBoxInputWidget.MaxLength = textBox.MaxLength;
			_currentTextBoxInputWidget.IsReadOnly = textBox.IsReadOnly;
		}

		if (_currentPasswordBoxInputWidget is not null)
		{
			_currentPasswordBoxInputWidget.MaxLength = textBox.MaxLength;
		}

		void updateCommon(System.Windows.Controls.Control? control)
		{
			if (control is null)
			{
				return;
			}

			control.FontSize = textBox.FontSize;
			control.FontWeight = FontWeight.FromOpenTypeWeight(textBox.FontWeight.Weight);
		}


		//SetFont(textBox.FontWeight, textBox.FontSize);
		//SetForeground(textBox.Foreground);
		//SetSelectionHighlightColor(textBox.SelectionHighlightColor);
	}

	public void SetFocus(bool isFocused)
	{
		if (_isPasswordBox && !_isPasswordRevealed)
		{
			_currentPasswordBoxInputWidget!.Focus();
		}
		else
		{
			_currentTextBoxInputWidget!.Focus();
		}
	}

	public void SetSize(double width, double height)
	{
		updateSizeCore(_currentTextBoxInputWidget);
		updateSizeCore(_currentPasswordBoxInputWidget);

		void updateSizeCore(FrameworkElement? frameworkElement)
		{
			if (frameworkElement is not null && textInputLayer.Children.Contains(frameworkElement))
			{
				frameworkElement.Width = _contentElement.ActualWidth - _contentElement.Padding.Horizontal();
				frameworkElement.Height = _contentElement.ActualHeight - _contentElement.Padding.Vertical();
			}
		}
	}

	public void SetPosition(double x, double y)
	{
		void updatePositionCore(FrameworkElement? frameworkElement)
		{
			if (frameworkElement is not null && textInputLayer.Children.Contains(frameworkElement))
			{
				WpfCanvas.SetLeft(frameworkElement, point.X);
				WpfCanvas.SetTop(frameworkElement, point.Y);
			}
		}
	}

	private void SetFont(FontWeight fontWeight, double fontSize)
	{
		var fontDescription = new FontDescription
		{
			Weight = fontWeight.ToPangoWeight(),
			AbsoluteSize = fontSize * Pango.Scale.PangoScale,
		};
#pragma warning disable CS0612 // Type or member is obsolete
		InputWidget.OverrideFont(fontDescription);
#pragma warning restore CS0612 // Type or member is obsolete
	}



	private void SetForeground(Windows.UI.Xaml.Media.Brush brush)
	{
		var wpfBrush = brush.ToWpfBrush();
		if (_currentTextBoxInputWidget != null)
		{
			_currentTextBoxInputWidget.Foreground = wpfBrush;
			_currentTextBoxInputWidget.CaretBrush = wpfBrush;
		}

		if (_currentPasswordBoxInputWidget != null)
		{
			_currentPasswordBoxInputWidget.Foreground = wpfBrush;
			_currentPasswordBoxInputWidget.CaretBrush = wpfBrush;
		}
	}

	private void SetSelectionHighlightColor(Windows.UI.Xaml.Media.Brush brush)
	{
		var wpfBrush = brush.ToWpfBrush();
		if (_currentTextBoxInputWidget != null)
		{
			_currentTextBoxInputWidget.SelectionBrush = wpfBrush;
		}

		if (_currentPasswordBoxInputWidget != null)
		{
			_currentPasswordBoxInputWidget.SelectionBrush = wpfBrush;
		}
	}




	public void Select(int start, int length)
	{
		if (_isPasswordBox)
		{
			return;
		}

		if (_currentTextBoxInputWidget == null)
		{
			this.StartEntry();
		}

		_currentTextBoxInputWidget!.Select(start, length);
	}

	public int GetSelectionStart()
	{
		if (!_isPasswordBox)
		{
			return _currentTextBoxInputWidget?.SelectionStart ?? 0;
		}

		return 0;
	}

	public int GetSelectionLength()
	{
		if (!_isPasswordBox)
		{
			return _currentTextBoxInputWidget?.SelectionLength ?? 0;
		}

		return 0;
	}

}
