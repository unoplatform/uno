// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.



namespace Windows.UI.Xaml.Automation.Peers
{

 void
LoopingSelectorAutomationPeer.InitializeImpl(
     xaml_primitives.ILoopingSelector pOwner)
{
    
    wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeerFactory> spInnerFactory;
    wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeer> spInnerInstance;
    wrl.ComPtr<xaml.FrameworkElement> spLoopingSelectorAsFE;
    wrl.ComPtr<xaml_primitives.ILoopingSelector> spOwner(pOwner);
    wrl.ComPtr<DependencyObject> spInnerInspectable;

    ARG_NOTnull(pOwner, "pOwner");

    LoopingSelectorAutomationPeerGenerated.InitializeImpl(pOwner);
    (wf.GetActivationFactory(
          wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
          &spInnerFactory));

    (((DependencyObject)(pOwner)).QueryInterface<xaml.FrameworkElement>(
        &spLoopingSelectorAsFE));

    (spInnerFactory.CreateInstanceWithOwner(
            spLoopingSelectorAsFE,
            (xaml_automation_peers.ILoopingSelectorAutomationPeer)(this),
            &spInnerInspectable,
            &spInnerInstance));

    (SetComposableBasePointers(
            spInnerInspectable,
            spInnerFactory));

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.GetOwnerAsInternalPtrNoRef(out xaml_primitives.LoopingSelector ppOwnerNoRef)
{
    
    wrl.ComPtr<UIElement> spOwnerAsUIElement;
    wrl.ComPtr<xaml_primitives.ILoopingSelector> spOwner;
    wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeer> spThisAsFEAP;

    ppOwnerNoRef = null;

    (QueryInterface(
        __uuidof(xaml.Automation.Peers.FrameworkElementAutomationPeer),
        &spThisAsFEAP));
spOwnerAsUIElement =     spThisAsFEAP.Owner;
    if (spOwnerAsUIElement)
    {
        spOwnerAsUIElement.As(spOwner);
        // No is passed back to the caller.
        ppOwnerNoRef = (xaml_primitives.LoopingSelector)(spOwner);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.ClearPeerMap()
{
    for(PeerMap.iterator iter = _peerMap.begin(); iter != _peerMap.end(); iter++)
    {
        iter.second.Release();
        iter.second = null;
    }

    _peerMap.clear();

    return S_OK;
}

void LoopingSelectorAutomationPeer.RealizeItemAtIndex(INT index)
{
    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);

    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationRealizeItemForAP(index);
    }

    return S_OK;
}

#region IAutomationPeerOverrides

 void
LoopingSelectorAutomationPeer.GetPatternCoreImpl(
     xaml.Automation.Peers.PatternInterface patternInterface,
    out  DependencyObject returnValue)
{
    

    if (patternInterface == xaml.Automation.Peers.PatternInterface_Scroll ||
        patternInterface == xaml.Automation.Peers.PatternInterface_Selection ||
        patternInterface == xaml.Automation.Peers.PatternInterface_ItemContainer)
    {
        returnValue = (ILoopingSelectorAutomationPeer)(this);
        AddRef();
    }
    else
    {
        LoopingSelectorAutomationPeerGenerated.GetPatternCoreImpl(patternInterface, returnValue);
    }

// Cleanup
    // return hr;
}

 void
LoopingSelectorAutomationPeer.GetAutomationControlTypeCoreImpl(
    out xaml.Automation.Peers.AutomationControlType returnValue)
{
    
    if(returnValue == null) throw new ArgumentNullException();
    returnValue = xaml.Automation.Peers.AutomationControlType_List;

// Cleanup
    // return hr;
}

 void
LoopingSelectorAutomationPeer.GetChildrenCoreImpl(
    out  wfc.IList<xaml.Automation.Peers.AutomationPeer> returnValue)
{
    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    wrl.ComPtr<wfci_.Vector<xaml.Automation.Peers.AutomationPeer>> spReturnValue;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    wfci_.Vector<xaml.Automation.Peers.AutomationPeer>.Make(spReturnValue);

    if (pOwnerNoRef)
    {
        wrl.ComPtr<wfc.IList<DependencyObject>> spItems;
        UINT count = 0;

spItems =         pOwnerNoRef.Items;
count =         spItems.Size;

        for (UINT itemIdx = 0; itemIdx < count; itemIdx++)
        {
            wrl.ComPtr<DependencyObject> spItem;
            wrl.ComPtr<ILoopingSelectorItemDataAutomationPeer> spLSIDAP;
            wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spLSIDAPAsAP;

            spItems.GetAt(itemIdx, &spItem);
            GetDataAutomationPeerForItem(spItem, &spLSIDAP);
            if (spLSIDAP)
            {
                ((LoopingSelectorItemDataAutomationPeer)(spLSIDAP)).SetItemIndex(itemIdx);

                // Update\set the EventsSource here for corresponding container peer for the Item, this ensures
                // its always the data peer that the lower layer is working with. This is especially required
                // anytime bottom up approach comes into play like hit-testing (after finding the right UI target)
                // that code moves bottom up to find relevant AutomationPeer, another case is UIA events.
                wrl.ComPtr<ILoopingSelectorItemAutomationPeer> spContainerPeer;
                GetContainerAutomationPeerForItem(spItem, &spContainerPeer);
                if (spContainerPeer && spLSIDAP)
                {
                    ((LoopingSelectorItemAutomationPeer)(spContainerPeer)).SetEventSource(spLSIDAP);
                }
            }

            spLSIDAP.As(spLSIDAPAsAP);
            spReturnValue.Append(spLSIDAPAsAP);
        }
    }

    spReturnValue.CopyTo(returnValue);

    return S_OK;
}

 void
LoopingSelectorAutomationPeer.GetClassNameCoreImpl(
    out string returnValue)
{
    
    if(returnValue == null) throw new ArgumentNullException();
    wrl_wrappers.Hstring("LoopingSelector").CopyTo(returnValue);
// Cleanup
    // return hr;
}

#endregion 

#region ISelectionProvider
 void
LoopingSelectorAutomationPeer.get_CanSelectMultipleImpl(
    out bool value)
{
    

    value = false;

    // return hr;
}

 void
LoopingSelectorAutomationPeer.get_IsSelectionRequiredImpl(
    out bool value)
{
    

    value = true;

    // return hr;
}

 void LoopingSelectorAutomationPeer.GetSelectionImpl(
    out Uint pReturnValueSize,
    out result_buffer_maybenull_(pReturnValueSize) xaml.Automation.Provider.IIRawElementProviderSimple *pppReturnValue)
{
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;

    pReturnValueSize = 0;
    pppReturnValue = null;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationTryGetSelectionUIAPeer(spAutomationPeer);
        if (spAutomationPeer)
        {
            wrl.ComPtr<xaml.Automation.Peers.IAutomationPeerProtected> spAutomationPeerAsProtected;
            wrl.ComPtr<xaml.Automation.Provider.IIRawElementProviderSimple> spProvider;

            spAutomationPeer.As(spAutomationPeerAsProtected);
            spAutomationPeerAsProtected.ProviderFromPeer(spAutomationPeer, &spProvider);
            pppReturnValue = (xaml.Automation.Provider.IIRawElementProviderSimple)(CoTaskMemAlloc(sizeof(xaml.Automation.Provider.IIRawElementProviderSimple)));
            if (pppReturnValue)
            {
                (pppReturnValue) = spProvider.Detach();
                pReturnValueSize = 1;
            }
        }
    }

// Cleanup
    // return hr;
}

#endregion 

#region IItemsContainerProvider

