// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference RichTextBlockOverflow.cpp (OnPointerEntered/Moved/Exited/Pressed/Released, HitTestLink),
// tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

// An overflow column hosts the master's content, so it has to answer for the links inside its own
// slice: without this every column after the first is inert - no cursor, no click.
partial class RichTextBlockOverflow
{
	private Hyperlink? _hyperlinkOver;

	private Hyperlink? HyperlinkOver
	{
		get => _hyperlinkOver;
		set
		{
			if (_hyperlinkOver != value)
			{
				_hyperlinkOver = value;
				UpdateProtectedCursor();
			}
		}
	}

	// A column with no Background is not hit-testable by default, so pointer events never reach it.
	// It is hit-testable exactly when it is hosting a slice of the master's content.
	internal override bool IsViewHit() => _pPageNode is not null || base.IsViewHit();

	private void SubscribeToInput()
	{
		PointerPressed += OnPointerPressedForLinks;
		PointerReleased += OnPointerReleasedForLinks;
		PointerMoved += OnPointerMovedForLinks;
		PointerExited += OnPointerExitedForLinks;
		PointerCanceled += OnPointerCanceledForLinks;
		PointerCaptureLost += OnPointerCanceledForLinks;
	}

	private void OnPointerPressedForLinks(object sender, PointerRoutedEventArgs e)
	{
		if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
		{
			return;
		}

		if (FindHyperlinkAt(e) is { } hyperlink && CapturePointer(e.Pointer))
		{
			hyperlink.SetPointerPressed(e.Pointer);
			e.Handled = true;
			CompleteGesture();
		}
	}

	private void OnPointerReleasedForLinks(object sender, PointerRoutedEventArgs e)
	{
		if (FindHyperlinkAt(e) is { } hyperlink && hyperlink.ReleasePointerPressed(e.Pointer))
		{
			e.Handled = true;
		}
		else
		{
			AbortHyperlinkPress(e);
		}

		ReleasePointerCapture(e.Pointer);
	}

	private void OnPointerMovedForLinks(object sender, PointerRoutedEventArgs e)
		=> HyperlinkOver = FindHyperlinkAt(e);

	private void OnPointerExitedForLinks(object sender, PointerRoutedEventArgs e)
		=> HyperlinkOver = null;

	private void OnPointerCanceledForLinks(object sender, PointerRoutedEventArgs e)
	{
		AbortHyperlinkPress(e);
		HyperlinkOver = null;
	}

	private void AbortHyperlinkPress(PointerRoutedEventArgs e)
	{
		if (_pMaster is not null)
		{
			foreach (var hyperlink in _pMaster.GetHyperlinks())
			{
				hyperlink.AbortPointerPressed(e.Pointer);
			}
		}
	}

	private void UpdateProtectedCursor()
		=> ProtectedCursor = HyperlinkOver is not null
			? InputSystemCursor.Create(InputSystemCursorShape.Hand)
			: null;

	// CRichTextBlockOverflow::HitTestLink — the slice this column arranged, in its own coordinates.
	private Hyperlink? FindHyperlinkAt(PointerRoutedEventArgs e)
	{
		var point = e.GetCurrentPoint(this).Position;
		var adjustedPoint = new Point(point.X - Padding.Left, point.Y - Padding.Top);

		foreach (var layout in _paragraphLayouts)
		{
			var paraTop = layout.YOffset;
			var paraBottom = paraTop + layout.Size.Height;

			if (adjustedPoint.Y >= paraTop && adjustedPoint.Y < paraBottom)
			{
				// The column draws its first line at the top, so add back the lines it skipped to
				// reach the paragraph's own coordinate space.
				var localPoint = new Point(
					adjustedPoint.X - layout.Margin.Left,
					adjustedPoint.Y - layout.YOffset + layout.ParsedText.GetLineTop(layout.FirstLine));

				return layout.ParsedText.GetHyperlinkAt(localPoint);
			}
		}

		return null;
	}
}
