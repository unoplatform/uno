// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarView.g.h"
#include "CalendarViewDayItem.g.h"
#include "CalendarViewItem.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarPanel.g.h"
#include "TrackableDateCollection.h"
#include "CalendarViewSelectedDatesChangedEventArgs.g.h"
#include "AutomationPeer.g.h"
#include "CalendarViewAutomationPeer.g.h"
#include "DateComparer.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

#undef min
#undef max

_Check_return_ HRESULT CalendarView.GetContainerByDate(
     wf.DateTime datetime,
    _Outptr_result_maybenull_ CalendarViewDayItem** ppItem)
{
    HRESULT hr = S_OK;

    *ppItem = null;

    var pMonthpanel = m_tpMonthViewItemHost.GetPanel();
    if (pMonthpanel)
    {
        int index = -1;
        ctl.ComPtr<IDependencyObject> spChildAsI;

        IFC(m_tpMonthViewItemHost.CalculateOffsetFromMinDate(datetime, &index));

        if (index >= 0)
        {
            IFC(pMonthpanel.ContainerFromIndex(index, &spChildAsI));
            if (spChildAsI)
            {
                ctl.ComPtr<CalendarViewDayItem> spContainer;

                IFC(spChildAsI.As(&spContainer));
                IFC(spContainer.MoveTo(ppItem));
            }
        }
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarView.OnSelectDayItem( CalendarViewDayItem* pItem)
{
    HRESULT hr = S_OK;

    xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;

    ASSERT(m_tpMonthViewItemHost.GetPanel());
    IFC(get_SelectionMode(&selectionMode));

    if (selectionMode != xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None)
    {
        BOOLEAN isBlackout = FALSE;

        IFC(pItem.get_IsBlackout(&isBlackout));
        if (!isBlackout)    // can't select a blackout item.
        {
            unsigned size = 0;
            wf.DateTime date;
            unsigned index = 0;
            boolean found = false;

            IFC(m_tpSelectedDates.get_Size(&size));

            m_isSelectedDatesChangingInternally = true;

            ASSERT(size <= 1 || selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Multiple);

            IFC(pItem.get_Date(&date));

            IFC(m_tpSelectedDates.IndexOf(date, &index, &found));
            if (found)
            {
                // when user deselect an item, we remove all equivalent dates from selectedDates.
                // so the item will be unselected.
                // (the opposite case is when developer removes a date from selectedDates,
                // we only remove that date from selectedDates, so the corresponding item
                // will be still selected until all equivalent dates are removed from selectedDates)

                IFC(m_tpSelectedDates.Cast<TrackableDateCollection>().RemoveAll(date, &index));
            }
            else
            {
                if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Single && size == 1)
                {
                    // there was one selected date, remove it.
                    IFC(m_tpSelectedDates.Clear());
                }

                IFC(m_tpSelectedDates.Append(date));
            }

            IFC(RaiseSelectionChangedEventIfChanged());
        }
    }

Cleanup:
    m_isSelectedDatesChangingInternally = false;
    return hr;
}

// when we select a monthitem or yearitem, we changed to the corresponding view.
_Check_return_ HRESULT CalendarView.OnSelectMonthYearItem(
     CalendarViewItem* pItem,
     xaml.FocusState focusState)
{
    HRESULT hr = S_OK;
    wf.DateTime date = {};

    xaml_controls.CalendarViewDisplayMode displayMode = xaml_controls.CalendarViewDisplayMode_Month;

    IFC(get_DisplayMode(&displayMode));
    IFC(pItem.GetDate(&date));

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
        IFC(put_DisplayMode(xaml_controls.CalendarViewDisplayMode_Month));
    }
    else if (displayMode == xaml_controls.CalendarViewDisplayMode_Decade && m_tpYearViewItemHost.GetPanel())
    {
        // when we switch back to YearView, we try to keep the same day and same month and use the selected year (and era)
        IFC(CopyDate(
            displayMode,
            date,
            m_lastDisplayedDate));
        IFC(put_DisplayMode(xaml_controls.CalendarViewDisplayMode_Year));
    }
    else
    {
        ASSERT(FALSE);  // corresponding panel part is missing.
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarView.OnSelectionModeChanged()
{
    HRESULT hr = S_OK;

    xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;

    IFC(get_SelectionMode(&selectionMode));

    // when selection mode is changed, e.g. from Multiple . Single or from Single . None
    // we need to deselect some or all items and raise SelectedDates changed event
    m_isSelectedDatesChangingInternally = true;

    if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None)
    {
        IFC(m_tpSelectedDates.Clear());
    }
    else if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Single)
    {
        unsigned size = 0;

        // remove all but keep the first selected item.
        IFC(m_tpSelectedDates.get_Size(&size));

        while (size > 1)
        {
            IFC(m_tpSelectedDates.RemoveAt(size - 1));
            size--;
        }
    }

    IFC(RaiseSelectionChangedEventIfChanged());

Cleanup:
    m_isSelectedDatesChangingInternally = false;
    return hr;
}

