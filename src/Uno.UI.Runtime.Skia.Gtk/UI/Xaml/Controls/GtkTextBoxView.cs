#nullable enable

using System;
using System.Linq;
using Gtk;
using Pango;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.UI.Text;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.XamlHost.Skia.Gtk.Hosting;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Windows.UI.Xaml.Shapes.BorderLayerRenderer;
using GtkWindow = Gtk.Window;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal abstract class GtkTextBoxView : ITextBoxView
{
	private const string TextBoxViewCssClass = "textboxview";

	private static bool _warnedAboutSelectionColorChanges;

	private readonly string _textBoxViewId = Guid.NewGuid().ToString();
	private CssProvider? _foregroundCssProvider;
	private Windows.UI.Color? _lastForegroundColor;

	public GtkTextBoxView()
	{
		InputWidget.StyleContext.AddClass(TextBoxViewCssClass);
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
		textBox is PasswordBox || !textBox.AcceptsReturn ?
			new SinglelineTextBoxView(textBox is PasswordBox) :
			new MultilineTextBoxView();

	public void AddToTextInputLayer(XamlRoot xamlRoot)
	{
		if (GetOverlayLayer(xamlRoot) is { } layer && RootWidget.Parent != layer)
		{
			layer.Put(RootWidget, 0, 0);
			layer.ShowAll();
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
		SetFont(textBox.FontWeight, textBox.FontSize);
		SetForeground(textBox.Foreground);
		SetSelectionHighlightColor(textBox.SelectionHighlightColor);
		RootWidget.Opacity = textBox.Opacity;
	}

	public void SetFocus(bool isFocused) => InputWidget.HasFocus = isFocused;

	public void SetSize(double width, double height)
	{
		RootWidget.SetSizeRequest((int)width, (int)height);
		InputWidget.SetSizeRequest((int)width, (int)height);
	}

	public void SetPosition(double x, double y)
	{
		if (RootWidget.Parent is Fixed layer)
		{
			layer.Move(RootWidget, (int)x, (int)y);
		}
	}

	internal static Fixed? GetOverlayLayer(XamlRoot xamlRoot) =>
		XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

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

	private void SetForeground(Brush brush)
	{
		if (brush is not SolidColorBrush scb)
		{
			return;
		}

		if (_lastForegroundColor == scb.ColorWithOpacity &&
			_foregroundCssProvider is not null)
		{
			return;
		}

		_lastForegroundColor = scb.ColorWithOpacity;
		RemoveForegroundCssProvider();
		_foregroundCssProvider = new CssProvider();
		var color = $"rgba({scb.ColorWithOpacity.R},{scb.ColorWithOpacity.G},{scb.ColorWithOpacity.B},{scb.ColorWithOpacity.A})";
		var cssClassName = $"textbox_foreground_{_textBoxViewId}";
		var data = $".{cssClassName}, .{cssClassName} text {{ caret-color: {color}; color: {color}; }}";
		_foregroundCssProvider.LoadFromData(data);
		StyleContext.AddProviderForScreen(Gdk.Screen.Default, _foregroundCssProvider, priority: uint.MaxValue);
		if (!InputWidget.StyleContext.HasClass(cssClassName))
		{
			InputWidget.StyleContext.AddClass(cssClassName);
		}
	}

	private void RemoveForegroundCssProvider()
	{
		if (_foregroundCssProvider is not null)
		{
			StyleContext.RemoveProviderForScreen(Gdk.Screen.Default, _foregroundCssProvider);
			_foregroundCssProvider.Dispose();
			_foregroundCssProvider = null;
		}
	}

	private void SetSelectionHighlightColor(Brush brush)
	{
		if (!_warnedAboutSelectionColorChanges)
		{
			_warnedAboutSelectionColorChanges = true;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				// Selection highlight color change is not supported on GTK currently
				this.Log().LogWarning("SelectionHighlightColor changes are currently not supported on GTK");
			}
		}
	}
}
