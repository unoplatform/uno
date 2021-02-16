// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.



namespace Windows.UI.Xaml.Automation.Peers
{

LoopingSelectorItemDataAutomationPeer.LoopingSelectorItemDataAutomationPeer() :
    _itemIndex(-1)
{
}

 void
LoopingSelectorItemDataAutomationPeer.InitializeImpl(
    DependencyObject pItem,
    xaml_automation_peers.ILoopingSelectorAutomationPeer pOwner)
{
    
    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeerFactory> spInnerFactory;
    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spInnerInstance;
    wrl.ComPtr<DependencyObject> spInnerInspectable;

    LoopingSelectorItemDataAutomationPeerGenerated.InitializeImpl(pItem, pOwner);
    (wf.GetActivationFactory(
          wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_AutomationPeer),
          &spInnerFactory));

    (spInnerFactory.CreateInstance(
        (xaml_automation_peers.ILoopingSelectorItemDataAutomationPeer)(this),
        &spInnerInspectable,
        &spInnerInstance));

    (SetComposableBasePointers(
            spInnerInspectable,
            spInnerFactory));

    SetParent(pOwner);
    SetItem(pItem);
// Cleanup
    // return hr;
}

 void
LoopingSelectorItemDataAutomationPeer.SetParent( xaml_automation_peers.ILoopingSelectorAutomationPeer pOwner)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spThisAsAP;

    (QueryInterface(
        __uuidof(xaml.Automation.Peers.IAutomationPeer),
        &spThisAsAP));

    wrl.ComPtr<xaml_automation_peers.ILoopingSelectorAutomationPeer>(pOwner).AsWeak(_wrParent);
    if (pOwner)
    {
        wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spLSAsAP;
        pOwner.QueryInterface<xaml.Automation.Peers.IAutomationPeer>(spLSAsAP);
        // NOTE: This causes an addmost likely, I think that's a good idea.
        spThisAsAP.SetParent(spLSAsAP);
    }
    else
    {
        spThisAsAP.SetParent(null);
    }

// Cleanup
    // return hr;
}

 void
LoopingSelectorItemDataAutomationPeer.SetItem( DependencyObject pItem)
{
    return _tpItem = pItem;
}

 void
LoopingSelectorItemDataAutomationPeer.GetItem(out DependencyObject ppItem)
{
    ppItem = null;
    _tpItem.CopyTo(ppItem);
    return S_OK;
}

void LoopingSelectorItemDataAutomationPeer.SetItemIndex(int index)
{
    _itemIndex = index;
    return S_OK;
}

 void
LoopingSelectorItemDataAutomationPeer.ThrowElementNotAvailableException()
{
    return UIA_E_INVALIDOPERATION;
}

 void
LoopingSelectorItemDataAutomationPeer.GetContainerAutomationPeer(out xaml.Automation.Peers.IAutomationPeer ppContainer)
{
    ppContainer = null;
    wrl.ComPtr<xaml_automation_peers.ILoopingSelectorAutomationPeer> spParent;
    _wrParent.As(spParent);
    if (spParent)
    {
        wrl.ComPtr<ILoopingSelectorItemAutomationPeer> spLSIAP;
        ((LoopingSelectorAutomationPeer)(spParent)).GetContainerAutomationPeerForItem(_tpItem, &spLSIAP);

        if (!spLSIAP)
        {
            // If the item has not been realized, spLSIAP will be null.
            // Realize the item on demand now and try again
            Realize();
            ((LoopingSelectorAutomationPeer)(spParent)).GetContainerAutomationPeerForItem(_tpItem, &spLSIAP);
        }

        if (spLSIAP)
        {
            spLSIAP.CopyTo(ppContainer);
        }
    }

    return S_OK;
}

 void
LoopingSelectorItemDataAutomationPeer.RealizeImpl()
{
    wrl.ComPtr<xaml_automation_peers.ILoopingSelectorAutomationPeer> spParent;
    _wrParent.As(spParent);
    if (spParent && _tpItem)
    {
        ((LoopingSelectorAutomationPeer)(spParent)).RealizeItemAtIndex(_itemIndex);
    }

    return S_OK;
}

