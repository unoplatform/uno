#nullable enable

using System;
using System.Linq;
using Gtk;
using Pango;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Gtk.UI.Text;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Runtime.Skia.Gtk.Extensions;
using static Microsoft.UI.Xaml.Shapes.BorderLayerRenderer;
using GtkWindow = Gtk.Window;
using Uno.UI.Runtime.Skia.Gtk.Helpers.Dpi;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Xaml.Controls;

internal abstract class GtkTextBoxView : IOverlayTextBoxView
{
	private const string TextBoxViewCssClass = "textboxview";

	private static bool _warnedAboutSelectionColorChanges;

	private readonly string _textBoxViewId = Guid.NewGuid().ToString();
	private readonly DisplayInformation _displayInformation;
	private CssProvider? _foregroundCssProvider;
	private Windows.UI.Color? _lastForegroundColor;

	protected GtkTextBoxView()
	{
		_displayInformation = DisplayInformation.GetForCurrentView();

		// Applies themes from Theming/UnoGtk.css
		InputWidget.StyleContext.AddClass(TextBoxViewCssClass);
	}

	public event TextControlPasteEventHandler? Paste;

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

	public static IOverlayTextBoxView Create(TextBox textBox) =>
		textBox is PasswordBox || !textBox.AcceptsReturn ?
			new SinglelineTextBoxView(textBox is PasswordBox) :
			new MultilineTextBoxView();

	public void AddToTextInputLayer(XamlRoot xamlRoot)
	{
		if (GtkNativeElementHostingExtension.GetOverlayLayer(xamlRoot) is { } layer && RootWidget.Parent != layer)
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

	public abstract (int start, int length) Selection { get; set; }

	// On Gtk, KeyDown is fired before Selection is updated, so nothing special needs to be done.
	public (int start, int length) SelectionBeforeKeyDown => Selection;

	public abstract bool IsCompatible(TextBox textBox);

	public abstract IDisposable ObserveTextChanges(EventHandler onChanged);

	public virtual void UpdateProperties(TextBox textBox)
	{
		SetFont(textBox);
		SetForeground(textBox.Foreground);
		SetSelectionHighlightColor(textBox.SelectionHighlightColor);
		RootWidget.Opacity = textBox.Opacity;
	}

	public void SetFocus() => InputWidget.HasFocus = true;

	public void SetSize(double width, double height)
	{
		var sizeAdjustment = _displayInformation.FractionalScaleAdjustment;
		RootWidget.SetSizeRequest((int)(width * sizeAdjustment), (int)(height * sizeAdjustment));
		InputWidget.SetSizeRequest((int)(width * sizeAdjustment), (int)(height * sizeAdjustment));
	}

	public void SetPosition(double x, double y)
	{
		if (RootWidget.Parent is Fixed layer)
		{
			var sizeAdjustment = _displayInformation.FractionalScaleAdjustment;
			layer.Move(RootWidget, (int)(x * sizeAdjustment), (int)(y * sizeAdjustment));
		}
	}

	protected void RaisePaste(TextControlPasteEventArgs args) => Paste?.Invoke(this, args);

	private void SetFont(TextBox textBox)
	{
		var sizeAdjustment = _displayInformation.FractionalScaleAdjustment;
		var fontDescription = new FontDescription
		{
			Weight = textBox.FontWeight.ToPangoWeight(),
			Style = textBox.FontStyle.ToGtkFontStyle(),
			Stretch = textBox.FontStretch.ToGtkFontStretch(),
			AbsoluteSize = textBox.FontSize * Pango.Scale.PangoScale * sizeAdjustment,
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

	public virtual void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
}
