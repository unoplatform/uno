#nullable enable

using System;
using System.Windows;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfElement = System.Windows.FrameworkElement;
using WpfFontWeight = System.Windows.FontWeight;
using WpfPasswordBox = System.Windows.Controls.PasswordBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal abstract class WpfTextBoxView : IOverlayTextBoxView
{
	protected WpfTextBoxView()
	{
	}

	public abstract string Text { get; set; }

	public bool IsDisplayed => RootElement.Parent is not null;

	public abstract (int start, int length) Selection { get; set; }

	public abstract (int start, int length) SelectionBeforeKeyDown { get; protected set; }

	/// <summary>
	/// Represents the root element of the input layout.
	/// </summary>
	protected abstract WpfElement RootElement { get; }

	public event Windows.UI.Xaml.Controls.TextControlPasteEventHandler? Paste;

	public static IOverlayTextBoxView Create(Windows.UI.Xaml.Controls.TextBox textBox) =>
		textBox is not Windows.UI.Xaml.Controls.PasswordBox ?
			new TextTextBoxView() :
			new PasswordTextBoxView();

	public void AddToTextInputLayer(XamlRoot xamlRoot)
	{
		if (GetOverlayLayer(xamlRoot) is { } layer && RootElement.Parent != layer)
		{
			layer.Children.Add(RootElement);
			DataObject.AddPastingHandler(RootElement, PasteHandler);
		}
	}

	private void PasteHandler(object sender, DataObjectPastingEventArgs e)
	{
		var args = new TextControlPasteEventArgs();
		Paste?.Invoke(sender, args);
		if (args.Handled)
		{
			e.CancelCommand();
		}
	}

	public void RemoveFromTextInputLayer()
	{
		if (RootElement.Parent is WpfCanvas layer)
		{
			layer.Children.Remove(RootElement);
			DataObject.RemovePastingHandler(RootElement, PasteHandler);
		}
	}

	public abstract bool IsCompatible(Windows.UI.Xaml.Controls.TextBox textBox);

	public abstract IDisposable ObserveTextChanges(EventHandler onChanged);

	public abstract void UpdateProperties(Windows.UI.Xaml.Controls.TextBox textBox);

	public abstract void SetFocus();

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

	public virtual void SetPasswordRevealState(Windows.UI.Xaml.Controls.PasswordRevealState passwordRevealState) { }

	internal static WpfCanvas? GetOverlayLayer(XamlRoot xamlRoot) =>
		WpfManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

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
		if (wpfControl is WpfTextBox textBox)
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

	protected void SetControlProperties(WpfControl wpfControl, Windows.UI.Xaml.Controls.TextBox source)
	{
		wpfControl.Opacity = source.Opacity;
		SetFont(wpfControl, source);
		SetForegroundAndHighlightColor(wpfControl, source);
	}

	protected void SetTextBoxProperties(WpfTextBox wpfTextBox, Windows.UI.Xaml.Controls.TextBox source)
	{
		wpfTextBox.IsReadOnly = source.IsReadOnly || !source.IsTabStop;
		wpfTextBox.MaxLength = source.MaxLength;
		wpfTextBox.AcceptsReturn = source.AcceptsReturn;
		wpfTextBox.TextAlignment = source.TextAlignment switch
		{
			Windows.UI.Xaml.TextAlignment.Center => System.Windows.TextAlignment.Center,
			Windows.UI.Xaml.TextAlignment.Left => System.Windows.TextAlignment.Left,
			Windows.UI.Xaml.TextAlignment.Right => System.Windows.TextAlignment.Right,
			Windows.UI.Xaml.TextAlignment.Justify => System.Windows.TextAlignment.Justify,
			_ => System.Windows.TextAlignment.Left
		};
		wpfTextBox.TextWrapping = source.TextWrapping switch
		{
			Windows.UI.Xaml.TextWrapping.NoWrap => System.Windows.TextWrapping.NoWrap,
			Windows.UI.Xaml.TextWrapping.Wrap => System.Windows.TextWrapping.Wrap,
			Windows.UI.Xaml.TextWrapping.WrapWholeWords => System.Windows.TextWrapping.WrapWithOverflow,
			_ => System.Windows.TextWrapping.NoWrap
		};
	}

	protected void SetPasswordBoxProperties(WpfPasswordBox wpfPasswordBox, Windows.UI.Xaml.Controls.TextBox source)
	{
		wpfPasswordBox.IsTabStop = source.IsReadOnly || !source.IsTabStop;
		wpfPasswordBox.MaxLength = source.MaxLength;
	}
}