#region Method forwarders

 void LoopingSelectorItemDataAutomationPeer.GetPatternCoreImpl( xaml_automation_peers.PatternInterface patternInterface, out  DependencyObject returnValue)
{
    

    if (patternInterface == xaml.Automation.Peers.PatternInterface_VirtualizedItem)
    {
        returnValue = (ILoopingSelectorItemDataAutomationPeer)(this);
        AddRef();
    }
    else
    {
        wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
        GetContainerAutomationPeer(spAutomationPeer);
        if (spAutomationPeer)
        {
            spAutomationPeer.GetPattern(patternInterface, returnValue);
        }
        else
        {
            LoopingSelectorItemDataAutomationPeerGenerated.GetPatternCoreImpl(patternInterface, returnValue);
        }
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetAcceleratorKeyCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetAcceleratorKey(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetAccessKeyCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetAccessKey(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetAutomationControlTypeCoreImpl(out xaml_automation_peers.AutomationControlType returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetAutomationControlType(returnValue);
    }
    else
    {
        returnValue = xaml.Automation.Peers.AutomationControlType_ListItem;
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetAutomationIdCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetAutomationId(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetBoundingRectangleCoreImpl(out wf.Rect returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetBoundingRectangle(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetChildrenCoreImpl(out  wfc.IList<xaml_automation_peers.AutomationPeer> returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetChildren(returnValue);
    }
    else
    {
        returnValue = null;
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetClassNameCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetClassName(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetClickablePointCoreImpl(out wf.Point returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetClickablePoint(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetHelpTextCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetHelpText(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetItemStatusCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetItemStatus(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetItemTypeCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetItemType(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetLabeledByCoreImpl(out  xaml_automation_peers.IAutomationPeer returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetLabeledBy(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetLocalizedControlTypeCoreImpl(out string returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetLocalizedControlType(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetNameCoreImpl(out string returnValue)
{
    

    returnValue = null;

    if (_tpItem)
    {
        Private.AutomationHelper.GetPlainText(_tpItem, returnValue);
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetOrientationCoreImpl(out xaml_automation_peers.AutomationOrientation returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetOrientation(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetLiveSettingCoreImpl(out xaml_automation_peers.AutomationLiveSetting returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.GetLiveSetting(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetControlledPeersCoreImpl(out  wfc.IVectorView<xaml_automation_peers.AutomationPeer> returnValue)
{
    

    returnValue = null;

    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.HasKeyboardFocusCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.HasKeyboardFocus(returnValue);
    }
    else
    {
        returnValue = false;
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsContentElementCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsContentElement(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsControlElementCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsControlElement(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsEnabledCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsEnabled(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsKeyboardFocusableCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsKeyboardFocusable(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsOffscreenCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsOffscreen(returnValue);
    }
    else
    {
        returnValue = true;
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsPasswordCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsPassword(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.IsRequiredForFormCoreImpl(out bool returnValue)
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.IsRequiredForForm(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.SetFocusCoreImpl()
{
    

    wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);
    if (spAutomationPeer)
    {
        spAutomationPeer.SetFocus();
    }

// Cleanup
    // return hr;
}

 void LoopingSelectorItemDataAutomationPeer.GetAnnotationsCoreImpl(out  wfc.IList<xaml_automation_peers.AutomationPeerAnnotation> returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);

    if (spAutomationPeer)
    {
        spAutomationPeer.GetAnnotations(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

    return S_OK;
}

 void LoopingSelectorItemDataAutomationPeer.GetPositionInSetCoreImpl(out INT returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);

    if (spAutomationPeer)
    {
        spAutomationPeer.GetPositionInSet(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

    return S_OK;
}

 void LoopingSelectorItemDataAutomationPeer.GetSizeOfSetCoreImpl(out INT returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);

    if (spAutomationPeer)
    {
        spAutomationPeer.GetSizeOfSet(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

    return S_OK;
}

 void LoopingSelectorItemDataAutomationPeer.GetLevelCoreImpl(out INT returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);

    if (spAutomationPeer)
    {
        spAutomationPeer.GetLevel(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

    return S_OK;
}

 void LoopingSelectorItemDataAutomationPeer.GetLandmarkTypeCoreImpl(out xaml_automation_peers.AutomationLandmarkType returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);

    if (spAutomationPeer)
    {
        spAutomationPeer.GetLandmarkType(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

    return S_OK;
}

 void LoopingSelectorItemDataAutomationPeer.GetLocalizedLandmarkTypeCoreImpl(out string returnValue)
{
    wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
    GetContainerAutomationPeer(spAutomationPeer);

    if (spAutomationPeer)
    {
        spAutomationPeer.GetLocalizedLandmarkType(returnValue);
    }
    else
    {
        ThrowElementNotAvailableException();
    }

    return S_OK;
}

#endregion 

} } } } } XAML_ABI_NAMESPACE_END
