// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

// Called when the user presses a pointer down over the CalendarViewBaseItem.
private void OnPointerPressed(
     IPointerRoutedEventArgs pArgs)
{
    bool isHandled = false;

    CalendarViewBaseItemGenerated.OnPointerPressed(pArgs);

    isHandled = pArgs.Handled;
    if (!isHandled)
    {
        SetIsPressed(true);
        UpdateVisualStateInternal();
    }

}

// Called when the user releases a pointer over the CalendarViewBaseItem.
private void OnPointerReleased(
     xaml_input.IPointerRoutedEventArgs pArgs)
{
    bool isHandled = false;

    CalendarViewBaseItemGenerated.OnPointerReleased(pArgs);

    isHandled = pArgs.Handled;
    if (!isHandled)
    {
        SetIsPressed(false);
        UpdateVisualStateInternal();
    }

}

// Called when a pointer enters a CalendarViewBaseItem.
private void OnPointerEntered(
     IPointerRoutedEventArgs pArgs)
{
    IPointer spPointer;
    wdei.PointerDeviceType pointerDeviceType = wdei.PointerDeviceType_Touch;

    CalendarViewBaseItemGenerated.OnPointerEntered(pArgs);

    // Only update hover state if the pointer type isn't touch
    spPointer = pArgs.Pointer;
    pointerDeviceType = spPointer.PointerDeviceType;
    if (pointerDeviceType != wdei.PointerDeviceType_Touch)
    {
        SetIsHovered(true);
        UpdateVisualStateInternal();
    }

}

// Called when a pointer leaves a CalendarViewBaseItem.
private void OnPointerExited(
     IPointerRoutedEventArgs pArgs)
{
    CalendarViewBaseItemGenerated.OnPointerExited(pArgs);

    SetIsHovered(false);
    SetIsPressed(false);
    UpdateVisualStateInternal();

}

// Called when the CalendarViewBaseItem or its children lose pointer capture.
private void OnPointerCaptureLost(
     xaml_input.IPointerRoutedEventArgs pArgs)
{
    CalendarViewBaseItemGenerated.OnPointerCaptureLost(pArgs);

    SetIsHovered(false);
    SetIsPressed(false);
    UpdateVisualStateInternal();

}

// Called when the CalendarViewBaseItem receives focus.
private void OnGotFocus(
     IRoutedEventArgs pArgs)
{
    xaml.FocusState focusState = xaml.FocusState_Unfocused;

    CalendarViewBaseItemGenerated.OnGotFocus(pArgs);

    if (var pCalendarView = GetParentCalendarView())
    {
        pCalendarView.OnItemFocused(this);
    }

    focusState = FocusState;

    SetIsKeyboardFocused(focusState == xaml.FocusState_Keyboard);


}

// Called when the CalendarViewBaseItem loses focus.
private void OnLostFocus(
     IRoutedEventArgs pArgs)
{
    CalendarViewBaseItemGenerated.OnLostFocus(pArgs);

    // remove keyboard focused state
    SetIsKeyboardFocused(false);

}

private void OnRightTapped(
     IRightTappedRoutedEventArgs pArgs)
{
    CalendarViewBaseItemGenerated.OnRightTapped(pArgs);

    bool isHandled = false;

    isHandled = pArgs.Handled;

    if (!isHandled)
    {
        bool ignored = false;
        ignored = FocusSelfOrChild(xaml.FocusState.FocusState_Pointer);
        pArgs.Handled = true;
    }
    return;
}

private void OnIsEnabledChanged( IsEnabledChangedEventArgs pArgs)
{
    return UpdateTextBlockForeground();
}

private void EnterImpl(
     XBOOL bLive,
     XBOOL bSkipNameRegistration,
     XBOOL bCoercedIsEnabled,
     XBOOL bUseLayoutRounding)
{
    CalendarViewBaseItemGenerated.EnterImpl(bLive, bSkipNameRegistration, bCoercedIsEnabled, bUseLayoutRounding);

    if (bLive)
    {
        // In case any of the TextBlock properties have been updated while
        // we were out of the visual tree, we should update them in order to ensure
        // that we always have the most up-to-date values.
        // An example where this can happen is if the theme changes while
        // the flyout holding the CalendarView for a CalendarDatePicker is closed.
        UpdateTextBlockForeground();
        UpdateTextBlockFontProperties();
        UpdateTextBlockAlignments();
        UpdateVisualStateInternal();
    }

    return;
}

