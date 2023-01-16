#nullable enable

using System;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WpfElement = System.Windows.FrameworkElement;
using WpfControl = System.Windows.Controls.Control;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfFontWeight = System.Windows.FontWeight;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal abstract class WpfTextBoxView : ITextBoxView
{
	public WpfTextBoxView()
	{		
	}

	/// <summary>
	/// Represents the root element of the input layout.
	/// </summary>
	protected abstract WpfElement RootElement { get; }

	/// <summary>
	/// Represents the actual input widget.
	/// </summary>
	protected abstract WpfControl[] InputControls { get; }

	public abstract string Text { get; set; }

	public bool IsDisplayed => RootElement.GetParent() is not null;

	public static ITextBoxView Create(TextBox textBox) =>
		textBox is not PasswordBox ?
			new TextTextBoxView() :
			new PasswordTextBoxView();

	public void AddToTextInputLayer(XamlRoot xamlRoot)
	{
		if (GetOverlayLayer(xamlRoot) is { } layer && RootElement.GetParent() != layer)
		{
			layer.Children.Add(RootElement);

		}
	}

	public void RemoveFromTextInputLayer()
	{
		if (RootElement.GetParent() is WpfCanvas layer)
		{
			layer.Children.Remove(RootElement);
		}
	}

	public abstract (int start, int end) GetSelectionBounds();

	public abstract void SetSelectionBounds(int start, int end);

	public abstract bool IsCompatible(TextBox textBox);

	public abstract IDisposable ObserveTextChanges(EventHandler onChanged);

	public virtual void UpdateProperties(TextBox textBox)
	{
		SetFont(textBox.FontWeight, textBox.FontSize);
		SetForeground(textBox.Foreground);
		SetSelectionHighlightColor(textBox.SelectionHighlightColor);

		//if (_currentTextBoxInputWidget is not null)
		//{
		//	_currentTextBoxInputWidget.AcceptsReturn = textBox.AcceptsReturn;
		//	_currentTextBoxInputWidget.TextWrapping = textBox.TextWrapping switch
		//	{
		//		Windows.UI.Xaml.TextWrapping.Wrap => TextWrapping.WrapWithOverflow,
		//		Windows.UI.Xaml.TextWrapping.WrapWholeWords => TextWrapping.Wrap,
		//		_ => TextWrapping.NoWrap,
		//	};
		//	_currentTextBoxInputWidget.MaxLength = textBox.MaxLength;
		//	_currentTextBoxInputWidget.IsReadOnly = textBox.IsReadOnly;
		//}

		//if (_currentPasswordBoxInputWidget is not null)
		//{
		//	_currentPasswordBoxInputWidget.MaxLength = textBox.MaxLength;
		//}
	}

	public void SetFocus(bool isFocused)
	{	
		//if (_isPasswordBox && !_isPasswordRevealed)
		//{
		//	_currentPasswordBoxInputWidget!.Focus();
		//}
		//else
		//{
		//	_currentTextBoxInputWidget!.Focus();
		//}
	}

	public void SetSize(double width, double height)
	{
		RootElement.Width = RootElement.MaxWidth = width;
		RootElement.Height = RootElement.MaxHeight = height;
	}

	public void SetPosition(double x, double y)
	{
		WpfCanvas.SetLeft(RootElement, x);
		WpfCanvas.SetTop(RootElement, y);
	}

	internal static WpfCanvas? GetOverlayLayer(XamlRoot xamlRoot) =>
		XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

	private void SetFont(FontWeight fontWeight, double fontSize)
	{
		foreach (var control in InputControls)
		{
			control.FontSize = fontSize;
			control.FontWeight = WpfFontWeight.FromOpenTypeWeight(fontWeight.Weight);
		}
	}

	private void SetForeground(Windows.UI.Xaml.Media.Brush brush)
	{
		//var wpfBrush = brush.ToWpfBrush();
		
		//if (_currentTextBoxInputWidget != null)
		//{
		//	_currentTextBoxInputWidget.Foreground = wpfBrush;
		//	_currentTextBoxInputWidget.CaretBrush = wpfBrush;
		//}

		//if (_currentPasswordBoxInputWidget != null)
		//{
		//	_currentPasswordBoxInputWidget.Foreground = wpfBrush;
		//	_currentPasswordBoxInputWidget.CaretBrush = wpfBrush;
		//}
	}

	private void SetSelectionHighlightColor(Windows.UI.Xaml.Media.Brush brush)
	{
		//var wpfBrush = brush.ToWpfBrush();
		//if (_currentTextBoxInputWidget != null)
		//{
		//	_currentTextBoxInputWidget.SelectionBrush = wpfBrush;
		//}

		//if (_currentPasswordBoxInputWidget != null)
		//{
		//	_currentPasswordBoxInputWidget.SelectionBrush = wpfBrush;
		//}
	}

	public void Select(int start, int length)
	{
		//if (_isPasswordBox)
		//{
		//	return;
		//}

		//if (_currentTextBoxInputWidget == null)
		//{
		//	this.StartEntry();
		//}

		//_currentTextBoxInputWidget!.Select(start, length);
	}

	public int GetSelectionStart()
	{
		//if (!_isPasswordBox)
		//{
		//	return _currentTextBoxInputWidget?.SelectionStart ?? 0;
		//}

		return 0;
	}

	public int GetSelectionLength()
	{
		//if (!_isPasswordBox)
		//{
		//	return _currentTextBoxInputWidget?.SelectionLength ?? 0;
		//}
		return 0;
	}
}
