// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextPointer_Partial.cpp, TextPointerWrapper.cpp/.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// Represents a position within the content of a RichTextBlock.
/// </summary>
/// <remarks>
/// WinUI splits this across the public projection (TextPointer_Partial.cpp) and the core
/// CTextPointerWrapper, which wraps a CPlainTextPosition and a weak reference to the text
/// container. Uno folds both into this single hand-written class: it wraps a PlainTextPosition
/// (the Uno equivalent of CPlainTextPosition) and validates the position the same way before
/// answering. The Skia-only logic lives in TextPointer.skia.cs; non-Skia targets return
/// default values, mirroring the layout-less behavior WinUI exhibits before a text view exists.
/// </remarks>
public partial class TextPointer
{
	// A TextPointer is only ever created internally with a valid PlainTextPosition (see the
	// skia partial's CreateInstanceWithInternalPointer). The public ctor is hidden.
	internal TextPointer()
	{
	}

	/// <summary>
	/// Gets the logical direction associated with the position.
	/// </summary>
	public LogicalDirection LogicalDirection => GetLogicalDirection();

	/// <summary>
	/// Gets the offset of this position within the content.
	/// </summary>
	public int Offset => GetOffset();

	/// <summary>
	/// Gets the logical parent that contains the position.
	/// </summary>
	public DependencyObject? Parent => GetParent();

	/// <summary>
	/// Gets the visual parent of the position.
	/// </summary>
	public FrameworkElement? VisualParent => GetVisualParent();

	/// <summary>
	/// Returns a bounding box for the content at the position.
	/// </summary>
	public Rect GetCharacterRect(LogicalDirection direction) => GetCharacterRectCore(direction);

	/// <summary>
	/// Returns a TextPointer at the specified offset from this position.
	/// </summary>
	public TextPointer? GetPositionAtOffset(int offset, LogicalDirection direction)
		=> GetPositionAtOffsetCore(offset, direction);

#if !__SKIA__
	private LogicalDirection GetLogicalDirection() => LogicalDirection.Forward;

	private int GetOffset() => 0;

	private DependencyObject? GetParent() => null;

	private FrameworkElement? GetVisualParent() => null;

	private Rect GetCharacterRectCore(LogicalDirection direction) => default;

	private TextPointer? GetPositionAtOffsetCore(int offset, LogicalDirection direction) => null;
#endif
}
