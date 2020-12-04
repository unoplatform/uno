// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

#undef min
#undef max

private void CalendarView.GetContainerByDate(
     DateTime datetime,
    out result_maybenull_ CalendarViewDayItem* ppItem)
{
    ppItem = null;

    var pMonthpanel = m_tpMonthViewItemHost.GetPanel();
    if (pMonthpanel)
    {
        int index = -1;
        IDependencyObject spChildAsI;

        index = m_tpMonthViewItemHost.CalculateOffsetFromMinDate(datetime);

        if (index >= 0)
        {
            spChildAsI = pMonthpanel.ContainerFromIndex(index);
            if (spChildAsI)
            {
                CalendarViewDayItem spContainer;

                spContainer = spChildAsI.As);
                spContainer.MoveTo(ppItem);
            }
        }
    }

}

private void CalendarView.OnSelectDayItem( CalendarViewDayItem pItem)
{
    xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;

    global::System.Diagnostics.Debug.Assert(m_tpMonthViewItemHost.GetPanel());
    selectionMode = SelectionMode;

    if (selectionMode != xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None)
    {
        bool isBlackout = false;

        isBlackout = pItem.IsBlackout;
        if (!isBlackout)    // can't select a blackout item.
        {
            unsigned size = 0;
            DateTime date;
            unsigned index = 0;
            boolean found = false;

            size = m_tpSelectedDates.Size;

            m_isSelectedDatesChangingInternally = true;

            global::System.Diagnostics.Debug.Assert(size <= 1 || selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Multiple);

            date = pItem.Date;

            found = m_tpSelectedDates.IndexOf(date, &index);
            if (found)
            {
                // when user deselect an item, we remove all equivalent dates from selectedDates.
                // so the item will be unselected.
                // (the opposite case is when developer removes a date from selectedDates,
                // we only remove that date from selectedDates, so the corresponding item
                // will be still selected until all equivalent dates are removed from selectedDates)

                index = m_tpSelectedDates as TrackableDateCollection.RemoveAll(date);
            }
            else
            {
                if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Single && size == 1)
                {
                    // there was one selected date, remove it.
                    m_tpSelectedDates.Clear();
                }

                m_tpSelectedDates.Append(date);
            }

            RaiseSelectionChangedEventIfChanged();
        }
    }

Cleanup:
    m_isSelectedDatesChangingInternally = false;
    return hr;
}

// when we select a monthitem or yearitem, we changed to the corresponding view.
private void CalendarView.OnSelectMonthYearItem(
     CalendarViewItem pItem,
     xaml.FocusState focusState)
{
    DateTime date  = default;

    xaml_controls.CalendarViewDisplayMode displayMode = xaml_controls.CalendarViewDisplayMode_Month;

    displayMode = DisplayMode;
    date = pItem.GetDate);

    // after display mode changed, we'll focus a new item, we want that item to be focused by the specified state.
    m_focusItemAfterDisplayModeChanged = true;
    m_focusStateAfterDisplayModeChanged = focusState;

    if (displayMode == xaml_controls.CalendarViewDisplayMode_Year && m_tpMonthViewItemHost.GetPanel())
    {
        // when we switch back to MonthView, we try to keep the same day and use the selected month and year (and era)
        IFC(CopyDate(
            displayMode,
            date,
            m_lastDisplayedDate));
        put_DisplayMode(xaml_controls.CalendarViewDisplayMode_Month);
    }
    else if (displayMode == xaml_controls.CalendarViewDisplayMode_Decade && m_tpYearViewItemHost.GetPanel())
    {
        // when we switch back to YearView, we try to keep the same day and same month and use the selected year (and era)
        IFC(CopyDate(
            displayMode,
            date,
            m_lastDisplayedDate));
        put_DisplayMode(xaml_controls.CalendarViewDisplayMode_Year);
    }
    else
    {
        global::System.Diagnostics.Debug.Assert(false);  // corresponding panel part is missing.
    }

}

