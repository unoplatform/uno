// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextSelectionManager.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class TextSelectionManager
{
	// ---- Gripper mapping ----------------------------------------------------
	//
	// WinUI's manager owns two CTextSelectionGripper elements (in popups) and drives all of the
	// touch-selection geometry itself (ShowGrippers/HideGrippers/UpdateGripper* + the snapping
	// calculator + crossover swapping in NotifyGripperPositionChanged). On Uno that whole apparatus
	// already exists as TextSelectionGripperPresenter, which the owning control creates and drives
	// from its own ITextSelectionGripperHost. So the manager's gripper surface is reduced to
	// forwarding the show/hide/update/ensure/release notifications to the owner, which relays them to
	// the presenter. The gripper-drag selection edits (ChangeSelection/ExtendSelectionRange/snapping)
	// are performed by the presenter via ITextSelectionGripperHost.SetGripperSelection, so they are
	// not re-ported here.

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::EnsureGrippers
	//------------------------------------------------------------------------
	private void EnsureGrippers() => m_owner.EnsureGrippers();

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::ReleaseGrippers
	//------------------------------------------------------------------------
	public void ReleaseGrippers()
	{
		HideGrippers(false /* fAnimate */);
		m_owner?.ReleaseGrippers();
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::ShowGrippers
	//------------------------------------------------------------------------
	public void ShowGrippers(bool fAnimate)
	{
		if (!IsSelectionVisible() || m_pTextSelection!.IsEmpty())
		{
			return;
		}

		m_owner.ShowGrippers(fAnimate);
		m_endGripperLastMoved = false;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::HideGrippers
	//------------------------------------------------------------------------
	public void HideGrippers(bool fAnimate) => m_owner.HideGrippers(fAnimate);

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::UpdateGripperPositions
	//
	//  Synopsis: Refreshes the gripper screen coordinates.
	//------------------------------------------------------------------------
	public void UpdateGripperPositions() => m_owner.UpdateGripperPositions();

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::SnapGrippersToSelection
	//------------------------------------------------------------------------
	public void SnapGrippersToSelection() => ShowGrippers(true /* fAnimate */);

	// Mirrors the WinUI OnTapped guard that checks whether either gripper currently has pointer capture.
	private bool AreGrippersBeingManipulated() => m_owner.AreGrippersBeingManipulated;

	// ---- Caret browsing (deferred) ------------------------------------------
	//
	// WinUI's manager also hosts a blinking caret (CCaretBrowsingCaret + storyboard + DispatcherTimer)
	// used only when caret-browsing mode is enabled. This is out of scope for the 66-test Stage-7 set
	// and is stubbed here. ITextSelectionNotify.OnSelectionChanged calls UpdateCaretElement(), which is
	// a no-op until caret browsing is wired up.

	// TODO Uno (Stage 7 P2): caret browsing — port ShowCaretElement/EnsureCaret/UpdateCaretElement/
	// RemoveCaret + the blink storyboard/timer (CaretBrowsingCaret) and CaretOnKeyDown navigation.
	private void RemoveCaret()
	{
		// TODO Uno (Stage 7 P2): caret browsing
	}

	private void UpdateCaretElement()
	{
		// TODO Uno (Stage 7 P2): caret browsing
	}

	public void RemoveCaretFromTextObject(DependencyObject textOwnerObject)
	{
		// TODO Uno (Stage 7 P2): caret browsing
	}
}
