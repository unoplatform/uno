// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

#undef min
#undef max


// Handles when a key is pressed down on the CalendarView.
CalendarView.OnKeyDown(
     xaml_input.IKeyRoutedEventArgs pArgs)
{
    boolean isHandled = false;
    bool handled = false;
    IKeyRoutedEventArgs spKeyDownArgsFromCalendarItem;

    wsy.VirtualKey key = wsy.VirtualKey_None;
    wsy.VirtualKeyModifiers modifiers = wsy.VirtualKeyModifiers_None;

    CalendarViewGenerated.OnKeyDown(pArgs);

    // Check if this keydown events comes from CalendarItem.
    spKeyDownArgsFromCalendarItem = m_wrKeyDownEventArgsFromCalendarItem.As);

    if (!spKeyDownArgsFromCalendarItem)
    {
        return;
    }

    if (spKeyDownArgsFromCalendarItem != pArgs)
    {
        m_wrKeyDownEventArgsFromCalendarItem.Reset();
        return;
    }

    // Ignore already handled events
    isHandled = pArgs.Handled;
    if (isHandled)
    {
        return;
    }

    modifiers = GetKeyboardModifiers);
    key = pArgs.Key;

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
            originalKey = (KeyRoutedEventArgs)(pArgs).OriginalKey;

            handled = OnKeyboardNavigation(key, originalKey);
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

            mode = DisplayMode;

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
                global::System.Diagnostics.Debug.Assert(false);
                break;
            }

            if (newMode != mode)
            {
                handled = true;
                // after display mode changed, we want the new item to be focused by keyboard.
                m_focusItemAfterDisplayModeChanged = true;
                m_focusStateAfterDisplayModeChanged = xaml.FocusState.FocusState_Keyboard;
                put_DisplayMode(newMode);
            }
        }
            break;
        default:
            break;
        }
    }

    if (handled)
    {
        pArgs.Handled = true;
    }

    return;
}

private void CalendarView.OnKeyboardNavigation(
     wsy.VirtualKey key,
     wsy.VirtualKey originalKey,
    out bool pHandled)
{
    CalendarViewGeneratorHost spHost;

    spHost = GetActiveGeneratorHost);
    pHandled = false;

    if (var pPanel = spHost.GetPanel())
    {
        int lastFocusedIndex = -1;

        lastFocusedIndex = spHost.CalculateOffsetFromMinDate(m_lastDisplayedDate);

        global::System.Diagnostics.Debug.Assert(lastFocusedIndex >= 0);
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
            newFocusedIndex = spHost.CalculateOffsetFromMinDate(newFocusedDate);
            global::System.Diagnostics.Debug.Assert(newFocusedIndex >= 0);
        }
            break;
            // PageUp/PageDown goes to the previous/next scope
        case wsy.VirtualKey_PageUp:
        case wsy.VirtualKey_PageDown:
        {
            var newFocusedDate = m_lastDisplayedDate;
            HRESULT hr2 = spHost.AddScopes(newFocusedDate, key == wsy.VirtualKey_PageUp ? -1 : 1);
            // beyond calendar's limit, fall back to min/max date, i.e. the first/last item.
            if (/FAILED/(hr2))
            {
                if (key == wsy.VirtualKey_PageUp)
                {
                    newFocusedIndex = 0;
                }
                else
                {
                    unsigned size = 0;
                    size = spHost.Size;
                    newFocusedIndex = size - 1;
                }
            }
            else
            {
                // probably beyond {min, max}, let's make sure.
                CoerceDate(newFocusedDate);
                newFocusedIndex = spHost.CalculateOffsetFromMinDate(newFocusedDate);
                global::System.Diagnostics.Debug.Assert(newFocusedIndex >= 0);
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
            Uint newFocusedIndexUint = 0;
            var newFocusedType = xaml_controls.ElementType_ItemContainer;
            bool isValidKey = false;
            bool actionValidForSourceIndex = false;

            isValidKey = TranslateKeyToKeyNavigationAction(key, &action);
            global::System.Diagnostics.Debug.Assert(isValidKey);
            IFC(pPanel.GetTargetIndexFromNavigationAction(
                lastFocusedIndex,
                xaml_controls.ElementType_ItemContainer,
                action,
                !XboxUtility.IsGamepadNavigationDirection(originalKey), /allowWrap/
                -1, /itemIndexHintForHeaderNavigation/
                &newFocusedIndexUint,
                &newFocusedType,
                &actionValidForSourceIndex));

            global::System.Diagnostics.Debug.Assert(newFocusedType == xaml_controls.ElementType_ItemContainer);

            if (actionValidForSourceIndex)
            {
                newFocusedIndex = (int)(newFocusedIndexUint);
            }
        }
            break;
        default:
            global::System.Diagnostics.Debug.Assert(false);
            break;
        }

        // Check if we can focus on a new index or not.
        if (newFocusedIndex != lastFocusedIndex)
        {
            FocusItemByIndex(spHost, newFocusedIndex, xaml.FocusState.FocusState_Keyboard, pHandled);
        }
    }

}



// Given a key, returns a focus navigation action.
private void CalendarView.TranslateKeyToKeyNavigationAction(
     wsy.VirtualKey key,
    out xaml_controls.KeyNavigationAction pNavAction,
    out bool pIsValidKey)
{
    xaml.FlowDirection flowDirection = xaml.FlowDirection_LeftToRight;

    pIsValidKey = false;
    pNavAction = xaml_controls.KeyNavigationAction_Up;

    if (m_tpViewsGrid)
    {
        flowDirection = m_tpViewsGrid as Grid.FlowDirection;
    }
    else
    {
        flowDirection = FlowDirection;
    }

    KeyboardNavigation.TranslateKeyToKeyNavigationAction(flowDirection, key, pNavAction, pIsValidKey);

}

