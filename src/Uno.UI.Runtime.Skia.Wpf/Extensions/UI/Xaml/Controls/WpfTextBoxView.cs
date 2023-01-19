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

	public abstract string Text { get; set; }

	public bool IsDisplayed => RootElement.Parent is not null;

	public abstract (int start, int length) Selection { get; set; }

	public static ITextBoxView Create(Windows.UI.Xaml.Controls.TextBox textBox) =>
		textBox is not Windows.UI.Xaml.Controls.PasswordBox ?
			new TextTextBoxView() :
			new PasswordTextBoxView();

	public void AddToTextInputLayer(XamlRoot xamlRoot)
	{
		if (GetOverlayLayer(xamlRoot) is { } layer && RootElement.Parent != layer)
		{
			layer.Children.Add(RootElement);
		}
	}

	public void RemoveFromTextInputLayer()
	{
		if (RootElement.Parent is WpfCanvas layer)
		{
			layer.Children.Remove(RootElement);
		}
	}

	public abstract bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox);

	public abstract IDisposable ObserveTextChanges(EventHandler onChanged);

	public abstract void UpdateProperties(Windows.UI.Xaml.Controls.TextBox textBox);
		//SetFont(textBox.FontWeight, textBox.FontSize);
		//SetForeground(textBox.Foreground);
		//SetSelectionHighlightColor(textBox.SelectionHighlightColor);

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

	public abstract void SetFocus(bool isFocused);

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

	protected void SetFont(WpfControl wpfControl, Windows.UI.Xaml.Controls.TextBox source)
	{
		wpfControl.FontSize = source.FontSize;
		wpfControl.FontWeight = WpfFontWeight.FromOpenTypeWeight(source.FontWeight.Weight);
		wpfControl.FontStretch = source.FontStretch.ToWpfFontStretch();
		wpfControl.FontStyle = source.FontStyle.ToWpfFontStyle();
		wpfControl.FontFamily = source.FontFamily.ToWpfFontFamily();
	}

	protected void SetForegroundAndHighlightColor(WpfControl wpfControl, Windows.UI.Xaml.Controls.TextBox source)
	{
		var foregroundBrush = source.Foreground.ToWpfBrush();
		var selectionHighlightBrush = source.SelectionHighlightColor.ToWpfBrush();
		wpfControl.Foreground = foregroundBrush;
		if (wpfControl is System.Windows.Controls.TextBox textBox)
		{
			textBox.CaretBrush = foregroundBrush;
			textBox.SelectionBrush = selectionHighlightBrush;
		}
		else if (wpfControl is System.Windows.Controls.PasswordBox passwordBox)
		{
			passwordBox.CaretBrush = foregroundBrush;
			passwordBox.SelectionBrush = selectionHighlightBrush;
		}
	}
}