 void
LoopingSelectorAutomationPeer.FindItemByPropertyImpl(
     xaml.Automation.Provider.IIRawElementProviderSimple startAfter,
     xaml_automation.IAutomationProperty automationProperty,
     DependencyObject value,
    out  xaml_automation.Provider.IIRawElementProviderSimple returnValue)
{
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    wrl.ComPtr<wfc.IList<DependencyObject>> spItemsCollection;

    returnValue = null;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
spItemsCollection =         pOwnerNoRef.Items;
    }

    if (spItemsCollection)
    {
        INT startIdx = 0;
        UINT totalItems = 0;
        string nameProperty;
        wrl.ComPtr<xaml.Automation.Peers.IAutomationPeerProtected> spThisAsProtected;

        // For the Name and IsSelected property search cases we cache these
        // values outside of the loop. Otherwise these variables remain unused.
        bool isSelected = false;
        string strNameToFind;
        wrl.ComPtr<DependencyObject> spSelectedItem;

        Private.AutomationHelper.AutomationPropertyEnum propertyAsEnum =
            Private.AutomationHelper.AutomationPropertyEnum.EmptyProperty;

totalItems =         spItemsCollection.Size;
        (FindStartIndex(
            startAfter,
            spItemsCollection,
            &startIdx));
        (Private.AutomationHelper.ConvertPropertyToEnum(
            automationProperty,
            &propertyAsEnum));

        (QueryInterface(
            __uuidof(xaml.Automation.Peers.IAutomationPeerProtected),
            &spThisAsProtected));

        if (propertyAsEnum == Private.AutomationHelper.AutomationPropertyEnum.NameProperty && value)
        {
            Private.ValueBoxer.UnboxString(value, strNameToFind);
        }
        else if (propertyAsEnum == Private.AutomationHelper.AutomationPropertyEnum.IsSelectedProperty && value)
        {
            wrl.ComPtr<wf.IPropertyValue> spValueAsPropertyValue;
            value.QueryInterface<wf.IPropertyValue>(spValueAsPropertyValue);
            spValueAsPropertyValue.GetBoolean(isSelected);
spSelectedItem =             pOwnerNoRef.SelectedItem;
        }

        for (INT itemIdx = startIdx + 1; itemIdx < (INT)(totalItems); itemIdx++)
        {
            bool breakOnPeer = false;

            wrl.ComPtr<ILoopingSelectorItemDataAutomationPeer> spItemDataAP;
            {
                wrl.ComPtr<DependencyObject> spItem;
                spItemsCollection.GetAt(itemIdx, &spItem);
                GetDataAutomationPeerForItem(spItem, &spItemDataAP);
            }

            switch(propertyAsEnum)
            {
            case Private.AutomationHelper.AutomationPropertyEnum.EmptyProperty:
                breakOnPeer = true;
                break;
            case Private.AutomationHelper.AutomationPropertyEnum.NameProperty:
                {
                    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
                    spItemDataAP.As(spAutomationPeer);
                    string strNameToCompare;
                    spAutomationPeer.GetName(strNameToCompare);
                    if (strNameToCompare == strNameToFind)
                    {
                        breakOnPeer = true;
                    }
                }
                break;
            case Private.AutomationHelper.AutomationPropertyEnum.IsSelectedProperty:
                {
                    wrl.ComPtr<DependencyObject> spItem;
                    ((LoopingSelectorItemDataAutomationPeer)(spItemDataAP)).GetItem(spItem);
                    if (isSelected && spSelectedItem == spItem ||
                        !isSelected && spSelectedItem != spItem)
                    {
                        breakOnPeer = true;
                    }
                }
                break;
            default:
                break;
            }

            if (breakOnPeer)
            {
                wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spItemPeerAsAP;

                spItemDataAP.As(spItemPeerAsAP);
                spThisAsProtected.ProviderFromPeer(spItemPeerAsAP, returnValue);
                break;
            }
        }
    }

// Cleanup
    // return hr;
}

