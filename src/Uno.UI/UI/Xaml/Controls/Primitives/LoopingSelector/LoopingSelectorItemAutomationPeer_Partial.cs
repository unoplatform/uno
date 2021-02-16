// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.



namespace Windows.UI.Xaml.Automation.Peers
{

 void
LoopingSelectorItemAutomationPeer.InitializeImpl(
     xaml_primitives.ILoopingSelectorItem pOwner)
{
    
    wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeerFactory> spInnerFactory;
    wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeer> spInnerInstance;
    wrl.ComPtr<xaml.FrameworkElement> spLoopingSelectorItemAsFE;
    wrl.ComPtr<xaml_primitives.ILoopingSelectorItem> spOwner(pOwner);
    wrl.ComPtr<DependencyObject> spInnerInspectable;

    LoopingSelectorItemAutomationPeerGenerated.InitializeImpl(pOwner);
    (wf.GetActivationFactory(
          wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
          &spInnerFactory));

    (((DependencyObject)(pOwner)).QueryInterface<xaml.FrameworkElement>(
        &spLoopingSelectorItemAsFE));

    (spInnerFactory.CreateInstanceWithOwner(
            spLoopingSelectorItemAsFE,
            (xaml_automation_peers.ILoopingSelectorItemAutomationPeer)(this),
            &spInnerInspectable,
            &spInnerInstance));

    (SetComposableBasePointers(
            spInnerInspectable,
            spInnerFactory));

    UpdateEventSource();
// Cleanup
    // return hr;
}

 void LoopingSelectorItemAutomationPeer.UpdateEventSource()
{
    wrl.ComPtr<xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer> spLSIDAP;

    GetDataAutomationPeer(spLSIDAP);

    if (spLSIDAP)
    {
        SetEventSource(spLSIDAP);
    }

    return;
}

 void LoopingSelectorItemAutomationPeer.UpdateItemIndex( int itemIndex)
{
    wrl.ComPtr<xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer> spLSIDAP;

    GetDataAutomationPeer(spLSIDAP);

    if (spLSIDAP)
    {
        (xaml_automation_peers.LoopingSelectorItemDataAutomationPeer)(spLSIDAP).SetItemIndex(itemIndex);
    }

    return;
}

 void LoopingSelectorItemAutomationPeer.SetEventSource( xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer pLSIDAP)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spLSIDAPAsAP;
    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spThisAsAP;

    pLSIDAP.QueryInterface<xaml.Automation.Peers.IAutomationPeer>(spLSIDAPAsAP);
    (QueryInterface(
        __uuidof(xaml.Automation.Peers.IAutomationPeer),
        &spThisAsAP));
    spThisAsAP.EventsSource = spLSIDAPAsAP;
// Cleanup
    // return hr;
}

 void LoopingSelectorItemAutomationPeer.GetDataAutomationPeer(out xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer ppLSIDAP)
{
    xaml_primitives.LoopingSelectorItem pOwnerNoRef = null;
    xaml_primitives.LoopingSelector pLoopingSelectorNoRef = null;
    wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeerStatics> spFEAPStatics;
    wrl.ComPtr<UIElement> spLSAsUIE;
    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spLSAPAsAP;
    wrl.ComPtr<xaml_automation_peers.ILoopingSelectorAutomationPeer> spLSAP;
    LoopingSelectorAutomationPeer pLoopingSelectorAPNoRef = null;
    wrl.ComPtr<DependencyObject> spItem;
    wrl.ComPtr<xaml_controls.IContentControl> spLSIAsCC;

    ppLSIDAP = null;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);

    if (pOwnerNoRef)
    {
        (pOwnerNoRef.QueryInterface(
            __uuidof(xaml_controls.IContentControl),
            &spLSIAsCC));
spItem =         spLSIAsCC.Content;

        // If we don't have an item yet, then we don't want to generate a data automation peer yet.
        // Otherwise, we'll insert an entry into our LoopingSelector's automation peer map
        // corresponding to a null item, which gets us into a bad state.
        // See LoopingSelectorAutomationPeer.GetDataAutomationPeerForItem().
        if (spItem)
        {
            pOwnerNoRef.GetParentNoRef(pLoopingSelectorNoRef);

            NT_global.System.Diagnostics.Debug.Assert(pLoopingSelectorNoRef);

            (wf.GetActivationFactory(
                wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
                &spFEAPStatics));
            (pLoopingSelectorNoRef.QueryInterface(
                __uuidof(UIElement),
                &spLSAsUIE));
            spFEAPStatics.CreatePeerForElement(spLSAsUIE, &spLSAPAsAP);

            spLSAPAsAP.As(spLSAP);
            pLoopingSelectorAPNoRef = (LoopingSelectorAutomationPeer)(spLSAP);

            pLoopingSelectorAPNoRef.GetDataAutomationPeerForItem(spItem, ppLSIDAP);
        }
    }

    return;
}

 void LoopingSelectorItemAutomationPeer.GetOwnerAsInternalPtrNoRef(out xaml_primitives.LoopingSelectorItem ppOwnerNoRef)
{
    
    wrl.ComPtr<UIElement> spOwnerAsUIElement;
    wrl.ComPtr<xaml_primitives.ILoopingSelectorItem> spOwner;
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
        ppOwnerNoRef = (xaml_primitives.LoopingSelectorItem)(spOwner);
    }

