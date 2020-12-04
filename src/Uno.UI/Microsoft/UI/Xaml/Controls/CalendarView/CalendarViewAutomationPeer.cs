// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

private void GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, out  DependencyObject ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    ppReturnValue = null;

    if (patternInterface == xaml_automation_peers.PatternInterface_Table ||
        patternInterface == xaml_automation_peers.PatternInterface_Grid ||
        patternInterface == xaml_automation_peers.PatternInterface_Value ||
        patternInterface == xaml_automation_peers.PatternInterface_Selection)
    {
        ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        CalendarViewAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue);
    }

    return;
}

private void GetClassNameCore(out HSTRING pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    stringReference("CalendarView").CopyTo(pReturnValue);

    return;
}

private void GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    pReturnValue = xaml_automation_peers.AutomationControlType_Calendar;

    return;
}

private void GetChildrenCore(out  wfc.IVector<xaml_automation_peers.AutomationPeer>** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);

    UIElement spOwner;
    spOwner = Owner;

    CalendarViewAutomationPeerGenerated.GetChildrenCore(ppReturnValue);
    if (ppReturnValue != null)
    {
        RemoveAPs(ppReturnValue);
    }
    return;
}

// This function removes views that are not active.
HRESULT RemoveAPs(__inout wfc.IVector<xaml_automation_peers.AutomationPeer>* pAPCollection)
{
    UIElement spOwner;
    spOwner = Owner;

    Uint count = 0;
    count = pAPCollection.Size;
    for (int index = (int)count - 1; index >= 0; index--)
    {
        xaml_automation_peers.IAutomationPeer spAutomationPeer;
        spAutomationPeer = pAPCollection.GetAt(index);
        if (spAutomationPeer != null)
        {
            UIElement spCurrent;
            spCurrent = spAutomationPeer as FrameworkElementAutomationPeer.Owner;

            while (spCurrent != null && spCurrent != spOwner)
            {
                DOUBLE opacity = 1.0;
                opacity = spCurrent.Opacity;
                if (opacity == 0.0)
                {
                    pAPCollection.RemoveAt(index);
                    break;
                }
                xaml.IDependencyObject spParent;
                spParent = spCurrent as FrameworkElement.Parent;
                spCurrent = spParent as UIElement;
            }
        }
    }

    return;
}

// Properties.
private void get_CanSelectMultipleImpl(out BOOLEAN pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = false;

    UIElement spOwner;
    spOwner = Owner;


    xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;
    selectionMode = spOwner as CalendarView.SelectionMode;

    if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Multiple)
    {
        pValue = true;
    }
    return;
}

private void get_IsSelectionRequiredImpl(out BOOLEAN pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = false;
    return;
}

private void get_IsReadOnlyImpl(out BOOLEAN pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = true;
    return;
}

// This will be date string if single date is selected otherwise the name of header of view
private void get_ValueImpl(out HSTRING pValue)
{
    UIElement spOwner;
    spOwner = Owner;

    Uint count = 0;
    count = spOwner as CalendarView.m_tpSelectedDates.Size;
    if (count == 1)
    {
        string string;
        wg.DateTimeFormatting.IDateTimeFormatter spFormatter;
        spFormatter = spOwner as CalendarView.CreateDateTimeFormatter(stringReference(STR_LEN_PAIR("day month.full year")));

        DateTime date;
        date = spOwner as CalendarView.m_tpSelectedDates.GetAt(0);

        spFormatter.Format(date, string());
        string.CopyTo(pValue);
    }
    else
    {
        CalendarViewGeneratorHost spHost;
        spHost = spOwner as CalendarView.GetActiveGeneratorHost);
        spHost.GetHeaderTextOfCurrentScope().CopyTo(pValue);
    }

    return;
}

private void get_RowOrColumnMajorImpl(out xaml_automation.RowOrColumnMajor pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = xaml_automation.RowOrColumnMajor.RowOrColumnMajor_RowMajor;
    return;
}

private void get_ColumnCountImpl(out INT pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = 0;

    UIElement spOwner;
    spOwner = Owner;

    CalendarViewGeneratorHost spHost;
    spHost = spOwner as CalendarView.GetActiveGeneratorHost);
    CalendarPanel pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        pCalendarPanel.get_Cols(pValue);
    }
    return;
}

private void get_RowCountImpl(out INT pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = 0;

    UIElement spOwner;
    spOwner = Owner;

    CalendarViewGeneratorHost spHost;
    spHost = spOwner as CalendarView.GetActiveGeneratorHost);
    CalendarPanel pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        pCalendarPanel.get_Rows(pValue);
    }
    return;
}

