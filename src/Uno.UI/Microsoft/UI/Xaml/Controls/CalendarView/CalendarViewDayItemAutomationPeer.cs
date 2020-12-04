// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

private void GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, out  DependencyObject ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    ppReturnValue = null;

    if (patternInterface == xaml_automation_peers.PatternInterface_TableItem ||
        patternInterface == xaml_automation_peers.PatternInterface_SelectionItem)
    {
        ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        CalendarViewDayItemAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue);
    }
    return;
}

private void GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    pReturnValue = xaml_automation_peers.AutomationControlType_DataItem;

    return;
}

private void GetClassNameCore(out HSTRING pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    stringReference("CalendarViewDayItem").CopyTo(pReturnValue);
    return;
}

private void IsEnabledCore(out BOOLEAN pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    UIElement spOwner;
    spOwner = Owner;

    bool isBlackout = false;
    isBlackout = spOwner as CalendarViewDayItem.IsBlackout;

    // This property also takes in consideration of a day is 'blackout' or not. Blackout dates are disabled
    if (isBlackout)
    {
        pReturnValue = false;
    }
    else
    {
        CalendarViewDayItemAutomationPeerGenerated.IsEnabledCore(pReturnValue);
    }
    return;
}

private void GetColumnHeaderItemsImpl(out UINT pReturnValueCount, _Out_writes_to_ptr_(pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
{
    pReturnValueCount = 0;
    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewDayItem.GetDate);

    CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    Uint nCount = 0;
    Grid spWeekDayNames;

    // Gets the 'WeekDayNames' part of the container template and find weekindex position as elment
    pParent.GetTemplatePart<Grid>("WeekDayNames", spWeekDayNames.ReleaseAn());
    if (spWeekDayNames)
    {
        wfc.IVector<xaml.UIElement> spChildren;
        spChildren = spWeekDayNames as Grid.Children;
        nCount = spChildren.Size;

		int weekindex = 0;
        weekindex = ColumnImpl;

        xaml_automation_peers.IAutomationPeer spItemPeerAsAP;
        xaml_automation.Provider.IIRawElementProviderSimple spProvider;
        UIElement spChild;
        spChild = spChildren.GetAt(weekindex);
		if (spChild)
		{
			spItemPeerAsAP = spChild as UIElement.GetOrCreateAutomationPeer);
			if (spItemPeerAsAP)
			{
				Uint allocSize = sizeof(IIRawElementProviderSimple);
				ppReturnValue = (IIRawElementProviderSimple*)(CoTaskMemAlloc(allocSize));
                IFCOOMFAILFAST(ppReturnValue);
				ZeroMemory(ppReturnValue, allocSize);

				spProvider = ProviderFromPeer(spItemPeerAsAP);
				(ppReturnValue)[0] = spProvider.Detach();
				pReturnValueCount = 1;
			}
		}
    }
    return;
}

private void GetRowHeaderItemsImpl(out UINT pReturnValueCount, _Out_writes_to_ptr_(pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
{
    UIElement spOwner;
    spOwner = Owner;

    CalendarViewDayItem item = spOwner as CalendarViewDayItem;
    CalendarView pParent = item.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    DateTime itemDate;
    itemDate = item.GetDate);

    pParent.GetRowHeaderForItemAutomationPeer(itemDate, xaml_controls.CalendarViewDisplayMode_Month, pReturnValueCount, ppReturnValue);

    return;
}

private void get_SelectionContainerImpl(out result_maybenull_ xaml_automation.Provider.IIRawElementProviderSimple* ppValue)
{
    return get_ContainingGridImpl(ppValue);
}

private void AddToSelectionImpl()
{
    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewDayItem.GetDate);

    CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    bool isSelected = false;
    isSelected = pParent.IsSelected(date);
    if (!isSelected)
    {
        pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);
    }

    return;
}

private void RemoveFromSelectionImpl()
{
    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewDayItem.GetDate);

    CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    bool isSelected = false;
    isSelected = pParent.IsSelected(date);
    if (isSelected)
    {
        pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);
    }

    return;
}

private void get_IsSelectedImpl(out BOOLEAN pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = false;

    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewDayItem.GetDate);
    bool isSelected = false;
    CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    isSelected = pParent.IsSelected(date);
    if (isSelected)
    {
        pValue = true;
    }
    return;
}

private void SelectImpl()
{
	UIElement spOwner;
	spOwner = Owner;

	DateTime date;
	date = spOwner as CalendarViewDayItem.GetDate);

	CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
	IFCPTR_RETURN(pParent);

	wfc.IVector<DateTime> spSelectedItems;
	spSelectedItems = pParent.SelectedDates;
	spSelectedItems.Clear();

	pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);

	return;
}

// calculate visible column from index of the item
private void get_ColumnImpl(out INT pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = 0;

    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewDayItem.GetDate);

    CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    CalendarViewGeneratorHost spHost;
    spHost = pParent.GetActiveGeneratorHost);

    CalendarPanel pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        int itemIndex = 0;

        // Get realized item index
        itemIndex = spHost.CalculateOffsetFromMinDate(date);

        int cols = 1;
        cols = pCalendarPanel.Cols;
        int firstVisibleIndex = 0;
        firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;

        // Calculate the relative positon w.r.to the visible elements from item index
        int relativePos = (itemIndex - firstVisibleIndex);
        if (firstVisibleIndex < cols)
        {
            int monthViewStartIndex = 0;
            monthViewStartIndex = pCalendarPanel.StartIndex;
            relativePos += monthViewStartIndex;
        }

        pValue = relativePos % cols;
        if (pValue < 0)
        {
            pValue += cols;
        }
    }
    return;
}

private void get_RowImpl(out INT pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = 0;

    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewDayItem.GetDate);

    CalendarView pParent = spOwner as CalendarViewDayItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    CalendarViewGeneratorHost spHost;
    spHost = pParent.GetActiveGeneratorHost);

    CalendarPanel pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        int itemIndex = 0;
        itemIndex = spHost.CalculateOffsetFromMinDate(date);

        int cols = 1;
        cols = pCalendarPanel.Cols;
        int firstVisibleIndex = 0;
        firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;

        wg.DayOfWeek firstDayOfWeek = wg.DayOfWeek_Sunday;
        firstDayOfWeek = pParent.FirstDayOfWeek;

        // Find the relative row position w.r.to visible rows
        int relativePos = (itemIndex - firstVisibleIndex);
        if (firstVisibleIndex < cols)
        {
            int monthViewStartIndex = 0;
            monthViewStartIndex = pCalendarPanel.StartIndex;
            relativePos += monthViewStartIndex;
        }

        pValue = relativePos / cols;
        // the element is not visible and we can't define row
        if (pValue < 0)
        {
            return E_NOT_SUPPORTED;
        }
    }
    return;
}
