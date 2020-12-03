// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "calendarviewdayitem.g.h"
#include "CalendarViewDayItemChangingEventArgs.g.h"
#include "CalendarView.g.h"
#include "CalendarViewDayItemAutomationPeer.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;


// Handle the custom property changed event and call the OnPropertyChanged methods.
_Check_return_ HRESULT CalendarViewDayItem.OnPropertyChanged2(
     const PropertyChangedParams& args)
{
    HRESULT hr = S_OK;

    IFC(CalendarViewDayItemGenerated.OnPropertyChanged2(args));

    if (args.m_pDP.GetIndex() == KnownPropertyIndex.CalendarViewDayItem_IsBlackout)
    {
        bool isBlackout = false;

        IFC(args.m_pNewValue.GetBool(isBlackout));

        IFC(SetIsBlackout(isBlackout));

        // when setting an item to blackout, we need remove it from selectedDates (if it exists)
        if (isBlackout)
        {
            ctl.ComPtr<CalendarView> spParentCalendarView(GetParentCalendarView());
            if (spParentCalendarView)
            {
                IFC(spParentCalendarView.OnDayItemBlackoutChanged(this, isBlackout));
            }
        }
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarViewDayItem.SetDensityColorsImpl( wfc.IEnumerable<wu.Color>* pColors)
{
    return (CCalendarViewBaseItemChrome*)(GetHandle()).SetDensityColors(pColors);
}

_Check_return_ HRESULT CalendarViewDayItem.GetBuildTreeArgs(out ctl.ComPtr<xaml_controls.ICalendarViewDayItemChangingEventArgs>* pspArgs)
{
    HRESULT hr = S_OK;

    if (!m_tpBuildTreeArgs)
    {
        ctl.ComPtr<CalendarViewDayItemChangingEventArgs> spArgs;

        IFC(ctl.make(&spArgs));
        SetPtrValue(m_tpBuildTreeArgs, spArgs.Get());
        *pspArgs = std.move(spArgs);
    }
    else
    {
        *pspArgs = m_tpBuildTreeArgs.Get();
    }

Cleanup:
    return hr;
}



// Called when a pointer makes a tap gesture on a CalendarViewBaseItem.
IFACEMETHODIMP CalendarViewDayItem.OnTapped(
     ITappedRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    BOOLEAN isHandled = FALSE;

    IFC(CalendarViewDayItemGenerated.OnTapped(pArgs));

    IFC(pArgs.get_Handled(&isHandled));

    if (!isHandled)
    {
        ctl.ComPtr<CalendarView> spParentCalendarView(GetParentCalendarView());

        if (spParentCalendarView)
        {
            bool ignored = false;
            IFC(FocusSelfOrChild(xaml.FocusState.FocusState_Pointer, &ignored));
            IFC(spParentCalendarView.OnSelectDayItem(this));
            IFC(pArgs.put_Handled(TRUE));

            ElementSoundPlayerService* soundPlayerService = DXamlCore.GetCurrent().GetElementSoundPlayerServiceNoRef();
            IFC(soundPlayerService.RequestInteractionSoundForElement(xaml.ElementSoundKind_Invoke, this));
        }
    }

Cleanup:
    return hr;
}


// Handles when a key is pressed down on the CalendarView.
IFACEMETHODIMP CalendarViewDayItem.OnKeyDown(
     xaml_input.IKeyRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    BOOLEAN isHandled = FALSE;

    IFC(CalendarViewDayItemGenerated.OnKeyDown(pArgs));

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
                IFC(spParentCalendarView.OnSelectDayItem(this));
                IFC(pArgs.put_Handled(true));
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
_Check_return_ HRESULT CalendarViewDayItem.put_Date( wf.DateTime value)
{
    HRESULT hr = S_OK;

    IFC(SetDateForDebug(value));
    IFC(CalendarViewDayItemGenerated.put_Date(value));

Cleanup:
    return hr;
}
#endif

IFACEMETHODIMP CalendarViewDayItem.OnCreateAutomationPeer(_Outptr_result_maybenull_ xaml_automation_peers.IAutomationPeer** ppAutomationPeer)
{
    IFCPTR_RETURN(ppAutomationPeer);
    *ppAutomationPeer = null;

    ctl.ComPtr<CalendarViewDayItemAutomationPeer> spAutomationPeer;
    IFC_RETURN(ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.CalendarViewDayItemAutomationPeer, GetHandle(), spAutomationPeer.GetAddressOf()));
    IFC_RETURN(spAutomationPeer.put_Owner(this));
    *ppAutomationPeer = spAutomationPeer.Detach();
    return S_OK;
}
