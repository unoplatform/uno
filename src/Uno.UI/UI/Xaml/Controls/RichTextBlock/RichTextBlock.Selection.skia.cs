// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

// Stage 9b (Uno glue): wires the faithfully-ported TextSelectionManager into RichTextBlock.
// WinUI reached these behaviors through CRichTextBlock virtual methods, the core services
// (CTextCore, system brushes) and FxCallbacks. Here RichTextBlock satisfies the manager's
// ITextSelectionManagerOwner callback seam and the text-position layer's ITextViewHost.
//
// This file only provides the owner surface; the manager is created and the events are routed
// by the platform-specific RichTextBlock partial (the lead's swap), which assigns _pTextView.
partial class RichTextBlock : ITextSelectionManagerOwner, ITextViewHost
{
	// The view the manager hit-tests / measures against. Assigned by the platform partial when
	// the BlockLayout page node is (re)built; null until the first measure/arrange completes.
	internal RichTextBlockView? _pTextView;

	#region ITextViewHost

	ITextView? ITextViewHost.GetTextView() => _pTextView;

	#endregion

	#region ITextSelectionManagerOwner

	ITextView? ITextSelectionManagerOwner.GetTextView() => _pTextView;

	void ITextSelectionManagerOwner.OnSelectionChanged(
		uint previousSelectionStartOffset,
		uint previousSelectionEndOffset,
		uint newSelectionStartOffset,
		uint newSelectionEndOffset)
	{
		// Mirror the managed selection model so SelectedText / SelectionChanged stay in sync, then
		// re-render the highlight. OnSelectionChanged() (Properties partial) updates SelectedText and
		// raises SelectionChanged; setting Selection drives the existing highlight path.
		Selection = new Range((int)newSelectionStartOffset, (int)newSelectionEndOffset);
		InvalidateInlineAndRequireRepaint();
	}

	void ITextSelectionManagerOwner.OnSelectionVisibilityChanged(
		uint selectionStartOffset,
		uint selectionEndOffset)
		=> InvalidateInlineAndRequireRepaint();

	bool ITextSelectionManagerOwner.IsFocused => IsFocused;

	bool ITextSelectionManagerOwner.Focus(FocusState focusState) => Focus(focusState);

	bool ITextSelectionManagerOwner.CanSelectText => IsTextSelectionEnabled;

	void ITextSelectionManagerOwner.InvalidateRender() => InvalidateInlineAndRequireRepaint();

	// Single-owner equivalent of CTextCore's "last selected text element" tracking. RichTextBlock
	// owns a single container, so there is no cross-control bookkeeping to perform.
	// TODO Uno (Stage 9b): wire to a core-level "last selected text element" if multi-control
	// selection clearing is required for parity.
	void ITextSelectionManagerOwner.SetLastSelectedTextElement() { }
	void ITextSelectionManagerOwner.ClearLastSelectedTextElement() { }

	// TODO Uno (Stage 9b): resolve from the real OS high-contrast setting. Defaulting to false keeps
	// the manager on the user/property selection brushes (the normal path).
	bool ITextSelectionManagerOwner.IsHighContrast => false;

	// TODO Uno (Stage 9b): return true system colors (SystemColors highlight / highlight-text / window).
	// DefaultBrushes mirrors the WinUI accent selection colors and is the existing render-path default.
	SolidColorBrush ITextSelectionManagerOwner.GetSystemTextSelectionBackgroundBrush()
		=> SelectionHighlightColor ?? DefaultBrushes.SelectionHighlightColor;

	SolidColorBrush ITextSelectionManagerOwner.GetSystemTextSelectionForegroundBrush()
		=> DefaultBrushes.SelectedTextForegroundColor;

	SolidColorBrush ITextSelectionManagerOwner.GetSystemColorWindowBrush()
		=> DefaultBrushes.SelectedTextForegroundColor;

	void ITextSelectionManagerOwner.RaiseContextMenuOpening(Point worldPoint, bool isSelectionEmpty)
	{
		// The manager supplies root/world-space coordinates (GetPosition(null)); raise the event
		// directly without re-transforming (FireContextMenuOpeningEventSynchronously expects a local
		// point and transforms it, so it is not reused here).
		var args = new ContextMenuEventArgs(worldPoint.X, worldPoint.Y);
		ContextMenuOpening?.Invoke(this, args);
	}

	void ITextSelectionManagerOwner.QueueUpdateSelectionFlyoutVisibility()
		=> QueueUpdateSelectionFlyoutVisibility(_lastInputDeviceType, _lastPointerPosition);

	void ITextSelectionManagerOwner.CloseSelectionFlyoutIfOpen() => SelectionFlyout?.Hide();

	bool ITextSelectionManagerOwner.ShouldForceFocusedVisualState() => ShouldForceFocusedVisualState();

	bool ITextSelectionManagerOwner.TryGetLastPointerPosition(out Point worldPosition)
	{
		worldPosition = _lastPointerPosition;
		return true;
	}

	// ---- Touch-selection grippers -------------------------------------------------------------
	// RichTextBlock has no TextSelectionGripperPresenter host yet. WinUI's manager forwards these to
	// the presenter via TextSelectionManager.Gripper.skia.cs; on Uno the presenter is driven by an
	// ITextSelectionGripperHost the control would implement. Until that host is added these are safe
	// no-ops so the manager's touch path does not crash.
	// TODO Uno (Stage 9b/grippers): implement ITextSelectionGripperHost and drive
	// TextSelectionGripperPresenter (Show/Hide/Update/Ensure/Release/UpdatePositions).
	void ITextSelectionManagerOwner.ShowGrippers(bool animate) { }
	void ITextSelectionManagerOwner.HideGrippers(bool animate) { }
	void ITextSelectionManagerOwner.EnsureGrippers() { }
	void ITextSelectionManagerOwner.ReleaseGrippers() { }
	void ITextSelectionManagerOwner.UpdateGripperPositions() { }
	bool ITextSelectionManagerOwner.AreGrippersBeingManipulated => false;

	#endregion
}