private void CalendarView.OnSelectionModeChanged()
{
    xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;

    selectionMode = SelectionMode;

    // when selection mode is changed, e.g. from Multiple . Single or from Single . None
    // we need to deselect some or all items and raise SelectedDates changed event
    m_isSelectedDatesChangingInternally = true;

    if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None)
    {
        m_tpSelectedDates.Clear();
    }
    else if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Single)
    {
        unsigned size = 0;

        // remove all but keep the first selected item.
        size = m_tpSelectedDates.Size;

        while (size > 1)
        {
            m_tpSelectedDates.RemoveAt(size - 1);
            size--;
        }
    }

    RaiseSelectionChangedEventIfChanged();

Cleanup:
    m_isSelectedDatesChangingInternally = false;
    return hr;
}

private void CalendarView.RaiseSelectionChangedEventIfChanged()
{
    var lessThanComparer = m_dateComparer.GetLessThanComparer();
    TrackableDateCollection.DateSetType addedDates(lessThanComparer);
    TrackableDateCollection.DateSetType removedDates(lessThanComparer);

    var pSelectedDates = m_tpSelectedDates as TrackableDateCollection;

    // grab all the changes since last time SelectedDates changed.
    pSelectedDates.FetchAndResetChange(addedDates, removedDates);

    // we don't support extended selection mode, so we should have only up to one added date.
    global::System.Diagnostics.Debug.Assert(addedDates.size() <= 1);

    if (addedDates.size() == 1)
    {
        unsigned count = 0;
        DateTime date = addedDates.begin();

        count = pSelectedDates.CountOf(date);

        // given that we have one date in addedDates, so it must exist in SelectedDates.
        global::System.Diagnostics.Debug.Assert(count >= 1);

        if (count > 1)
        {
            // we had this date in SelectedDates before, adding this date will not affect the
            // selection state on this item, so actually we haven't added this date into SelectedDates.
            addedDates.erase(addedDates.begin());
        }
        else if (count == 1)
        {
            // this date doesn't exist in SelectedDates before,
            // which means we change the selection state on this item from Not Selected to Selected.
            CalendarViewDayItem spChild;

            spChild = GetContainerByDate(date);
            if (spChild)
            {
#if DEBUG
                bool isBlackout = false;

                isBlackout = spChild.IsBlackout;
                // we already handle blackout in CollectionChanging, so here the date must not be blackout.
                global::System.Diagnostics.Debug.Assert(!isBlackout);

#endif
                spChild.SetIsSelected(true);
            }
            // else this item is not realized yet, we'll update the selection state when this item is prepared.
        }

    }

    // now handle removedDates

    // we'll check all dates in RemovedDates, to see if there is still an equivalent date
    // in SelectedDates, if yes, this date is still selected and actually not being removed,
    // if no we need update selection state and raise selectedDatesChanged event.

    if (!removedDates.empty())
    {
        // removedDates is sorted and unique, so let's search all SelectedDates from removedDates. time cost O(M x lg(N))
        unsigned size = 0;
        size = pSelectedDates.Size;

        for (unsigned i = 0; i < size; ++i)
        {
            DateTime date{};
            KeyValuePair<TrackableDateCollection.DateSetType.iterator, TrackableDateCollection.DateSetType.iterator> result;

            date = pSelectedDates.GetAt(i);

            // binary_search only tells us if the item exists or not, it doesn't tell us the position:(
            result = removedDates.equal_range(date);
            if (result.first != result.second)
            {
                // because removedDates is unique and sorted, so we should have only up to 1 record.
                global::System.Diagnostics.Debug.Assert(std.distance(result.first, result.second) == 1);
                removedDates.erase(result.first);
            }
        }

        // now removedDates contains all the dates that we finally removed and we are going to
        // mark them as un-selected (if they are realized)

        foreach (var it in removedDates)
        {
            CalendarViewDayItem spChild;

            spChild = GetContainerByDate(it);
            if (spChild)
            {
                spChild.SetIsSelected(false);
            }
        }
    }

    // developer could change SelectedDates in SelectedDatesChanged event
    // it is the good time allow they do so now.
    m_isSelectedDatesChangingInternally = false;

    // now raise selectedDatesChanged event if there are any actual changes
    if (!addedDates.empty() || !removedDates.empty())
    {
        SelectedDatesChangedEventSourceType pEventSource = null;
        CalendarViewSelectedDatesChangedEventArgs spEventArgs;
        ValueTypeCollection<DateTime> spAddedDates;
        ValueTypeCollection<DateTime> spRemovedDates;

        spAddedDates = default;
        spRemovedDates = default;

        foreach (var it in addedDates)
        {
            spAddedDates.Append(it);
        }

        foreach (var it in removedDates)
        {
            spRemovedDates.Append(it);
        }

        spEventArgs = default;
        spEventArgs.AddedDates = spAddedDates as wfc.IVectorView<DateTime>;
        spEventArgs.RemovedDates = spRemovedDates as wfc.IVectorView<DateTime>;
        pEventSource = GetSelectedDatesChangedEventSourceNoRef);
        pEventSource.Raise(this, spEventArgs);


        bool bAutomationListener = false;
        bAutomationListener = AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionPatternOnInvalidated);
        if (!bAutomationListener)
        {
            bAutomationListener = AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementSelected);
        }
        if (!bAutomationListener)
        {
            bAutomationListener = AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementAddedToSelection);
        }
        if (!bAutomationListener)
        {
            bAutomationListener = AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementRemovedFromSelection);
        }
        if (bAutomationListener)
        {
            xaml_automation_peers.IAutomationPeer spAutomationPeer;
            spAutomationPeer = GetOrCreateAutomationPeer);
            if (spAutomationPeer)
            {
                spAutomationPeer as CalendarViewAutomationPeer.RaiseSelectionEvents(spEventArgs);
            }
        }
    }

    return;
}

