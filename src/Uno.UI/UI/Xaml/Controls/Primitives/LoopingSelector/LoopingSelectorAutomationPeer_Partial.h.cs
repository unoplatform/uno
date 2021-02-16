// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once



namespace Windows.UI.Xaml.Automation.Peers
{
    typedef std.map<DependencyObject, ILoopingSelectorItemDataAutomationPeer> PeerMap;

    class LoopingSelectorAutomationPeer :
        public LoopingSelectorAutomationPeerGenerated
    {

    // public
         // Internal automation methods
         void GetDataAutomationPeerForItem( DependencyObject pItem, out  xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer ppPeer);
         void GetContainerAutomationPeerForItem( DependencyObject pItem, out xaml_automation_peers.ILoopingSelectorItemAutomationPeer ppPeer);
         void TryScrollItemIntoView( DependencyObject pItem);
         void ClearPeerMap();
         void RealizeItemAtIndex( INT index);

    // protected

        // IAutomationPeerOverrides
         void GetPatternCoreImpl(
             xaml.Automation.Peers.PatternInterface patternInterface,
            out  DependencyObject returnValue) override;
         void GetAutomationControlTypeCoreImpl(
            out xaml.Automation.Peers.AutomationControlType returnValue) override;
         void GetChildrenCoreImpl(
            out  wfc.IList<xaml.Automation.Peers.AutomationPeer> returnValue) override;
         void GetClassNameCoreImpl(
            out string returnValue) override;

    // private
        ~LoopingSelectorAutomationPeer();

         void InitializeImpl(
             xaml_primitives.ILoopingSelector pOwner) override;

         void GetOwnerAsInternalPtrNoRef(out  xaml_primitives.LoopingSelector ppOwnerNoRef);

         void FindStartIndex(
             xaml.Automation.Provider.IIRawElementProviderSimple pStartAfter,
             wfc.IList<DependencyObject> pItems,
            out INT pIndex);

        // IScrollProvider Impl
         void get_HorizontallyScrollableImpl(out bool pValue) override;
         void get_HorizontalScrollPercentImpl(out DOUBLE pValue) override;
         void get_HorizontalViewSizeImpl(out DOUBLE pValue) override;
         void get_VerticallyScrollableImpl(out bool pValue) override;
         void get_VerticalScrollPercentImpl(out DOUBLE pValue) override;
         void get_VerticalViewSizeImpl(out DOUBLE pValue) override;

    // public
         void ScrollImpl (
             xaml_automation.ScrollAmount horizontalAmount,
             xaml_automation.ScrollAmount verticalAmount);

         void SetScrollPercentImpl (
             DOUBLE horizontalPercent,
             DOUBLE verticalPercent);

          // IItemsContainerProvider Impl
         void FindItemByPropertyImpl(
             xaml_automation.Provider.IIRawElementProviderSimple startAfter,
             xaml_automation.IAutomationProperty automationProperty,
             DependencyObject value,
            out  xaml_automation.Provider.IIRawElementProviderSimple returnValue);

         // ISelectionProvider
         void get_CanSelectMultipleImpl(out bool value) override;
         void get_IsSelectionRequiredImpl(out bool value) override;

         void GetSelectionImpl(
            out Uint pReturnValueSize,
            out result_buffer_maybenull_(pReturnValueSize) xaml_automation.Provider.IIRawElementProviderSimple *pppReturnValue);

    // private
        // PeerMap is used to keep a 1:1 mapping between items in the list and
        // their respective data automation peers. This is done to ensure that
        // no duplicate data peers are created for items. This class does not
        // keep a on the data peers, and the peers are responsible for removing
        // themselves from this map when deructed.
        PeerMap _peerMap;
    };

} } } } } XAML_ABI_NAMESPACE_END
