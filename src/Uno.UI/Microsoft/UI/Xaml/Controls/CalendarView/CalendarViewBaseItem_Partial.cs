// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "calendarviewbaseitem.g.h"
#include "CalendarView.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

// Called when the user presses a pointer down over the CalendarViewBaseItem.
IFACEMETHODIMP CalendarViewBaseItem.OnPointerPressed(
     IPointerRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    BOOLEAN isHandled = FALSE;

    IFC(CalendarViewBaseItemGenerated.OnPointerPressed(pArgs));

    IFC(pArgs.get_Handled(&isHandled));
    if (!isHandled)
    {
        IFC(SetIsPressed(true));
        IFC(UpdateVisualStateInternal());
    }

Cleanup:
    return hr;
}

// Called when the user releases a pointer over the CalendarViewBaseItem.
IFACEMETHODIMP CalendarViewBaseItem.OnPointerReleased(
     xaml_input.IPointerRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    BOOLEAN isHandled = FALSE;

    IFC(CalendarViewBaseItemGenerated.OnPointerReleased(pArgs));

    IFC(pArgs.get_Handled(&isHandled));
    if (!isHandled)
    {
        IFC(SetIsPressed(false));
        IFC(UpdateVisualStateInternal());
    }

Cleanup:
    return hr;
}

// Called when a pointer enters a CalendarViewBaseItem.
IFACEMETHODIMP CalendarViewBaseItem.OnPointerEntered(
     IPointerRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    ctl.ComPtr<IPointer> spPointer;
    wdei.PointerDeviceType pointerDeviceType = wdei.PointerDeviceType_Touch;

    IFC(CalendarViewBaseItemGenerated.OnPointerEntered(pArgs));

    // Only update hover state if the pointer type isn't touch
    IFC(pArgs.get_Pointer(&spPointer));
    IFCPTR(spPointer);
    IFC(spPointer.get_PointerDeviceType(&pointerDeviceType));
    if (pointerDeviceType != wdei.PointerDeviceType_Touch)
    {
        IFC(SetIsHovered(true));
        IFC(UpdateVisualStateInternal());
    }

Cleanup:
    return hr;
}

// Called when a pointer leaves a CalendarViewBaseItem.
IFACEMETHODIMP CalendarViewBaseItem.OnPointerExited(
     IPointerRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;

    IFC(CalendarViewBaseItemGenerated.OnPointerExited(pArgs));

    IFC(SetIsHovered(false));
    IFC(SetIsPressed(false));
    IFC(UpdateVisualStateInternal());

Cleanup:
    return hr;

}

// Called when the CalendarViewBaseItem or its children lose pointer capture.
IFACEMETHODIMP CalendarViewBaseItem.OnPointerCaptureLost(
     xaml_input.IPointerRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;

    IFC(CalendarViewBaseItemGenerated.OnPointerCaptureLost(pArgs));

    IFC(SetIsHovered(false));
    IFC(SetIsPressed(false));
    IFC(UpdateVisualStateInternal());

Cleanup:
    return hr;
}

// Called when the CalendarViewBaseItem receives focus.
IFACEMETHODIMP CalendarViewBaseItem.OnGotFocus(
     IRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;
    xaml.FocusState focusState = xaml.FocusState_Unfocused;

    IFC(CalendarViewBaseItemGenerated.OnGotFocus(pArgs));

    if (var pCalendarView = GetParentCalendarView())
    {
        IFC(pCalendarView.OnItemFocused(this));
    }

    IFC(get_FocusState(&focusState));

    IFC(SetIsKeyboardFocused(focusState == xaml.FocusState_Keyboard));


Cleanup:
    return hr;
}

// Called when the CalendarViewBaseItem loses focus.
IFACEMETHODIMP CalendarViewBaseItem.OnLostFocus(
     IRoutedEventArgs* pArgs)
{
    HRESULT hr = S_OK;

    IFC(CalendarViewBaseItemGenerated.OnLostFocus(pArgs));

    // remove keyboard focused state
    IFC(SetIsKeyboardFocused(false));

Cleanup:
    return hr;
}

IFACEMETHODIMP CalendarViewBaseItem.OnRightTapped(
     IRightTappedRoutedEventArgs* pArgs)
{
    IFC_RETURN(CalendarViewBaseItemGenerated.OnRightTapped(pArgs));

    BOOLEAN isHandled = FALSE;

    IFC_RETURN(pArgs.get_Handled(&isHandled));

    if (!isHandled)
    {
        bool ignored = false;
        IFC_RETURN(FocusSelfOrChild(xaml.FocusState.FocusState_Pointer, &ignored));
        IFC_RETURN(pArgs.put_Handled(TRUE));
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItem.OnIsEnabledChanged( IsEnabledChangedEventArgs* pArgs)
{
    return UpdateTextBlockForeground();
}

_Check_return_ HRESULT CalendarViewBaseItem.EnterImpl(
     XBOOL bLive,
     XBOOL bSkipNameRegistration,
     XBOOL bCoercedIsEnabled,
     XBOOL bUseLayoutRounding)
{
    IFC_RETURN(CalendarViewBaseItemGenerated.EnterImpl(bLive, bSkipNameRegistration, bCoercedIsEnabled, bUseLayoutRounding));

    if (bLive)
    {
        // In case any of the TextBlock properties have been updated while
        // we were out of the visual tree, we should update them in order to ensure
        // that we always have the most up-to-date values.
        // An example where this can happen is if the theme changes while
        // the flyout holding the CalendarView for a CalendarDatePicker is closed.
        IFC_RETURN(UpdateTextBlockForeground());
        IFC_RETURN(UpdateTextBlockFontProperties());
        IFC_RETURN(UpdateTextBlockAlignments());
        IFC_RETURN(UpdateVisualStateInternal());
    }

    return S_OK;
}

void CalendarViewBaseItem.SetParentCalendarView( CalendarView* pCalendarView)
{
    m_pParentCalendarView = pCalendarView;
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());

    pChrome.SetOwner(pCalendarView ? (CCalendarView*)(pCalendarView.GetHandle()) : null);
}

CalendarView* CalendarViewBaseItem.GetParentCalendarView()
{
    return m_pParentCalendarView;
}

_Check_return_ HRESULT CalendarViewBaseItem.UpdateMainText( HSTRING mainText)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.UpdateMainText(mainText);
}