// Cleanup
    // return hr;
}

#region IAutomationPeerOverrides

 void
LoopingSelectorItemAutomationPeer.GetPatternCoreImpl(
     xaml.Automation.Peers.PatternInterface patternInterface,
    out  DependencyObject returnValue)
{
    

    if (patternInterface == xaml.Automation.Peers.PatternInterface_ScrollItem ||
        patternInterface == xaml.Automation.Peers.PatternInterface_SelectionItem )
    {
        returnValue = (ILoopingSelectorItemAutomationPeer)(this);
        AddRef();
    }
    else
    {
        LoopingSelectorItemAutomationPeerGenerated.GetPatternCoreImpl(patternInterface, returnValue);
    }

// Cleanup
    // return hr;
}

 void
LoopingSelectorItemAutomationPeer.GetAutomationControlTypeCoreImpl(
    out xaml.Automation.Peers.AutomationControlType returnValue)
{
    
    if(returnValue == null) throw new ArgumentNullException();
    returnValue = xaml.Automation.Peers.AutomationControlType_ListItem;

// Cleanup
    // return hr;
}

 void
LoopingSelectorItemAutomationPeer.GetClassNameCoreImpl(
    out string returnValue)
{
    
    if(returnValue == null) throw new ArgumentNullException();
    wrl_wrappers.Hstring("LoopingSelectorItem").CopyTo(returnValue);
// Cleanup
    // return hr;
}

 void
LoopingSelectorItemAutomationPeer.IsKeyboardFocusableCoreImpl(
    out bool returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> loopingSelectorAP;

    returnValue = false;

    // LoopingSelectorItems aren't actually keyboard focusable,
    // but we need to give them automation focus in order to have
    // UIA clients like Narrator read their contents when they're
    // selected, so we'll act as though they're keyboard focusable
    // to enable that to be possible.  In order to do this,
    // for the keyboard focus status of a LoopingSelectorItem,
    // we'll just report the keyboard focus status of its parent LoopingSelector.
    GetParentAutomationPeer(loopingSelectorAP);

    if (loopingSelectorAP)
    {
        loopingSelectorAP.IsKeyboardFocusable(returnValue);
    }

    return;
}

 void
LoopingSelectorItemAutomationPeer.HasKeyboardFocusCoreImpl(
    out bool returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> loopingSelectorAP;

    returnValue = false;

    // In order to support giving automation focus to selected LoopingSelectorItem
    // automation peers, we'll report that a LoopingSelectorItem has keyboard focus
    // if its parent LoopingSelector has keyboard focus, and if this LoopingSelectorItem
    // is selected.
    GetParentAutomationPeer(loopingSelectorAP);

    if (loopingSelectorAP)
    {
        bool hasKeyboardFocus = false;

        loopingSelectorAP.HasKeyboardFocus(hasKeyboardFocus);

        if (hasKeyboardFocus)
        {
            returnValue = IsSelectedImpl;
        }
    }

    return;
}