// When an item got focus, we save the date of this item (to m_lastDisplayedDate).
// When we switching the view mode, we always focus this date.
private void CalendarView.OnItemFocused( CalendarViewBaseItem pItem)
{
    DateTime date;
    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;

    date = pItem.GetDate);

    mode = DisplayMode;

    IFC(CopyDate(
        mode,
        date,
        m_lastDisplayedDate));

}


private void CalendarView.FocusItemByIndex(
     CalendarViewGeneratorHost pHost,
     int index,
     xaml.FocusState focusState,
    out bool pFocused)
{
    DateTime date;

    pFocused = false;

    global::System.Diagnostics.Debug.Assert(index >= 0);

    date = pHost.GetDateAt(index);
    FocusItem(pHost, date, index, focusState, pFocused);

    return;
}

private void CalendarView.FocusItemByDate(
     CalendarViewGeneratorHost pHost,
     DateTime date,
     xaml.FocusState focusState,
    out bool pFocused)
{
    int index = -1;

    pFocused = false;

    index = pHost.CalculateOffsetFromMinDate(date);
    FocusItem(pHost, date, index, focusState, pFocused);

    return;
}

private void CalendarView.FocusItem(
     CalendarViewGeneratorHost pHost,
     DateTime date,
     int index,
     xaml.FocusState focusState,
    out bool pFocused)
{
    IDependencyObject spItemAsI;
    CalendarViewBaseItem spItem;

    pFocused = false;

    SetDisplayDateInternal(date);  // scroll item into view so we can move focus to it.

    spItemAsI = pHost.GetPanel().ContainerFromIndex(index);
    global::System.Diagnostics.Debug.Assert(spItemAsI);
    spItem = spItemAsI.As);

    spItem.FocusSelfOrChild(focusState, pFocused);

    return;
}

// UIElement override for getting next tab stop on path from focus candidate element to root.
private void CalendarView.ProcessCandidateTabStopOverride(
     DependencyObject pFocusedElement,
     DependencyObject pCandidateTabStopElement,
     DependencyObject pOverriddenCandidateTabStopElement,
     bool isBackward,
    out  DependencyObject* ppNewTabStop,
    out BOOLEAN pIsCandidateTabStopOverridden)
{
    // There is no ICalendarViewBaseItem interface so we can't use ctl.is to check an element is CalendarViewBaseItem or not
    // because in ctl.is, it will call ReleaseInterface and CalendarViewBaseItem has multiple interfaces.
    // ComPtr will do this in a smarter way - it always casts to IUnknown before release/addso there is no ambiguity.
    CalendarViewBaseItem spCandidateAsCalendarViewBaseItem(ctl.query_interface<CalendarViewBaseItem>(pCandidateTabStopElement));

    ppNewTabStop = null;
    pIsCandidateTabStopOverridden = false;

    // Check if the candidate is a calendaritem and the currently focused is not a calendaritem.
    // Which means we Tab (or shift+Tab) into the scrollviewer and we are going to put focus on the candidate.
    // However the candidate is the first (or last if we shift+Tab) realized item in the Panel, we should use the last
    // focused item to override the candidate (and ignore isBackward, i.e. Shift).

    if (spCandidateAsCalendarViewBaseItem)
    {
        CalendarViewBaseItem spFocusedAsCalendarViewBaseItem(ctl.query_interface<CalendarViewBaseItem>(pFocusedElement));
        if (!spFocusedAsCalendarViewBaseItem)
        {
            CalendarViewGeneratorHost spHost;
            spHost = GetActiveGeneratorHost);

            var pScrollViewer = spHost.GetScrollViewer();
            if (pScrollViewer)
            {
                xaml_input.KeyboardNavigationMode mode = xaml_input.KeyboardNavigationMode_Local;

                mode = pScrollViewer.TabNavigation;
                // The tab navigation on this scrollviewer must be Once (it's default value in the template).
                // For other modes (Local or Cycle) we don't want to override.
                if (mode == xaml_input.KeyboardNavigationMode_Once)
                {
                    bool isAncestor = false;

                    // Are we tabbing from/to another view?
                    // if developer makes other view focusable by re-template and the candidate is not
                    // in the active view, we'll not override the candidate.
                    isAncestor = pScrollViewer.IsAncestorOf(pCandidateTabStopElement);

                    if (isAncestor)
                    {
                        var pPanel = spHost.GetPanel();
                        if (pPanel)
                        {
                            int index = -1;
                            xaml.IDependencyObject spContainer;
                            CalendarViewGeneratorHost spHost;
                            spHost = GetActiveGeneratorHost);

                            index = spHost.CalculateOffsetFromMinDate(m_lastDisplayedDate);

                            // This container might not have focus so it could be recycled, bring
                            // it into view so it can take focus.
                            IFC(pPanel.ScrollItemIntoView(
                                index,
                                xaml_controls.ScrollIntoViewAlignment_Default,
                                0.0, /offset/
                                true /forceSynchronous/));

                            spContainer = pPanel.ContainerFromIndex(index);

                            global::System.Diagnostics.Debug.Assert(spContainer);

                            spContainer.CopyTo(ppNewTabStop);
                            pIsCandidateTabStopOverridden = true;
                        }
                    }
                }
            }
        }
    }

}
