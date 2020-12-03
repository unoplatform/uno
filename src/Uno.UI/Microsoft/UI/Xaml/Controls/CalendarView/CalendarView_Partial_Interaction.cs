// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarView.g.h"
#include "CalendarViewDayItem.g.h"
#include "CalendarViewItem.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarPanel.g.h"
#include "ScrollViewer.g.h"
#include "KeyRoutedEventArgs.g.h"
#include "XboxUtility.h"
#include "KeyboardNavigation.h"
#include "Grid.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

#undef min
#undef max


// Handles when a key is pressed down on the CalendarView.
IFACEMETHODIMP CalendarView.OnKeyDown(
     xaml_input.IKeyRoutedEventArgs* pArgs)
{
    boolean isHandled = false;
    bool handled = false;
    ctl.ComPtr<IKeyRoutedEventArgs> spKeyDownArgsFromCalendarItem;

    wsy.VirtualKey key = wsy.VirtualKey_None;
    wsy.VirtualKeyModifiers modifiers = wsy.VirtualKeyModifiers_None;

    IFC_RETURN(CalendarViewGenerated.OnKeyDown(pArgs));

    // Check if this keydown events comes from CalendarItem.
    IFC_RETURN(m_wrKeyDownEventArgsFromCalendarItem.As(&spKeyDownArgsFromCalendarItem));

    if (!spKeyDownArgsFromCalendarItem)
    {
        return S_OK;
    }

    if (spKeyDownArgsFromCalendarItem.Get() != pArgs)
    {
        m_wrKeyDownEventArgsFromCalendarItem.Reset();
        return S_OK;
    }

    // Ignore already handled events
    IFC_RETURN(pArgs.get_Handled(&isHandled));
    if (isHandled)
    {
        return S_OK;
    }

    IFC_RETURN(GetKeyboardModifiers(&modifiers));
    IFC_RETURN(pArgs.get_Key(&key));

    if (key == wsy.VirtualKey_GamepadLeftTrigger)
    {
        key = wsy.VirtualKey_PageUp;
    }

    if (key == wsy.VirtualKey_GamepadRightTrigger)
    {
        key = wsy.VirtualKey_PageDown;
    }

    // navigation keys without modifier . do keyboard navigation.
    if (modifiers == wsy.VirtualKeyModifiers_None)
    {
        switch (key)
        {
        case wsy.VirtualKey_Home:
        case wsy.VirtualKey_End:
        case wsy.VirtualKey_PageUp:
        case wsy.VirtualKey_PageDown:
        case wsy.VirtualKey_Left:
        case wsy.VirtualKey_Right:
        case wsy.VirtualKey_Up:
        case wsy.VirtualKey_Down:
        {
            wsy.VirtualKey originalKey = wsy.VirtualKey_None;
            IFC_RETURN((KeyRoutedEventArgs*)(pArgs).get_OriginalKey(&originalKey));

            IFC_RETURN(OnKeyboardNavigation(key, originalKey, &handled));
            break;
        }
        default:
            break;
        }
    }
    // Ctrl+Up/Down . switch display modes.
    else if (modifiers == wsy.VirtualKeyModifiers_Control)
    {
        switch (key)
        {
        case wsy.VirtualKey_Up:
        case wsy.VirtualKey_Down:
        {
            xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;

            IFC_RETURN(get_DisplayMode(&mode));

            xaml_controls.CalendarViewDisplayMode newMode = mode;
            switch (mode)
            {
            case xaml_controls.CalendarViewDisplayMode_Month:
                if (key == wsy.VirtualKey_Up)
                {
                    newMode = xaml_controls.CalendarViewDisplayMode_Year;
                }
                break;
            case xaml_controls.CalendarViewDisplayMode_Year:
                if (key == wsy.VirtualKey_Up)
                {
                    newMode = xaml_controls.CalendarViewDisplayMode_Decade;
                }
                else
                {
                    newMode = xaml_controls.CalendarViewDisplayMode_Month;
                }
                break;
            case xaml_controls.CalendarViewDisplayMode_Decade:
                if (key == wsy.VirtualKey_Down)
                {
                    newMode = xaml_controls.CalendarViewDisplayMode_Year;
                }
                break;
            default:
                ASSERT(FALSE);
                break;
            }

            if (newMode != mode)
            {
                handled = true;
                // after display mode changed, we want the new item to be focused by keyboard.
                m_focusItemAfterDisplayModeChanged = true;
                m_focusStateAfterDisplayModeChanged = xaml.FocusState.FocusState_Keyboard;
                IFC_RETURN(put_DisplayMode(newMode));
            }
        }
            break;
        default:
            break;
        }
    }

    if (handled)
    {
        IFC_RETURN(pArgs.put_Handled(true));
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarView.OnKeyboardNavigation(
     wsy.VirtualKey key,
     wsy.VirtualKey originalKey,
    out bool* pHandled)
{
    HRESULT hr = S_OK;
    ctl.ComPtr<CalendarViewGeneratorHost> spHost;

    IFC(GetActiveGeneratorHost(&spHost));
    *pHandled = false;

    if (var pPanel = spHost.GetPanel())
    {
        int lastFocusedIndex = -1;

        IFC(spHost.CalculateOffsetFromMinDate(m_lastDisplayedDate, &lastFocusedIndex));

        ASSERT(lastFocusedIndex >= 0);
        int newFocusedIndex = lastFocusedIndex;
        switch (key)
        {
            // Home/End goes to the first/last date in current scope.
        case wsy.VirtualKey_Home:
        case wsy.VirtualKey_End:
        {
            var newFocusedDate = key == wsy.VirtualKey_Home ?
                spHost.GetMinDateOfCurrentScope() :
                spHost.GetMaxDateOfCurrentScope();
            IFC(spHost.CalculateOffsetFromMinDate(newFocusedDate, &newFocusedIndex));
            ASSERT(newFocusedIndex >= 0);
        }
            break;
            // PageUp/PageDown goes to the previous/next scope
        case wsy.VirtualKey_PageUp:
        case wsy.VirtualKey_PageDown:
        {
            var newFocusedDate = m_lastDisplayedDate;
            HRESULT hr2 = spHost.AddScopes(newFocusedDate, key == wsy.VirtualKey_PageUp ? -1 : 1);
            // beyond calendar's limit, fall back to min/max date, i.e. the first/last item.
            if (FAILED(hr2))
            {
                if (key == wsy.VirtualKey_PageUp)
                {
                    newFocusedIndex = 0;
                }
                else
                {
                    unsigned size = 0;
                    IFC(spHost.get_Size(&size));
                    newFocusedIndex = size - 1;
                }
            }
            else
            {
                // probably beyond {min, max}, let's make sure.
                CoerceDate(newFocusedDate);
                IFC(spHost.CalculateOffsetFromMinDate(newFocusedDate, &newFocusedIndex));
                ASSERT(newFocusedIndex >= 0);
            }
        }
            break;
            // Arrow keys: use the panel's default behavior.
        case wsy.VirtualKey_Left:
        case wsy.VirtualKey_Up:
        case wsy.VirtualKey_Right:
        case wsy.VirtualKey_Down:
        {
            xaml_controls.KeyNavigationAction action = xaml_controls.KeyNavigationAction_Up;
            UINT newFocusedIndexUint = 0;
            var newFocusedType = xaml_controls.ElementType_ItemContainer;
            bool isValidKey = false;
            BOOLEAN actionValidForSourceIndex = FALSE;

            IFC(TranslateKeyToKeyNavigationAction(key, &action, &isValidKey));
            ASSERT(isValidKey);
            IFC(pPanel.GetTargetIndexFromNavigationAction(
                lastFocusedIndex,
                xaml_controls.ElementType_ItemContainer,
                action,
                !XboxUtility.IsGamepadNavigationDirection(originalKey), /*allowWrap*/
                -1, /*itemIndexHintForHeaderNavigation*/
                &newFocusedIndexUint,
                &newFocusedType,
                &actionValidForSourceIndex));

            ASSERT(newFocusedType == xaml_controls.ElementType_ItemContainer);

            if (actionValidForSourceIndex)
            {
                newFocusedIndex = (int)(newFocusedIndexUint);
            }
        }
            break;
        default:
            ASSERT(FALSE);
            break;
        }

        // Check if we can focus on a new index or not.
        if (newFocusedIndex != lastFocusedIndex)
        {
            IFC(FocusItemByIndex(spHost.Get(), newFocusedIndex, xaml.FocusState.FocusState_Keyboard, pHandled));
        }
    }

Cleanup:
    return hr;
}



// Given a key, returns a focus navigation action.
_Check_return_ HRESULT CalendarView.TranslateKeyToKeyNavigationAction(
     wsy.VirtualKey key,
    out xaml_controls.KeyNavigationAction* pNavAction,
    out bool* pIsValidKey)
{
    HRESULT hr = S_OK;
    xaml.FlowDirection flowDirection = xaml.FlowDirection_LeftToRight;

    *pIsValidKey = false;
    *pNavAction = xaml_controls.KeyNavigationAction_Up;

    if (m_tpViewsGrid)
    {
        IFC(m_tpViewsGrid.Cast<Grid>().get_FlowDirection(&flowDirection));
    }
    else
    {
        IFC(get_FlowDirection(&flowDirection));
    }

    KeyboardNavigation.TranslateKeyToKeyNavigationAction(flowDirection, key, pNavAction, pIsValidKey);

Cleanup:
    return hr;
}

// When an item got focus, we save the date of this item (to m_lastDisplayedDate).
// When we switching the view mode, we always focus this date.
_Check_return_ HRESULT CalendarView.OnItemFocused( CalendarViewBaseItem* pItem)
{
    HRESULT hr = S_OK;
    wf.DateTime date;
    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;

    IFC(pItem.GetDate(&date));

    IFC(get_DisplayMode(&mode));

    IFC(CopyDate(
        mode,
        date,
        m_lastDisplayedDate));

Cleanup:
    return hr;
}


_Check_return_ HRESULT CalendarView.FocusItemByIndex(
     CalendarViewGeneratorHost* pHost,
     int index,
     xaml.FocusState focusState,
    out bool* pFocused)
{
    wf.DateTime date;

    *pFocused = false;

    ASSERT(index >= 0);

    IFC_RETURN(pHost.GetDateAt(index, &date));
    IFC_RETURN(FocusItem(pHost, date, index, focusState, pFocused));

    return S_OK;
}

_Check_return_ HRESULT CalendarView.FocusItemByDate(
     CalendarViewGeneratorHost* pHost,
     wf.DateTime date,
     xaml.FocusState focusState,
    out bool* pFocused)
{
    int index = -1;

    *pFocused = false;

    IFC_RETURN(pHost.CalculateOffsetFromMinDate(date, &index));
    IFC_RETURN(FocusItem(pHost, date, index, focusState, pFocused));

    return S_OK;
}

_Check_return_ HRESULT CalendarView.FocusItem(
     CalendarViewGeneratorHost* pHost,
     wf.DateTime date,
     int index,
     xaml.FocusState focusState,
    out bool* pFocused)
{
    ctl.ComPtr<IDependencyObject> spItemAsI;
    ctl.ComPtr<CalendarViewBaseItem> spItem;

    *pFocused = false;

    IFC_RETURN(SetDisplayDateInternal(date));  // scroll item into view so we can move focus to it.

    IFC_RETURN(pHost.GetPanel().ContainerFromIndex(index, &spItemAsI));
    ASSERT(spItemAsI);
    IFC_RETURN(spItemAsI.As(&spItem));

    IFC_RETURN(spItem.FocusSelfOrChild(focusState, pFocused));

    return S_OK;
}

// UIElement override for getting next tab stop on path from focus candidate element to root.
_Check_return_ HRESULT CalendarView.ProcessCandidateTabStopOverride(
     DependencyObject* pFocusedElement,
     DependencyObject* pCandidateTabStopElement,
     DependencyObject* pOverriddenCandidateTabStopElement,
    const bool isBackward,
    _Outptr_ DependencyObject** ppNewTabStop,
    out BOOLEAN* pIsCandidateTabStopOverridden)
{
    HRESULT hr = S_OK;

    // There is no ICalendarViewBaseItem interface so we can't use ctl.is to check an element is CalendarViewBaseItem or not
    // because in ctl.is, it will call ReleaseInterface and CalendarViewBaseItem has multiple interfaces.
    // ComPtr will do this in a smarter way - it always casts to IUnknown before release/addso there is no ambiguity.
    ctl.ComPtr<CalendarViewBaseItem> spCandidateAsCalendarViewBaseItem(ctl.query_interface<CalendarViewBaseItem>(pCandidateTabStopElement));

    *ppNewTabStop = null;
    *pIsCandidateTabStopOverridden = FALSE;

    // Check if the candidate is a calendaritem and the currently focused is not a calendaritem.
    // Which means we Tab (or shift+Tab) into the scrollviewer and we are going to put focus on the candidate.
    // However the candidate is the first (or last if we shift+Tab) realized item in the Panel, we should use the last
    // focused item to override the candidate (and ignore isBackward, i.e. Shift).

    if (spCandidateAsCalendarViewBaseItem)
    {
        ctl.ComPtr<CalendarViewBaseItem> spFocusedAsCalendarViewBaseItem(ctl.query_interface<CalendarViewBaseItem>(pFocusedElement));
        if (!spFocusedAsCalendarViewBaseItem)
        {
            ctl.ComPtr<CalendarViewGeneratorHost> spHost;
            IFC(GetActiveGeneratorHost(&spHost));

            var pScrollViewer = spHost.GetScrollViewer();
            if (pScrollViewer)
            {
                xaml_input.KeyboardNavigationMode mode = xaml_input.KeyboardNavigationMode_Local;

                IFC(pScrollViewer.get_TabNavigation(&mode));
                // The tab navigation on this scrollviewer must be Once (it's default value in the template).
                // For other modes (Local or Cycle) we don't want to override.
                if (mode == xaml_input.KeyboardNavigationMode_Once)
                {
                    BOOLEAN isAncestor = FALSE;

                    // Are we tabbing from/to another view?
                    // if developer makes other view focusable by re-template and the candidate is not
                    // in the active view, we'll not override the candidate.
                    IFC(pScrollViewer.IsAncestorOf(pCandidateTabStopElement, &isAncestor));

                    if (isAncestor)
                    {
                        var pPanel = spHost.GetPanel();
                        if (pPanel)
                        {
                            int index = -1;
                            ctl.ComPtr<xaml.IDependencyObject> spContainer;
                            ctl.ComPtr<CalendarViewGeneratorHost> spHost;
                            IFC(GetActiveGeneratorHost(&spHost));

                            IFC(spHost.CalculateOffsetFromMinDate(m_lastDisplayedDate, &index));

                            // This container might not have focus so it could be recycled, bring
                            // it into view so it can take focus.
                            IFC(pPanel.ScrollItemIntoView(
                                index,
                                xaml_controls.ScrollIntoViewAlignment_Default,
                                0.0, /*offset*/
                                TRUE /*forceSynchronous*/));

                            IFC(pPanel.ContainerFromIndex(index, &spContainer));

                            ASSERT(spContainer);

                            IFC(spContainer.CopyTo(ppNewTabStop));
                            *pIsCandidateTabStopOverridden = TRUE;
                        }
                    }
                }
            }
        }
    }

Cleanup:
    return hr;
}
