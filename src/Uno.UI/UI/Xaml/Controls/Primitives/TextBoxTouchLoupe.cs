#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// The magnifier shown above the finger while the caret (press-and-hold drag) or a selection handle is dragged,
/// for platforms with no system one to use (the browser, iOS before 17). It paints a magnified copy of the text
/// around the caret in a popup, so it works on every Skia host. The platform loupe (<c>INativeTextLoupeExtension</c>)
/// wins whenever there is one.
/// </summary>
internal sealed class TextBoxTouchLoupe
{
	private const double LoupeWidth = 120;
	private const double LoupeHeight = 52;
	private const double CornerRadius = 12;
	private const double Magnification = 1.3;
	private const double GapAboveCaret = 12;
	private const double EdgeMargin = 4;

	private readonly Border _frame;
	private readonly Canvas _content;
	private readonly TextBlock _text;
	private readonly Rectangle _caret;
	private readonly MatrixTransform _textTransform = new();
	private readonly MatrixTransform _placement = new();
	private Popup? _popup;

	public TextBoxTouchLoupe()
	{
		_text = new TextBlock { RenderTransform = _textTransform };
		_caret = new Rectangle { Width = 1.5 };
		_content = new Canvas
		{
			Width = LoupeWidth - 2,
			Height = LoupeHeight - 2,
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, LoupeWidth - 2, LoupeHeight - 2) },
			Children = { _text, _caret }
		};
		_frame = new Border
		{
			Width = LoupeWidth,
			Height = LoupeHeight,
			CornerRadius = new CornerRadius(CornerRadius),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x80, 0x80, 0x80)),
			IsHitTestVisible = false,
			RenderTransform = _placement,
			Child = _content
		};
	}

	// Test hook: whether the loupe is actually painted.
	internal bool IsShowing => _popup?.IsOpen is true;

	/// <summary>Shows the loupe magnifying <paramref name="source"/> around <paramref name="localCaret"/>.</summary>
	/// <param name="source">The text surface being edited. Its text and font are copied.</param>
	/// <param name="localCaret">The caret, in <paramref name="source"/>'s coordinates.</param>
	/// <param name="toRoot">Maps <paramref name="source"/>'s coordinates to the XamlRoot's.</param>
	public void Show(XamlRoot xamlRoot, TextBlock source, Rect localCaret, GeneralTransform toRoot)
	{
		CopyLook(source);

		if (_popup is null || _popup.XamlRoot != xamlRoot)
		{
			_popup?.ClearValue(Popup.ChildProperty);
			_popup = new Popup
			{
				Child = _frame,
				IsLightDismissEnabled = false,
				XamlRoot = xamlRoot
			};
			_popup.PopupPanel.Visual.ZIndex = VisualTree.TextBoxTouchKnobPopupZIndex;
		}

		Move(xamlRoot, localCaret, toRoot);

		if (!_popup.IsOpen)
		{
			_popup.IsOpen = true;
		}
	}

	public void Move(XamlRoot xamlRoot, Rect localCaret, GeneralTransform toRoot)
	{
		// The magnified text is centered on the caret's middle: the text surface's own point goes to the loupe's center.
		var anchorX = localCaret.X;
		var anchorY = localCaret.Y + (localCaret.Height / 2);
		var contentCenter = new Point(_content.Width / 2, _content.Height / 2);
		_textTransform.Matrix = new Matrix(
			Magnification, 0,
			0, Magnification,
			contentCenter.X - (Magnification * anchorX),
			contentCenter.Y - (Magnification * anchorY));

		var caretHeight = localCaret.Height * Magnification;
		_caret.Height = caretHeight;
		Canvas.SetLeft(_caret, contentCenter.X - (_caret.Width / 2));
		Canvas.SetTop(_caret, contentCenter.Y - (caretHeight / 2));

		// Above the finger, centered on the caret, kept inside the window.
		var caretInRoot = toRoot.TransformBounds(localCaret);
		var rootSize = xamlRoot.Size;
		var x = Math.Clamp(caretInRoot.X - (LoupeWidth / 2), EdgeMargin, Math.Max(EdgeMargin, rootSize.Width - LoupeWidth - EdgeMargin));
		var y = Math.Max(EdgeMargin, caretInRoot.Y - LoupeHeight - GapAboveCaret);
		_placement.Matrix = new Matrix(1, 0, 0, 1, x, y);
	}

	public void Hide()
	{
		if (_popup is not null)
		{
			_popup.IsOpen = false;
		}
	}

	private void CopyLook(TextBlock source)
	{
		_text.Text = source.Text;
		_text.FontFamily = source.FontFamily;
		_text.FontSize = source.FontSize;
		_text.FontStyle = source.FontStyle;
		_text.FontWeight = source.FontWeight;
		_text.FontStretch = source.FontStretch;
		_text.CharacterSpacing = source.CharacterSpacing;
		_text.TextWrapping = source.TextWrapping;
		_text.TextAlignment = source.TextAlignment;
		_text.LineHeight = source.LineHeight;
		_text.LineStackingStrategy = source.LineStackingStrategy;
		_text.Foreground = source.Foreground;
		// Wrapping (and so every line's position) depends on the width the text had.
		_text.Width = source.ActualWidth;

		_caret.Fill = source.Foreground;

		// The loupe has no theme of its own: it takes the contrast of the text it shows.
		var foreground = (source.Foreground as SolidColorBrush)?.Color;
		var textIsLight = foreground is { } color && ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) > 128;
		_frame.Background = new SolidColorBrush(textIsLight ? Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2E) : Colors.White);
	}
}
