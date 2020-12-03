// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "calendarviewitem.g.h"
#include "CalendarView.g.h"
#include "CalendarViewItemAutomationPeer.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

// Called when a pointer makes a tap gesture on a CalendarViewBaseItem.
IFACEMETHODIMP CalendarViewItem.OnTapped(
     ITappedRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    BOOLEAN isHandled = FALSE;

    IFC(CalendarViewItemGenerated.OnTapped(pArgs));

    IFC(pArgs.get_Handled(&isHandled));

    if (!isHandled)
    {
        ctl.ComPtr<CalendarView> spParentCalendarView(GetParentCalendarView());

        if (spParentCalendarView)
        {
            IFC(spParentCalendarView.OnSelectMonthYearItem(this, xaml.FocusState.FocusState_Pointer));
            IFC(pArgs.put_Handled(TRUE));

            ElementSoundPlayerService* soundPlayerService = DXamlCore.GetCurrent().GetElementSoundPlayerServiceNoRef();
            IFC(soundPlayerService.RequestInteractionSoundForElement(xaml.ElementSoundKind_Invoke, this));
        }
    }

Cleanup:
    return hr;
}


// Handles when a key is pressed down on the CalendarView.
IFACEMETHODIMP CalendarViewItem.OnKeyDown(
     xaml_input.IKeyRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    BOOLEAN isHandled = FALSE;

    IFC(CalendarViewItemGenerated.OnKeyDown(pArgs));

    IFC(pArgs.get_Handled(&isHandled));

    if (!isHandled)
    {
        ctl.ComPtr<CalendarView> spParentCalendarView(GetParentCalendarView());

        if (spParentCalendarView)
        {
            wsy.VirtualKey key = wsy.VirtualKey_None;
            IFC(pArgs.get_Key(&key));

            if (key == wsy.VirtualKey_Space || key == wsy.VirtualKey_Enter)
            {
                IFC(spParentCalendarView.OnSelectMonthYearItem(this, xaml.FocusState.FocusState_Keyboard));
                IFC(pArgs.put_Handled(true));
                // note: though we are going to change the display mode and move the focus to the new item,
                // we still want to show a keyboard focus border before that happens (in case later we have an animation to change the display mode).
                IFC(SetIsKeyboardFocused(true));

                ElementSoundPlayerService* soundPlayerService = DXamlCore.GetCurrent().GetElementSoundPlayerServiceNoRef();
                IFC(soundPlayerService.RequestInteractionSoundForElement(xaml.ElementSoundKind_Invoke, this));
            }
            else
            {
                // let CalendarView handle this event and tell calendarview the event comes from a MonthYearItem
                IFC(spParentCalendarView.SetKeyDownEventArgsFromCalendarItem(pArgs));
            }
        }
    }

Cleanup:
    return hr;
}

#if DBG
_Check_return_ HRESULT CalendarViewItem.put_Date( wf.DateTime value)
{
    HRESULT hr = S_OK;

    IFC(SetDateForDebug(value));
    IFC(CalendarViewItemGenerated.put_Date(value));

Cleanup:
    return hr;
}
#endif

IFACEMETHODIMP CalendarViewItem.OnCreateAutomationPeer(_Outptr_result_maybenull_ xaml_automation_peers.IAutomationPeer** ppAutomationPeer)
{
    IFCPTR_RETURN(ppAutomationPeer);
    *ppAutomationPeer = null;

    ctl.ComPtr<CalendarViewItemAutomationPeer> spAutomationPeer;
    IFC_RETURN(ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.CalendarViewItemAutomationPeer, GetHandle(), spAutomationPeer.GetAddressOf()));
    IFC_RETURN(spAutomationPeer.put_Owner(this));
    *ppAutomationPeer = spAutomationPeer.Detach();
    return S_OK;
}