#endregion 

#region IScrollProvider

 void LoopingSelectorAutomationPeer.get_HorizontallyScrollableImpl(out bool pValue)
{
    

    pValue = false;

    // return hr;
}

 void LoopingSelectorAutomationPeer.get_VerticallyScrollableImpl(out bool pValue)
{
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    pValue = false;

    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationGetIsScrollable(pValue);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.get_HorizontalScrollPercentImpl(out DOUBLE pValue)
{
    

    pValue = UIA_ScrollPatternNoScroll;

    // return hr;
}

 void LoopingSelectorAutomationPeer.get_VerticalScrollPercentImpl(out DOUBLE pValue)
{
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    pValue = 0.0;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationGetScrollPercent(pValue);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.get_VerticalViewSizeImpl(out DOUBLE pValue)
{
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    pValue = 0.0;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationGetScrollViewSize(pValue);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.get_HorizontalViewSizeImpl(out DOUBLE pValue)
{
    

    pValue = 100.0;

    // return hr;
}

 void LoopingSelectorAutomationPeer.ScrollImpl( xaml_automation.ScrollAmount horizontalAmount,  xaml_automation.ScrollAmount verticalAmount)
{
    UNREFERENCED_PARAMETER(horizontalAmount);
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationScroll(verticalAmount);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.SetScrollPercentImpl( DOUBLE horizontalPercent,  DOUBLE verticalPercent)
{
    UNREFERENCED_PARAMETER(horizontalPercent);
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        IFC_NOTRACE(pOwnerNoRef.AutomationSetScrollPercent(verticalPercent));
    }

// Cleanup
    // return hr;
}

#endregion 

#region DataItem Support

 void LoopingSelectorAutomationPeer.GetDataAutomationPeerForItem(
     DependencyObject pItem,
    out  xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer ppPeer)
{
    

    PeerMap.iterator peerIter;

    peerIter = _peerMap.find(pItem);

    if (peerIter == _peerMap.end())
    {
        wrl.ComPtr<ILoopingSelectorItemDataAutomationPeer> spDataPeer;
        (wrl.MakeAndInitialize<LoopingSelectorItemDataAutomationPeer>(
            &spDataPeer,
            pItem,
            (ILoopingSelectorAutomationPeer)(this)));

        // PeerMap keeps a pointer to this automation peer.
        // The peers lifetime is owned by this map and is released
        // when LoopingSelectorAP dies or when the items collection
        // changes.
        _peerMap[pItem] = spDataPeer;
        spDataPeer.AddRef();
        spDataPeer.CopyTo(ppPeer);
    }
    else
    {
        ppPeer = peerIter.second;
        (ppPeer).AddRef();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.GetContainerAutomationPeerForItem(
     DependencyObject pItem,
    out xaml_automation_peers.ILoopingSelectorItemAutomationPeer ppPeer)
{
    

    ppPeer = null;
    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationGetContainerUIAPeerForItem(pItem, ppPeer);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorAutomationPeer.FindStartIndex(
     xaml.Automation.Provider.IIRawElementProviderSimple pStartAfter,
     wfc.IList<DependencyObject> pItems,
    out INT pIndex)
{
    


    LoopingSelectorItemDataAutomationPeer pDataPeerNoRef = null;

    pIndex = -1;

    if (pStartAfter)
    {
        wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spProviderAsPeer;
        wrl.ComPtr<xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer> spDataPeer;
        wrl.ComPtr<xaml_automation_peers.IAutomationPeerProtected> spThisAsAPProtected;

        (QueryInterface(
            __uuidof(xaml.Automation.Peers.IAutomationPeerProtected),
            &spThisAsAPProtected));

        spThisAsAPProtected.PeerFromProvider(pStartAfter, &spProviderAsPeer);
        spProviderAsPeer.As(spDataPeer);
        pDataPeerNoRef = (LoopingSelectorItemDataAutomationPeer)(spDataPeer);
    }

    if (pDataPeerNoRef)
    {
        wrl.ComPtr<DependencyObject> spItem = null;
        bool found = false;
        UINT index = 0;

        pDataPeerNoRef.GetItem(spItem);
        pItems.IndexOf(spItem, &index, &found);
        if (found)
        {
            pIndex = (INT)(index);
        }
    }

// Cleanup
    // return hr;
}

 void
LoopingSelectorAutomationPeer.TryScrollItemIntoView( DependencyObject pItem)
{
    

    xaml_primitives.LoopingSelector pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationTryScrollItemIntoView(pItem);
    }

// Cleanup
    // return hr;
}

#endregion 

LoopingSelectorAutomationPeer.~LoopingSelectorAutomationPeer()
{
    /VERIFYHR/(ClearPeerMap());
}

} } } } } XAML_ABI_NAMESPACE_END