void SetParentCalendarView( CalendarView pCalendarView)
{
    m_pParentCalendarView = pCalendarView;
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());

    pChrome.SetOwner(pCalendarView ? (CCalendarView)(pCalendarView.GetHandle()) : null);
}

CalendarView GetParentCalendarView()
{
    return m_pParentCalendarView;
}

private void UpdateMainText( string mainText)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.UpdateMainText(mainText);
}

private void UpdateLabelText( string labelText)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.UpdateLabelText(labelText);
}

private void ShowLabelText( bool showLabel)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.ShowLabelText(showLabel);
}

private void GetMainText(out HSTRING pMainText)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.GetMainText(pMainText);
}

private void SetIsToday( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsToday(state);
}

private void SetIsKeyboardFocused( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsKeyboardFocused(state);
}

private void SetIsSelected( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsSelected(state);
}

private void SetIsBlackout( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsBlackout(state);
}

private void SetIsHovered( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsHovered(state);
}

private void SetIsPressed( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsPressed(state);
}

private void SetIsOutOfScope( bool state)
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.SetIsOutOfScope(state);
}

// If this item is unfocused, sets focus on the CalendarViewBaseItem.
// Otherwise, sets focus to whichever element currently has focus
// (so focusState can be propagated).
private void FocusSelfOrChild(
     xaml.FocusState focusState,
    out bool pFocused,
     xaml_input.FocusNavigationDirection focusNavigationDirection)
{
    bool isItemAlreadyFocused = false;
    DependencyObject spItemToFocus = NULL;

    pFocused = false;

    isItemAlreadyFocused = HasFocus);
    if (isItemAlreadyFocused)
    {
        // Re-focus the currently focused item to propagate focusState (the item might be focused
        // under a different FocusState value).
        spItemToFocus = GetFocusedElement);
    }
    else
    {
        spItemToFocus = this;
    }

    if (spItemToFocus)
    {
        bool focused = false;
        SetFocusedElementWithDirection(spItemToFocus, focusState, false /animateIfBringIntoView/, &focused, focusNavigationDirection);
        pFocused = !!focused;
    }

}

#if DBG
// DateTime has an int64 member which is not intutive enough. This method will convert it
// into numbers that we can easily read.
private void SetDateForDebug( DateTime value)
{
    var pCalendarView = GetParentCalendarView();
    if (pCalendarView)
    {
        var pCalendar = pCalendarView.GetCalendar();
        pCalendar.SetDateTime(value);
        m_eraForDebug = pCalendar.Era;
        m_yearForDebug = pCalendar.Year;
        m_monthForDebug = pCalendar.Month;
        m_dayForDebug = pCalendar.Day;
    }

}
#endif


private void InvalidateRender()
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    pChrome.InvalidateRender();
    return;
}

private void UpdateTextBlockForeground()
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.UpdateTextBlocksForeground();
}

private void UpdateTextBlockFontProperties()
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.UpdateTextBlocksFontProperties();
}

private void UpdateTextBlockAlignments()
{
    CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
    return pChrome.UpdateTextBlocksAlignments();
}

// Change to the correct visual state for the CalendarViewBaseItem.
private void ChangeVisualState(
    // true to use transitions when updating the visual state, false
    // to snap directly to the new visual state.
    bool bUseTransitions)
{
    CalendarViewBaseItemGenerated.ChangeVisualState(bUseTransitions);

    CCalendarViewBaseItemChrome chrome = (CCalendarViewBaseItemChrome)(GetHandle());
    bool ignored = false;
    bool isPointerOver = chrome.IsHovered();
    bool isPressed = chrome.IsPressed();

    // Common States Group
    if (isPressed)
    {
        ignored = GoToState(bUseTransitions, "Pressed");
    }
    else if (isPointerOver)
    {
        ignored = GoToState(bUseTransitions, "PointerOver");
    }
    else
    {
        ignored = GoToState(bUseTransitions, "Normal");
    }

    return;
}

private void UpdateVisualStateInternal()
{
    CCalendarViewBaseItemChrome chrome = (CCalendarViewBaseItemChrome)(GetHandle());
    if (chrome.HasTemplateChild()) // If !HasTemplateChild, then there is no visual in ControlTemplate for CalendarViewDayItemStyle
                                    // There should be no VisualStateGroup defined, so ignore UpdateVisualState
    {
        UpdateVisualState(false /* fUseTransitions */);
    }

    return;
}