// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ITextSelection.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;

namespace Microsoft.UI.Xaml.Controls;

/*
Summary:
    A logical text selection.

Remarks:
    A text selection can either represent a caret or a (non-empty) text range. A text selection
    is composed of two 'positions':
        1) Start position
        2) End position

    In the case of a caret, both positions are the same.

    One of the start/end positions is a 'dynamic' end, which can be moved by cursor keys or mouse
    clicking to extend the selection, and the other's a 'fixed' end. The dynamic end is referred to
    as the 'moving' position, while the fixed end is the 'anchor' position.

    The selection exposes APIs to:
        * Get/Set the underlying text.
        * Query text range start/end/length
        * Update the selection position

    Selection start/end text positions have 'gravity'. Text gravity is used to resolve
    certain ambiguities related to cursor positioning.

See also:
    <TextPosition>
    <TextGravity>
    <ITextView> for examples of text-gravity-based decisions.
*/
internal interface IJupiterTextSelection
{
	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'moving' end of the selection.
	//------------------------------------------------------------------------
	bool GetMovingTextPosition(out TextPosition pPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'anchor' (fixed) end of the selection.
	//------------------------------------------------------------------------
	bool GetAnchorTextPosition(out TextPosition pPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'start' of the selection.
	//------------------------------------------------------------------------
	bool GetStartTextPosition(out TextPosition pPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'end' of the selection.
	//------------------------------------------------------------------------
	bool GetEndTextPosition(out TextPosition pPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the moving position.
	//------------------------------------------------------------------------
	TextGravity GetMovingGravity();

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the cursor, set through <Select>.
	//------------------------------------------------------------------------
	TextGravity GetCursorGravity();

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the start position.
	//------------------------------------------------------------------------
	TextGravity GetStartGravity();

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the end position.
	//------------------------------------------------------------------------
	TextGravity GetEndGravity();

	//
	// Text APIs
	//

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text underlying the selection. If the selection is empty,
	//      returns a reference to the static const xstring_ptr::NullString()
	//------------------------------------------------------------------------
	bool GetText(out string pstrText);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the XAML representing the selection. If the selection is empty,
	//      or if XAML format is not supported, returns a reference to the static
	//      const xstring_ptr::NullString()
	//------------------------------------------------------------------------
	bool GetXaml(out string pstrXaml);

	//
	// Selection length
	//

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the selection is empty (i.e. represents a caret).
	//------------------------------------------------------------------------
	bool IsEmpty();

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the length of the selection (== 0 for a caret).
	//------------------------------------------------------------------------
	bool GetLength(out uint pLength);

	//
	// Point-oriented selection APIs
	//

	//------------------------------------------------------------------------
	//  Summary:
	//      Resets the selection to represent a caret at the text position indicated
	//      by the given point.
	//------------------------------------------------------------------------
	bool SetCaretPositionFromPoint(Point point);

	//------------------------------------------------------------------------
	//  Summary:
	//      Extends the selection by moving the 'moving position' to the text
	//      position indicated by the given point.
	//------------------------------------------------------------------------
	bool ExtendSelectionByMouse(Point point);

	//------------------------------------------------------------------------
	//  Summary:
	//      Selects the word that falls under the given point, if any.
	//------------------------------------------------------------------------
	bool SelectWord(Point point);

	//
	// Position-oriented selection APIs
	//

	//------------------------------------------------------------------------
	//  Summary:
	//      Resets the selection to cover the given range. XUINT32-based overload.
	//
	//  Remarks:
	//      Anchor position and moving position can be the same.
	//
	//      In an ideal world, we wouldn't have to expose an XUINT32-based overload.
	//      However, most of our code currently uses XUINT32s to represent text positions,
	//      so we can't rely on having an <TextPosition>-only interface. Moving forward,
	//      it's likely that we'll unify our codebase to use <TextPosition>s everywhere.
	//------------------------------------------------------------------------
	bool Select(
		uint iAnchorTextPosition,
		uint iMovingTextPosition,
		TextGravity eCursorGravity);

	//------------------------------------------------------------------------
	//  Summary:
	//      Resets the selection to cover the given range. TextPosition-based overload.
	//
	//  Remarks:
	//      Anchor position and moving position can be the same.
	//------------------------------------------------------------------------
	bool Select(
		in TextPosition anchorTextPosition,
		in TextPosition movingTextPosition,
		TextGravity eCursorGravity);

	//------------------------------------------------------------------------
	//  Summary:
	//      Extends the selection by moving the 'moving position' to the text
	//      position indicated by the given point.
	//------------------------------------------------------------------------
	bool ExtendSelectionByMouse(in TextPosition cursorPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Extends the selection by moving the 'moving position' to the text
	//      position indicated by the given point.
	//------------------------------------------------------------------------
	bool SetCaretPositionFromTextPosition(in TextPosition position);

	//------------------------------------------------------------------------
	//  Summary:
	//      Extends the selection by moving the 'moving position' to the text
	//      position indicated by the given point.
	//------------------------------------------------------------------------
	bool SelectWord(in TextPosition position);

	void ResetSelection();
}