_Check_return_ HRESULT CalendarView.RaiseSelectionChangedEventIfChanged()
{
    var lessThanComparer = m_dateComparer.GetLessThanComparer();
    TrackableDateCollection.DateSetType addedDates(lessThanComparer);
    TrackableDateCollection.DateSetType removedDates(lessThanComparer);

    var pSelectedDates = m_tpSelectedDates.Cast<TrackableDateCollection>();

    // grab all the changes since last time SelectedDates changed.
    pSelectedDates.FetchAndResetChange(addedDates, removedDates);

    // we don't support extended selection mode, so we should have only up to one added date.
    ASSERT(addedDates.size() <= 1);

    if (addedDates.size() == 1)
    {
        unsigned count = 0;
        wf.DateTime date = *addedDates.begin();

        IFC_RETURN(pSelectedDates.CountOf(date, &count));

        // given that we have one date in addedDates, so it must exist in SelectedDates.
        ASSERT(count >= 1);

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
            ctl.ComPtr<CalendarViewDayItem> spChild;

            IFC_RETURN(GetContainerByDate(date, &spChild));
            if (spChild)
            {
#ifdef DBG
                BOOLEAN isBlackout = FALSE;

                IFC_RETURN(spChild.get_IsBlackout(&isBlackout));
                // we already handle blackout in CollectionChanging, so here the date must not be blackout.
                ASSERT(!isBlackout);

#endif
                IFC_RETURN(spChild.SetIsSelected(true));
            }
            // else this item is not realized yet, we'll update the selection state when this item is prepared.
        }

    }

    // now handle removedDates

    // we'll check all dates in RemovedDates, to see if there is still an equivalent date
    // in SelectedDates, if yes, this date is still selected and *actually* not being removed,
    // if no we need update selection state and raise selectedDatesChanged event.

    if (!removedDates.empty())
    {
        // removedDates is sorted and unique, so let's search all SelectedDates from removedDates. time cost O(M x lg(N))
        unsigned size = 0;
        IFC_RETURN(pSelectedDates.get_Size(&size));

        for (unsigned i = 0; i < size; ++i)
        {
            wf.DateTime date{};
            KeyValuePair<TrackableDateCollection.DateSetType.iterator, TrackableDateCollection.DateSetType.iterator> result;

            IFC_RETURN(pSelectedDates.GetAt(i, &date));

            // binary_search only tells us if the item exists or not, it doesn't tell us the position:(
            result = removedDates.equal_range(date);
            if (result.first != result.second)
            {
                // because removedDates is unique and sorted, so we should have only up to 1 record.
                ASSERT(std.distance(result.first, result.second) == 1);
                removedDates.erase(result.first);
            }
        }

        // now removedDates contains all the dates that we finally removed and we are going to
        // mark them as un-selected (if they are realized)

        for (var it = removedDates.begin(); it != removedDates.end(); ++it)
        {
            ctl.ComPtr<CalendarViewDayItem> spChild;

            IFC_RETURN(GetContainerByDate(*it, &spChild));
            if (spChild)
            {
                IFC_RETURN(spChild.SetIsSelected(false));
            }
        }
    }

    // developer could change SelectedDates in SelectedDatesChanged event
    // it is the good time allow they do so now.
    m_isSelectedDatesChangingInternally = false;

    // now raise selectedDatesChanged event if there are any actual changes
    if (!addedDates.empty() || !removedDates.empty())
    {
        SelectedDatesChangedEventSourceType* pEventSource = null;
        ctl.ComPtr<CalendarViewSelectedDatesChangedEventArgs> spEventArgs;
        ctl.ComPtr<ValueTypeCollection<wf.DateTime>> spAddedDates;
        ctl.ComPtr<ValueTypeCollection<wf.DateTime>> spRemovedDates;

        IFC_RETURN(ctl.make(&spAddedDates));
        IFC_RETURN(ctl.make(&spRemovedDates));

        for (var it = addedDates.begin(); it != addedDates.end(); ++it)
        {
            IFC_RETURN(spAddedDates.Append(*it));
        }

        for (var it = removedDates.begin(); it != removedDates.end(); ++it)
        {
            IFC_RETURN(spRemovedDates.Append(*it));
        }

        IFC_RETURN(ctl.make(&spEventArgs));
        IFC_RETURN(spEventArgs.put_AddedDates(spAddedDates.Cast<wfc.IVectorView<wf.DateTime>>()));
        IFC_RETURN(spEventArgs.put_RemovedDates(spRemovedDates.Cast<wfc.IVectorView<wf.DateTime>>()));
        IFC_RETURN(GetSelectedDatesChangedEventSourceNoRef(&pEventSource));
        IFC_RETURN(pEventSource.Raise(this, spEventArgs.Get()));


        BOOLEAN bAutomationListener = FALSE;
        IFC_RETURN(AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionPatternOnInvalidated, &bAutomationListener));
        if (!bAutomationListener)
        {
            IFC_RETURN(AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementSelected, &bAutomationListener));
        }
        if (!bAutomationListener)
        {
            IFC_RETURN(AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementAddedToSelection, &bAutomationListener));
        }
        if (!bAutomationListener)
        {
            IFC_RETURN(AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementRemovedFromSelection, &bAutomationListener));
        }
        if (bAutomationListener)
        {
            ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
            IFC_RETURN(GetOrCreateAutomationPeer(&spAutomationPeer));
            if (spAutomationPeer)
            {
                IFC_RETURN(spAutomationPeer.Cast<CalendarViewAutomationPeer>().RaiseSelectionEvents(spEventArgs.Get()));
            }
        }
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarView.OnDayItemBlackoutChanged( CalendarViewDayItem* pItem,  bool isBlackOut)
{
    HRESULT hr = S_OK;

    if (isBlackOut)
    {
        wf.DateTime date;
        unsigned index = 0;
        boolean found = false;

        IFC(pItem.get_Date(&date));
        IFC(m_tpSelectedDates.IndexOf(date, &index, &found));

        if (found)
        {
            // this item is selected, remove the selection and raise event.
            m_isSelectedDatesChangingInternally = true;

            IFC(m_tpSelectedDates.Cast<TrackableDateCollection>().RemoveAll(date, &index));

            IFC(RaiseSelectionChangedEventIfChanged());
        }
    }

Cleanup:
    m_isSelectedDatesChangingInternally = false;
    return hr;
}

