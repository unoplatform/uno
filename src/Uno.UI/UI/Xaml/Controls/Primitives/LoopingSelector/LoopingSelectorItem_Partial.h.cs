// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

namespace Windows.UI.Xaml.Controls.Primitives
{
    class LoopingSelectorItem :
        public LoopingSelectorItemGenerated
    {

    // public
        enum class State
        {
            Normal,
            Expanded,
            Selected,
            PointerOver,
            Pressed,
        };

        LoopingSelectorItem();

        // UIElementOverrides
         void OnCreateAutomationPeerImpl(
            out  xaml.Automation.Peers.IAutomationPeer returnValue) override;

        // Internal automation methods
         void AutomationSelect();
         void AutomationGetSelectionContainerUIAPeer(out xaml.Automation.Peers.IAutomationPeer ppPeer);
         void AutomationGetIsSelected(out bool value);
         void AutomationUpdatePeerIfExists( int itemIndex);

        // Internal methods
         void SetState( State newState,  bool useTransitions);

         void GetVisualIndex(out INT idx)
        {
            idx = _visualIndex;
            return;
        }

         void SetVisualIndex( INT idx)
        {
            _visualIndex = idx;
            return;
        }

         void SetParent( LoopingSelector pValue)
        {
            _pParentNoRef = pValue;
            return;
        }

         void GetParentNoRef(out  LoopingSelector ppValue)
        {
            ppValue = _pParentNoRef;
            return;
        }

    // protected

         void OnPointerEnteredImpl( xaml_input.IPointerRoutedEventArgs e) override;
         void OnPointerPressedImpl( xaml_input.IPointerRoutedEventArgs e) override;
         void OnPointerExitedImpl( xaml_input.IPointerRoutedEventArgs e) override;
         void OnPointerCaptureLostImpl( xaml_input.IPointerRoutedEventArgs e) override;

    // private
        ~LoopingSelectorItem() {}

         void InitializeImpl() override;

         void GoToState( State newState,  bool useTransitions);

        State _state;

        // The visual index of the data item this item is displaying.
        // Note: Due to the looping behavior, this is not equal to the index of the item in the collection.
        // e.g. consider the case of Minute 59 looping around to 0. The Item after 59 does not have an index of 0. It has an index of 60.
        INT _visualIndex;

        // The parent is used by the AutomationPeer for ScrollIntoView
        // and Selection. We don't keep a strong to prevent cycles.
        LoopingSelector _pParentNoRef;

        // There's no way to know if an AP has been created except to
        // keep track with an internal boolean.
        bool _hasPeerBeenCreated;
    };

} } } } } XAML_ABI_NAMESPACE_END
