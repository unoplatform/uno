// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

namespace Windows.UI.Xaml.Automation.Peers
{
    class LoopingSelectorItemAutomationPeer :
        public LoopingSelectorItemAutomationPeerGenerated
    {

    // public

         void UpdateEventSource();
         void SetEventSource( xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer pLSIDAP);
         void UpdateItemIndex( int itemIndex);

        // IAutomationPeerOverrides
         void GetPatternCoreImpl(
             xaml.Automation.Peers.PatternInterface patternInterface,
            out  DependencyObject returnValue) override;
         void GetAutomationControlTypeCoreImpl(
            out xaml.Automation.Peers.AutomationControlType returnValue) override;
         void GetClassNameCoreImpl(
            out string returnValue) override;

         void IsKeyboardFocusableCoreImpl(out bool returnValue) override;
         void HasKeyboardFocusCoreImpl(out bool returnValue) override;

    // private

        ~LoopingSelectorItemAutomationPeer() {}

         void InitializeImpl(
             xaml_primitives.ILoopingSelectorItem pOwner) override;

         void GetOwnerAsInternalPtrNoRef(out  xaml_primitives.LoopingSelectorItem ppOwnerNoRef);
         void GetDataAutomationPeer(out  xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer ppLSIDAP);

    // public
         // IScrollItemProvider Impl
         void ScrollIntoViewImpl();

        // ISelectionItemProvider Impl
         void get_IsSelectedImpl(out bool value) override;

         void get_SelectionContainerImpl(
            out  xaml_automation.Provider.IIRawElementProviderSimple value) override;

         void AddToSelectionImpl();
         void RemoveFromSelectionImpl();
         void SelectImpl();

    // private
         void GetParentAutomationPeer(out  xaml_automation_peers.IAutomationPeer parentAutomationPeer);
    };

} } } } } XAML_ABI_NAMESPACE_END

