// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once


using namespace DirectUI;
using namespace DirectUISynonyms;

//CalendarDatePickerAutomationPeerFactory.CreateInstanceWithOwnerImpl
IMPLEMENT_CONTROL_AUTOMATIONPEERFACTORY_CREATEINSTANCE(CalendarDatePicker)

// Initializes a new instance of the CalendarDatePickerAutomationPeer class.
CalendarDatePickerAutomationPeer()
{
}

// Deructor
CalendarDatePickerAutomationPeer.~CalendarDatePickerAutomationPeer()
{
}

private void GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, out  DependencyObject ppReturnValue)
{
    ppReturnValue = NULL;

    if (patternInterface == xaml_automation_peers.PatternInterface_Invoke 
        || patternInterface == xaml_automation_peers.PatternInterface_Value)
    {
        ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        CalendarDatePickerAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue);
    }

}

private void GetClassNameCore(out HSTRING returnValue)
{
    return stringReference("CalendarDatePicker").CopyTo(returnValue);
}

private void GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType pReturnValue)
{
    pReturnValue = xaml_automation_peers.AutomationControlType_Button;
    return;
}

private void GetLocalizedControlTypeCore(out HSTRING returnValue)
{
    DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_AP_CALENDARDATEPICKER, returnValue);
    
    return;
}

private void  InvokeImpl()
{
    UIElement pOwner = NULL;
    bool bIsEnabled;

    bIsEnabled = IsEnabled);
    if (!bIsEnabled)
    {
        UIA_E_ELEMENTNOTENABLED;
    }

    pOwner = Owner;
    ((CalendarDatePicker)(pOwner)).IsCalendarOpen = true;

Cleanup:
    ReleaseInterface(pOwner);
    return hr;
}

private void get_IsReadOnlyImpl(out BOOLEAN value)
{
    value = true;
    return;
}

private void get_ValueImpl(out HSTRING value)
{
    UIElement spOwner;
    spOwner = Owner;
    
    var ownerItem = spOwner as CalendarDatePicker;
    IFCPTR_RETURN(ownerItem);

    ownerItem.GetCurrentFormattedDate(value);
    
    return;
}

private void SetValueImpl(string value)
{
    throw new NotImplementedException();
}