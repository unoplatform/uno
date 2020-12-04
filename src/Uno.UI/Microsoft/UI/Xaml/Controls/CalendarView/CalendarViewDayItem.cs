// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;


// Handle the custom property changed event and call the OnPropertyChanged methods.
private void OnPropertyChanged2(
      PropertyChangedParams& args)
{
    CalendarViewDayItemGenerated.OnPropertyChanged2(args);

    if (args.m_pDP.GetIndex() == KnownPropertyIndex.CalendarViewDayItem_IsBlackout)
    {
        bool isBlackout = false;

        isBlackout = (bool)args.NewValue;

        SetIsBlackout(isBlackout);

        // when setting an item to blackout, we need remove it from selectedDates (if it exists)
        if (isBlackout)
        {
            CalendarView spParentCalendarView(GetParentCalendarView());
            if (spParentCalendarView)
            {
                spParentCalendarView.OnDayItemBlackoutChanged(this, isBlackout);
            }
        }
    }

}

private void SetDensityColorsImpl( wfc.IIterable<wu.Color>* pColors)
{
    return (CCalendarViewBaseItemChrome)(GetHandle()).SetDensityColors(pColors);
}

private void GetBuildTreeArgs(out xaml_controls.ICalendarViewDayItemChangingEventArgs pspArgs)
{
    if (!m_tpBuildTreeArgs)
    {
        CalendarViewDayItemChangingEventArgs spArgs;

        spArgs = default;
        m_tpBuildTreeArgs = spArgs;
        pspArgs = std.move(spArgs);
    }
    else
    {
        pspArgs = m_tpBuildTreeArgs;
    }

}



// Called when a pointer makes a tap gesture on a CalendarViewBaseItem.
private void OnTapped(
     ITappedRoutedEventArgs pArgs)
{
    bool isHandled = false;

    CalendarViewDayItemGenerated.OnTapped(pArgs);

    isHandled = pArgs.Handled;

    if (!isHandled)
    {
        CalendarView spParentCalendarView(GetParentCalendarView());

        if (spParentCalendarView)
        {
            bool ignored = false;
            ignored = FocusSelfOrChild(xaml.FocusState.FocusState_Pointer);
            spParentCalendarView.OnSelectDayItem(this);
            pArgs.Handled = true;

            ElementSoundPlayerService soundPlayerService = DXamlCore.GetCurrent().GetElementSoundPlayerServiceNoRef();
            soundPlayerService.RequestInteractionSoundForElement(xaml.ElementSoundKind_Invoke, this);
        }
    }

}


// Handles when a key is pressed down on the CalendarView.
private void OnKeyDown(
     xaml_input.IKeyRoutedEventArgs pArgs)
{
    bool isHandled = false;

    CalendarViewDayItemGenerated.OnKeyDown(pArgs);

    isHandled = pArgs.Handled;

    if (!isHandled)
    {
        CalendarView spParentCalendarView(GetParentCalendarView());

        if (spParentCalendarView)
        {
            wsy.VirtualKey key = wsy.VirtualKey_None;
            key = pArgs.Key;

            if (key == wsy.VirtualKey_Space || key == wsy.VirtualKey_Enter)
            {
                spParentCalendarView.OnSelectDayItem(this);
                pArgs.Handled = true;
                SetIsKeyboardFocused(true);

                ElementSoundPlayerService soundPlayerService = DXamlCore.GetCurrent().GetElementSoundPlayerServiceNoRef();
                soundPlayerService.RequestInteractionSoundForElement(xaml.ElementSoundKind_Invoke, this);
            }
            else
            {
                // let CalendarView handle this event and tell calendarview the event comes from a MonthYearItem
                spParentCalendarView.SetKeyDownEventArgsFromCalendarItem(pArgs);
            }
        }
    }

}

#if DBG
private void put_Date( DateTime value)
{
    SetDateForDebug(value);
    CalendarViewDayItemGenerated.Date = value;

}
#endif

private void OnCreateAutomationPeer(out result_maybenull_ xaml_automation_peers.IAutomationPeer* ppAutomationPeer)
{
    IFCPTR_RETURN(ppAutomationPeer);
    ppAutomationPeer = null;

    CalendarViewDayItemAutomationPeer spAutomationPeer;
    ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.CalendarViewDayItemAutomationPeer, GetHandle(), spAutomationPeer());
    spAutomationPeer.Owner = this;
    ppAutomationPeer = spAutomationPeer.Detach();
    return;
}