_Check_return_ HRESULT CalendarViewBaseItem.UpdateLabelText( HSTRING labelText)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.UpdateLabelText(labelText);
}

_Check_return_ HRESULT CalendarViewBaseItem.ShowLabelText( bool showLabel)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.ShowLabelText(showLabel);
}

_Check_return_ HRESULT CalendarViewBaseItem.GetMainText(out HSTRING* pMainText)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.GetMainText(pMainText);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsToday( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsToday(state);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsKeyboardFocused( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsKeyboardFocused(state);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsSelected( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsSelected(state);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsBlackout( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsBlackout(state);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsHovered( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsHovered(state);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsPressed( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsPressed(state);
}

_Check_return_ HRESULT CalendarViewBaseItem.SetIsOutOfScope( bool state)
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.SetIsOutOfScope(state);
}

// If this item is unfocused, sets focus on the CalendarViewBaseItem.
// Otherwise, sets focus to whichever element currently has focus
// (so focusState can be propagated).
_Check_return_ HRESULT CalendarViewBaseItem.FocusSelfOrChild(
     xaml.FocusState focusState,
    out bool* pFocused,
     xaml_input.FocusNavigationDirection focusNavigationDirection)
{
    HRESULT hr = S_OK;
    BOOLEAN isItemAlreadyFocused = FALSE;
    ctl.ComPtr<DependencyObject> spItemToFocus = NULL;

    *pFocused = false;

    IFC(HasFocus(&isItemAlreadyFocused));
    if (isItemAlreadyFocused)
    {
        // Re-focus the currently focused item to propagate focusState (the item might be focused
        // under a different FocusState value).
        IFC(GetFocusedElement(&spItemToFocus));
    }
    else
    {
        spItemToFocus = this;
    }

    if (spItemToFocus)
    {
        BOOLEAN focused = FALSE;
        IFC(SetFocusedElementWithDirection(spItemToFocus.Get(), focusState, FALSE /*animateIfBringIntoView*/, &focused, focusNavigationDirection));
        *pFocused = !!focused;
    }

Cleanup:
    return hr;
}

#if DBG
// wf.DateTime has an int64 member which is not intutive enough. This method will convert it
// into numbers that we can easily read.
_Check_return_ HRESULT CalendarViewBaseItem.SetDateForDebug( wf.DateTime value)
{
    HRESULT hr = S_OK;

    var pCalendarView = GetParentCalendarView();
    if (pCalendarView)
    {
        var pCalendar = pCalendarView.GetCalendar();
        IFC(pCalendar.SetDateTime(value));
        IFC(pCalendar.get_Era(&m_eraForDebug));
        IFC(pCalendar.get_Year(&m_yearForDebug));
        IFC(pCalendar.get_Month(&m_monthForDebug));
        IFC(pCalendar.get_Day(&m_dayForDebug));
    }

Cleanup:
    return hr;
}
#endif


_Check_return_ HRESULT CalendarViewBaseItem.InvalidateRender()
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    pChrome.InvalidateRender();
    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItem.UpdateTextBlockForeground()
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.UpdateTextBlocksForeground();
}

_Check_return_ HRESULT CalendarViewBaseItem.UpdateTextBlockFontProperties()
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.UpdateTextBlocksFontProperties();
}

_Check_return_ HRESULT CalendarViewBaseItem.UpdateTextBlockAlignments()
{
    CCalendarViewBaseItemChrome* pChrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    return pChrome.UpdateTextBlocksAlignments();
}

// Change to the correct visual state for the CalendarViewBaseItem.
_Check_return_ HRESULT CalendarViewBaseItem.ChangeVisualState(
    // true to use transitions when updating the visual state, false
    // to snap directly to the new visual state.
    bool bUseTransitions)
{
    IFC_RETURN(CalendarViewBaseItemGenerated.ChangeVisualState(bUseTransitions));

    CCalendarViewBaseItemChrome* chrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    BOOLEAN ignored = FALSE;
    BOOLEAN isPointerOver = chrome.IsHovered();
    BOOLEAN isPressed = chrome.IsPressed();

    // Common States Group
    if (isPressed)
    {
        IFC_RETURN(GoToState(bUseTransitions, "Pressed", &ignored));
    }
    else if (isPointerOver)
    {
        IFC_RETURN(GoToState(bUseTransitions, "PointerOver", &ignored));
    }
    else
    {
        IFC_RETURN(GoToState(bUseTransitions, "Normal", &ignored));
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItem.UpdateVisualStateInternal()
{
    CCalendarViewBaseItemChrome* chrome = (CCalendarViewBaseItemChrome*)(GetHandle());
    if (chrome.HasTemplateChild()) // If !HasTemplateChild, then there is no visual in ControlTemplate for CalendarViewDayItemStyle
                                    // There should be no VisualStateGroup defined, so ignore UpdateVisualState
    {
        IFC_RETURN(UpdateVisualState(FALSE /* fUseTransitions */));
    }

    return S_OK;
}