// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class LoopingSelectorItem : ContentControl
	{

		// public
		internal enum State
		{
			Normal,
			Expanded,
			Selected,
			PointerOver,
			Pressed,
		};

		//LoopingSelectorItem();

		// UIElementOverrides
		//void OnCreateAutomationPeerImpl(
		//	out AutomationPeer returnValue);

		// Internal automation methods
		//void AutomationSelect();
		//void AutomationGetSelectionContainerUIAPeer(out AutomationPeer ppPeer);
		//void AutomationGetIsSelected(out bool value);
		//void AutomationUpdatePeerIfExists(int itemIndex);

		// Internal methods
		//void SetState(State newState, bool useTransitions);

		internal void GetVisualIndex(out int idx)
		{
			idx = _visualIndex;
			return;
		}

		internal void SetVisualIndex(int idx)
		{
			_visualIndex = idx;
			return;
		}

		internal void SetParent(LoopingSelector pValue)
		{
			_pParentNoRef = pValue;
			return;
		}

		internal void GetParentNoRef(out LoopingSelector ppValue)
		{
			ppValue = _pParentNoRef;
			return;
		}

		// protected

		//void OnPointerEnteredImpl(PointerRoutedEventArgs e);
		//void OnPointerPressedImpl(PointerRoutedEventArgs e);
		//void OnPointerExitedImpl(PointerRoutedEventArgs e);
		//void OnPointerCaptureLostImpl(PointerRoutedEventArgs e);

		// private

		//void InitializeImpl();

		//void GoToState(State newState, bool useTransitions);

		private State _state;

		// The visual index of the data item this item is displaying.
		// Note: Due to the looping behavior, this is not equal to the index of the item in the collection.
		// e.g. consider the case of Minute 59 looping around to 0. The Item after 59 does not have an index of 0. It has an index of 60.
		private int _visualIndex;

		// The parent is used by the AutomationPeer for ScrollIntoView
		// and Selection. We don't keep a strong to prevent cycles.
		private LoopingSelector _pParentNoRef;

		// There's no way to know if an AP has been created except to
		// keep track with an internal boolean.
		private bool _hasPeerBeenCreated;
	}
}