private void CalendarView.OnDayItemBlackoutChanged( CalendarViewDayItem pItem,  bool isBlackOut)
{
    if (isBlackOut)
    {
        DateTime date;
        unsigned index = 0;
        boolean found = false;

        date = pItem.Date;
        found = m_tpSelectedDates.IndexOf(date, &index);

        if (found)
        {
            // this item is selected, remove the selection and raise event.
            m_isSelectedDatesChangingInternally = true;

            index = m_tpSelectedDates as TrackableDateCollection.RemoveAll(date);

            RaiseSelectionChangedEventIfChanged();
        }
    }

Cleanup:
    m_isSelectedDatesChangingInternally = false;
    return hr;
}

private void CalendarView.IsSelected( DateTime date, out bool pIsSelected)
{
    unsigned index = 0;
    boolean found = false;

    found = m_tpSelectedDates.IndexOf(date, &index);

    pIsSelected = !!found;

}

private void CalendarView.OnSelectedDatesChanged(
     wfc.IObservableVector<DateTime>* pSender,
     wfc.IVectorChangedEventArgs e)
{
    // only raise event for the changes from external.
    if (!m_isSelectedDatesChangingInternally)
    {
        RaiseSelectionChangedEventIfChanged();
    }

    return;
}

private void CalendarView.OnSelectedDatesChanging(
     TrackableDateCollection_CollectionChanging action,
     DateTime addingDate)
{
    switch (action)
    {
    case DirectUI.TrackableDateCollection_CollectionChanging.ItemInserting:
    {
        // when inserting an item, we should verify the new adding date is not blackout.
        // also we need to verify this adding operation doesn't break the limition of Selection mode.
        ValidateSelectingDateIsNotBlackout(addingDate);

        unsigned size = 0;
        xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;

        selectionMode = SelectionMode;
        size = m_tpSelectedDates.Size;

        // if we already have 1 item selected in Single mode, or the selection mode is None, we can't select any more dates.
        if ((selectionMode == xaml_controls.CalendarViewSelectionMode_Single && size > 0)
            || (selectionMode == xaml_controls.CalendarViewSelectionMode_None))
        {
            ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_CANNOT_SELECT_MORE_DATES);
        }
    }
        break;
    case DirectUI.TrackableDateCollection_CollectionChanging.ItemChanging:
        // when item is changing, we don't change the total number of selected dates, so we
        // don't need to verify Selection mode. Here we only need to check if
        // the new addingDate is blackout or not.
        ValidateSelectingDateIsNotBlackout(addingDate);
        break;
    default:
        break;
    }

    return;
}

private void CalendarView.ValidateSelectingDateIsNotBlackout( DateTime date)
{
    CalendarViewDayItem spChild;

    spChild = GetContainerByDate(date);
    if (spChild)
    {
        bool isBlackout = false;

        isBlackout = spChild.IsBlackout;
        if (isBlackout)
        {
            ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_CANNOT_SELECT_BLACKOUT_DATE);
        }
    }

    return;
}