_Check_return_ HRESULT CalendarView.IsSelected( wf.DateTime date, out bool *pIsSelected)
{
    HRESULT hr = S_OK;
    unsigned index = 0;
    boolean found = false;

    IFC(m_tpSelectedDates.IndexOf(date, &index, &found));

    *pIsSelected = !!found;

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarView.OnSelectedDatesChanged(
     wfc.IObservableVector<wf.DateTime>* pSender,
     wfc.IVectorChangedEventArgs* e)
{
    // only raise event for the changes from external.
    if (!m_isSelectedDatesChangingInternally)
    {
        IFC_RETURN(RaiseSelectionChangedEventIfChanged());
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarView.OnSelectedDatesChanging(
     TrackableDateCollection_CollectionChanging action,
     wf.DateTime addingDate)
{
    switch (action)
    {
    case DirectUI.TrackableDateCollection_CollectionChanging.ItemInserting:
    {
        // when inserting an item, we should verify the new adding date is not blackout.
        // also we need to verify this adding operation doesn't break the limition of Selection mode.
        IFC_RETURN(ValidateSelectingDateIsNotBlackout(addingDate));

        unsigned size = 0;
        xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;

        IFC_RETURN(get_SelectionMode(&selectionMode));
        IFC_RETURN(m_tpSelectedDates.get_Size(&size));

        // if we already have 1 item selected in Single mode, or the selection mode is None, we can't select any more dates.
        if ((selectionMode == xaml_controls.CalendarViewSelectionMode_Single && size > 0)
            || (selectionMode == xaml_controls.CalendarViewSelectionMode_None))
        {
            IFC_RETURN(ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_CANNOT_SELECT_MORE_DATES));
        }
    }
        break;
    case DirectUI.TrackableDateCollection_CollectionChanging.ItemChanging:
        // when item is changing, we don't change the total number of selected dates, so we
        // don't need to verify Selection mode. Here we only need to check if
        // the new addingDate is blackout or not.
        IFC_RETURN(ValidateSelectingDateIsNotBlackout(addingDate));
        break;
    default:
        break;
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarView.ValidateSelectingDateIsNotBlackout( wf.DateTime date)
{
    ctl.ComPtr<CalendarViewDayItem> spChild;

    IFC_RETURN(GetContainerByDate(date, &spChild));
    if (spChild)
    {
        BOOLEAN isBlackout = FALSE;

        IFC_RETURN(spChild.get_IsBlackout(&isBlackout));
        if (isBlackout)
        {
            IFC_RETURN(ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_CANNOT_SELECT_BLACKOUT_DATE));
        }
    }

    return S_OK;
}