// This will be visible rows in the view, real number of rows will not provide any value
private void GetSelectionImpl(out UINT pReturnValueCount, _Out_writes_to_ptr_(pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
{
    IFCPTR_RETURN(pReturnValueCount);
    UIElement spOwner;
    spOwner = Owner;
    pReturnValueCount = 0;


    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;
    mode = spOwner as CalendarView.DisplayMode;
    if (mode == xaml_controls.CalendarViewDisplayMode_Month)
    {
        Uint count = 0;
        Uint realizedCount = 0;

        count = spOwner as CalendarView.m_tpSelectedDates.Size;
        if (count > 0)
        {
            CalendarViewGeneratorHost spHost;
            spHost = spOwner as CalendarView.GetActiveGeneratorHost);

            wfc.IVector<xaml_automation_peers.AutomationPeer> spAPChildren;
            ctl.ComObject<TrackerCollection<xaml_automation_peers.AutomationPeer>>.CreateInstance(spAPChildren.ReleaseAn());

            var pPanel = spHost.GetPanel();
            if (pPanel)
            {
                for (Uint i = 0; i < count; i++)
                {
                    DateTime date;
                    int itemIndex = 0;
                    IDependencyObject spItemAsI;
                    CalendarViewBaseItem spItem;
                    xaml_automation_peers.IAutomationPeer spItemPeerAsAP;

                    date = spOwner as CalendarView.m_tpSelectedDates.GetAt(i);
                    itemIndex = spHost.CalculateOffsetFromMinDate(date);

                    spItemAsI = pPanel.ContainerFromIndex(itemIndex);
                    if (spItemAsI)
                    {
                        spItem = spItemAsI.As);
                        spItemPeerAsAP = spItem.GetOrCreateAutomationPeer);
                        spAPChildren.Append(spItemPeerAsAP);
                    }
                }

                realizedCount = spAPChildren.Size;
                if (realizedCount > 0)
                {
                    Uint allocSize = sizeof(IIRawElementProviderSimple) * realizedCount;
                    ppReturnValue = (IIRawElementProviderSimple*)(CoTaskMemAlloc(allocSize));
                    IFCOOMFAILFAST(ppReturnValue);
                    ZeroMemory(ppReturnValue, allocSize);

                    for (Uint index = 0; index < realizedCount; index++)
                    {
                        xaml_automation_peers.IAutomationPeer spItemPeerAsAP;
                        xaml_automation.Provider.IIRawElementProviderSimple spProvider;
                        spItemPeerAsAP = spAPChildren.GetAt(index);
                        spProvider = ProviderFromPeer(spItemPeerAsAP);
                        (ppReturnValue)[index] = spProvider.Detach();
                    }
                }
                pReturnValueCount = realizedCount;
            }
        }
    }
    return;
}

private void SetValueImpl( string value)
{
    throw new NotImplementedException();
}

// This will returns the header text labels on the top only in the case of monthView
private void GetColumnHeadersImpl(out UINT pReturnValueCount, _Out_writes_to_ptr_(pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
{
    pReturnValueCount = 0;
    UIElement spOwner;
    spOwner = Owner;

    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;
    mode = spOwner as CalendarView.DisplayMode;
    if (mode == xaml_controls.CalendarViewDisplayMode_Month)
    {
        Uint nCount = 0;
        Grid spWeekDayNames;
        spOwner as CalendarView.GetTemplatePart<Grid>("WeekDayNames", spWeekDayNames.ReleaseAn());
        if (spWeekDayNames)
        {
            wfc.IVector<xaml.UIElement> spChildren;
            spChildren = spWeekDayNames as Grid.Children;
            nCount = spChildren.Size;

            Uint allocSize = sizeof(IIRawElementProviderSimple) * nCount;
            ppReturnValue = (IIRawElementProviderSimple*)(CoTaskMemAlloc(allocSize));
            IFCOOMFAILFAST(ppReturnValue);
            ZeroMemory(ppReturnValue, allocSize);
            for (Uint i = 0; i < nCount; i++)
            {
                xaml_automation_peers.IAutomationPeer spItemPeerAsAP;
                xaml_automation.Provider.IIRawElementProviderSimple spProvider;
                UIElement spChild;
                spChild = spChildren.GetAt(i);

                spItemPeerAsAP = spChild as UIElement.GetOrCreateAutomationPeer);
                spProvider = ProviderFromPeer(spItemPeerAsAP);
                (ppReturnValue)[i] = spProvider.Detach();
            }
            pReturnValueCount = nCount;
        }
    }

    return;
}

private void GetRowHeadersImpl(out UINT pReturnValueCount, _Out_writes_to_ptr_(pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
{
    pReturnValueCount = 0;
    return;
}


private void GetItemImpl( int row,  int column, out  xaml_automation.Provider.IIRawElementProviderSimple* ppReturnValue)
{
    ppReturnValue = null;

    UIElement spOwner;
    spOwner = Owner;

    CalendarViewGeneratorHost spHost;
    spHost = spOwner as CalendarView.GetActiveGeneratorHost);

    IDependencyObject spItemAsI;
    CalendarViewBaseItem spItem;
    xaml_automation_peers.IAutomationPeer spItemPeerAsAP;
    xaml_automation.Provider.IIRawElementProviderSimple spProvider;

    int colCount = 0;
    colCount = ColumnCountImpl;

    int firstVisibleIndex = 0;
    CalendarPanel pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;

        int itemIndex = firstVisibleIndex + row * colCount + column;
        // firstVisibleIndex is on the first row, we need to count how many space before the firstVisibleIndex.
        if (firstVisibleIndex < colCount)
        {
            int startIndex = 0;
            startIndex = pCalendarPanel.StartIndex;
            itemIndex -= startIndex;
        }

        if (itemIndex >= 0)
        {
            spItemAsI = pCalendarPanel.ContainerFromIndex(itemIndex);
            // This can be a virtualized item or item does not exist, check for null
            if (spItemAsI)
            {
                spItem = spItemAsI.As);

                spItemPeerAsAP = spItem.GetOrCreateAutomationPeer);
                spProvider = ProviderFromPeer(spItemPeerAsAP);
                ppReturnValue = spProvider.Detach();
            }
        }
    }
    return;
}

