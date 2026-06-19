// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference RichTextBlock.cpp (GetContentStart/End, GetSelectionStart/End, GetTextPositionFromPoint),
// RichTextBlock_Partial.cpp (ContentStart/End, SelectionStart/End, Select, GetPositionFromPoint),
// tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
#if __SKIA__
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
#endif

namespace Microsoft.UI.Xaml.Controls;

// The TextPointer surface is part of the cross-platform RichTextBlock API, but the underlying
// position model (PlainTextPosition, the BlockLayout view, the selection manager) is Skia-only.
// On non-Skia targets these return null/no-op, matching WinUI's pre-layout behavior.
partial class RichTextBlock
{
	/// <summary>
	/// Gets a TextPointer that represents the start of content in the RichTextBlock.
	/// </summary>
	public TextPointer? ContentStart => GetContentStart();

	/// <summary>
	/// Gets a TextPointer that represents the end of content in the RichTextBlock.
	/// </summary>
	public TextPointer? ContentEnd => GetContentEnd();

	/// <summary>
	/// Gets a TextPointer that represents the start of the current selection.
	/// </summary>
	public TextPointer? SelectionStart => GetSelectionStart();

	/// <summary>
	/// Gets a TextPointer that represents the end of the current selection.
	/// </summary>
	public TextPointer? SelectionEnd => GetSelectionEnd();

	/// <summary>
	/// Returns a TextPointer that corresponds to a Point in the coordinate space of the RichTextBlock.
	/// </summary>
	public TextPointer? GetPositionFromPoint(Point point) => GetTextPositionFromPoint(point);

	/// <summary>
	/// Selects the content between two positions.
	/// </summary>
	public void Select(TextPointer start, TextPointer end)
	{
		if (start is null || end is null)
		{
			return;
		}

		SelectCore(start, end);
	}

#if __SKIA__
	// CRichTextBlock::GetContentStart
	private TextPointer? GetContentStart()
	{
		var container = Blocks.GetTextContainer();

		EnsureBlockLayout();
		var view = GetTextView();
		if (view is null)
		{
			return null;
		}

		var contentStartPosition = view.GetContentStartPosition();

		// ContentStart always has backward gravity since it's the first content position in the collection.
		var textPosition = new PlainTextPosition(container, contentStartPosition, TextGravity.LineForwardCharacterBackward);
		return TextPointer.CreateInstanceWithInternalPointer(textPosition);
	}

	// CRichTextBlock::GetContentEnd
	private TextPointer? GetContentEnd()
	{
		var container = Blocks.GetTextContainer();

		EnsureBlockLayout();
		var view = GetTextView();
		if (view is null)
		{
			return null;
		}

		// ContentEnd is the position just after the end of content. If there's a break, its gravity is
		// backward (the forward-gravity position lives in the next link); without a break it's forward
		// since it's the end of all content.
		var contentEndPosition = view.GetContentStartPosition() + view.GetContentLength();
		var gravity = _break is null ? TextGravity.LineForwardCharacterForward : TextGravity.LineForwardCharacterBackward;

		var textPosition = new PlainTextPosition(container, contentEndPosition, gravity);
		return TextPointer.CreateInstanceWithInternalPointer(textPosition);
	}

	// CRichTextBlock::GetSelectionStart
	private TextPointer? GetSelectionStart()
	{
		if (IsSelectionEnabled() && _pSelectionManager?.GetTextSelection() is { } selection)
		{
			selection.GetStartTextPosition(out var startTextPosition);
			return TextPointer.CreateInstanceWithInternalPointer(startTextPosition.GetPlainPosition());
		}

		return null;
	}

	// CRichTextBlock::GetSelectionEnd
	private TextPointer? GetSelectionEnd()
	{
		if (IsSelectionEnabled() && _pSelectionManager?.GetTextSelection() is { } selection)
		{
			selection.GetEndTextPosition(out var endTextPosition);
			return TextPointer.CreateInstanceWithInternalPointer(endTextPosition.GetPlainPosition());
		}

		return null;
	}

	// CRichTextBlock::GetTextPositionFromPoint — public hit-test API.
	private TextPointer? GetTextPositionFromPoint(Point point)
	{
		EnsureBlockLayout();
		var view = GetTextView();

		// Use this element's view to query the pixel position. No coordinate transformation is
		// necessary - this API is assumed to be called with element-relative coordinates.
		if (view is not null)
		{
			// Recognise hits after newline = false (matches WinUI's PixelPositionToTextPosition call).
			var position = view.PixelPositionToTextPosition(point, false, out var gravity);

			var container = Blocks.GetTextContainer();
			if (container is not null)
			{
				var textPosition = new PlainTextPosition(container, position, gravity);
				return TextPointer.CreateInstanceWithInternalPointer(textPosition);
			}
		}

		return null;
	}

	// CRichTextBlock::Select
	private void SelectCore(TextPointer start, TextPointer end)
	{
		// Only act on valid positions and when selection is enabled.
		if (start.IsValid() && end.IsValid() && IsSelectionEnabled())
		{
			_pSelectionManager?.Select(start.GetPlainTextPosition(), end.GetPlainTextPosition());
		}
	}

	// CRichTextBlock::IsSelectionEnabled
	private bool IsSelectionEnabled() => IsTextSelectionEnabled && _pSelectionManager is not null;
#else
	private TextPointer? GetContentStart() => null;
	private TextPointer? GetContentEnd() => null;
	private TextPointer? GetSelectionStart() => null;
	private TextPointer? GetSelectionEnd() => null;
	private TextPointer? GetTextPositionFromPoint(Point point) => null;
	private void SelectCore(TextPointer start, TextPointer end) { }
#endif
}