#endregion 

#region IScrollItemProvider

 void
LoopingSelectorItemAutomationPeer.ScrollIntoViewImpl()
{
    xaml_primitives.LoopingSelectorItem pOwnerNoRef = null;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);

    if (pOwnerNoRef)
    {
        xaml_primitives.LoopingSelector pOwnerParentNoRef = null;

        pOwnerNoRef.GetParentNoRef(pOwnerParentNoRef);

        if (pOwnerParentNoRef)
        {
            wrl.ComPtr<xaml_controls.IContentControl> spOwnerAsContentControl;
            wrl.ComPtr<DependencyObject> spContent;

            pOwnerNoRef.QueryInterface(__uuidof(xaml_controls.IContentControl), &spOwnerAsContentControl);
spContent =             spOwnerAsContentControl.Content;
            pOwnerParentNoRef.AutomationTryScrollItemIntoView(spContent);
        }
    }

    return;
}

#endregion 

#region ISelectionItemProvider

 void
LoopingSelectorItemAutomationPeer.get_IsSelectedImpl(out bool value)
{
    
    xaml_primitives.LoopingSelectorItem pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationGetIsSelected(value);
    }

// Cleanup
    // return hr;
}

 void
LoopingSelectorItemAutomationPeer.get_SelectionContainerImpl(out  xaml_automation.Provider.IIRawElementProviderSimple ppValue)
{
    
    xaml_primitives.LoopingSelectorItem pOwnerNoRef = null;
    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;

    ppValue = null;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationGetSelectionContainerUIAPeer(spAutomationPeer);
        if (spAutomationPeer)
        {
            wrl.ComPtr<xaml.Automation.Peers.IAutomationPeerProtected> spAutomationPeerAsProtected;
            wrl.ComPtr<xaml.Automation.Provider.IIRawElementProviderSimple> spProvider;

            spAutomationPeer.As(spAutomationPeerAsProtected);
            spAutomationPeerAsProtected.ProviderFromPeer(spAutomationPeer, &spProvider);
            ppValue = spProvider.Detach();
        }
    }

// Cleanup
    // return hr;
}

 void
LoopingSelectorItemAutomationPeer.AddToSelectionImpl()
{
    return UIA_E_INVALIDOPERATION;
}

 void
LoopingSelectorItemAutomationPeer.RemoveFromSelectionImpl()
{
    return UIA_E_INVALIDOPERATION;
}

 void
LoopingSelectorItemAutomationPeer.SelectImpl()
{
    
    xaml_primitives.LoopingSelectorItem pOwnerNoRef = null;
    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);
    if (pOwnerNoRef)
    {
        pOwnerNoRef.AutomationSelect();
    }

// Cleanup
    // return hr;
}

#endregion 

 void
LoopingSelectorItemAutomationPeer.GetParentAutomationPeer(
    out  xaml_automation_peers.IAutomationPeer parentAutomationPeer)
{
    xaml_primitives.LoopingSelectorItem pOwnerNoRef = null;

    parentAutomationPeer = null;

    GetOwnerAsInternalPtrNoRef(pOwnerNoRef);

    if (pOwnerNoRef)
    {
        xaml_primitives.LoopingSelector pLoopingSelectorNoRef = null;

        pOwnerNoRef.GetParentNoRef(pLoopingSelectorNoRef);

        if (pLoopingSelectorNoRef)
        {
            wrl.ComPtr<xaml_primitives.LoopingSelector> loopingSelector(pLoopingSelectorNoRef);
            wrl.ComPtr<UIElement> loopingSelectorAsUIE;

            IGNOREHR(loopingSelector.As(loopingSelectorAsUIE));

            if (loopingSelectorAsUIE)
            {
                wrl.ComPtr<xaml_automation_peers.IAutomationPeer> loopingSelectorAP;

                (Private.AutomationHelper.CreatePeerForElement(
                    loopingSelectorAsUIE,
                    &loopingSelectorAP));

                parentAutomationPeer = loopingSelectorAP.Detach();
            }
        }
    }

    return;
}

} } } } } XAML_ABI_NAMESPACE_END