private void RaiseSelectionEvents( xaml_controls.ICalendarViewSelectedDatesChangedEventArgs pSelectionChangedEventArgs)
{
    UIElement spOwner;
    spOwner = Owner;

    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;
    mode = spOwner as CalendarView.DisplayMode;

    // No header for year or decade view
    if (mode == xaml_controls.CalendarViewDisplayMode_Month)
    {
        CalendarViewGeneratorHost spHost;
        spHost = spOwner as CalendarView.GetActiveGeneratorHost);

        var pPanel = spHost.GetPanel();
        if (pPanel)
        {
            DateTime date;
            IDependencyObject spItemAsI;
            CalendarViewBaseItem spItem;
            xaml_automation_peers.IAutomationPeer spItemPeerAsAP;
            int itemIndex = 0;
            unsigned selectedCount = 0;

            selectedCount = spOwner as CalendarView.m_tpSelectedDates.Size;

            wfc.IVectorView<DateTime> spAddedDates;
            wfc.IVectorView<DateTime> spRemovedDates;
            unsigned addedDatesSize = 0;
            unsigned removedDatesSize = 0;

            pSelectionChangedEventArgs.get_AddedDates(spAddedDates.ReleaseAn());
            addedDatesSize = spAddedDates.Size;

            pSelectionChangedEventArgs.get_RemovedDates(spRemovedDates.ReleaseAn());
            removedDatesSize = spRemovedDates.Size;

            // One selection added and that is the only selection
            if (addedDatesSize == 1 && selectedCount == 1)
            {

                date = spAddedDates.GetAt(0);
				itemIndex = spHost.CalculateOffsetFromMinDate(date);
                spItemAsI = pPanel.ContainerFromIndex(itemIndex);
                if (spItemAsI)
                {
                    spItem = spItemAsI.As);
                    spItemPeerAsAP = spItem.GetOrCreateAutomationPeer);
                    if (spItemPeerAsAP)
                    {
                        spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementSelected);
                    }
                }

                if (removedDatesSize == 1)
                {
                    date = spRemovedDates.GetAt(0);
                    itemIndex = spHost.CalculateOffsetFromMinDate(date);
                    spItemAsI = pPanel.ContainerFromIndex(itemIndex);
                    if (spItemAsI)
                    {
                        spItem = spItemAsI.As);
                        spItemPeerAsAP = spItem.GetOrCreateAutomationPeer);
                        if (spItemPeerAsAP)
                        {
                            spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementRemovedFromSelection);
                        }
                    }
                }
            }
            else
            {
                if (addedDatesSize + removedDatesSize > BulkChildrenLimit)
                {
                    RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionPatternOnInvalidated);
                }
                else
                {
                    unsigned i = 0;
                    for (i = 0; i < addedDatesSize; i++)
                    {
                        date = spAddedDates.GetAt(i);
                        itemIndex = spHost.CalculateOffsetFromMinDate(date);
                        spItemAsI = pPanel.ContainerFromIndex(itemIndex);
                        if (spItemAsI)
                        {
                            spItem = spItemAsI.As);
                            spItemPeerAsAP = spItem.GetOrCreateAutomationPeer);
                            if (spItemPeerAsAP)
                            {
                                spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementAddedToSelection);
                            }
                        }
                    }

                    for (i = 0; i < removedDatesSize; i++)
                    {
                        date = spRemovedDates.GetAt(i);
                        itemIndex = spHost.CalculateOffsetFromMinDate(date);
                        spItemAsI = pPanel.ContainerFromIndex(itemIndex);
                        if (spItemAsI)
                        {
                            spItem = spItemAsI.As);
                            spItemPeerAsAP = spItem.GetOrCreateAutomationPeer);
                            if (spItemPeerAsAP)
                            {
                                spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementRemovedFromSelection);
                            }
                        }
                    }
                }
            }
        }
    }
    return;
}
