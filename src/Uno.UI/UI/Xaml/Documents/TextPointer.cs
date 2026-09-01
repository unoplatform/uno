// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextPointer_Partial.cpp, TextPointerWrapper.cpp/.h, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// Represents a position within the content of a RichTextBlock.
/// </summary>
/// <remarks>
/// WinUI splits this across the public projection (TextPointer_Partial.cpp) and the core
/// CTextPointerWrapper, which wraps a CPlainTextPosition and a weak reference to the text
/// container. Uno folds both into this single hand-written class: it wraps a PlainTextPosition
/// (the Uno equivalent of CPlainTextPosition) and validates the position the same way before
/// answering.
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

	// The wrapped position. CTextPointerWrapper holds the CPlainTextPosition plus a weak ref to
	// its container; here PlainTextPosition already keeps the container and is validated by
	// CheckPositionAndContainerValid below, so a single field is enough. Managed GC removes the
	// need for the explicit weak reference WinUI used to detect an outlived container.
	private PlainTextPosition _plainTextPosition;

	// CTextPointerWrapper::Create — a TextPointer is always created with a valid PlainTextPosition.
	internal static TextPointer? CreateInstanceWithInternalPointer(PlainTextPosition plainTextPosition)
	{
		if (!plainTextPosition.IsValid())
		{
			return null;
		}

		return new TextPointer
		{
			_plainTextPosition = plainTextPosition,
		};
	}

	internal PlainTextPosition GetPlainTextPosition() => _plainTextPosition;

	internal bool IsValid() => CheckPositionAndContainerValid();

	// CTextPointerWrapper::GetLogicalDirection
	private LogicalDirection GetLogicalDirection()
	{
		var direction = LogicalDirection.Forward;
		var gravity = TextGravity.LineForwardCharacterBackward;

		// Plain text position is only queried if it's valid and we haven't outlived the TextContainer.
		if (CheckPositionAndContainerValid())
		{
			_plainTextPosition.GetGravity(out gravity);
			direction = TextBoxHelpers.CharacterGravityBackward(gravity)
				? LogicalDirection.Backward
				: LogicalDirection.Forward;
		}

		return direction;
	}

	// CTextPointerWrapper::GetOffset
	private int GetOffset()
	{
		uint offset = 0;

		// Plain text position is only queried if it's valid and we haven't outlived the TextContainer.
		if (CheckPositionAndContainerValid())
		{
			_plainTextPosition.GetOffset(out offset);
		}

		return (int)offset;
	}

	// CTextPointerWrapper::GetParent
	private DependencyObject? GetParent()
	{
		DependencyObject? parent = null;

		// Plain text position is only queried if it's valid and we haven't outlived the TextContainer.
		if (CheckPositionAndContainerValid())
		{
			_plainTextPosition.GetLogicalParent(out parent);
		}

		return parent;
	}

	// CTextPointerWrapper::GetVisualParent
	private FrameworkElement? GetVisualParent()
	{
		FrameworkElement? parent = null;

		// Plain text position is only queried if it's valid and we haven't outlived the TextContainer.
		if (CheckPositionAndContainerValid())
		{
			_plainTextPosition.GetVisualParent(out parent);
		}

		return parent;
	}

	// CTextPointerWrapper::GetCharacterRect
	private Rect GetCharacterRectCore(LogicalDirection direction)
	{
		Rect rect = default;
		var gravity = TextGravity.LineForwardCharacterForward;

		// Plain text position is only queried if it's valid and we haven't outlived the TextContainer.
		if (CheckPositionAndContainerValid())
		{
			if (direction == LogicalDirection.Backward)
			{
				gravity = TextGravity.LineForwardCharacterBackward;
			}

			_plainTextPosition.GetCharacterRect(gravity, out rect);
		}

		return rect;
	}

	// CTextPointerWrapper::GetPositionAtOffset
	private TextPointer? GetPositionAtOffsetCore(int offset, LogicalDirection direction)
	{
		if (!CheckPositionAndContainerValid())
		{
			return null;
		}

		var gravity = direction == LogicalDirection.Forward
			? TextGravity.LineForwardCharacterForward
			: TextGravity.LineForwardCharacterBackward;

		_plainTextPosition.GetPositionAtOffset(offset, gravity, out var foundPosition, out var plainTextPosition);
		if (foundPosition)
		{
			return CreateInstanceWithInternalPointer(plainTextPosition);
		}

		return null;
	}

	// CTextPointerWrapper::CheckPositionAndContainerValid — PlainTextPosition already pins the
	// container, so its IsValid is the whole check (GC stands in for WinUI's weak-ref liveness test).
	private bool CheckPositionAndContainerValid() => _plainTextPosition.IsValid();
}
